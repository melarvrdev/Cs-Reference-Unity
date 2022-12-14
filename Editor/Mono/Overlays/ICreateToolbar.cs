// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.Overlays
{
    public interface ICreateHorizontalToolbar
    {
        OverlayToolbar CreateHorizontalToolbarContent();
    }

    public interface ICreateVerticalToolbar
    {
        OverlayToolbar CreateVerticalToolbarContent();
    }

    public interface ICreateToolbar
    {
        public IEnumerable<string> toolbarElements { get; }
    }
}
