// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Search
{
    /// <summary>
    /// A QueryError holds the definition of a query parsing error.
    /// </summary>
    public class QueryError
    {
        /// <summary> Index where the error happened. </summary>
        public int index { get; set; }

        /// <summary> Length of the block that was being parsed. </summary>
        public int length { get; set; }

        /// <summary> Reason why the parsing failed. </summary>
        public string reason { get; set; }

        /// <summary> The error type. </summary>
        internal SearchQueryErrorType type { get; set; }

        /// <summary>
        /// Construct a new QueryError with a default length of 0.
        /// </summary>
        public QueryError()
        {
            index = 0;
            reason = "";
            length = 0;
            type = SearchQueryErrorType.Error;
        }

        /// <summary>
        /// Construct a new QueryError with a default length of 1.
        /// </summary>
        /// <param name="index">Index where the error happened.</param>
        /// <param name="reason">Reason why the parsing failed.</param>
        public QueryError(int index, string reason)
            : this(index, 1, reason)
        {}

        /// <summary>
        /// Construct a new QueryError.
        /// </summary>
        /// <param name="index">Index where the error happened.</param>
        /// <param name="length">Length of the block that was being parsed.</param>
        /// <param name="reason">Reason why the parsing failed.</param>
        public QueryError(int index, int length, string reason)
            : this(index, length, reason, SearchQueryErrorType.Error)
        {}

        internal QueryError(int index, string reason, SearchQueryErrorType type)
            : this(index, 1, reason, type)
        {}

        internal QueryError(int index, int length, string reason, SearchQueryErrorType type)
        {
            this.index = index;
            this.reason = reason;
            this.length = length;
            this.type = type;
        }
    }
}
