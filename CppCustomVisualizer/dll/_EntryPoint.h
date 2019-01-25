// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

// This file defines the CCppCustomVisualizerService class, which is the one and only
// COM object exported from the sample dll.

#include "CppCustomVisualizer.Contract.h"

class ATL_NO_VTABLE CCppCustomVisualizerService :
    // Inherit from CCppCustomVisualizerServiceContract to provide the list of interfaces that
    // this class implements (interface list comes from CppCustomVisualizer.vsdconfigxml)
    public CCppCustomVisualizerServiceContract,

    // Inherit from CComObjectRootEx to provide ATL support for reference counting and
    // object creation.
	public CComObjectRootEx<CComMultiThreadModel>,

    // Inherit from CComCoClass to provide ATL support for exporting this class from
    // DllGetClassObject
    public CComCoClass<CCppCustomVisualizerService, &CCppCustomVisualizerServiceContract::ClassId>
{
protected:
    CCppCustomVisualizerService()
    {
    }
    ~CCppCustomVisualizerService()
    {
    }

public:
    DECLARE_NO_REGISTRY();
    DECLARE_NOT_AGGREGATABLE(CCppCustomVisualizerService);

// IDkmCustomVisualizer methods
public:
    HRESULT STDMETHODCALLTYPE EvaluateVisualizedExpression(
        _In_ Evaluation::DkmVisualizedExpression* pVisualizedExpression,
        _Deref_out_opt_ Evaluation::DkmEvaluationResult** ppResultObject
        );
    HRESULT STDMETHODCALLTYPE UseDefaultEvaluationBehavior(
        _In_ Evaluation::DkmVisualizedExpression* pVisualizedExpression,
        _Out_ bool* pUseDefaultEvaluationBehavior,
        _Deref_out_opt_ Evaluation::DkmEvaluationResult** ppDefaultEvaluationResult
        );
    HRESULT STDMETHODCALLTYPE GetChildren(
        _In_ Evaluation::DkmVisualizedExpression* pVisualizedExpression,
        _In_ UINT32 InitialRequestSize,
        _In_ Evaluation::DkmInspectionContext* pInspectionContext,
        _Out_ DkmArray<Evaluation::DkmChildVisualizedExpression*>* pInitialChildren,
        _Deref_out_ Evaluation::DkmEvaluationResultEnumContext** ppEnumContext
        );
    HRESULT STDMETHODCALLTYPE GetItems(
        _In_ Evaluation::DkmVisualizedExpression* pVisualizedExpression,
        _In_ Evaluation::DkmEvaluationResultEnumContext* pEnumContext,
        _In_ UINT32 StartIndex,
        _In_ UINT32 Count,
        _Out_ DkmArray<Evaluation::DkmChildVisualizedExpression*>* pItems
        );
    HRESULT STDMETHODCALLTYPE SetValueAsString(
        _In_ Evaluation::DkmVisualizedExpression* pVisualizedExpression,
        _In_ DkmString* pValue,
        _In_ UINT32 Timeout,
        _Deref_out_opt_ DkmString** ppErrorText
        );
    HRESULT STDMETHODCALLTYPE GetUnderlyingString(
        _In_ Evaluation::DkmVisualizedExpression* pVisualizedExpression,
        _Deref_out_opt_ DkmString** ppStringValue
        );

private:
    static HRESULT FileTimeToText(const FILETIME& fileTime, CString& text);
};

OBJECT_ENTRY_AUTO(CCppCustomVisualizerService::ClassId, CCppCustomVisualizerService)
