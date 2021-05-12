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
using UnityEditorInternal;
using Unity.CodeEditor;
using UnityEditor.Profiling;

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
        [Serializable]
        class AssetPostProcessorAnalyticsData
        {
            public string importActionId;
            public List<AssetPostProcessorMethodCallAnalyticsData> postProcessorCalls = new List<AssetPostProcessorMethodCallAnalyticsData>();
        }

        [Serializable]
        struct AssetPostProcessorMethodCallAnalyticsData
        {
            public string methodName;
            public float duration_sec;
            public int invocationCount;
        }

        static void LogPostProcessorMissingDefaultConstructor(Type type)
        {
            Debug.LogErrorFormat("{0} requires a default constructor to be used as an asset post processor", type);
        }

        [RequiredByNativeCode]
        // Postprocess on all assets once an automatic import has completed
        static void PostprocessAllAssets(string[] importedAssets, string[] addedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPathAssets)
        {
            object[] args = { importedAssets, deletedAssets, movedAssets, movedFromPathAssets };
            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                const string methodName = "OnPostprocessAllAssets";
                MethodInfo method = assetPostprocessorClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    using (new EditorPerformanceMarker($"{assetPostprocessorClass.Name}.{methodName}", assetPostprocessorClass).Auto())
                        InvokeMethod(method, args);
                }
            }

            using (new EditorPerformanceMarker("SyncVS.PostprocessSyncProject").Auto())
                CodeEditorProjectSync.PostprocessSyncProject(importedAssets, addedAssets, deletedAssets, movedAssets, movedFromPathAssets);
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

        const string kCameraPostprocessorDependencyName = "postprocessor/camera";
        const string kLightPostprocessorDependencyName = "postprocessor/light";

        static ArrayList m_PostprocessStack = null;
        static ArrayList m_ImportProcessors = null;
        static Type[] m_PostprocessorClasses = null;
        static string m_MeshProcessorsHashString = null;
        static string m_TextureProcessorsHashString = null;
        static string m_AudioProcessorsHashString = null;
        static string m_SpeedTreeProcessorsHashString = null;
        static string m_PrefabProcessorsHashString = null;
        static string m_CameraProcessorsHashString = null;
        static string m_LightProcessorsHashString = null;

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
            var analyticsEvent = new AssetPostProcessorAnalyticsData();

            // This may happen if the processors are initialized outside a proper importer context. This is currently
            // used by the TextureGenerator to be able to invoke the postprocessors on the generated texture.
            if (AssetImporter.GetAtPath(pathName) != null)
            {
                analyticsEvent.importActionId = ((int)Math.Floor(AssetImporter.GetAtPath(pathName).GetImportStartTime() * 1000)).ToString();
            }
            else
            {
                analyticsEvent.importActionId = "None";
            }
            s_AnalyticsEventsStack.Push(analyticsEvent);

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

            if (s_AnalyticsEventsStack.Peek().postProcessorCalls.Count != 0)
                EditorAnalytics.SendAssetPostprocessorsUsage(s_AnalyticsEventsStack.Peek());

            s_AnalyticsEventsStack.Pop();
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
                    if (hasAnyPostprocessMethod)
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
        static void PreprocessModel(string pathName)
        {
            CallPostProcessMethods("OnPreprocessModel", null);
        }

        [RequiredByNativeCode]
        static void PreprocessSpeedTree(string pathName)
        {
            CallPostProcessMethods("OnPreprocessSpeedTree", null);
        }

        [RequiredByNativeCode]
        static void PreprocessAnimation(string pathName)
        {
            CallPostProcessMethods("OnPreprocessAnimation", null);
        }

        [RequiredByNativeCode]
        static void PostprocessAnimation(GameObject root, AnimationClip clip)
        {
            object[] args = { root, clip };
            CallPostProcessMethods("OnPostprocessAnimation", args);
        }

        [RequiredByNativeCode]
        static Material ProcessMeshAssignMaterial(Renderer renderer, Material material)
        {
            object[] args = { material, renderer };
            Material assignedMaterial;
            CallPostProcessMethodsUntilReturnedObjectIsValid("OnAssignMaterialModel", args, out assignedMaterial);

            return assignedMaterial;
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
            object[] args = { root };
            CallPostProcessMethods("OnPostprocessMeshHierarchy", args);
        }

        static void PostprocessMesh(GameObject gameObject)
        {
            object[] args = { gameObject };
            CallPostProcessMethods("OnPostprocessModel", args);
        }

        static void PostprocessSpeedTree(GameObject gameObject)
        {
            object[] args = { gameObject };
            CallPostProcessMethods("OnPostprocessSpeedTree", args);
        }

        [RequiredByNativeCode]
        static void PostprocessMaterial(Material material)
        {
            object[] args = { material };
            CallPostProcessMethods("OnPostprocessMaterial", args);
        }

        [RequiredByNativeCode]
        static void PreprocessCameraDescription(AssetImportContext assetImportContext, CameraDescription description, Camera camera, AnimationClip[] animations)
        {
            assetImportContext.DependsOnCustomDependency(kCameraPostprocessorDependencyName);
            object[] args = { description, camera, animations };
            CallPostProcessMethods("OnPreprocessCameraDescription", args);
        }

        [RequiredByNativeCode]
        static void PreprocessLightDescription(AssetImportContext assetImportContext, LightDescription description, Light light, AnimationClip[] animations)
        {
            assetImportContext.DependsOnCustomDependency(kLightPostprocessorDependencyName);
            object[] args = { description, light, animations };
            CallPostProcessMethods("OnPreprocessLightDescription", args);
        }

        [RequiredByNativeCode]
        static void PreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] animations)
        {
            object[] args = { description, material, animations };
            CallPostProcessMethods("OnPreprocessMaterialDescription", args);
        }

        [RequiredByNativeCode]
        static void PostprocessGameObjectWithUserProperties(GameObject go, string[] prop_names, object[] prop_values)
        {
            object[] args = { go, prop_names, prop_values };
            CallPostProcessMethods("OnPostprocessGameObjectWithUserProperties", args);
        }

        [RequiredByNativeCode]
        static EditorCurveBinding[] PostprocessGameObjectWithAnimatedUserProperties(GameObject go, EditorCurveBinding[] bindings)
        {
            object[] args = { go, bindings };
            CallPostProcessMethods("OnPostprocessGameObjectWithAnimatedUserProperties", args);
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
                        (type.GetMethod("OnPostprocessCubemap", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null) ||
                        (type.GetMethod("OnPostprocessTexture3D", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null) ||
                        (type.GetMethod("OnPostprocessTexture2DArray", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null);
                    uint version = inst.GetVersion();
                    if (hasPreProcessMethod || hasPostProcessMethod)
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
            CallPostProcessMethods("OnPreprocessTexture", null);
        }

        [RequiredByNativeCode]
        static void PostprocessTexture(Texture2D tex, string pathName)
        {
            object[] args = { tex };
            CallPostProcessMethods("OnPostprocessTexture", args);
        }

        [RequiredByNativeCode]
        static void PostprocessCubemap(Cubemap tex, string pathName)
        {
            object[] args = { tex };
            CallPostProcessMethods("OnPostprocessCubemap", args);
        }

        [RequiredByNativeCode]
        static void PostprocessTexture3D(Texture3D tex, string pathName)
        {
            object[] args = { tex };
            CallPostProcessMethods("OnPostprocessTexture3D", args);
        }

        [RequiredByNativeCode]
        static void PostprocessTexture2DArray(Texture2DArray tex, string pathName)
        {
            object[] args = { tex };
            CallPostProcessMethods("OnPostprocessTexture2DArray", args);
        }

        [RequiredByNativeCode]
        static void PostprocessSprites(Texture2D tex, string pathName, Sprite[] sprites)
        {
            object[] args = { tex, sprites };
            CallPostProcessMethods("OnPostprocessSprites", args);
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
                    if (hasPreProcessMethod || hasPostProcessMethod)
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
            CallPostProcessMethods("OnPreprocessAudio", null);
        }

        static Stack<AssetPostProcessorAnalyticsData> s_AnalyticsEventsStack = new Stack<AssetPostProcessorAnalyticsData>();

        [RequiredByNativeCode]
        static void PostprocessAudio(AudioClip clip, string pathName)
        {
            object[] args = { clip };
            CallPostProcessMethods("OnPostprocessAudio", args);
        }

        [RequiredByNativeCode]
        static string GetPrefabProcessorsHashString()
        {
            if (m_PrefabProcessorsHashString != null)
                return m_PrefabProcessorsHashString;

            var versionsByType = new SortedList<string, uint>();

            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                try
                {
                    var inst = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                    var type = inst.GetType();
                    bool hasPostProcessMethod = type.GetMethod("OnPostprocessPrefab", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    uint version = inst.GetVersion();
                    if (version != 0 && hasPostProcessMethod)
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

            m_PrefabProcessorsHashString = BuildHashString(versionsByType);
            return m_PrefabProcessorsHashString;
        }

        [RequiredByNativeCode]
        static void PostprocessPrefab(GameObject prefabAssetRoot)
        {
            object[] args = { prefabAssetRoot };
            CallPostProcessMethods("OnPostprocessPrefab", args);
        }

        [RequiredByNativeCode]
        static void PostprocessAssetbundleNameChanged(string assetPath, string prevoiusAssetBundleName, string newAssetBundleName)
        {
            object[] args = { assetPath, prevoiusAssetBundleName, newAssetBundleName };

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
                    if (hasPreProcessMethod || hasPostProcessMethod)
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

        [InitializeOnLoadMethod]
        static void RefreshCustomDependencies()
        {
            AssetDatabase.RegisterCustomDependency(kCameraPostprocessorDependencyName, Hash128.Compute(GetCameraProcessorsHashString()));
            AssetDatabase.RegisterCustomDependency(kLightPostprocessorDependencyName, Hash128.Compute(GetLightProcessorsHashString()));
        }

        static void GetProcessorHashString(string methodName, ref string hashString)
        {
            if (hashString != null)
                return;

            var versionsByType = new SortedList<string, uint>();

            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                try
                {
                    if (assetPostprocessorClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null)
                    {
                        var inst = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                        uint version = inst.GetVersion();
                        versionsByType.Add(assetPostprocessorClass.FullName, version);
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

            hashString = BuildHashString(versionsByType);
        }

        [RequiredByNativeCode]
        static string GetCameraProcessorsHashString()
        {
            GetProcessorHashString("OnPreprocessCameraDescription", ref m_CameraProcessorsHashString);
            return m_CameraProcessorsHashString;
        }

        [RequiredByNativeCode]
        static string GetLightProcessorsHashString()
        {
            GetProcessorHashString("OnPreprocessLightDescription", ref m_LightProcessorsHashString);
            return m_LightProcessorsHashString;
        }

        static bool IsAssetPostprocessorAnalyticsEnabled()
        {
            return EditorAnalytics.enabled;
        }

        static void CallPostProcessMethodsUntilReturnedObjectIsValid<T>(string methodName, object[] args, out T returnedObject) where T : class
        {
            returnedObject = default(T);
            int invocationCount = 0;
            float startTime = Time.realtimeSinceStartup;

            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                if (InvokeMethodIfAvailable(inst, methodName, args, ref returnedObject))
                {
                    invocationCount++;
                    break;
                }
            }

            if (IsAssetPostprocessorAnalyticsEnabled() && invocationCount > 0)
            {
                var methodCallAnalytics = new AssetPostProcessorMethodCallAnalyticsData();
                methodCallAnalytics.invocationCount = invocationCount;
                methodCallAnalytics.methodName = methodName;
                methodCallAnalytics.duration_sec = Time.realtimeSinceStartup - startTime;
                s_AnalyticsEventsStack.Peek().postProcessorCalls.Add(methodCallAnalytics);
            }
        }

        static void CallPostProcessMethods(string methodName, object[] args)
        {
            if (m_ImportProcessors == null)
            {
                throw new Exception("m_ImportProcessors is null, InitPostProcessors should be called before any of the post process methods are called.");
            }

            if (IsAssetPostprocessorAnalyticsEnabled())
            {
                int invocationCount = 0;
                float startTime = Time.realtimeSinceStartup;
                foreach (AssetPostprocessor inst in m_ImportProcessors)
                {
                    if (InvokeMethodIfAvailable(inst, methodName, args))
                        invocationCount++;
                }

                if (invocationCount > 0)
                {
                    var methodCallAnalytics = new AssetPostProcessorMethodCallAnalyticsData();
                    methodCallAnalytics.invocationCount = invocationCount;
                    methodCallAnalytics.methodName = methodName;
                    methodCallAnalytics.duration_sec = Time.realtimeSinceStartup - startTime;
                    s_AnalyticsEventsStack.Peek().postProcessorCalls.Add(methodCallAnalytics);
                }
            }
            else
            {
                foreach (AssetPostprocessor inst in m_ImportProcessors)
                {
                    InvokeMethodIfAvailable(inst, methodName, args);
                }
            }
        }

        static object InvokeMethod(MethodInfo method, object[] args)
        {
            object res = null;
            using (new EditorPerformanceMarker(method.DeclaringType.Name + "." + method.Name, method.DeclaringType).Auto())
            {
                res = method.Invoke(null, args);
            }

            return res;
        }

        static bool InvokeMethodIfAvailable(object target, string methodName, object[] args)
        {
            var type = target.GetType();
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                using (new EditorPerformanceMarker(type.Name + "." + methodName, type).Auto())
                {
                    method.Invoke(target, args);
                }

                return true;
            }
            return false;
        }

        static bool InvokeMethodIfAvailable<T>(object target, string methodName, object[] args, ref T returnedObject) where T : class
        {
            var type = target.GetType();
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                using (new EditorPerformanceMarker(type.Name + "." + methodName, type).Auto())
                {
                    returnedObject = method.Invoke(target, args) as T;
                }

                return returnedObject != null;
            }
            return false;
        }
    }
}
