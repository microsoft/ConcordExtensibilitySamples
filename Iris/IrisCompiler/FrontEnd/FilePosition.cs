// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace IrisCompiler.FrontEnd
{
    /// <summary>
    /// Struct representing a source location as file line/column number.
    /// The line and column numbers are 1-based.
    /// </summary>
    public struct FilePosition
    {
        /// <summary>
        /// Initializes a new instance of the FilePosition struct.
        /// </summary>
        /// <param name="line">The 1-based line number of the position</param>
        /// <param name="column">The 1-based column number of the position</param>
        public FilePosition(int line, int column)
        {
            Line = line;
            Column = column;
        }

        /// <summary>
        /// Gets the position representing the beginning of the file.
        /// </summary>
        public static FilePosition Begin
        {
            get
            {
                return new FilePosition(1, 1);
            }
        }

        /// <summary>
        /// Gets the 1-based line number of the position.
        /// </summary>
        public int Line
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the 1-based column number of the position.
        /// </summary>
        public int Column
        {
            get;
            private set;
        }

        /// <summary>
        /// Expand the file position forward by a given number of columns (or characters) and
        /// return the resulting source range.
        /// </summary>
        /// <param name="columns">Number of columns</param>
        /// <returns>Source range of expanded position</returns>
        public SourceRange Expand(int columns)
        {
            FilePosition end = new FilePosition(Line, Column + columns);
            return new SourceRange(this, end);
        }
    }
}
