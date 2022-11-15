// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#include "stdafx.h"
#include "ChildVisualizer.h"

HRESULT CChildVisualizer::Initialize(
    _In_ DkmVisualizedExpression* pVisualizedExpression,
    _In_ size_t vectorSize,
    _In_ size_t parentIndex
)
{
    m_pVisualizedExpression = pVisualizedExpression;
    m_vectorSize = vectorSize;
    m_parentIndex = parentIndex;
    return S_OK;
}

HRESULT CChildVisualizer::CreateEvaluationResult(
    _In_ DkmString* pName,
    _In_ DkmString* pFullName,
    _In_ DkmString* pType,
    _In_ DkmRootVisualizedExpressionFlags_t flags,
    _In_opt_ DkmVisualizedExpression* pParent,
    _In_ DkmInspectionContext* pInspectionContext,
    _In_ size_t index,
    _Deref_out_ DkmEvaluationResult** ppResultObject
)
{
    HRESULT hr = S_OK;

    CComPtr<DkmPointerValueHome> pPointerValueHome = DkmPointerValueHome::TryCast(m_pVisualizedExpression->ValueHome());
    if (pPointerValueHome == nullptr)
    {
        // This sample only handles visualizing in-memory Sample structures
        return E_NOTIMPL;
    }

    // Create method for DkmDataAddress takes a runtime instance.
    CComPtr<DkmDataAddress> pAddress;
    hr = DkmDataAddress::Create(m_pVisualizedExpression->InspectionContext()->RuntimeInstance(), pPointerValueHome->Address(), NULL, &pAddress);
    if (FAILED(hr))
    {
        return hr;
    }

    CString strValue;
    strValue.Format(L"%i", index);
    if (FAILED(hr))
    {
        strValue = "<Invalid Value>";
    }

    CComPtr<DkmString> pValue;
    hr = DkmString::Create(DkmSourceString(strValue), &pValue);
    if (FAILED(hr))
    {
        return hr;
    }

    CComPtr<DkmSuccessEvaluationResult> pSuccessEvaluationResult;
    hr = DkmSuccessEvaluationResult::Create(
        m_pVisualizedExpression->InspectionContext(),
        m_pVisualizedExpression->StackFrame(),
        pName,
        pFullName,
        DkmEvaluationResultFlags::Expandable | DkmEvaluationResultFlags::ReadOnly,
        pValue,
        pValue,
        pType,
        DkmEvaluationResultCategory::Class,
        DkmEvaluationResultAccessType::None,
        DkmEvaluationResultStorageType::None,
        DkmEvaluationResultTypeModifierFlags::None,
        nullptr,
        nullptr,
        (DkmReadOnlyCollection<DkmModuleInstance*>*)nullptr,
        DkmDataItem::Null(),
        &pSuccessEvaluationResult
    );
    if (FAILED(hr))
    {
        return hr;
    }

    *ppResultObject = (DkmEvaluationResult*)pSuccessEvaluationResult.Detach();

    return hr;
}

HRESULT CChildVisualizer::GetChildren(
    _In_ UINT32 InitialRequestSize,
    _In_ DkmInspectionContext* pInspectionContext,
    _Out_ DkmArray<DkmChildVisualizedExpression*>* pInitialChildren,
    _Deref_out_ DkmEvaluationResultEnumContext** ppEnumContext
)
{
    HRESULT hr = S_OK;

    // A and B
    UINT32 childCount = 2;

    CComPtr<DkmEvaluationResultEnumContext> pEnumContext;
    hr = DkmEvaluationResultEnumContext::Create(
        childCount,
        m_pVisualizedExpression->StackFrame(),
        pInspectionContext,
        this,
        &pEnumContext);
    if (FAILED(hr))
    {
        return hr;
    }

    if (InitialRequestSize > 0)
    {
        GetItems(m_pVisualizedExpression, pEnumContext, 0, childCount, pInitialChildren);
    }
    else
    {
        DkmAllocArray(0, pInitialChildren);
    }

    *ppEnumContext = pEnumContext.Detach();

    return hr;
}

HRESULT CChildVisualizer::GetItems(
    _In_ DkmVisualizedExpression* pVisualizedExpression,
    _In_ DkmEvaluationResultEnumContext* pEnumContext,
    _In_ UINT32 StartIndex,
    _In_ UINT32 Count,
    _Out_ DkmArray<DkmChildVisualizedExpression*>* pItems
)
{
    HRESULT hr = S_OK;

    if (StartIndex != 0 && Count != 2)
    {
        return E_INVALIDARG;
    }

    CComPtr<DkmString> pType;
    hr = DkmString::Create(DkmSourceString(L"int"), &pType);
    if (FAILED(hr))
    {
        return hr;
    }

    CAutoDkmArray<DkmChildVisualizedExpression*> resultValues;
    hr = DkmAllocArray(Count, &resultValues);
    if (FAILED(hr))
    {
        return hr;
    }

    CComPtr<DkmRootVisualizedExpression> pRootVisualizedExpression = DkmRootVisualizedExpression::TryCast(pVisualizedExpression);
    CComPtr<DkmString> pFullName;
    if (pRootVisualizedExpression == nullptr)
    {
        hr = pVisualizedExpression->CreateDefaultChildFullName(0, &pFullName);
        if (FAILED(hr))
        {
            return hr;
        }
    }
    else
    {
        pFullName = pRootVisualizedExpression->FullName();
    }

    CString evalTextA;
    evalTextA.Format(L"%s.a[%i]", pFullName->Value(), m_parentIndex);
    CComPtr<DkmString> pEvalTextA;
    hr = DkmString::Create(DkmSourceString(evalTextA), &pEvalTextA);
    if (FAILED(hr))
    {
        return hr;
    }

    CComPtr<DkmString> pDisplayNameA;
    hr = DkmString::Create(DkmSourceString(L"A"), &pDisplayNameA);
    if (FAILED(hr))
    {
        return hr;
    }

    CComPtr<DkmChildVisualizedExpression> pChildVisualizedExpressionA;
    hr = CreateItemVisualizedExpression(
        pEvalTextA,
        pDisplayNameA,
        pType,
        0,
        &pChildVisualizedExpressionA
    );
    if (FAILED(hr))
    {
        return hr;
    }

    CString evalTextB;
    evalTextB.Format(L"%s.b[%i]", pFullName->Value(), m_parentIndex);
    CComPtr<DkmString> pEvalTextB;
    hr = DkmString::Create(DkmSourceString(evalTextB), &pEvalTextB);
    if (FAILED(hr))
    {
        return hr;
    }

    CComPtr<DkmString> pDisplayNameB;
    hr = DkmString::Create(DkmSourceString(L"B"), &pDisplayNameB);
    if (FAILED(hr))
    {
        return hr;
    }

    CComPtr<DkmChildVisualizedExpression> pChildVisualizedExpressionB;
    hr = CreateItemVisualizedExpression(
        pEvalTextB,
        pDisplayNameB,
        pType,
        1,
        &pChildVisualizedExpressionB
    );
    if (FAILED(hr))
    {
        return hr;
    }

    resultValues.Members[0] = pChildVisualizedExpressionA.Detach();
    resultValues.Members[1] = pChildVisualizedExpressionB.Detach();

    *pItems = resultValues.Detach();

    return hr;
}

HRESULT CChildVisualizer::CreateItemVisualizedExpression(
    _In_ DkmString* pEvalText,
    _In_ DkmString* pDisplayName,
    _In_ DkmString* pType,
    _In_ size_t index,
    _Deref_out_ DkmChildVisualizedExpression** ppResult
)
{
    HRESULT hr = S_OK;

    CComPtr<DkmLanguageExpression> pLanguageExpression;
    hr = DkmLanguageExpression::Create(
        m_pVisualizedExpression->InspectionContext()->Language(),
        DkmEvaluationFlags::TreatAsExpression,
        pEvalText,
        DkmDataItem::Null(),
        &pLanguageExpression
    );
    if (FAILED(hr))
    {
        return hr;
    }
    CComPtr<DkmEvaluationResult> pEvalResult;
    hr = m_pVisualizedExpression->EvaluateExpressionCallback(
        m_pVisualizedExpression->InspectionContext(),
        pLanguageExpression,
        m_pVisualizedExpression->StackFrame(),
        &pEvalResult
    );
    if (FAILED(hr))
    {
        return hr;
    }

    CComPtr<DkmSuccessEvaluationResult> pSuccessEvalResult = DkmSuccessEvaluationResult::TryCast(pEvalResult);
    if (pSuccessEvalResult == nullptr)
    {
        return E_FAIL;
    }

    CComPtr<DkmSuccessEvaluationResult> pEvalResultNewName;
    hr = DkmSuccessEvaluationResult::Create(
        m_pVisualizedExpression->InspectionContext(),
        m_pVisualizedExpression->StackFrame(),
        pDisplayName,
        pSuccessEvalResult->FullName(),
        pSuccessEvalResult->Flags() | DkmEvaluationResultFlags::ReadOnly,
        pSuccessEvalResult->Value(),
        pSuccessEvalResult->EditableValue(),
        pSuccessEvalResult->Type(),
        pSuccessEvalResult->Category(),
        pSuccessEvalResult->Access(),
        pSuccessEvalResult->StorageType(),
        pSuccessEvalResult->TypeModifierFlags(),
        pSuccessEvalResult->Address(),
        pSuccessEvalResult->CustomUIVisualizers(),
        pSuccessEvalResult->ExternalModules(),
        DkmDataItem::Null(),
        &pEvalResultNewName
    );
    if (FAILED(hr))
    {
        return hr;
    }

    CComPtr<DkmChildVisualizedExpression> pChildVisualizedExpression;
    hr = DkmChildVisualizedExpression::Create(
        m_pVisualizedExpression->InspectionContext(),
        m_pVisualizedExpression->VisualizerId(),
        m_pVisualizedExpression->SourceId(),
        m_pVisualizedExpression->StackFrame(),
        nullptr,
        pEvalResultNewName,
        m_pVisualizedExpression,
        index,
        this,
        &pChildVisualizedExpression
    );
    if (FAILED(hr))
    {
        return hr;
    }

    *ppResult = pChildVisualizedExpression.Detach();

    return hr;
}