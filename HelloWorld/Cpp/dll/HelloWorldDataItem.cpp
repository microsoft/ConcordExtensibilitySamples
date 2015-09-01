// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "StdAfx.h"
#include "HelloWorldDataItem.h"

// Returns the instance of CHelloWorldDataItem associated with the input DkmStackContext
// object. If there is not currently an associated CHelloWorldDataItem, a new data item
// will be created.
HRESULT CHelloWorldDataItem::GetInstance(
    DkmStackContext* pContext,
    CHelloWorldDataItem** ppStateObject
    )
{
    HRESULT hr;

    // If there is already an associated item, return it.
    hr = GetExistingInstance(pContext, ppStateObject);
    if (hr == S_OK)
        return hr;

    // Otherwise create a new object
    CComObject<CHelloWorldDataItem>* pComObject;
    hr = CComObject<CHelloWorldDataItem>::CreateInstance(&pComObject);
    if (FAILED(hr))
    {
        return hr;
    }

    // Assign it to a CComPtr so that it is AddRef'ed
    CComPtr<CHelloWorldDataItem> pCreatedInstance(pComObject);

    // Then associate the new data item with pContext
    hr = pContext->SetDataItem(DkmDataCreationDisposition::CreateNew, pCreatedInstance);
    if (FAILED(hr))
    {
        // NOTE: Since call stack walking is already synchronized, this should never 
        // fail (absent out-of-memory). In other scenarios, it might be important to 
        // handle the case that a component is trying to set the data item 
        // simultaneously from multiple threads.
        return hr;
    }

    // Return the new object
    *ppStateObject = pCreatedInstance.Detach();
    return S_OK;
}

// Returns the instance of CHelloWorldDataItem associated with the input DkmStackContext
// object. If there is not currently an associated CHelloWorldDataItem, this method will
// fail.
HRESULT CHelloWorldDataItem::GetExistingInstance(
    DkmStackContext* pContext,
    CHelloWorldDataItem** ppStateObject
    )
{
    return pContext->GetDataItem(ppStateObject);
}
