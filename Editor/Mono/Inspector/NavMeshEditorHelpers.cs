// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.AI
{
    [InitializeOnLoad]
    internal static class CheckNavigationPackage
    {
        const string k_NavigationPackageId = "com.unity.ai.navigation";
        const string k_NavigationComponentMenuRoot = "Component/Navigation";

        static string s_NavigationPackagePath = $"Packages/{k_NavigationPackageId}/package.json";

        static CheckNavigationPackage()
        {
            if (!IsInstalled())
            {
                EditorApplication.CallDelayed(HideNavComponents);
                EditorApplication.CallDelayed(CloseNavigationWindow);
            }
        }

        static void CloseNavigationWindow()
        {
            if (EditorWindow.HasOpenInstances<NavMeshEditorWindow>())
                EditorWindow.GetWindow<NavMeshEditorWindow>().Close();
        }

        static void HideNavComponents()
        {
            // Look for existing navigation menus, if none then nothing to do
            var navMenuItems = Menu.GetMenuItems(k_NavigationComponentMenuRoot, false, false);
            if (navMenuItems != null && navMenuItems.Length > 0)
            {
                // Remove trunk registered components entries
                Menu.RemoveMenuItem($"{k_NavigationComponentMenuRoot}/Nav Mesh Agent");
                Menu.RemoveMenuItem($"{k_NavigationComponentMenuRoot}/Nav Mesh Obstacle");
                Menu.RemoveMenuItem($"{k_NavigationComponentMenuRoot}/Off Mesh Link");
            }

            // Register for the next menu modifications as we need to ensure components entries are not added again...
            Menu.menuChanged += OnMenuChanged;
        }

        static void OnMenuChanged()
        {
            // Unregister from the menu modifications callback as we don't want to be notified until we actually try to remove the components
            Menu.menuChanged -= OnMenuChanged;

            EditorApplication.CallDelayed(HideNavComponents);
        }

        internal static bool IsInstalled()
        {
            var packagePath = Path.GetFullPath(s_NavigationPackagePath);
            return !String.IsNullOrEmpty(packagePath) && File.Exists(packagePath);
        }
    }

    public static partial class NavMeshEditorHelpers
    {
        internal static event Action<int> agentTypeSettingsClicked;
        internal static event Action areaSettingsClicked;

        public static void OpenAgentSettings(int agentTypeID)
        {
            if (!CheckNavigationPackage.IsInstalled())
                Debug.LogWarning("Unable to open Agent settings because the Navigation window is not available. Please install the AI Navigation package to add that window.");

            if (agentTypeSettingsClicked != null)
                agentTypeSettingsClicked(agentTypeID);
        }

        public static void OpenAreaSettings()
        {
            if (!CheckNavigationPackage.IsInstalled())
                Debug.LogWarning("Unable to open Area settings because the Navigation window is not available. Please install the AI Navigation package to add that window.");

            if (areaSettingsClicked != null)
                areaSettingsClicked();
        }

        public static void DrawAgentDiagram(Rect rect, float agentRadius, float agentHeight, float agentClimb, float agentSlope)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            float cylinderRadius = agentRadius;
            float cylinderHeight = agentHeight;
            float stepHeight = agentClimb;
            float scale = 1.0f;
            float squash = 0.35f;
            float paddingTop = 20.0f;
            float paddingBottom = 10.0f;

            // Scale cylinder to fit to view.
            // Find nice scale factor to fit the cylinder inside the view.
            float viewSize = rect.height - (paddingTop + paddingBottom);
            scale = Mathf.Min(viewSize / (cylinderHeight + cylinderRadius * 2.0f * squash),
                viewSize / (cylinderRadius * 2.0f));

            cylinderHeight *= scale;
            cylinderRadius *= scale;
            stepHeight *= scale;
            stepHeight = Mathf.Min(stepHeight, viewSize - cylinderRadius * squash);

            // Position of the base of the cylinder.
            float baseX = rect.xMin + rect.width * 0.5f;
            float baseY = rect.yMax - paddingBottom - cylinderRadius * squash;

            const int kDivs = 20;
            Vector3[] cylinderOutline = new Vector3[kDivs * 2];
            Vector3[] topRim = new Vector3[kDivs];
            Vector3[] climbRim = new Vector3[kDivs];

            for (int i = 0; i < kDivs; i++)
            {
                float angle = (float)i / (float)(kDivs - 1) * Mathf.PI; // Half circle
                float dx = Mathf.Cos(angle);
                float dy = Mathf.Sin(angle);
                cylinderOutline[i] = new Vector3(baseX + dx * cylinderRadius, baseY - cylinderHeight - dy * cylinderRadius * squash, 0);
                cylinderOutline[i + kDivs] = new Vector3(baseX - dx * cylinderRadius, baseY + dy * cylinderRadius * squash, 0);

                topRim[i] = new Vector3(baseX - dx * cylinderRadius, baseY - cylinderHeight + dy * cylinderRadius * squash, 0);
                climbRim[i] = new Vector3(baseX - dx * cylinderRadius, baseY - stepHeight + dy * cylinderRadius * squash, 0);
            }

            Color oldColor = Handles.color;

            float startX = rect.xMin;
            float startY = baseY - stepHeight;
            float stepX = baseX - viewSize * 0.75f;
            float stepY = baseY;
            float slopeStartX = baseX + viewSize * 0.75f;
            float slopeStartY = baseY;
            float slopeEndX = slopeStartX;
            float slopeEndY = slopeStartY;
            float length = Mathf.Min(rect.xMax - slopeStartX, viewSize);
            slopeEndX += Mathf.Cos(agentSlope * Mathf.Deg2Rad) * length;
            slopeEndY -= Mathf.Sin(agentSlope * Mathf.Deg2Rad) * length;

            Vector3[] groundReference = new Vector3[2];
            groundReference[0] = new Vector3(startX, baseY, 0.0f);
            groundReference[1] = new Vector3(slopeStartX + length, baseY, 0.0f);

            Vector3[] ground = new Vector3[5];
            ground[0] = new Vector3(startX, startY, 0.0f);
            ground[1] = new Vector3(stepX, startY, 0.0f);
            ground[2] = new Vector3(stepX, stepY, 0.0f);
            ground[3] = new Vector3(slopeStartX, slopeStartY, 0.0f);
            ground[4] = new Vector3(slopeEndX, slopeEndY, 0.0f);

            // Draw reference line on ground, used for measures
            Handles.color = EditorGUIUtility.isProSkin ? new Color(0, 0, 0, 0.5f) : new Color(1, 1, 1, 0.5f);
            Handles.DrawAAPolyLine(2.0f, groundReference);

            // Draw ground
            Handles.color = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.5f) : new Color(0, 0, 0, 0.5f);
            Handles.DrawAAPolyLine(3.0f, ground);

            // Draw cylinder background
            Handles.color = Color.Lerp(new Color(0.0f, 0.75f, 1.0f, 1.0f), new Color(0.5f, 0.5f, 0.5f, 0.5f), 0.2f);
            Handles.DrawAAConvexPolygon(cylinderOutline);

            // Draw step height on cylinder
            if (agentClimb <= agentHeight)
            {
                Handles.color = new Color(0, 0, 0, 0.5f);
                Handles.DrawAAPolyLine(2.0f, climbRim);
            }

            // Draw cylinder outline
            Handles.color = new Color(1, 1, 1, 0.4f);
            Handles.DrawAAPolyLine(2.0f, topRim);

            // Draw line depicting the radius of the cylinder.
            Vector3[] radiusLine = new Vector3[2];
            radiusLine[0] = new Vector3(baseX, baseY - cylinderHeight, 0);
            radiusLine[1] = new Vector3(baseX + cylinderRadius, baseY - cylinderHeight, 0);
            Handles.color = new Color(0, 0, 0, 0.5f);
            Handles.DrawAAPolyLine(2.0f, radiusLine);

            // Labels
            GUI.Label(new Rect(baseX + cylinderRadius + 5, baseY - cylinderHeight * 0.5f - 10, 150, 20), UnityString.Format("H = {0}", agentHeight));
            GUI.Label(new Rect(baseX, baseY - cylinderHeight - cylinderRadius * squash - 15, 150, 20), UnityString.Format("R = {0}", agentRadius));

            GUI.Label(new Rect((startX + stepX) * 0.5f - 20, startY - 15, 150, 20), UnityString.Format("{0}", agentClimb));
            GUI.Label(new Rect(slopeStartX + 20, slopeStartY - 15, 150, 20), UnityString.Format("{0}\u00b0", agentSlope));

            Handles.color = oldColor;
        }
    }
}
