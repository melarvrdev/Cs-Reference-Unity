// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEngine.Bindings;
using UnityEngine.Experimental;

using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEditor.Experimental.XR
{
    [NativeType(Header = "Modules/XREditor/Build/XRBuildSystem.h")]
    [StaticAccessor("XRBuildSystem", StaticAccessorType.DoubleColon)]
    internal class BuildUtilities
    {
        internal static bool IsLibraryRegisteredWithXR(string libraryName)
        {
            return Internal_IsLibraryRegisteredWithXR(libraryName);
        }

        internal static bool HasRegisteredPlugins()
        {
            return Internal_HasRegisteredPlugins();
        }

        internal static bool ShouldDisableLegacyVR()
        {
            List<ISubsystemDescriptor> descriptors = new List<ISubsystemDescriptor>();
            SubsystemManager.GetAllSubsystemDescriptors(descriptors);

            foreach (var descriptor in descriptors)
            {
                if (descriptor.disablesLegacyVr)
                    return true;
            }
            return false;
        }

        extern internal static bool Internal_IsLibraryRegisteredWithXR(string libraryName);

        extern internal static bool Internal_HasRegisteredPlugins();
    }
}
