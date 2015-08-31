// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IrisCompiler.Import;
using System;
using System.Collections.Generic;

namespace IrisCompiler
{
    /// <summary>
    /// This class is the symbol table for the Iris compiler.
    /// The symbol table is rather simple because we only have global and local scopes.
    /// </summary>
    public class SymbolTable
    {
        private Dictionary<string, Symbol> _global = new Dictionary<string, Symbol>(StringComparer.InvariantCultureIgnoreCase);
        private Dictionary<string, Symbol> _local; // No local scope until we start parsing a method
        private int _nextGlobalVariable;
        private int _nextGlobalMethod;
        private int _nextLocal;
        private int _nextArgument;

        public SymbolTable()
        {
        }

        /// <summary>
        /// Used to help debugging
        /// </summary>
        public IEnumerable<Symbol> Local
        {
            get
            {
                return _local.Values;
            }
        }

        /// <summary>
        /// Used to help debugging
        /// </summary>
        public IEnumerable<Symbol> Global
        {
            get
            {
                return _global.Values;
            }
        }

        public Symbol Add(string name, IrisType type, StorageClass storage, int location, ImportedMember importInfo = null)
        {
            Symbol symbol = new Symbol(name, type, storage, location, importInfo);
            if (storage == StorageClass.Global)
                _global.Add(name, symbol);
            else
                _local.Add(name, symbol);

            return symbol;
        }

        public Symbol Add(string name, IrisType type, StorageClass storage, ImportedMember importInfo = null)
        {
            int location = GetNextLocation(storage, type);
            return Add(name, type, storage, location, importInfo);
        }

        public Symbol CreateUndefinedSymbol(string name)
        {
            // We want to add entries to the symbol table for undefined symbols we encounter.
            // This prevents us from spewing many duplicate errors for the same symbol being
            // undefined.
            Symbol symbol;
            if (_local != null)
            {
                symbol = new Symbol(name, IrisType.Invalid, StorageClass.Local, -1, null);
                _local.Add(name, symbol);
            }
            else
            {
                symbol = new Symbol(name, IrisType.Invalid, StorageClass.Global, -1, null);
                _global.Add(name, symbol);
            }

            return symbol;
        }

        public Symbol OpenMethod(string name, IrisType type)
        {
            Symbol methodSymbol = Add(name, type, StorageClass.Global); // Add the method itself to the symbol table

            // Create the local scope
            _local = new Dictionary<string, Symbol>(StringComparer.InvariantCultureIgnoreCase);

            return methodSymbol;
        }

        public void CloseMethod()
        {
            _local = null;
            _nextArgument = 0;
            _nextLocal = 0;
        }

        public Symbol LookupLocal(string name)
        {
            Symbol sym;
            if (_local != null && _local.TryGetValue(name, out sym))
                return sym;

            return null; // Symbol not found
        }

        public Symbol LookupGlobal(string name)
        {
            Symbol sym;
            if (_global.TryGetValue(name, out sym))
                return sym;

            return null; // Symbol not found
        }

        public Symbol Lookup(string name)
        {
            Symbol sym;
            if (_local != null && _local.TryGetValue(name, out sym))
            {
                return sym;
            }

            if (_global.TryGetValue(name, out sym))
            {
                return sym;
            }

            return null; // Symbol not found
        }

        public int LookupMethodIndex(string name)
        {
            return _global[name].Location;
        }

        private int GetNextLocation(StorageClass storage, IrisType type)
        {
            if (storage == StorageClass.Global)
            {
                if (type.IsMethod)
                    return _nextGlobalMethod++;
                else
                    return _nextGlobalVariable++;
            }
            else if (storage == StorageClass.Argument)
            {
                return _nextArgument++;
            }
            else
            {
                return _nextLocal++;
            }
        }
    }
}
