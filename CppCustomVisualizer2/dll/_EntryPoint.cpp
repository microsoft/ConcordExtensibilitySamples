// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#include "stdafx.h"
#include "_EntryPoint.h"

HRESULT STDMETHODCALLTYPE CCppCustomVisualizerService::EvaluateVisualizedExpression(
    _In_ Evaluation::DkmVisualizedExpression* pVisualizedExpression,
    _Deref_out_opt_ Evaluation::DkmEvaluationResult** ppResultObject
    )
{
    HRESULT hr;

    hr = CRootVisualizer::CreateEvaluationResult(pVisualizedExpression, ppResultObject);
    if (FAILED(hr))
    {
        return hr;
    }

    hr = (*ppResultObject)->SetDataItem(DkmDataCreationDisposition::CreateNew, pVisualizedExpression);

    return hr;
}

HRESULT STDMETHODCALLTYPE CCppCustomVisualizerService::UseDefaultEvaluationBehavior(
    _In_ Evaluation::DkmVisualizedExpression* pVisualizedExpression,
    _Out_ bool* pUseDefaultEvaluationBehavior,
    _Deref_out_opt_ Evaluation::DkmEvaluationResult** ppDefaultEvaluationResult
    )
{
    *pUseDefaultEvaluationBehavior = false;
    *ppDefaultEvaluationResult = NULL;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CCppCustomVisualizerService::GetChildren(
    _In_ Evaluation::DkmVisualizedExpression* pVisualizedExpression,
    _In_ UINT32 InitialRequestSize,
    _In_ Evaluation::DkmInspectionContext* pInspectionContext,
    _Out_ DkmArray<Evaluation::DkmChildVisualizedExpression*>* pInitialChildren,
    _Deref_out_ Evaluation::DkmEvaluationResultEnumContext** ppEnumContext
    )
{
    HRESULT hr = S_OK;

    CComPtr<CRootVisualizer> pRootVisualizer;
    hr = pVisualizedExpression->GetDataItem(&pRootVisualizer);

    if (SUCCEEDED(hr))
    {
        hr = pRootVisualizer->GetChildren(InitialRequestSize, pInspectionContext, pInitialChildren, ppEnumContext);
    }
    else
    {
        CComPtr<CChildVisualizer> PChildVisualizer;
        hr = pVisualizedExpression->GetDataItem(&PChildVisualizer);

        if (SUCCEEDED(hr))
        {
            hr = PChildVisualizer->GetChildren(InitialRequestSize, pInspectionContext, pInitialChildren, ppEnumContext);
        }
    }

    return hr;
}

HRESULT STDMETHODCALLTYPE CCppCustomVisualizerService::GetItems(
    _In_ Evaluation::DkmVisualizedExpression* pVisualizedExpression,
    _In_ Evaluation::DkmEvaluationResultEnumContext* pEnumContext,
    _In_ UINT32 StartIndex,
    _In_ UINT32 Count,
    _Out_ DkmArray<Evaluation::DkmChildVisualizedExpression*>* pItems
    )
{
    HRESULT hr = S_OK;

    CComPtr<CRootVisualizer> pRootVisualizer;
    hr = pVisualizedExpression->GetDataItem(&pRootVisualizer);

    if (SUCCEEDED(hr))
    {
        hr = pRootVisualizer->GetItems(pVisualizedExpression, pEnumContext, StartIndex, Count, pItems);
    }
    else
    {
        CComPtr<CChildVisualizer> PChildVisualizer;
        hr = pVisualizedExpression->GetDataItem(&PChildVisualizer);

        if (SUCCEEDED(hr))
        {
            hr = PChildVisualizer->GetItems(pVisualizedExpression, pEnumContext, StartIndex, Count, pItems);
        }
    }

    return hr;
}

HRESULT STDMETHODCALLTYPE CCppCustomVisualizerService::SetValueAsString(
    _In_ Evaluation::DkmVisualizedExpression* pVisualizedExpression,
    _In_ DkmString* pValue,
    _In_ UINT32 Timeout,
    _Deref_out_opt_ DkmString** ppErrorText
    )
{
    // This sample delegates setting values to the C++ EE, so this method doesn't need to be implemented
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CCppCustomVisualizerService::GetUnderlyingString(
    _In_ Evaluation::DkmVisualizedExpression* pVisualizedExpression,
    _Deref_out_opt_ DkmString** ppStringValue
    )
{
    // Sample doesn't have an underlying string (no DkmEvaluationResultFlags::RawString), so this method
    // doesn't need to be implemented
    return E_NOTIMPL;
}