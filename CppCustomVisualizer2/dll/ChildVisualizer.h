// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#pragma once

class ATL_NO_VTABLE __declspec(uuid("61131513-4f8d-4d5f-a2e3-8e346fe5ff20")) CChildVisualizer :
    public IUnknown,
    public CComObjectRootEx<CComMultiThreadModel>
{
private:
    CComPtr<DkmVisualizedExpression> m_pVisualizedExpression;
    unsigned long long m_vectorSize;
    unsigned long long m_parentIndex;
    bool m_fRootIsPointer;

public:
    CChildVisualizer()
    {
        m_vectorSize = 0;
        m_parentIndex = 0;
        m_fRootIsPointer = false;
    }
    ~CChildVisualizer()
    {
    }

    DECLARE_NO_REGISTRY();
    DECLARE_NOT_AGGREGATABLE(CChildVisualizer);

    HRESULT STDMETHODCALLTYPE Initialize(
        _In_ DkmVisualizedExpression* pVisualizedExpression,
        _In_ unsigned long long vectorSize,
        _In_ unsigned long long parentIndex,
        _In_ bool rootIsPointer
    );

    HRESULT STDMETHODCALLTYPE CreateEvaluationResult(
        _In_ DkmString* pName,
        _In_ DkmString* pFullName,
        _In_opt_ DkmString* pType,
        _In_ Evaluation::DkmRootVisualizedExpressionFlags_t flags,
        _In_opt_ Evaluation::DkmVisualizedExpression* pParent,
        _In_ Evaluation::DkmInspectionContext* pInspectionContext,
        _In_ unsigned long long index,
        _Deref_out_ Evaluation::DkmEvaluationResult** ppResultObject
    );
    HRESULT STDMETHODCALLTYPE GetChildren(
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

protected:
    HRESULT STDMETHODCALLTYPE _InternalQueryInterface(REFIID riid, void** ppvObject)
    {
        if (ppvObject == NULL)
            return E_POINTER;

        if (riid == __uuidof(IUnknown))
        {
            *ppvObject = static_cast<IUnknown*>(this);
            AddRef();
            return S_OK;
        }

        *ppvObject = NULL;
        return E_NOINTERFACE;
    }

private:
    HRESULT STDMETHODCALLTYPE CreateItemVisualizedExpression(
        _In_ DkmString* pEvalText,
        _In_ DkmString* pDisplayName,
        _In_ DkmString* pType,
        _In_ UINT32 index,
        _Deref_out_ DkmChildVisualizedExpression** ppResult
    );
};