// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    internal interface IPackageActionDropdownItem : IExtension
    {
        bool isChecked { set; get; }
        string text { set; get; }
        bool insertSeparatorBefore { set; get; }
        Action<PackageSelectionArgs> action { set; get; }
    }
}
