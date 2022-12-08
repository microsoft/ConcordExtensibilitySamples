// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#include "stdafx.h"
#include "RootVisualizer.h"

HRESULT CRootVisualizer::Initialize(
    _In_ DkmVisualizedExpression* pVisualizedExpression,
    _In_ unsigned long long size,
    _In_ bool isPointer)
{
    m_pVisualizedExpression = pVisualizedExpression;
    m_size = size;
    m_fIsPointer = isPointer;
    return S_OK;
}

//static 
HRESULT CRootVisualizer::CreateEvaluationResult(_In_ DkmVisualizedExpression* pVisualizedExpression, _Deref_out_ DkmEvaluationResult** ppResultObject)
{
    HRESULT hr = S_OK;

    CComPtr<DkmRootVisualizedExpression> pRootVisualizedExpression = DkmRootVisualizedExpression::TryCast(pVisualizedExpression);
    if (pRootVisualizedExpression == nullptr)
    {
        // This sample doesn't provide child evaluation results, so only root expressions are expected
        return E_NOTIMPL;
    }
    CComPtr<DkmString> pName = pRootVisualizedExpression->Name();
    CComPtr<DkmString> pFullName = pRootVisualizedExpression->FullName();
    CComPtr<DkmString> pType = pRootVisualizedExpression->Type();
    DkmRootVisualizedExpressionFlags_t flags = pRootVisualizedExpression->Flags();

    CString evalText;
    bool isPointer = (pType != nullptr && wcschr(pType->Value(), '*') != nullptr);
    unsigned long long sizeA;
    hr = GetSize(
        pVisualizedExpression,
        pFullName,
        L"a",
        isPointer,
        &sizeA
    );
    if (FAILED(hr))
    {
        return hr;
    }

    unsigned long long sizeB;
    hr = GetSize(
        pVisualizedExpression,
        pFullName,
        L"b",
        isPointer,
        &sizeB
    );
    if (FAILED(hr))
    {
        return hr;
    }

    if (sizeA != sizeB)
    {
        return E_FAIL;
    }

    CComObject<CRootVisualizer>* pRootVisualizer;
    CComObject<CRootVisualizer>::CreateInstance(&pRootVisualizer);
    pRootVisualizer->Initialize(pVisualizedExpression, sizeA, isPointer);
    pVisualizedExpression->SetDataItem(DkmDataCreationDisposition::CreateNew, pRootVisualizer);

    hr = pRootVisualizer->CreateEvaluationResult(
        pName,
        pFullName,
        pType,
        flags,
        nullptr,
        pVisualizedExpression->InspectionContext(),
        ppResultObject
    );

    return hr;
}

HRESULT CRootVisualizer::CreateEvaluationResult(
    _In_ DkmString* pName,
    _In_ DkmString* pFullName,
    _In_ DkmString* pType,
    _In_ DkmRootVisualizedExpressionFlags_t flags,
    _In_opt_ DkmVisualizedExpression* pParent,
    _In_ DkmInspectionContext* pInspectionContext,
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
    hr = DkmDataAddress::Create(m_pVisualizedExpression->RuntimeInstance(), pPointerValueHome->Address(), NULL, &pAddress);
    if (FAILED(hr))
    {
        return hr;
    }

    CString strValue;
    strValue.Format(L"Size = %llu", m_size);

    CString strEditableValue;

    // If we are formatting a pointer, we want to also show the address of the pointer
    if (m_fIsPointer)
    {
        // Make the editable value just the pointer string
        UINT64 address = pPointerValueHome->Address();
        DkmProcess* pTargetProcess = pInspectionContext->RuntimeInstance()->Process();
        if ((pTargetProcess->SystemInformation()->Flags() & DefaultPort::DkmSystemInformationFlags::Is64Bit) != 0)
        {
            strEditableValue.Format(L"0x%08x%08x", static_cast<DWORD>(address >> 32), static_cast<DWORD>(address));
        }
        else
        {
            strEditableValue.Format(L"0x%08x", static_cast<DWORD>(address));
        }

        // Prefix the value with the address
        CString strValueWithAddress;
        strValueWithAddress.Format(L"%s {%s}", static_cast<LPCWSTR>(strEditableValue), static_cast<LPCWSTR>(strValue));
        strValue = strValueWithAddress;
    }

    CComPtr<DkmString> pValue;
    hr = DkmString::Create(DkmSourceString(strValue), &pValue);
    if (FAILED(hr))
    {
        return hr;
    }

    CComPtr<DkmString> pEditableValue;
    hr = DkmString::Create(strEditableValue, &pEditableValue);
    if (FAILED(hr))
    {
        return hr;
    }

    DkmEvaluationResultFlags_t resultFlags = DkmEvaluationResultFlags::None;
    if (m_size != 0)
    {
        resultFlags |= DkmEvaluationResultFlags::Expandable;
    }
    if (strEditableValue.IsEmpty())
    {
        // We only allow editting pointers, so mark non-pointers as read-only
        resultFlags |= DkmEvaluationResultFlags::ReadOnly;
    }

    CComPtr<DkmSuccessEvaluationResult> pSuccessEvaluationResult;
    hr = DkmSuccessEvaluationResult::Create(
        m_pVisualizedExpression->InspectionContext(),
        m_pVisualizedExpression->StackFrame(),
        pName,
        pFullName,
        resultFlags,
        pValue,
        pValue,
        pType,
        DkmEvaluationResultCategory::Class,
        DkmEvaluationResultAccessType::None,
        DkmEvaluationResultStorageType::None,
        DkmEvaluationResultTypeModifierFlags::None,
        pAddress,
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

HRESULT CRootVisualizer::GetChildren(
    _In_ UINT32 InitialRequestSize,
    _In_ DkmInspectionContext* pInspectionContext,
    _Out_ DkmArray<DkmChildVisualizedExpression*>* pInitialChildren,
    _Deref_out_ DkmEvaluationResultEnumContext** ppEnumContext
)
{
    HRESULT hr = S_OK;

    CComPtr<DkmEvaluationResultEnumContext> pEnumContext;
    hr = DkmEvaluationResultEnumContext::Create(
        m_size,
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
        GetItems(m_pVisualizedExpression, pEnumContext, 0, InitialRequestSize, pInitialChildren);
    }

    *ppEnumContext = pEnumContext.Detach();

    return hr;
}

HRESULT CRootVisualizer::GetItems(
    _In_ DkmVisualizedExpression* pVisualizedExpression,
    _In_ DkmEvaluationResultEnumContext* pEnumContext,
    _In_ UINT32 StartIndex,
    _In_ UINT32 Count,
    _Out_ DkmArray<DkmChildVisualizedExpression*>* pItems
)
{
    HRESULT hr = S_OK;

    CComPtr<DkmPointerValueHome> pPointerValueHome = DkmPointerValueHome::TryCast(pVisualizedExpression->ValueHome());

    CAtlList<CComPtr<DkmChildVisualizedExpression>> childItems;

    for (UINT32 i = StartIndex; i < Count + StartIndex && i < m_size; i++)
    {
        CComPtr<DkmPointerValueHome> pParentPointerValueHome = DkmPointerValueHome::TryCast(pVisualizedExpression->ValueHome());

        CComPtr<DkmString> pChildName;
        hr = DkmString::Create(DkmSourceString(L"[Index]"), &pChildName);
        if (FAILED(hr))
        {
            return hr;
        }

        CComPtr<DkmString> pChildFullName;
        hr = m_pVisualizedExpression->CreateDefaultChildFullName(0, &pChildFullName);
        if (FAILED(hr))
        {
            return hr;
        }

        CComObject<CChildVisualizer>* pChildVisualizer;
        CComObject<CChildVisualizer>::CreateInstance(&pChildVisualizer);
        pChildVisualizer->Initialize(m_pVisualizedExpression, m_size, i, m_fIsPointer);

        CComPtr<DkmEvaluationResult> pEvaluationResult;
        hr = pChildVisualizer->CreateEvaluationResult(
            pChildName,
            pChildFullName,
            nullptr,
            DkmRootVisualizedExpressionFlags::None,
            m_pVisualizedExpression,
            m_pVisualizedExpression->InspectionContext(),
            i,
            &pEvaluationResult
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
            pPointerValueHome,
            pEvaluationResult,
            m_pVisualizedExpression,
            i,
            pChildVisualizer,
            &pChildVisualizedExpression
        );
        if (FAILED(hr))
        {
            return hr;
        }

        childItems.AddTail(pChildVisualizedExpression);
    }

    CAutoDkmArray<DkmChildVisualizedExpression*> resultValues;
    DkmAllocArray(childItems.GetCount(), &resultValues);
    if (FAILED(hr))
    {
        return hr;
    }

    UINT32 j = 0;
    POSITION pos = childItems.GetHeadPosition();
    while (pos != NULL)
    {
        CComPtr<DkmChildVisualizedExpression> pCurr = childItems.GetNext(pos);
        resultValues.Members[j] = pCurr.Detach();
        j++;
    }

    *pItems = resultValues.Detach();

    return hr;
}

HRESULT CRootVisualizer::GetSize(
    _In_ Evaluation::DkmVisualizedExpression* pVisualizedExpression,
    _In_ DkmString* pFullName,
    _In_ LPCWSTR pMemberName,
    _In_ bool rootIsPointer,
    _Out_ unsigned long long* pSize
)
{
    HRESULT hr = S_OK;

    CString evalText;
    if (rootIsPointer)
    {
        evalText.Format(L"(%s)->%s.size()", pFullName->Value(), pMemberName);
    }
    else
    {
        evalText.Format(L"(%s).%s.size()", pFullName->Value(), pMemberName);
    }
    CComPtr<DkmString> pEvalText;
    hr = DkmString::Create(DkmSourceString(evalText), &pEvalText);
    if (FAILED(hr))
    {
        return hr;
    }

    CComPtr<DkmLanguageExpression> pLanguageExpression;
    hr = DkmLanguageExpression::Create(
        pVisualizedExpression->InspectionContext()->Language(),
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
    hr = pVisualizedExpression->EvaluateExpressionCallback(
        pVisualizedExpression->InspectionContext(),
        pLanguageExpression,
        pVisualizedExpression->StackFrame(),
        &pEvalResult
    );
    if (FAILED(hr))
    {
        return hr;
    }

    CComPtr<DkmSuccessEvaluationResult> pSuccessEvalResult = DkmSuccessEvaluationResult::TryCast(pEvalResult);
    if (pSuccessEvalResult == nullptr || pSuccessEvalResult->Value() == nullptr)
    {
        return E_FAIL;
    }

    LPCWSTR sizeStr = pSuccessEvalResult->Value()->Value();
    LPWSTR endPtr;
    *pSize = wcstoull(sizeStr, &endPtr, 0);
    if (sizeStr == endPtr)
    {
        return E_FAIL;
    }

    return hr;
}