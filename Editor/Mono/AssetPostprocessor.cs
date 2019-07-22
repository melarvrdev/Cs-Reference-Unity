// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using UnityEngine.Profiling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.AssetImporters;
using Object = UnityEngine.Object;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor
{
    // AssetPostprocessor lets you hook into the import pipeline and run scripts prior or after importing assets.
    public partial class AssetPostprocessor
    {
        private string m_PathName;
        private AssetImportContext m_Context;

        // The path name of the asset being imported.
        public string assetPath { get { return m_PathName; } set { m_PathName = value; } }

        // The context of the import, used to specify dependencies
        public AssetImportContext context { get { return m_Context; } internal set { m_Context = value; } }

        // Logs an import warning to the console.
        [ExcludeFromDocs]
        public void LogWarning(string warning)
        {
            Object context = null;
            LogWarning(warning, context);
        }

        public void LogWarning(string warning, [DefaultValue("null")]  Object context) { Debug.LogWarning(warning, context); }

        // Logs an import error message to the console.
        [ExcludeFromDocs]
        public void LogError(string warning)
        {
            Object context = null;
            LogError(warning, context);
        }

        public void LogError(string warning, [DefaultValue("null")]  Object context) { Debug.LogError(warning, context); }

        // Returns the version of the asset postprocessor.
        public virtual uint GetVersion() { return 0; }

        // Reference to the asset importer
        public AssetImporter assetImporter { get { return AssetImporter.GetAtPath(assetPath); } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("To set or get the preview, call EditorUtility.SetAssetPreview or AssetPreview.GetAssetPreview instead", true)]
        public Texture2D preview { get { return null; } set {} }

        // Override the order in which importers are processed.
        public virtual int GetPostprocessOrder() { return 0; }

    }

    internal class AssetPostprocessingInternal
    {
        static void LogPostProcessorMissingDefaultConstructor(Type type)
        {
            Debug.LogErrorFormat("{0} requires a default constructor to be used as an asset post processor", type);
        }

        [RequiredByNativeCode]
        // Postprocess on all assets once an automatic import has completed
        static void PostprocessAllAssets(string[] importedAssets, string[] addedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPathAssets)
        {
            bool profile = Profiler.enabled;
            object[] args = { importedAssets, deletedAssets, movedAssets, movedFromPathAssets };
            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                MethodInfo method = assetPostprocessorClass.GetMethod("OnPostprocessAllAssets", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    InvokeMethod(method, args);
                }
            }

            Profiler.BeginSample("SyncVS.PostprocessSyncProject");
            ///@TODO: we need addedAssets for SyncVS. Make this into a proper API and write tests
            CodeEditorProjectSync.PostprocessSyncProject(importedAssets, addedAssets, deletedAssets, movedAssets, movedFromPathAssets);
            Profiler.EndSample();
        }

        [RequiredByNativeCode]
        static void PreprocessAssembly(string pathName)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                InvokeMethodIfAvailable(inst, "OnPreprocessAssembly", new[] { pathName });
            }
        }

        internal class CompareAssetImportPriority : IComparer
        {
            int IComparer.Compare(System.Object xo, System.Object yo)
            {
                int x = ((AssetPostprocessor)xo).GetPostprocessOrder();
                int y = ((AssetPostprocessor)yo).GetPostprocessOrder();
                return x.CompareTo(y);
            }
        }

        private static string BuildHashString(SortedList<string, uint> list)
        {
            var hashStr = "";
            foreach (var pair in list)
            {
                hashStr += pair.Key;
                hashStr += '.';
                hashStr += pair.Value;
                hashStr += '|';
            }

            return hashStr;
        }

        internal class PostprocessStack
        {
            internal ArrayList m_ImportProcessors = null;
        }

        static ArrayList m_PostprocessStack = null;
        static ArrayList m_ImportProcessors = null;
        static Type[] m_PostprocessorClasses = null;
        static string m_MeshProcessorsHashString = null;
        static string m_TextureProcessorsHashString = null;
        static string m_AudioProcessorsHashString = null;
        static string m_SpeedTreeProcessorsHashString = null;

        static Type[] GetCachedAssetPostprocessorClasses()
        {
            if (m_PostprocessorClasses == null)
                m_PostprocessorClasses = TypeCache.GetTypesDerivedFrom<AssetPostprocessor>().ToArray();
            return m_PostprocessorClasses;
        }

        [RequiredByNativeCode]
        static void InitPostprocessors(AssetImportContext context, string pathName)
        {
            m_ImportProcessors = new ArrayList();

            // @TODO: This is just a temporary workaround for the import settings.
            // We should add importers to the asset, persist them and show an inspector for them.
            foreach (Type assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                try
                {
                    var assetPostprocessor = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                    assetPostprocessor.assetPath = pathName;
                    assetPostprocessor.context = context;
                    m_ImportProcessors.Add(assetPostprocessor);
                }
                catch (MissingMethodException)
                {
                    LogPostProcessorMissingDefaultConstructor(assetPostprocessorClass);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            m_ImportProcessors.Sort(new CompareAssetImportPriority());

            // Setup postprocessing stack to support rentrancy (Import asset immediate)
            PostprocessStack postStack = new PostprocessStack();
            postStack.m_ImportProcessors = m_ImportProcessors;
            if (m_PostprocessStack == null)
                m_PostprocessStack = new ArrayList();
            m_PostprocessStack.Add(postStack);
        }

        [RequiredByNativeCode]
        static void CleanupPostprocessors()
        {
            if (m_PostprocessStack != null)
            {
                m_PostprocessStack.RemoveAt(m_PostprocessStack.Count - 1);
                if (m_PostprocessStack.Count != 0)
                {
                    PostprocessStack postStack = (PostprocessStack)m_PostprocessStack[m_PostprocessStack.Count - 1];
                    m_ImportProcessors = postStack.m_ImportProcessors;
                }
            }
        }

        static bool ImplementsAnyOfTheses(Type type, string[] methods)
        {
            foreach (var method in methods)
            {
                if (type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null)
                    return true;
            }
            return false;
        }

        [RequiredByNativeCode]
        static string GetMeshProcessorsHashString()
        {
            if (m_MeshProcessorsHashString != null)
                return m_MeshProcessorsHashString;

            var versionsByType = new SortedList<string, uint>();

            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                try
                {
                    var inst = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                    var type = inst.GetType();
                    bool hasAnyPostprocessMethod = ImplementsAnyOfTheses(type, new[]
                    {
                        "OnPreprocessModel",
                        "OnPostprocessMeshHierarchy",
                        "OnPostprocessModel",
                        "OnPreprocessAnimation",
                        "OnPostprocessAnimation",
                        "OnPostprocessGameObjectWithAnimatedUserProperties",
                        "OnPostprocessGameObjectWithUserProperties",
                        "OnPostprocessMaterial",
                        "OnAssignMaterialModel",
                        "OnPreprocessMaterialDescription"
                    });
                    uint version = inst.GetVersion();
                    if (version != 0 && hasAnyPostprocessMethod)
                    {
                        versionsByType.Add(type.FullName, version);
                    }
                }
                catch (MissingMethodException)
                {
                    LogPostProcessorMissingDefaultConstructor(assetPostprocessorClass);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            m_MeshProcessorsHashString = BuildHashString(versionsByType);
            return m_MeshProcessorsHashString;
        }

        [RequiredByNativeCode]
        static void PreprocessAsset()
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                InvokeMethodIfAvailable(inst, "OnPreprocessAsset", null);
            }
        }

        [RequiredByNativeCode]
        static void PreprocessMesh(string pathName)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                InvokeMethodIfAvailable(inst, "OnPreprocessModel", null);
            }
        }

        [RequiredByNativeCode]
        static void PreprocessSpeedTree(string pathName)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                InvokeMethodIfAvailable(inst, "OnPreprocessSpeedTree", null);
            }
        }

        [RequiredByNativeCode]
        static void PreprocessAnimation(string pathName)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                InvokeMethodIfAvailable(inst, "OnPreprocessAnimation", null);
            }
        }

        [RequiredByNativeCode]
        static void PostprocessAnimation(GameObject root, AnimationClip clip)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { root, clip };
                InvokeMethodIfAvailable(inst, "OnPostprocessAnimation", args);
            }
        }

        [RequiredByNativeCode]
        static Material ProcessMeshAssignMaterial(Renderer renderer, Material material)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { material, renderer };
                object assignedMaterial = InvokeMethodIfAvailable(inst, "OnAssignMaterialModel", args);
                if (assignedMaterial as Material)
                    return assignedMaterial as Material;
            }

            return null;
        }

        [RequiredByNativeCode]
        static bool ProcessMeshHasAssignMaterial()
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                if (inst.GetType().GetMethod("OnAssignMaterialModel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null)
                    return true;
            }

            return false;
        }

        [RequiredByNativeCode]
        static void PostprocessMeshHierarchy(GameObject root)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { root };
                InvokeMethodIfAvailable(inst, "OnPostprocessMeshHierarchy", args);
            }
        }

        static void PostprocessMesh(GameObject gameObject)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { gameObject };
                InvokeMethodIfAvailable(inst, "OnPostprocessModel", args);
            }
        }

        static void PostprocessSpeedTree(GameObject gameObject)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { gameObject };
                InvokeMethodIfAvailable(inst, "OnPostprocessSpeedTree", args);
            }
        }

        [RequiredByNativeCode]
        static void PostprocessMaterial(Material material)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { material };
                InvokeMethodIfAvailable(inst, "OnPostprocessMaterial", args);
            }
        }

        [RequiredByNativeCode]
        static void PreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] animations)
        {
            object[] args = { description, material, animations };
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                InvokeMethodIfAvailable(inst, "OnPreprocessMaterialDescription", args);
            }
        }

        [RequiredByNativeCode]
        static void PostprocessGameObjectWithUserProperties(GameObject go, string[] prop_names, object[] prop_values)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { go, prop_names, prop_values };
                InvokeMethodIfAvailable(inst, "OnPostprocessGameObjectWithUserProperties", args);
            }
        }

        [RequiredByNativeCode]
        static EditorCurveBinding[] PostprocessGameObjectWithAnimatedUserProperties(GameObject go, EditorCurveBinding[] bindings)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { go, bindings };
                InvokeMethodIfAvailable(inst, "OnPostprocessGameObjectWithAnimatedUserProperties", args);
            }
            return bindings;
        }

        [RequiredByNativeCode]
        static string GetTextureProcessorsHashString()
        {
            if (m_TextureProcessorsHashString != null)
                return m_TextureProcessorsHashString;

            var versionsByType = new SortedList<string, uint>();

            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                try
                {
                    var inst = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                    var type = inst.GetType();
                    bool hasPreProcessMethod = type.GetMethod("OnPreprocessTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    bool hasPostProcessMethod = (type.GetMethod("OnPostprocessTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null) ||
                        (type.GetMethod("OnPostprocessCubemap", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null);
                    uint version = inst.GetVersion();
                    if (version != 0 && (hasPreProcessMethod || hasPostProcessMethod))
                    {
                        versionsByType.Add(type.FullName, version);
                    }
                }
                catch (MissingMethodException)
                {
                    LogPostProcessorMissingDefaultConstructor(assetPostprocessorClass);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            m_TextureProcessorsHashString = BuildHashString(versionsByType);
            return m_TextureProcessorsHashString;
        }

        [RequiredByNativeCode]
        static void PreprocessTexture(string pathName)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                InvokeMethodIfAvailable(inst, "OnPreprocessTexture", null);
            }
        }

        [RequiredByNativeCode]
        static void PostprocessTexture(Texture2D tex, string pathName)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { tex };
                InvokeMethodIfAvailable(inst, "OnPostprocessTexture", args);
            }
        }

        [RequiredByNativeCode]
        static void PostprocessCubemap(Cubemap tex, string pathName)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { tex };
                InvokeMethodIfAvailable(inst, "OnPostprocessCubemap", args);
            }
        }

        [RequiredByNativeCode]
        static void PostprocessSprites(Texture2D tex, string pathName, Sprite[] sprites)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { tex, sprites };
                InvokeMethodIfAvailable(inst, "OnPostprocessSprites", args);
            }
        }

        [RequiredByNativeCode]
        static string GetAudioProcessorsHashString()
        {
            if (m_AudioProcessorsHashString != null)
                return m_AudioProcessorsHashString;

            var versionsByType = new SortedList<string, uint>();

            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                try
                {
                    var inst = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                    var type = inst.GetType();
                    bool hasPreProcessMethod = type.GetMethod("OnPreprocessAudio", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    bool hasPostProcessMethod = type.GetMethod("OnPostprocessAudio", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    uint version = inst.GetVersion();
                    if (version != 0 && (hasPreProcessMethod || hasPostProcessMethod))
                    {
                        versionsByType.Add(type.FullName, version);
                    }
                }
                catch (MissingMethodException)
                {
                    LogPostProcessorMissingDefaultConstructor(assetPostprocessorClass);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            m_AudioProcessorsHashString = BuildHashString(versionsByType);
            return m_AudioProcessorsHashString;
        }

        [RequiredByNativeCode]
        static void PreprocessAudio(string pathName)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                InvokeMethodIfAvailable(inst, "OnPreprocessAudio", null);
            }
        }

        [RequiredByNativeCode]
        static void PostprocessAudio(AudioClip tex, string pathName)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { tex };
                InvokeMethodIfAvailable(inst, "OnPostprocessAudio", args);
            }
        }

        [RequiredByNativeCode]
        static void PostprocessAssetbundleNameChanged(string assetPAth, string prevoiusAssetBundleName, string newAssetBundleName)
        {
            object[] args = { assetPAth, prevoiusAssetBundleName, newAssetBundleName };

            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                var assetPostprocessor = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                InvokeMethodIfAvailable(assetPostprocessor, "OnPostprocessAssetbundleNameChanged", args);
            }
        }

        [RequiredByNativeCode]
        static string GetSpeedTreeProcessorsHashString()
        {
            if (m_SpeedTreeProcessorsHashString != null)
                return m_SpeedTreeProcessorsHashString;

            var versionsByType = new SortedList<string, uint>();

            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                try
                {
                    var inst = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                    var type = inst.GetType();
                    bool hasPreProcessMethod = type.GetMethod("OnPreprocessSpeedTree", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    bool hasPostProcessMethod = type.GetMethod("OnPostprocessSpeedTree", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    uint version = inst.GetVersion();
                    if (version != 0 && (hasPreProcessMethod || hasPostProcessMethod))
                    {
                        versionsByType.Add(type.FullName, version);
                    }
                }
                catch (MissingMethodException)
                {
                    LogPostProcessorMissingDefaultConstructor(assetPostprocessorClass);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            m_SpeedTreeProcessorsHashString = BuildHashString(versionsByType);
            return m_SpeedTreeProcessorsHashString;
        }

        static object InvokeMethod(MethodInfo method, object[] args)
        {
            bool profile = Profiler.enabled;
            if (profile)
                Profiler.BeginSample(method.DeclaringType.FullName + "." + method.Name);

            var res = method.Invoke(null, args);

            if (profile)
                Profiler.EndSample();

            return res;
        }

        static object InvokeMethodIfAvailable(object target, string methodName, object[] args)
        {
            bool profile = Profiler.enabled;

            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                if (profile)
                    Profiler.BeginSample(target.GetType().FullName + "." + methodName);

                var res = method.Invoke(target, args);

                if (profile)
                    Profiler.EndSample();

                return res;
            }
            else
            {
                return null;
            }
        }
    }
}
