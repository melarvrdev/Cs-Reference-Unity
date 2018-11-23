// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    static class EditorToolUtility
    {
        struct CustomEditorToolAssociation
        {
            public Type targetBehaviour;
            public Type editorTool;
        }

        static CustomEditorToolAssociation[] s_CustomEditorTools;
        static Dictionary<Type, List<Type>> s_CustomEditorToolsTypeAssociations = new Dictionary<Type, List<Type>>();

        static CustomEditorToolAssociation[] customEditorTools
        {
            get
            {
                if (s_CustomEditorTools == null)
                {
                    Type[] editorTools = EditorAssemblies.GetAllTypesWithAttribute<EditorToolAttribute>()
                        .Where(x => !x.IsAbstract)
                        .ToArray();

                    int len = editorTools.Length;

                    s_CustomEditorTools = new CustomEditorToolAssociation[len];

                    for (int i = 0; i < len; i++)
                    {
                        var customToolAttribute = (EditorToolAttribute)editorTools[i].GetCustomAttributes(typeof(EditorToolAttribute), false).FirstOrDefault();

                        s_CustomEditorTools[i] = new CustomEditorToolAssociation()
                        {
                            editorTool = editorTools[i],
                            targetBehaviour = customToolAttribute.targetType
                        };
                    }
                }

                return s_CustomEditorTools;
            }
        }

        internal static List<Type> GetCustomEditorToolsForType(Type type)
        {
            List<Type> res;

            if (type == null)
            {
                res = new List<Type>();
                foreach (var tool in customEditorTools)
                    if (!IsBuiltinTool(tool.editorTool) && tool.targetBehaviour == null)
                        res.Add(tool.editorTool);
                return res;
            }

            if (s_CustomEditorToolsTypeAssociations.TryGetValue(type, out res))
                return res;

            s_CustomEditorToolsTypeAssociations.Add(type, res = new List<Type>());

            for (int i = 0, c = customEditorTools.Length; i < c; i++)
            {
                if (customEditorTools[i].targetBehaviour != null
                    && (customEditorTools[i].targetBehaviour.IsAssignableFrom(type)
                        || type.IsAssignableFrom(customEditorTools[i].targetBehaviour)))
                    res.Add(customEditorTools[i].editorTool);
            }

            return res;
        }

        internal static string GetToolName(Type tool)
        {
            var attributes = tool.GetCustomAttributes(typeof(EditorToolAttribute), false);

            foreach (var attrib in attributes)
            {
                var menuAttrib = attrib as EditorToolAttribute;

                if (menuAttrib != null)
                    return menuAttrib.displayName;
            }

            return ObjectNames.NicifyVariableName(tool.Name);
        }

        internal static string GetToolName(EditorTool tool)
        {
            return GetToolName(tool != null ? tool.GetType() : typeof(EditorTool));
        }

        static EditorToolAttribute GetEditorToolAttribute(EditorTool tool)
        {
            if (tool == null)
                return null;
            return GetEditorToolAttribute(tool.GetType());
        }

        internal static EditorToolAttribute GetEditorToolAttribute(Type type)
        {
            if (type == null)
                return null;

            return (EditorToolAttribute)type.GetCustomAttributes(typeof(EditorToolAttribute), false).FirstOrDefault();
        }

        internal static Type GetCustomEditorToolTargetType(EditorTool tool)
        {
            var attr = GetEditorToolAttribute(tool);
            if (attr == null)
                return null;
            return attr.targetType;
        }

        internal static Dictionary<Type, List<Component>> FindActiveCustomEditorTools()
        {
            var selection = Selection.transforms;
            var tools = new Dictionary<Type, List<Component>>();

            for (int i = 0, c = selection.Length; i < c; i++)
            {
                foreach (var component in selection[i].GetComponents<Component>())
                {
                    if (component != null)
                    {
                        var eligibleToolTypes = GetCustomEditorToolsForType(component.GetType());

                        foreach (var type in eligibleToolTypes)
                        {
                            List<Component> targets;

                            if (tools.TryGetValue(type, out targets))
                                tools[type].Add(component);
                            else
                                tools.Add(type, new List<Component>() { component });
                        }
                    }
                }
            }

            return tools;
        }

        internal static EditorTool GetEditorToolWithEnum(Tool type)
        {
            if (type == Tool.View)
                return EditorToolContext.GetSingleton<ViewModeTool>();
            if (type == Tool.Transform)
                return EditorToolContext.GetSingleton<TransformTool>();
            if (type == Tool.Move)
                return EditorToolContext.GetSingleton<MoveTool>();
            if (type == Tool.Rotate)
                return EditorToolContext.GetSingleton<RotateTool>();
            if (type == Tool.Scale)
                return EditorToolContext.GetSingleton<ScaleTool>();
            if (type == Tool.Rect)
                return EditorToolContext.GetSingleton<RectTool>();
            if (type == Tool.Custom)
            {
                var tool = EditorToolContext.GetLastTool(x => GetEnumWithEditorTool(x) == Tool.Custom);
                if (tool != null)
                    return tool;
            }

            return EditorToolContext.GetSingleton<NoneTool>();
        }

        internal static Tool GetEnumWithEditorTool(EditorTool tool)
        {
            if (tool == null || tool is NoneTool)
                return Tool.None;
            if (tool is ViewModeTool)
                return Tool.View;
            if (tool is TransformTool)
                return Tool.Transform;
            if (tool is MoveTool)
                return Tool.Move;
            if (tool is RotateTool)
                return Tool.Rotate;
            if (tool is ScaleTool)
                return Tool.Scale;
            if (tool is RectTool)
                return Tool.Rect;

            return Tool.Custom;
        }

        static bool IsBuiltinTool(Type type)
        {
            return type == typeof(ViewModeTool) ||
                type == typeof(TransformTool) ||
                type == typeof(MoveTool) ||
                type == typeof(RotateTool) ||
                type == typeof(ScaleTool) ||
                type == typeof(RectTool);
        }

        internal static bool IsCustomEditorTool(Type type)
        {
            for (int i = 0, c = customEditorTools.Length; i < c; i++)
                if (customEditorTools[i].editorTool == type)
                    return customEditorTools[i].targetBehaviour != null;
            return false;
        }
    }
}
