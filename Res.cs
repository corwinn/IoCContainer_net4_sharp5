/**** BEGIN LICENSE BLOCK ****

BSD 3-Clause License

Copyright (c) 2022, the wind.
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

**** END LICENCE BLOCK ****/

namespace IoCContainer_net4_sharp5
{
    /// <summary>Resources.</summary>
    internal static class Res
    {
        private static string FMT_ONE(string fmt, string arg0) { return string.Format (fmt, arg0); }
        private static string FMT_TWO(string fmt, string arg0, string arg1) { return string.Format (fmt, arg0, arg1); }

        private static readonly string INFINITE_LOOP_PROBABLY = "Fixme: Infinite loop prevention. Either: redesign or increase {0}";
        public static string FMT_INFINITE_LOOP_PROBABLY(string upper_limit_name)
        {
            return FMT_ONE (INFINITE_LOOP_PROBABLY, upper_limit_name);
        }
        public static readonly string NO_NULLS = "No nulls allowed.";
        private static readonly string UPPER_LIMIT_CONSIDERATION =
            "You should either: reconsider the number of {0}, or modify {1}";
        public static string FMT_UPPER_LIMIT_CONSIDERATION(string what, string upper_limit_name)
        {
            return FMT_TWO (UPPER_LIMIT_CONSIDERATION, what, upper_limit_name);
        }
        public static readonly string BINDING_INCOMPLETE = "Incomplete binding - can't help you.";
        public static readonly string BINDING_DUPLICATE = "Replacing an implementations is off limits.";
        public static readonly string BINDING_UNKNOWN = "Unknown binding.";
        public static readonly string MEANINGFUL_BUILDER = "Please set ServiceCollection.Builder to something meaningful.";
        public static readonly string ONE_CALLME_CONSTRUCTOR = "Please mark exactly one public constructor with the CallMeAttribute.";
        private static readonly string NO_CONSTRUCTORS = "No constructors found for \"{0}: {1}\".";
        public static string FMT_NO_CONSTRUCTORS(string type1, string type2)
        {
            return FMT_TWO (NO_CONSTRUCTORS, type1, type2);
        }
        private static readonly string NO_SUITABLE_CONSTRUCTOR = "No suitable constructor found for service \"{0}\"";
        public static string FMT_NO_SUITABLE_CONSTRUCTOR(string service)
        {
            return FMT_ONE (NO_SUITABLE_CONSTRUCTOR, service);
        }
        private static readonly string AMBIGUOUS_CONSTRCUTORS = "Ambiguous constructors found for service \"{0}\"";
        public static string FMT_AMBIGUOUS_CONSTRCUTORS(string service)
        {
            return FMT_ONE (AMBIGUOUS_CONSTRCUTORS, service);
        }
        private static readonly string SERVICE_UNKNOWN = "Unknown service: \"{0}\"";
        public static string FMT_SERVICE_UNKNOWN(string service)
        {
            return FMT_ONE (SERVICE_UNKNOWN, service);
        }
    }// internal static class Res
}
