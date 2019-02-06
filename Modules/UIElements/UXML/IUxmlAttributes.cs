// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    public interface IUxmlAttributes
    {
        bool TryGetAttributeValue(string attributeName, out string value);
    }
}
