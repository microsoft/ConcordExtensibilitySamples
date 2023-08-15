// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#include "stdafx.h"
#include "ChildVisualizer.h"

HRESULT CChildVisualizer::Initialize(
    _In_ DkmVisualizedExpression* pVisualizedExpression,
    _In_ unsigned long long vectorSize,
    _In_ unsigned long long parentIndex,
    _In_ bool rootIsPointer
)
{
    m_pVisualizedExpression = pVisualizedExpression;
    m_vectorSize = vectorSize;
    m_parentIndex = parentIndex;
    m_fRootIsPointer = rootIsPointer;
    return S_OK;
}

HRESULT CChildVisualizer::CreateEvaluationResult(
    _In_ DkmString* pName,
    _In_ DkmString* pFullName,
    _In_ DkmRootVisualizedExpressionFlags_t flags,
    _In_opt_ DkmVisualizedExpression* pParent,
    _In_ DkmInspectionContext* pInspectionContext,
    _In_ unsigned long long index,
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
    strValue.Format(L"%llu", index);

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
        nullptr,
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
    pInitialChildren->Members = nullptr;
    pInitialChildren->Length = 0;

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

    *ppEnumContext = pEnumContext.Detach();

    return hr;
}

static LPCWSTR itemNames[2] = { L"A", L"B" };
static LPCWSTR itemExprs[2] = { L"(%s).a[%llu]", L"(%s).b[%llu]" };
static LPCWSTR itemExprsPtr[2] = { L"(%s)->a[%llu]", L"(%s)->b[%llu]" };

HRESULT CChildVisualizer::GetItems(
    _In_ DkmVisualizedExpression* pVisualizedExpression,
    _In_ DkmEvaluationResultEnumContext* pEnumContext,
    _In_ UINT32 StartIndex,
    _In_ UINT32 Count,
    _Out_ DkmArray<DkmChildVisualizedExpression*>* pItems
)
{
    HRESULT hr = S_OK;

    if (Count == 0 || StartIndex > 1 || StartIndex + Count > 2)
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

    for (UINT32 i = 0; i < Count; i++)
    {
        CString evalText;

        UINT32 index = StartIndex + 1;
        VSAnalysisAssume(index < _countof(itemExprsPtr) && index < _countof(itemExprs) , "Should be impossible: already validated at start of function");
        if (m_fRootIsPointer)
        {
            evalText.Format(itemExprsPtr[index], pFullName->Value(), m_parentIndex);
        }
        else
        {
            evalText.Format(itemExprs[index], pFullName->Value(), m_parentIndex);
        }
        CComPtr<DkmString> pEvalText;
        hr = DkmString::Create(DkmSourceString(evalText), &pEvalText);
        if (FAILED(hr))
        {
            return hr;
        }

        CComPtr<DkmString> pDisplayName;
        hr = DkmString::Create(DkmSourceString(itemNames[index]), &pDisplayName);
        if (FAILED(hr))
        {
            return hr;
        }

        CComPtr<DkmChildVisualizedExpression> pChildVisualizedExpression;
        hr = CreateItemVisualizedExpression(
            pEvalText,
            pDisplayName,
            pType,
            index,
            &pChildVisualizedExpression
        );
        if (FAILED(hr))
        {
            return hr;
        }

        resultValues.Members[i] = pChildVisualizedExpression.Detach();
    }

    *pItems = resultValues.Detach();

    return hr;
}

HRESULT CChildVisualizer::CreateItemVisualizedExpression(
    _In_ DkmString* pEvalText,
    _In_ DkmString* pDisplayName,
    _In_ DkmString* pType,
    _In_ UINT32 index,
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