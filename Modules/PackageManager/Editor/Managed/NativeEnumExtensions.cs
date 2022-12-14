// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager
{
    internal static class NativeEnumExtensions
    {
        public static StatusCode ConvertToManaged(this NativeStatusCode status)
        {
            switch (status)
            {
                case NativeStatusCode.InProgress:
                case NativeStatusCode.InQueue:
                    return StatusCode.InProgress;
                case NativeStatusCode.Error:
                case NativeStatusCode.NotFound:
                case NativeStatusCode.Cancelled:
                    return StatusCode.Failure;
                case NativeStatusCode.Done:
                    return StatusCode.Success;
            }

            throw new NotSupportedException(string.Format("Unknown native status code {0}", status));
        }

        public static ErrorCode ConvertToManaged(this NativeErrorCode errorCode)
        {
            switch (errorCode)
            {
                case NativeErrorCode.Unknown:
                case NativeErrorCode.Cancelled:
                case NativeErrorCode.Success:
                    return ErrorCode.Unknown;
                case NativeErrorCode.NotFound:
                    return ErrorCode.NotFound;
                case NativeErrorCode.Forbidden:
                    return ErrorCode.Forbidden;
                case NativeErrorCode.InvalidParameter:
                    return ErrorCode.InvalidParameter;
                case NativeErrorCode.Conflict:
                    return ErrorCode.Conflict;
                case NativeErrorCode.AggregateError:
                    return ErrorCode.AggregateError;
            }

            throw new NotSupportedException(string.Format("Unknown native error code {0}",  errorCode));
        }

        public static bool IsCompleted(this NativeStatusCode status)
        {
            return (ConvertToManaged(status) != StatusCode.InProgress);
        }
    }
}
