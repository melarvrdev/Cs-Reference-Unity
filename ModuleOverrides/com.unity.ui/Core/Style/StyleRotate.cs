// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents a style value that can be either a <see cref="Rotate"/> or a <see cref="StyleKeyword"/>.
    /// </summary>
    public struct StyleRotate : IStyleValue<Rotate>, IEquatable<StyleRotate>
    {
        /// <summary>
        /// The <see cref="Rotate"/> value.
        /// </summary>
        public Rotate value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : default(Rotate); }
            set
            {
                m_Value = value;
                m_Keyword = StyleKeyword.Undefined;
            }
        }

        /// <summary>
        /// The style keyword.
        /// </summary>
        public StyleKeyword keyword
        {
            get { return m_Keyword; }
            set { m_Keyword = value; }
        }

        /// <summary>
        /// Creates a StyleRotate from either a <see cref="Rotate"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleRotate(Rotate v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates a StyleRotate from either a <see cref="Rotate"/> or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleRotate(StyleKeyword keyword)
            : this(default(Rotate), keyword)
        {}

        internal StyleRotate(Rotate v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private Rotate m_Value;
        private StyleKeyword m_Keyword;

        /// <undoc/>
        public static bool operator==(StyleRotate lhs, StyleRotate rhs)
        {
            return lhs.m_Keyword == rhs.m_Keyword && lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(StyleRotate lhs, StyleRotate rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleRotate(StyleKeyword keyword)
        {
            return new StyleRotate(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleRotate(Rotate v)
        {
            return new StyleRotate(v);
        }

        /// <undoc/>
        public bool Equals(StyleRotate other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleRotate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_Value.GetHashCode() * 397) ^ (int)m_Keyword;
            }
        }

        public override string ToString()
        {
            return this.DebugString();
        }
    }
}
