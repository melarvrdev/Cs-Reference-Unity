// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text.RegularExpressions;

namespace UnityEditor.Scripting.Compilers
{
    internal class MicrosoftCSharpCompilerOutputParser : CompilerOutputParserBase
    {
        public static Regex sCompilerOutput = new Regex(@"(?<filename>.+)(\((?<line>\d+),(?<column>\d+)\)):\s*(?<type>warning|error|info)\s*(?<id>[^:]*):\s*(?<message>.*)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        protected override string GetInformationIdentifier()
        {
            return "info";
        }

        protected override Regex GetOutputRegex()
        {
            return sCompilerOutput;
        }

        protected override string GetErrorIdentifier()
        {
            return "error";
        }
    }
}
