// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Audio
{
    [ExcludeFromPreset]
    partial class AudioMixerGroupController
    {
        public static Dictionary<AudioMixerEffectController, AudioMixerGroupController> GetEffectMapSlow(List<AudioMixerGroupController> allGroups)
        {
            var effectMap = new Dictionary<AudioMixerEffectController, AudioMixerGroupController>();

            foreach (var g in allGroups)
                foreach (var e in g.effects)
                    effectMap[e] = g;

            return effectMap;
        }

        public void InsertEffect(AudioMixerEffectController effect, int index)
        {
            var modifiedEffectsList = new List<AudioMixerEffectController>(effects);
            modifiedEffectsList.Add(null);
            for (int i = modifiedEffectsList.Count - 1; i > index; i--)
                modifiedEffectsList[i] = modifiedEffectsList[i - 1];
            modifiedEffectsList[index] = effect;
            effects = modifiedEffectsList.ToArray();
        }

        public bool HasAttenuation()
        {
            foreach (var e in effects)
                if (e.IsAttenuation())
                    return true;
            return false;
        }

        public void DumpHierarchy(string title, int level)
        {
            if (title != "")
                Console.WriteLine(title);

            string prefix = "";
            int l = level;
            while (l-- > 0)
                prefix += "  ";
            Console.WriteLine(prefix + "name=" + name);

            prefix += "  ";
            foreach (var f in effects)
                Console.WriteLine(prefix + "effect=" + f);

            foreach (var g in children)
                g.DumpHierarchy("", level + 1);
        }

        public string GetDisplayString()
        {
            return name; // AudioMixerController.FixNameForPopupMenu(name);
        }

        public override string ToString()
        {
            return name;
        }
    }

    class MixerGroupControllerCompareByName : IComparer<AudioMixerGroupController>
    {
        public int Compare(AudioMixerGroupController x, AudioMixerGroupController y)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(x.GetDisplayString(), y.GetDisplayString());
        }
    }
}
