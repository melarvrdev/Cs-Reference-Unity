// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.AI
{
    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    [Flags]
    public enum NavMeshBuildDebugFlags
    {
        None = 0,
        InputGeometry = 1 << 0,
        Voxels = 1 << 1,
        Regions = 1 << 2,
        RawContours = 1 << 3,
        SimplifiedContours = 1 << 4,
        PolygonMeshes = 1 << 5,
        PolygonMeshesDetail = 1 << 6,
        All = unchecked((int)(~(~0U << 7)))
    }

    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    public enum NavMeshBuildSourceShape
    {
        Mesh = 0,
        Terrain = 1,
        Box = 2,
        Sphere = 3,
        Capsule = 4,
        ModifierBox = 5
    }

    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    public enum NavMeshCollectGeometry
    {
        RenderMeshes = 0,
        PhysicsColliders = 1
    }

    // Struct containing source geometry data and annotation for runtime navmesh building
    [UsedByNativeCode]
    [NativeHeader("Modules/AI/Public/NavMeshBindingTypes.h")]
    public struct NavMeshBuildSource
    {
        public Matrix4x4 transform { get { return m_Transform; } set { m_Transform = value; } }
        public Vector3 size { get { return m_Size; } set { m_Size = value; } }
        public NavMeshBuildSourceShape shape { get { return m_Shape; } set { m_Shape = value; } }
        public int area { get { return m_Area; } set { m_Area = value; } }
        public bool generateLinks { get { return m_GenerateLinks != 0; } set { m_GenerateLinks = value ? 1 : 0; } }
        public Object sourceObject { get { return InternalGetObject(m_InstanceID); } set { m_InstanceID = value != null ? value.GetInstanceID() : 0; } }
        public Component component { get { return InternalGetComponent(m_ComponentID); } set { m_ComponentID = value != null ? value.GetInstanceID() : 0; } }

        Matrix4x4 m_Transform;
        Vector3 m_Size;
        NavMeshBuildSourceShape m_Shape;
        int m_Area;
        int m_InstanceID;
        int m_ComponentID;
        int m_GenerateLinks;

        [StaticAccessor("NavMeshBuildSource", StaticAccessorType.DoubleColon)]
        static extern Component InternalGetComponent(int instanceID);

        [StaticAccessor("NavMeshBuildSource", StaticAccessorType.DoubleColon)]
        static extern Object InternalGetObject(int instanceID);
    }

    // Struct containing source geometry data and annotation for runtime navmesh building
    [NativeHeader("Modules/AI/Public/NavMeshBindingTypes.h")]
    public struct NavMeshBuildMarkup
    {
        public bool overrideArea { get { return m_OverrideArea != 0; } set { m_OverrideArea = value ? 1 : 0; } }
        public int area { get { return m_Area; } set { m_Area = value; } }
        public bool overrideIgnore { get { return m_InheritIgnoreFromBuild == 0; } set { m_InheritIgnoreFromBuild = value ? 0: 1; } }
        public bool ignoreFromBuild { get { return m_IgnoreFromBuild != 0; } set { m_IgnoreFromBuild = value ? 1 : 0; } }
        public bool overrideGenerateLinks { get { return m_OverrideGenerateLinks != 0; } set { m_OverrideGenerateLinks = value ? 1 : 0; } }
        public bool generateLinks { get { return m_GenerateLinks != 0; } set { m_GenerateLinks = value ? 1 : 0; } }
        public bool applyToChildren { get { return m_IgnoreChildren == 0; } set { m_IgnoreChildren = value ? 0 : 1; } }
        public Transform root { get { return InternalGetRootGO(m_InstanceID); } set { m_InstanceID = value != null ? value.GetInstanceID() : 0; } }

        int m_OverrideArea;
        int m_Area;
        int m_InheritIgnoreFromBuild; // backing field is reversed for the default value to align with the legacy default behaviour
        int m_IgnoreFromBuild;
        int m_OverrideGenerateLinks;
        int m_GenerateLinks;
        int m_InstanceID;
        int m_IgnoreChildren; // backing field is reversed for the default value to align with the legacy default behaviour

        [StaticAccessor("NavMeshBuildMarkup", StaticAccessorType.DoubleColon)]
        static extern Transform InternalGetRootGO(int instanceID);
    }
}
