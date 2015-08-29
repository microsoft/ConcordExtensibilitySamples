// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace IrisCompiler.FrontEnd
{
    /// <summary>
    /// This class contains a list of errors generated during compilation
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public class ErrorList
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private List<Error> _errors = new List<Error>();

        public int Count
        {
            get
            {
                return _errors.Count;
            }
        }

        public IEnumerable<Error> List
        {
            get
            {
                return _errors;
            }
        }

        internal void Add(int line, int column, string error)
        {
            FilePosition fp = new FilePosition(line, column);
            Add(fp, error);
        }

        internal void Add(FilePosition fp, string error)
        {
            _errors.Add(new Error(fp, error));
        }
    }

    /// <summary>
    /// Represents a compile error.  Contains a source position and error message.
    /// </summary>
    public class Error
    {
        public readonly FilePosition Position;
        public readonly string Text;

        public Error(FilePosition fp, string error)
        {
            Position = fp;
            Text = error;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0}, {1}) {2}", Position.Line, Position.Column, Text);
        }
    }
}
