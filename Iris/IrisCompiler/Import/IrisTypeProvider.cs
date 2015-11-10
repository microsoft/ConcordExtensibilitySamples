// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Decoding;

namespace IrisCompiler.Import
{
    internal class IrisTypeProvider : ISignatureTypeProvider<IrisType>
    {
        private MetadataReader _reader;

        public IrisTypeProvider(MetadataReader reader)
        {
            _reader = reader;
        }

        #region ITypeProvider<IrisType> implementation

        public MetadataReader Reader
        {
            get
            {
                return _reader;
            }
        }

        IrisType ITypeProvider<IrisType>.GetArrayType(IrisType elementType, ArrayShape shape)
        {
            if (shape.Rank != 1)
                return IrisType.Invalid;

            return elementType.MakeArrayType();
        }

        IrisType ITypeProvider<IrisType>.GetByReferenceType(IrisType elementType)
        {
            return elementType.MakeByRefType();
        }

        IrisType ITypeProvider<IrisType>.GetGenericInstance(IrisType genericType, ImmutableArray<IrisType> typeArguments)
        {
            return IrisType.Invalid;
        }

        IrisType ITypeProvider<IrisType>.GetSZArrayType(IrisType elementType)
        {
            return elementType.MakeArrayType();
        }

        #endregion

        #region ISignatureTypeProvider<IrisType> implementation

        IrisType ISignatureTypeProvider<IrisType>.GetFunctionPointerType(MethodSignature<IrisType> signature)
        {
            return IrisType.Invalid;
        }

        IrisType ISignatureTypeProvider<IrisType>.GetGenericMethodParameter(int index)
        {
            return IrisType.Invalid;
        }

        IrisType ISignatureTypeProvider<IrisType>.GetGenericTypeParameter(int index)
        {
            return IrisType.Invalid;
        }

        IrisType ISignatureTypeProvider<IrisType>.GetModifiedType(IrisType unmodifiedType, ImmutableArray<CustomModifier<IrisType>> customModifiers)
        {
            return unmodifiedType;
        }

        IrisType ISignatureTypeProvider<IrisType>.GetPinnedType(IrisType elementType)
        {
            return IrisType.Invalid;
        }

        IrisType ITypeProvider<IrisType>.GetPointerType(IrisType elementType)
        {
            return IrisType.Invalid;
        }

        IrisType ISignatureTypeProvider<IrisType>.GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            switch (typeCode)
            {
                case PrimitiveTypeCode.Boolean:
                    return IrisType.Boolean;
                case PrimitiveTypeCode.Int32:
                    return IrisType.Integer;
                case PrimitiveTypeCode.String:
                    return IrisType.String;
                case PrimitiveTypeCode.Void:
                    return IrisType.Void;
                default:
                    return IrisType.Invalid;
            }
        }

        IrisType ISignatureTypeProvider<IrisType>.GetTypeFromDefinition(TypeDefinitionHandle handle, bool? isValueType)
        {
            // Iris doesn't define any types that can be referenced.
            return IrisType.Invalid;
        }

        IrisType ISignatureTypeProvider<IrisType>.GetTypeFromReference(TypeReferenceHandle handle, bool? isValueType)
        {
            // We shouldn't be referencing any non-primitive types.
            return IrisType.Invalid;
        }

        #endregion
    }
}
