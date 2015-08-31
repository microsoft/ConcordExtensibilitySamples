// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IrisCompiler.Import;

namespace IrisCompiler
{
    /// <summary>
    /// Represents a 'symbol' in the Iris compiler.  At a minimum, a symbol consists of a name,
    /// type, and information about where the value is stored.
    /// Imported symbols also contain import information.
    /// </summary>
    public class Symbol
    {
        public readonly string Name;
        public readonly IrisType Type;
        public readonly StorageClass StorageClass;
        public readonly int Location;
        public readonly ImportedMember ImportInfo;

        internal Symbol(string name, IrisType type, StorageClass storage, int location, ImportedMember importInfo)
        {
            Name = name;
            Type = type;
            StorageClass = storage;
            Location = location;
            ImportInfo = importInfo;
        }

        public override string ToString()
        {
            return string.Format("{0} : {1} ({2} {3})", Name, Type, StorageClass, Location);
        }
    }

    public enum StorageClass
    {
        Global,
        Local,
        Argument,
    }
}
