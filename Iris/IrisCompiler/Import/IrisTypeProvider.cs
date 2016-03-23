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

        IrisType ITypeProvider<IrisType>.GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, SignatureTypeHandleCode code)
        {
            // Iris doesn't define any types that can be referenced.
            return IrisType.Invalid;
        }

        IrisType ITypeProvider<IrisType>.GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, SignatureTypeHandleCode code)
        {
            // We shouldn't be referencing any non-primitive types.
            return IrisType.Invalid;
        }

        IrisType ITypeProvider<IrisType>.GetTypeFromSpecification(MetadataReader reader, TypeSpecificationHandle handle, SignatureTypeHandleCode code)
        {
            TypeSpecification typeSpec = _reader.GetTypeSpecification(handle);
            return typeSpec.DecodeSignature(this);
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

        IrisType ISignatureTypeProvider<IrisType>.GetModifiedType(MetadataReader reader, bool isRequired, IrisType modifier, IrisType unmodifiedType)
        {
            return unmodifiedType;
        }

        IrisType ISignatureTypeProvider<IrisType>.GetPinnedType(IrisType elementType)
        {
            return IrisType.Invalid;
        }

        IrisType IConstructedTypeProvider<IrisType>.GetPointerType(IrisType elementType)
        {
            return IrisType.Invalid;
        }

        IrisType IConstructedTypeProvider<IrisType>.GetGenericInstance(IrisType genericType, ImmutableArray<IrisType> typeArguments)
        {
            return IrisType.Invalid;
        }

        IrisType IConstructedTypeProvider<IrisType>.GetArrayType(IrisType elementType, ArrayShape shape)
        {
            // In the world of .NET metadata, we only support SZArray.
            return IrisType.Invalid;
        }

        IrisType IConstructedTypeProvider<IrisType>.GetByReferenceType(IrisType elementType)
        {
            return elementType.MakeByRefType();
        }

        IrisType ISZArrayTypeProvider<IrisType>.GetSZArrayType(IrisType elementType)
        {
            return elementType.MakeArrayType();
        }

        IrisType IPrimitiveTypeProvider<IrisType>.GetPrimitiveType(PrimitiveTypeCode typeCode)
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

        #endregion
    }
}
