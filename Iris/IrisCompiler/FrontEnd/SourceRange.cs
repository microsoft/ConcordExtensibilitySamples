// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace IrisCompiler.FrontEnd
{
    /// <summary>
    /// Respresents a contiguous range of source code.
    /// </summary>
    public class SourceRange
    {
        public readonly FilePosition Start;
        public readonly FilePosition End;

        public SourceRange(FilePosition start, FilePosition end)
        {
            Start = start;
            End = end;
        }
    }
}
