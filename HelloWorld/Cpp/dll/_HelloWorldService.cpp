// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// _HelloWorldService.cpp : Implementation of CHelloWorldService

#include "stdafx.h"
#include "_HelloWorldService.h"
#include "HelloWorldDataItem.h"

HRESULT STDMETHODCALLTYPE CHelloWorldService::FilterNextFrame(
    DkmStackContext* pStackContext,
    DkmStackWalkFrame* pInput,
    DkmArray<DkmStackWalkFrame*>* pResult
    )
{
    HRESULT hr;

    // The HelloWorld sample is a very simple debugger component which modified the call stack
    // so that there is a '[Hello World]' frame at the top of the call stack. All the frames
    // below this are left the same.

    if (pInput == NULL) // NULL input frame indicates the end of the call stack. This sample does nothing on end-of-stack.
        return S_OK; 
    
    // Get the CHelloWorldDataItem which is associated with this stack walk. This
    // lets us keep data associated with this stack walk.
    CComPtr<CHelloWorldDataItem> pDataItem;
    hr = CHelloWorldDataItem::GetInstance(pStackContext, &pDataItem);
    if (FAILED(hr))
        return hr;

    // Now use this data item to see if we are looking at the first (top-most) frame
    if (pDataItem->CurrentState() == State::Initial)
    {
        // On the top most frame, we want to return back two different frames. First 
        // we place the '[Hello World]' frame, and under that we put the input frame.

        // Allocate an array with two elements. Store it in a CAutoDkmArray so that
        // if anything fails, the memory will be automatically freed.
        CAutoDkmArray<DkmStackWalkFrame*> result;
        hr = DkmAllocArray(2, &result);
        if (FAILED(hr))
        {
            return hr;
        }

        // Create a string object for 'hello world'
        CComPtr<DkmString> pDescription;
        hr = DkmString::Create(L"[Hello World]", &pDescription);
        if (FAILED(hr))
        {
            return hr;
        }

        // Create the hello world frame object, and stick it in the array
        hr = DkmStackWalkFrame::Create(
            pStackContext->Thread(),
            NULL,                           // Annotated frame, so no instruction address
            pInput->FrameBase(),            // Use the same frame base as the input frame
            0,                              // annoted frame uses zero bytes
            DkmStackWalkFrameFlags::None,
            pDescription,
            NULL,                           // Annotated frame, so no registers
            NULL,
            &result.Members[0]
            );
        if (FAILED(hr))
        {
            return hr;
        }

        // Add the input frame into the array as well
        result.Members[1] = pInput;
        result.Members[1]->AddRef();

        // Array succesfully created, so return the value in the out param, and update our
        // state so that on the next frame we know not to add '[Hello World]' again.
        *pResult = result.Detach();
        pDataItem->SetState(State::HelloWorldFrameAdded);
    }
    else
    {
        // We have already added '[Hello World]' to this call stack, so just return
        // the input frame.

        hr = DkmAllocArray(1, pResult);
        if (FAILED(hr))
        {
            return hr;
        }

        pResult->Members[0] = pInput;
        pResult->Members[0]->AddRef();
    }

    return S_OK;
}