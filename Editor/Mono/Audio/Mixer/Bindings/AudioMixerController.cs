// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace UnityEditor.Audio
{
    abstract class AudioParameterPath
    {
        public GUID parameter;

        public abstract string ResolveStringPath(bool getOnlyBasePath);
    }

    class AudioGroupParameterPath : AudioParameterPath
    {
        public AudioMixerGroupController group;

        public AudioGroupParameterPath(AudioMixerGroupController group, GUID parameter)
        {
            this.group = group;
            this.parameter = parameter;
        }

        public override string ResolveStringPath(bool getOnlyBasePath)
        {
            if (getOnlyBasePath)
                return GetBasePath(group.GetDisplayString(), null);

            if (group.GetGUIDForVolume() == parameter)
            {
                return "Volume" + GetBasePath(group.GetDisplayString(), null);
            }

            if (group.GetGUIDForPitch() == parameter)
            {
                return "Pitch" + GetBasePath(group.GetDisplayString(), null);
            }

            return "Error finding Parameter path.";
        }

        protected string GetBasePath(string group, string effect)
        {
            string result = " (of " + group;
            if (!string.IsNullOrEmpty(effect))
                result += "\u2794" + effect; //u2794 is a right arrow character
            result += ")";
            return result;
        }
    }

    sealed class AudioEffectParameterPath : AudioGroupParameterPath
    {
        public AudioMixerEffectController effect;

        public AudioEffectParameterPath(AudioMixerGroupController group, AudioMixerEffectController effect, GUID parameter)
            : base(group, parameter)
        {
            this.effect = effect;
        }

        public override string ResolveStringPath(bool getOnlyBasePath)
        {
            if (getOnlyBasePath)
                return GetBasePath(group.GetDisplayString(), effect.effectName);

            if (effect.GetGUIDForMixLevel() == parameter)
            {
                if (effect.IsSend())
                {
                    var allGroups = group.controller.GetAllAudioGroupsSlow();
                    var effectMap = AudioMixerGroupController.GetEffectMapSlow(allGroups);
                    return $"Send level{GetBasePath(group.GetDisplayString(), null)} to {effect.sendTarget.GetDisplayString(effectMap)}";
                }

                return $"Mix Level{GetBasePath(group.GetDisplayString(), effect.effectName)}";
            }

            MixerParameterDefinition[] paramDefs = MixerEffectDefinitions.GetEffectParameters(effect.effectName);

            for (int i = 0; i < paramDefs.Length; i++)
            {
                GUID guid = effect.GetGUIDForParameter(paramDefs[i].name);

                if (guid == parameter)
                {
                    return paramDefs[i].name + GetBasePath(group.GetDisplayString(), effect.effectName);
                }
            }

            return "Error finding Parameter path.";
        }
    }

    delegate void ChangedExposedParameterHandler();

    [ExcludeFromPreset]
    sealed partial class AudioMixerController : AudioMixer
    {
        public static float kMinVolume = -80.0f; // The minimum volume is the level at which sends and effects can be bypassed
        public static float kMaxEffect = 0.0f;
        public static float kVolumeWarp = 1.7f;
        public static string s_GroupEffectDisplaySeperator = "\\"; // Use backslash instead of forward slash to prevent OS menus from splitting path and creating submenus

        public event ChangedExposedParameterHandler ChangedExposedParameter;

        public void OnChangedExposedParameter()
        {
            if (ChangedExposedParameter != null)
                ChangedExposedParameter();
        }

        public void ClearEventHandlers()
        {
            if (ChangedExposedParameter != null)
            {
                foreach (Delegate d in ChangedExposedParameter.GetInvocationList())
                {
                    ChangedExposedParameter -= (ChangedExposedParameterHandler)d;
                }
            }
        }

        [NonSerialized]
        Dictionary<GUID, AudioParameterPath> m_ExposedParamPathCache;

        Dictionary<GUID, AudioParameterPath> exposedParamCache
        {
            get
            {
                if (m_ExposedParamPathCache == null)
                    m_ExposedParamPathCache = new Dictionary<GUID, AudioParameterPath>();

                return m_ExposedParamPathCache;
            }
        }

        string FindUniqueParameterName(string template, ExposedAudioParameter[] parameters)
        {
            string unique = template;
            int counter = 1;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (unique == parameters[i].name)
                {
                    unique = template + " " + counter++;
                    i = -1;
                }
            }

            return unique;
        }

        int SortFuncForExposedParameters(ExposedAudioParameter p1, ExposedAudioParameter p2)
        {
            return string.CompareOrdinal(ResolveExposedParameterPath(p1.guid, true), ResolveExposedParameterPath(p2.guid, true));
        }

        public void AddExposedParameter(AudioParameterPath path)
        {
            if (path == null)
            {
                Debug.LogError("Trying to expose null parameter.");
                return;
            }

            if (path.parameter == default)
            {
                Debug.LogError("Trying to expose parameter with default GUID.");
                return;
            }

            if (ContainsExposedParameter(path.parameter))
            {
                Debug.LogError("Cannot expose the same parameter more than once.");
                return;
            }

            var parameters = new List<ExposedAudioParameter>(exposedParameters);
            var newParam = new ExposedAudioParameter();
            newParam.name = FindUniqueParameterName("MyExposedParam", exposedParameters);
            newParam.guid = path.parameter;
            parameters.Add(newParam);

            // We sort the exposed params by path
            parameters.Sort(SortFuncForExposedParameters);

            exposedParameters = parameters.ToArray();
            OnChangedExposedParameter();

            //Cache the path!
            exposedParamCache[path.parameter] = path;

            AudioMixerUtility.RepaintAudioMixerAndInspectors();
        }

        public bool ContainsExposedParameter(GUID parameter)
        {
            return exposedParameters.Where(val => val.guid == parameter).ToArray().Length > 0;
        }

        public void RemoveExposedParameter(GUID parameterGuid)
        {
            exposedParameters = exposedParameters.Where(val => val.guid != parameterGuid).ToArray();
            OnChangedExposedParameter();

            //Tidy up the cache..
            if (exposedParamCache.ContainsKey(parameterGuid))
                exposedParamCache.Remove(parameterGuid);

            AudioMixerUtility.RepaintAudioMixerAndInspectors();
        }

        public string ResolveExposedParameterPath(GUID parameter, bool getOnlyBasePath)
        {
            // Consult the cache of parameters first.
            if (exposedParamCache.ContainsKey(parameter))
            {
                var path = exposedParamCache[parameter];

                return path.ResolveStringPath(getOnlyBasePath);
            }

            // Search through the whole mixer!
            List<AudioMixerGroupController> groups = GetAllAudioGroupsSlow();

            foreach (AudioMixerGroupController group in groups)
            {
                if (group.GetGUIDForVolume() == parameter || group.GetGUIDForPitch() == parameter)
                {
                    var newPath = new AudioGroupParameterPath(group, parameter);
                    exposedParamCache[parameter] = newPath;
                    return newPath.ResolveStringPath(getOnlyBasePath);
                }

                for (var i = 0; i < group.effects.Length; i++)
                {
                    var effect = group.effects[i];

                    if (effect.IsSend() && parameter == effect.GetGUIDForMixLevel())
                    {
                        var newPath = new AudioEffectParameterPath(group, effect, parameter);
                        exposedParamCache[parameter] = newPath;
                        return newPath.ResolveStringPath(getOnlyBasePath);
                    }

                    var paramDefs = MixerEffectDefinitions.GetEffectParameters(effect.effectName);

                    for (var j = 0; j < paramDefs.Length; j++)
                    {
                        var guid = effect.GetGUIDForParameter(paramDefs[j].name);

                        if (guid == parameter)
                        {
                            var newPath = new AudioEffectParameterPath(group, effect, parameter);
                            exposedParamCache[parameter] = newPath;
                            return newPath.ResolveStringPath(getOnlyBasePath);
                        }
                    }
                }
            }

            return "Error finding Parameter path";
        }

        public static AudioMixerController CreateMixerControllerAtPath(string path)
        {
            var controller = new AudioMixerController();
            controller.CreateDefaultAsset(path);
            return controller;
        }

        public void CreateDefaultAsset(string path)
        {
            masterGroup = new AudioMixerGroupController(this);
            masterGroup.name = "Master";
            masterGroup.PreallocateGUIDs();

            var attenuation = new AudioMixerEffectController("Attenuation");
            attenuation.PreallocateGUIDs();
            masterGroup.InsertEffect(attenuation, 0);

            AudioMixerSnapshotController snapshot = new AudioMixerSnapshotController(this);
            snapshot.name = "Snapshot";
            snapshots = new[] { snapshot };

            startSnapshot = snapshot;

            Object[] objectArray = { this, masterGroup, attenuation, snapshot };
            AssetDatabase.CreateAssetFromObjects(objectArray, path);
        }

        void BuildTestSetup(Random r, AudioMixerGroupController parent, int minSpan, int maxSpan, int maxGroups, string prefix, ref int numGroups)
        {
            int span = (numGroups == 0) ? maxSpan : r.Next(minSpan, maxSpan + 1);
            for (int i = 0; i < span; i++)
            {
                var name = prefix + i;
                var g = CreateNewGroup(name, false);
                AddChildToParent(g, parent);
                if (++numGroups >= maxGroups)
                    return;
                BuildTestSetup(r, g, minSpan, (maxSpan > minSpan) ? (maxSpan - 1) : minSpan, maxGroups, name, ref numGroups);
            }
        }

        public void BuildTestSetup(int minSpan, int maxSpan, int maxGroups)
        {
            int numGroups = 0;
            DeleteGroups(masterGroup.children);
            BuildTestSetup(new Random(), masterGroup, minSpan, maxSpan, maxGroups, "G", ref numGroups);
        }

        public List<AudioMixerGroupController> GetAllAudioGroupsSlow()
        {
            var groups = new List<AudioMixerGroupController>();

            if (masterGroup != null)
                GetAllAudioGroupsSlowRecurse(masterGroup, groups);

            return groups;
        }

        static void GetAllAudioGroupsSlowRecurse(AudioMixerGroupController g, List<AudioMixerGroupController> groups)
        {
            groups.Add(g);

            foreach (var c in g.children)
                GetAllAudioGroupsSlowRecurse(c, groups);
        }

        public bool HasMoreThanOneGroup()
        {
            return masterGroup.children.Length > 0;
        }

        // Returns true if there is an ancestor of child in the groups list
        bool IsChildOf(AudioMixerGroupController child, List<AudioMixerGroupController> groups)
        {
            var ancestor = child;

            while (ancestor != null)
            {
                ancestor = FindParentGroup(masterGroup, ancestor);

                if (ancestor != null && groups.Contains(ancestor))
                {
                    return true;
                }
            }

            return false;
        }

        // Checks whether the groups in the list are in some kind of child-parent relationship. They do of course have common ancestors, but there shouldn't be any
        // ancestor path from items in the list to any other items in the list.
        public bool AreAnyOfTheGroupsInTheListAncestors(List<AudioMixerGroupController> groups)
        {
            return groups.Any(g => IsChildOf(g, groups));
        }

        // Before duplicating or removing a selection, this function is called to make sure that the selection does not contain any items that are in a
        // child-parent relationship. By working on a cleaned list, duplication/removal is simplified a lot and just becomes a matter or recursively traversing
        // each of the items in the selection without the need to watch out for sub-items that already got deleted during the recursive traversal.
        void RemoveAncestorGroups(List<AudioMixerGroupController> groups)
        {
            groups.RemoveAll(g => IsChildOf(g, groups));
            Equals(AreAnyOfTheGroupsInTheListAncestors(groups), false);
        }

        void RemoveExposedParametersContainedInEffect(AudioMixerEffectController effect)
        {
            // Cleanup exposed parameters that were in the effect
            var exposedParams = exposedParameters;

            if (exposedParams.Length == 0)
            {
                return;
            }

            var undoString = "Remove Exposed Effect Parameter";

            if (exposedParams.Length > 1)
            {
                undoString = $"{undoString}s";
            }

            Undo.RecordObject(this, undoString);

            foreach (var param in exposedParams)
            {
                if (effect.ContainsParameterGUID(param.guid))
                {
                    RemoveExposedParameter(param.guid);
                }
            }
        }

        void RemoveExposedParametersContainedInGroup(AudioMixerGroupController group)
        {
            // Clean up the exposed parameters that were in the group.
            var exposedParams = exposedParameters;

            if (exposedParams.Length == 0)
            {
                return;
            }

            var undoString = "Remove Exposed Group Parameter";

            if (exposedParams.Length > 1)
            {
                undoString = $"{undoString}s";
            }

            Undo.RecordObject(this, undoString);

            foreach (var param in exposedParams)
            {
                if (group.GetGUIDForVolume() == param.guid || group.GetGUIDForPitch() == param.guid)
                    RemoveExposedParameter(param.guid);
            }
        }

        void PreprocessGroupsToDeleteRecursive(AudioMixerGroupController group, ICollection<Object> objecsToDelete)
        {
            var children = group.children;

            foreach (var child in children)
                PreprocessGroupsToDeleteRecursive(child, objecsToDelete);

            var effects = group.effects;

            foreach (var effect in effects)
            {
                ClearSendConnectionsTo(effect);
                RemoveExposedParametersContainedInEffect(effect);
                objecsToDelete.Add(effect);
            }

            RemoveExposedParametersContainedInGroup(group);
            objecsToDelete.Add(group);
        }

        public void DeleteGroups(AudioMixerGroupController[] groups)
        {
            if (groups.Length == 0)
            {
                Debug.LogError("Group array is empty.");
                return;
            }

            var filteredGroups = groups.ToList();
            RemoveAncestorGroups(filteredGroups);

            var undoGroupName = "Remove Group";

            if (groups.Length > 1)
            {
                undoGroupName = $"{undoGroupName}s";
            }

            Undo.RegisterCompleteObjectUndo(this, undoGroupName);

            var objectsToDestroy = new List<Object>();

            for (var i = filteredGroups.Count - 1; i >= 0; --i)
            {
                if (filteredGroups[i] == null)
                {
                    Debug.LogError("Skipping null element in group array.");
                    filteredGroups.RemoveAt(i);
                    continue;
                }

                PreprocessGroupsToDeleteRecursive(filteredGroups[i], objectsToDestroy);
            }

            var allGroups = GetAllAudioGroupsSlow();

            foreach (var group in allGroups)
            {
                var childGroupsToRemove = filteredGroups.Intersect(group.children).ToArray();

                if (childGroupsToRemove.Any())
                {
                    Undo.RecordObject(group, "Detach Group Children");
                    group.children = group.children.Except(childGroupsToRemove).ToArray();
                }
            }

            foreach (var o in objectsToDestroy)
            {
                Undo.DestroyObjectImmediate(o);
            }

            OnUnitySelectionChanged();

            Undo.SetCurrentGroupName(undoGroupName);
        }

        public void RemoveEffect(AudioMixerEffectController effect, AudioMixerGroupController group)
        {
            if (effect == null)
            {
                Debug.LogError("Effect is null.");
                return;
            }

            if (group == null)
            {
                Debug.LogError("Group is null.");
                return;
            }

            var effects = group.effects;
            var foundEffect = false;

            for (var i = 0; i < effects.Length; ++i)
            {
                if (effects[i] == effect)
                {
                    foundEffect = true;
                    break;
                }
            }

            if (!foundEffect)
            {
                Debug.LogError($"Group {group.name} does not contain effect {effect.name}");
                return;
            }

            ClearSendConnectionsTo(effect);
            RemoveExposedParametersContainedInEffect(effect);

            var modifiedEffectsList = new List<AudioMixerEffectController>(group.effects);
            modifiedEffectsList.Remove(effect);

            const string undoGroupName = "Remove Effect";

            Undo.RecordObject(group, undoGroupName);

            group.effects = modifiedEffectsList.ToArray();

            Undo.DestroyObjectImmediate(effect);
            Undo.SetCurrentGroupName(undoGroupName);
        }

        public void OnSubAssetChanged()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
        }

        public void CloneNewSnapshotFromTarget(bool storeUndoState)
        {
            var snapshotList = new List<AudioMixerSnapshotController>(snapshots);
            var ob = Instantiate(TargetSnapshot);
            var newSnapshot = ob;
            newSnapshot.name = TargetSnapshot.name + " - Copy";
            snapshotList.Add(newSnapshot);
            snapshots = snapshotList.ToArray();
            TargetSnapshot = snapshotList[snapshotList.Count - 1];

            AssetDatabase.AddObjectToAsset(newSnapshot, this);
            if (storeUndoState)
                Undo.RegisterCreatedObjectUndo(newSnapshot, "");

            OnSubAssetChanged();
        }

        public void RemoveTargetSnapshot()
        {
            if (snapshots.Length < 2)
                return;

            var deletedSnapshot = TargetSnapshot;

            Undo.RecordObject(this, "Remove Snapshot");

            var snapshotList = new List<AudioMixerSnapshotController>(snapshots);
            snapshotList.Remove(deletedSnapshot);
            snapshots = snapshotList.ToArray();

            Undo.DestroyObjectImmediate(deletedSnapshot);

            OnSubAssetChanged();
        }

        public void RemoveSnapshot(AudioMixerSnapshotController snapshot)
        {
            if (snapshots.Length < 2)
                return;

            var deletedSnapshot = snapshot;

            Undo.RecordObject(this, "Remove Snapshot");

            var snapshotList = new List<AudioMixerSnapshotController>(snapshots);
            snapshotList.Remove(deletedSnapshot);
            snapshots = snapshotList.ToArray();

            Undo.DestroyObjectImmediate(deletedSnapshot);

            OnSubAssetChanged();
        }

        public AudioMixerGroupController CreateNewGroup(string name, bool storeUndoState)
        {
            var group = new AudioMixerGroupController(this);
            group.name = name;
            group.PreallocateGUIDs();

            var effect = new AudioMixerEffectController("Attenuation");
            AddNewSubAsset(effect, storeUndoState);
            effect.PreallocateGUIDs();

            group.InsertEffect(effect, 0);
            AddNewSubAsset(group, storeUndoState);

            return group;
        }

        public void AddChildToParent(AudioMixerGroupController child, AudioMixerGroupController parent)
        {
            RemoveGroupsFromParent(new[] { child }, false);
            var modifiedChildList = new List<AudioMixerGroupController>(parent.children);
            modifiedChildList.Add(child);
            parent.children = modifiedChildList.ToArray();
        }

        void AddNewSubAsset(Object obj, bool storeUndoState)
        {
            AssetDatabase.AddObjectToAsset(obj, this);
            if (storeUndoState)
                Undo.RegisterCreatedObjectUndo(obj, "");
        }

        public void RemoveGroupsFromParent(AudioMixerGroupController[] groups, bool storeUndoState)
        {
            List<AudioMixerGroupController> filteredGroups = groups.ToList();
            RemoveAncestorGroups(filteredGroups);

            if (storeUndoState)
                Undo.RecordObject(this, "Remove group");
            foreach (var g in filteredGroups)
            {
                List<AudioMixerGroupController> allGroups = GetAllAudioGroupsSlow();
                foreach (var parent in allGroups)
                {
                    List<AudioMixerGroupController> modifiedChildList = new List<AudioMixerGroupController>(parent.children);
                    if (modifiedChildList.Contains(g))
                        modifiedChildList.Remove(g);
                    if (parent.children.Length != modifiedChildList.Count)
                        parent.children = modifiedChildList.ToArray();
                }
            }
        }

        public AudioMixerGroupController FindParentGroup(AudioMixerGroupController node, AudioMixerGroupController group)
        {
            for (int n = 0; n < node.children.Length; n++)
            {
                if (node.children[n] == group)
                    return node;
                var g = FindParentGroup(node.children[n], group);
                if (g != null)
                    return g;
            }

            return null;
        }

        public AudioMixerEffectController CopyEffect(AudioMixerEffectController sourceEffect)
        {
            var copiedEffect = new AudioMixerEffectController(sourceEffect.effectName);
            copiedEffect.name = sourceEffect.name;
            copiedEffect.PreallocateGUIDs();
            MixerParameterDefinition[] paramDefs = MixerEffectDefinitions.GetEffectParameters(sourceEffect.effectName);
            float value;
            foreach (var s in snapshots)
            {
                if (s.GetValue(sourceEffect.GetGUIDForMixLevel(), out value))
                    s.SetValue(copiedEffect.GetGUIDForMixLevel(), value);
                foreach (var p in paramDefs)
                {
                    if (s.GetValue(sourceEffect.GetGUIDForParameter(p.name), out value))
                        s.SetValue(copiedEffect.GetGUIDForParameter(p.name), value);
                }
            }

            AssetDatabase.AddObjectToAsset(copiedEffect, this);
            return copiedEffect;
        }

        AudioMixerGroupController DuplicateGroupRecurse(AudioMixerGroupController sourceGroup, bool recordUndo)
        {
            var targetGroup = new AudioMixerGroupController(this);

            // Copy effects
            var targetEffectsList = new List<AudioMixerEffectController>();
            foreach (var sourceEffect in sourceGroup.effects)
            {
                targetEffectsList.Add(CopyEffect(sourceEffect));
            }

            // Copy child groups
            var targetChildren = new List<AudioMixerGroupController>();
            foreach (var childGroup in sourceGroup.children)
                targetChildren.Add(DuplicateGroupRecurse(childGroup, recordUndo));

            targetGroup.name = sourceGroup.name + " - Copy";
            targetGroup.PreallocateGUIDs();
            targetGroup.effects = targetEffectsList.ToArray();
            targetGroup.children = targetChildren.ToArray();
            targetGroup.solo = sourceGroup.solo;
            targetGroup.mute = sourceGroup.mute;
            targetGroup.bypassEffects = sourceGroup.bypassEffects;
            float value;
            foreach (var s in snapshots)
            {
                if (s.GetValue(sourceGroup.GetGUIDForVolume(), out value))
                    s.SetValue(targetGroup.GetGUIDForVolume(), value);
                if (s.GetValue(sourceGroup.GetGUIDForPitch(), out value))
                    s.SetValue(targetGroup.GetGUIDForPitch(), value);
            }

            AssetDatabase.AddObjectToAsset(targetGroup, this);

            if (recordUndo)
                Undo.RegisterCreatedObjectUndo(targetGroup, targetGroup.name);

            // Add to view if source is shown (so it's visible in the channelstrip)
            if (CurrentViewContainsGroup(sourceGroup.groupID))
                targetGroup.controller.AddGroupToCurrentView(targetGroup);

            return targetGroup;
        }

        // Returns duplicated root groups (traverse group.children to get all groups duplicated)
        public List<AudioMixerGroupController> DuplicateGroups(AudioMixerGroupController[] sourceGroups, bool recordUndo)
        {
            List<AudioMixerGroupController> filteredGroups = sourceGroups.ToList();
            RemoveAncestorGroups(filteredGroups);

            var allRoots = new List<AudioMixerGroupController>();

            foreach (var source in filteredGroups)
            {
                AudioMixerGroupController parent = FindParentGroup(masterGroup, source);
                if (parent != null && source != null)
                {
                    if (recordUndo)
                        Undo.RecordObject(parent, "Reparent AudioMixerGroup");

                    AudioMixerGroupController copy = DuplicateGroupRecurse(source, recordUndo);

                    var modifiedChildList = new List<AudioMixerGroupController>(parent.children);
                    modifiedChildList.Add(copy);
                    parent.children = modifiedChildList.ToArray();

                    allRoots.Add(copy);
                }
            }

            return allRoots;
        }

        public void CopyEffectSettingsToAllSnapshots(AudioMixerGroupController group, int effectIndex, AudioMixerSnapshotController snapshot, bool includeWetParam)
        {
            AudioMixerSnapshotController[] snaps = snapshots;
            for (int n = 0; n < snaps.Length; n++)
            {
                if (snaps[n] == snapshot)
                    continue;
                var effect = group.effects[effectIndex];
                MixerParameterDefinition[] paramDefs = MixerEffectDefinitions.GetEffectParameters(effect.effectName);
                float value;
                if (includeWetParam)
                {
                    var guid = effect.GetGUIDForMixLevel();
                    if (snapshot.GetValue(guid, out value))
                        snaps[n].SetValue(guid, value);
                }

                foreach (var p in paramDefs)
                {
                    var guid = effect.GetGUIDForParameter(p.name);
                    if (snapshot.GetValue(guid, out value))
                        snaps[n].SetValue(guid, value);
                }
            }
        }

        public void CopyAllSettingsToAllSnapshots(AudioMixerGroupController group, AudioMixerSnapshotController snapshot)
        {
            for (int n = 0; n < group.effects.Length; n++)
                CopyEffectSettingsToAllSnapshots(group, n, snapshot, true);

            AudioMixerSnapshotController[] snaps = snapshots;
            for (int n = 0; n < snaps.Length; n++)
            {
                if (snaps[n] == snapshot)
                    continue;
                var dst = snaps[n];
                group.SetValueForVolume(this, dst, group.GetValueForVolume(this, snapshot));
                group.SetValueForPitch(this, dst, group.GetValueForPitch(this, snapshot));
            }
        }

        public void CopyAttenuationToAllSnapshots(AudioMixerGroupController group, AudioMixerSnapshotController snapshot)
        {
            AudioMixerSnapshotController[] snaps = snapshots;
            for (int n = 0; n < snaps.Length; n++)
            {
                if (snaps[n] == snapshot)
                    continue;
                var dst = snaps[n];
                group.SetValueForVolume(this, dst, group.GetValueForVolume(this, snapshot));
            }
        }

        public void ReparentSelection(AudioMixerGroupController newParent, int insertionIndex, List<AudioMixerGroupController> selection)
        {
            // We are moving items so we adjust the insertion index to accomodate that any items above the insertion index is removed before inserting
            if (insertionIndex >= 0)
                insertionIndex -= newParent.children.ToList().GetRange(0, insertionIndex).Count(selection.Contains);

            Undo.RecordObject(newParent, "Change Audio Mixer Group Parent");
            List<AudioMixerGroupController> groups = GetAllAudioGroupsSlow();
            foreach (var g in groups)
            {
                // Check if any groups in the selection is part of current groups child list
                if (g.children.Intersect(selection).Any())
                {
                    Undo.RecordObject(g, string.Empty); // empty string will use undo name above
                    var modifiedChildList = new List<AudioMixerGroupController>(g.children);
                    foreach (var c in selection)
                        modifiedChildList.Remove(c);
                    g.children = modifiedChildList.ToArray();
                }
            }

            // When dragging upon we insert as first child
            if (insertionIndex == -1)
                insertionIndex = 0;

            var newChildList = new List<AudioMixerGroupController>(newParent.children);
            newChildList.InsertRange(insertionIndex, selection);
            newParent.children = newChildList.ToArray();
        }

        public static bool InsertEffect(AudioMixerEffectController effect, ref List<AudioMixerEffectController> targetEffects, int targetIndex)
        {
            if (targetIndex < 0 || targetIndex > targetEffects.Count)
            {
                Debug.LogError("Inserting effect failed! size: " + targetEffects.Count + " at index: " + targetIndex);
                return false;
            }

            targetEffects.Insert(targetIndex, effect);
            return true;
        }

        public static bool MoveEffect(ref List<AudioMixerEffectController> sourceEffects, int sourceIndex, ref List<AudioMixerEffectController> targetEffects, int targetIndex)
        {
            if (sourceEffects == targetEffects)
            {
                if (targetIndex > sourceIndex)
                    targetIndex--;
                if (sourceIndex == targetIndex)
                    return false;
            }

            if (sourceIndex < 0 || sourceIndex >= sourceEffects.Count)
                return false;
            if (targetIndex < 0 || targetIndex > targetEffects.Count)
                return false;
            AudioMixerEffectController effect = sourceEffects[sourceIndex];
            sourceEffects.RemoveAt(sourceIndex);
            targetEffects.Insert(targetIndex, effect);
            return true;
        }

        void ClearSendConnectionsTo(AudioMixerEffectController sendTarget)
        {
            var allGroups = GetAllAudioGroupsSlow();

            foreach (var g in allGroups)
            {
                foreach (var e in g.effects)
                {
                    if (e.IsSend() && e.sendTarget == sendTarget)
                    {
                        var guid = e.GetGUIDForMixLevel();

                        if (ContainsExposedParameter(guid))
                        {
                            Undo.RecordObjects(new Object[] { this, e }, "Clear Send target");
                            RemoveExposedParameter(guid);
                        }
                        else
                        {
                            Undo.RecordObject(e, "Clear Send target");
                        }

                        e.sendTarget = null;
                    }
                }
            }
        }

        public class ConnectionNode
        {
            public bool visited;
            public object groupTail;
            public List<object> targets = new List<object>();
            public AudioMixerGroupController group;
            public AudioMixerEffectController effect;

            public string GetDisplayString()
            {
                string s = group.GetDisplayString();
                if (effect != null)
                    s += s_GroupEffectDisplaySeperator + effect.effectName;
                return s;
            }
        }

        // Builds a graph over all connections going out of a group (the parent counts as one).
        // While building the map, the existing routing of effectSlotUnderTest are ignored and pretended it is connected to targetToTest instead.
        // Then checks are performed whether this setup would cause any loops in the graph.
        static Dictionary<object, ConnectionNode> BuildTemporaryGraph(
            List<AudioMixerGroupController> allGroups,
            AudioMixerGroupController groupWhoseEffectIsChanged, AudioMixerEffectController effectWhoseTargetIsChanged, AudioMixerEffectController targetToTest,
            AudioMixerGroupController modifiedGroup1, List<AudioMixerEffectController> modifiedGroupEffects1,
            AudioMixerGroupController modifiedGroup2, List<AudioMixerEffectController> modifiedGroupEffects2
        )
        {
            // First build the chains of groups and their contained effects
            var graph = new Dictionary<object, ConnectionNode>();
            foreach (var group in allGroups)
            {
                var groupNode = new ConnectionNode();
                groupNode.group = group;
                groupNode.effect = null;
                graph[group] = groupNode;
                object groupTail = group;
                var reorderedEffects = (group == modifiedGroup1) ? modifiedGroupEffects1 : (group == modifiedGroup2) ? modifiedGroupEffects2 : group.effects.ToList();
                foreach (var effect in reorderedEffects)
                {
                    if (!graph.ContainsKey(effect))
                        graph[effect] = new ConnectionNode();
                    graph[effect].group = group;
                    graph[effect].effect = effect;
                    if (!graph[groupTail].targets.Contains(effect))
                        graph[groupTail].targets.Add(effect);
                    AudioMixerEffectController target = (group == groupWhoseEffectIsChanged && effectWhoseTargetIsChanged == effect) ? targetToTest : effect.sendTarget;
                    if (target != null)
                    {
                        if (!graph.ContainsKey(target))
                        {
                            graph[target] = new ConnectionNode();
                            graph[target].group = group;
                            graph[target].effect = target;
                        }

                        if (!graph[effect].targets.Contains(target))
                            graph[effect].targets.Add(target);
                    }

                    groupTail = effect;
                }

                graph[group].groupTail = groupTail;
            }

            return graph;
        }

        static void ListTemporaryGraph(Dictionary<object, ConnectionNode> graph)
        {
            Debug.Log("Listing temporary graph:");
            int nodeIndex = 0;
            foreach (var node in graph)
            {
                Debug.Log(string.Format("Node {0}: {1}", nodeIndex++, node.Value.GetDisplayString()));
                int targetIndex = 0;
                foreach (var t in node.Value.targets)
                    Debug.Log(string.Format("  Target {0}: {1}", targetIndex++, graph[t].GetDisplayString()));
            }
        }

        static bool CheckForCycle(object curr, Dictionary<object, ConnectionNode> graph, List<ConnectionNode> identifiedLoop)
        {
            var node = graph[curr];
            if (node.visited)
            {
                if (identifiedLoop != null)
                {
                    identifiedLoop.Clear();
                    identifiedLoop.Add(node);
                }

                return true;
            }

            node.visited = true;
            foreach (var s in node.targets)
            {
                if (CheckForCycle(s, graph, identifiedLoop))
                {
                    node.visited = false;
                    if (identifiedLoop != null)
                        identifiedLoop.Add(node);
                    return true;
                }
            }

            node.visited = false;
            return false;
        }

        public static bool DoesTheTemporaryGraphHaveAnyCycles(List<AudioMixerGroupController> allGroups, List<ConnectionNode> identifiedLoop, Dictionary<object, ConnectionNode> graph)
        {
            foreach (var g in allGroups)
            {
                if (CheckForCycle(g, graph, identifiedLoop))
                {
                    // Clean up identified loop so that we only show the looping part and not what lead to it
                    if (identifiedLoop != null)
                    {
                        var start = identifiedLoop[0];
                        int i;
                        for (i = 1; i < identifiedLoop.Count;)
                            if (identifiedLoop[i++] == start)
                                break;
                        identifiedLoop.RemoveRange(i, identifiedLoop.Count - i);
                        identifiedLoop.Reverse();
                    }

                    return true;
                }
            }

            return false;
        }

        public static bool WillChangeOfEffectTargetCauseFeedback(List<AudioMixerGroupController> allGroups, AudioMixerGroupController groupWhoseEffectIsChanged, int effectWhoseTargetIsChanged, AudioMixerEffectController targetToTest, List<ConnectionNode> identifiedLoop)
        {
            var graph = BuildTemporaryGraph(allGroups, groupWhoseEffectIsChanged, groupWhoseEffectIsChanged.effects[effectWhoseTargetIsChanged], targetToTest, null, null, null, null);

            // Connect the chains up using the group hierarchy
            foreach (var group in allGroups)
            {
                foreach (var childGroup in group.children)
                {
                    var tailOfChildGroup = graph[childGroup].groupTail;
                    if (!graph[tailOfChildGroup].targets.Contains(group))
                        graph[tailOfChildGroup].targets.Add(group);
                }
            }

            //ListTemporaryGraph(graph);

            return DoesTheTemporaryGraphHaveAnyCycles(allGroups, identifiedLoop, graph);
        }

        public static bool WillModificationOfTopologyCauseFeedback(List<AudioMixerGroupController> allGroups, List<AudioMixerGroupController> groupsToBeMoved, AudioMixerGroupController newParentForMovedGroups, List<ConnectionNode> identifiedLoop)
        {
            var graph = BuildTemporaryGraph(allGroups, null, null, null, null, null, null, null);

            // Connect the chains up using the group hierarchy, pretending that the groups in the list groupsToBeMoved belong to newParentForMovedGroups
            foreach (var group in allGroups)
            {
                foreach (var childGroup in group.children)
                {
                    var parentGroup = (groupsToBeMoved.Contains(childGroup)) ? newParentForMovedGroups : group;
                    var tailOfChildGroup = graph[childGroup].groupTail;
                    if (!graph[tailOfChildGroup].targets.Contains(parentGroup))
                        graph[tailOfChildGroup].targets.Add(parentGroup);
                }
            }

            //ListTemporaryGraph(graph);

            return DoesTheTemporaryGraphHaveAnyCycles(allGroups, identifiedLoop, graph);
        }

        public static bool WillMovingEffectCauseFeedback(List<AudioMixerGroupController> allGroups, AudioMixerGroupController sourceGroup, int sourceIndex, AudioMixerGroupController targetGroup, int targetIndex, List<ConnectionNode> identifiedLoop)
        {
            Dictionary<object, ConnectionNode> graph;

            if (sourceGroup == targetGroup)
            {
                var modifiedEffects = sourceGroup.effects.ToList();
                if (!MoveEffect(ref modifiedEffects, sourceIndex, ref modifiedEffects, targetIndex))
                    return false;
                graph = BuildTemporaryGraph(allGroups, null, null, null, sourceGroup, modifiedEffects, null, null);
            }
            else
            {
                var modifiedSourceEffects = sourceGroup.effects.ToList();
                var modifiedTargetEffects = targetGroup.effects.ToList();
                if (!MoveEffect(ref modifiedSourceEffects, sourceIndex, ref modifiedTargetEffects, targetIndex))
                    return false;
                graph = BuildTemporaryGraph(allGroups, null, null, null, sourceGroup, modifiedSourceEffects, targetGroup, modifiedTargetEffects);
            }

            // Connect the chains up using the group hierarchy
            foreach (var group in allGroups)
            {
                foreach (var childGroup in group.children)
                {
                    var tailOfChildGroup = graph[childGroup].groupTail;
                    if (!graph[tailOfChildGroup].targets.Contains(group))
                        graph[tailOfChildGroup].targets.Add(group);
                }
            }

            //ListTemporaryGraph(graph);

            return DoesTheTemporaryGraphHaveAnyCycles(allGroups, identifiedLoop, graph);
        }

        public static float DbToLin(float x)
        {
            // This check needs to be kept in sync with the runtime. It serves as a cutoff point at which connections can be cut or effects bypassed.
            if (x < kMinVolume)
                return 0.0f;

            return Mathf.Pow(10.0f, x * 0.05f);
        }

        public void CloneViewFromCurrent()
        {
            Undo.RecordObject(this, "Create view");

            var viewList = new List<MixerGroupView>(views);
            MixerGroupView view = new MixerGroupView();
            view.name = views[currentViewIndex].name + " - Copy";
            view.guids = views[currentViewIndex].guids;
            viewList.Add(view);
            views = viewList.ToArray();

            currentViewIndex = viewList.Count - 1;
        }

        public void DeleteView(int index)
        {
            Undo.RecordObject(this, "Delete view");

            var viewList = new List<MixerGroupView>(views);
            viewList.RemoveAt(index);
            views = viewList.ToArray();

            int newIndex = Mathf.Clamp(currentViewIndex, 0, viewList.Count - 1);
            ForceSetView(newIndex);
        }

        public void SetView(int index)
        {
            if (currentViewIndex != index)
                ForceSetView(index);
        }

        public void SanitizeGroupViews()
        {
            List<AudioMixerGroupController> allGroups = GetAllAudioGroupsSlow();
            var viewList = views;

            for (int i = 0; i < viewList.Length; i++)
            {
                viewList[i].guids =
                    (from x in viewList[i].guids
                        from y in allGroups
                        where y.groupID == x
                        select x).ToArray();
            }

            views = viewList.ToArray();
        }

        public void ForceSetView(int index)
        {
            currentViewIndex = index;
            SanitizeGroupViews();
        }

        public void AddGroupToCurrentView(AudioMixerGroupController group)
        {
            var viewList = views;
            List<GUID> guidList = viewList[currentViewIndex].guids.ToList();
            guidList.Add(group.groupID);
            viewList[currentViewIndex].guids = guidList.ToArray();
            views = viewList.ToArray();
        }

        public void SetCurrentViewVisibility(GUID[] guids)
        {
            var viewList = views;
            viewList[currentViewIndex].guids = guids;
            views = viewList.ToArray();
            SanitizeGroupViews();
        }

        public AudioMixerGroupController[] GetCurrentViewGroupList()
        {
            List<AudioMixerGroupController> allGroups = GetAllAudioGroupsSlow();
            MixerGroupView view = views[currentViewIndex];

            return (from g in allGroups
                where view.guids.Contains(g.groupID)
                select g).ToArray();
        }

        public static float VolumeToScreenMapping(float value, float screenRange, bool forward)
        {
            float screenRange1 = GetVolumeSplitPoint() * screenRange;
            float screenRange2 = screenRange - screenRange1;
            if (forward)
                return (value > 0.0f) ? (screenRange1 - Mathf.Pow(value / GetMaxVolume(), 1.0f / kVolumeWarp) * screenRange1) : ((Mathf.Pow(value / kMinVolume, 1.0f / kVolumeWarp) * screenRange2) + screenRange1);
            return (value < screenRange1) ? (Mathf.Pow(1.0f - (value / screenRange1), kVolumeWarp) * GetMaxVolume()) : (Mathf.Pow((value - screenRange1) / screenRange2, kVolumeWarp) * kMinVolume);
        }

        public void OnUnitySelectionChanged()
        {
            List<AudioMixerGroupController> allGroups = GetAllAudioGroupsSlow();

            var selected = Selection.GetFiltered(typeof(AudioMixerGroupController), SelectionMode.Deep);
            m_CachedSelection = allGroups.Intersect(selected.Select(g => (AudioMixerGroupController)g)).ToList();
        }
    }
}
