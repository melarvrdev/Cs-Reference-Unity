// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal static class AssetPreviewUpdater
    {
        public static Texture2D CreatePreviewForAsset(Object obj, Object[] subAssets, string assetPath)
        {
            return CreatePreview(obj, subAssets, assetPath, 128, 128);
        }

        // Generate a preview texture for an asset
        public static Texture2D CreatePreview(Object obj, Object[] subAssets, string assetPath, int width, int height)
        {
            if (obj == null)
                return null;

            System.Type type = CustomEditorAttributes.FindCustomEditorType(obj, false);
            if (type == null)
                return null;

            System.Reflection.MethodInfo info = type.GetMethod("RenderStaticPreview");
            if (info == null)
            {
                Debug.LogError("Fail to find RenderStaticPreview base method");
                return null;
            }

            if (info.DeclaringType == typeof(Editor))
                return null;


            Editor editor = Editor.CreateEditor(obj);

            if (editor == null)
                return null;

            Texture2D tex = editor.RenderStaticPreview(assetPath, subAssets, width, height);

            // For debugging we write the preview to a file (keep)
            //{
            //  var bytes = tex.EncodeToPNG();
            //  string previewFilePath = string.Format ("{0}/../SavedPreview{1}.png", Application.dataPath, (int)(EditorApplication.timeSinceStartup*1000));
            //  System.IO.File.WriteAllBytes(previewFilePath, bytes);
            //  Debug.Log ("Wrote preview file to: " +previewFilePath);
            //}

            Object.DestroyImmediate(editor);

            return tex;
        }
    }
}
