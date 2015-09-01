// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

// Defines the two possible states the HelloWorld stack frame filter can be in.
struct State
{
    enum e
    {
        // The initial state (state that we start out in). At this point, we
        // haven't seen any stack frames.
        Initial,

        // The state that we are in after we added the '[Hello World]' frame.
        HelloWorldFrameAdded
    };
};

// CHelloWorldDataItem is an internal COM object used to hold the data which the HelloWorld 
// component associates with a DkmStackContext. In other words, this is a state-store which
// the hello world sample can use to hold data associated with a stack walk session.
// The UUID of this object is the key used in a map maintained by DkmStackContext to provide
// this data container support.
class ATL_NO_VTABLE __declspec(uuid("0ac2b7f8-b29f-460c-b82d-311fd536624f")) CHelloWorldDataItem :
    public IUnknown,
    public CComObjectRootEx<CComMultiThreadModel>
{
private:
    State::e m_state;

// CHelloWorldDataItem is created through CComObject<CHelloWorldDataItem>::CreateInstance
protected:
    CHelloWorldDataItem() :
        m_state(State::Initial)
    {
    }
    ~CHelloWorldDataItem()
    {
    }

public:
    State::e CurrentState()
    {
        return m_state;
    }
    void SetState(State::e newValue)
    {
        m_state = newValue;
    }

    // Returns the instance of CHelloWorldDataItem associated with the input DkmStackContext
    // object. If there is not currently an associated CHelloWorldDataItem, a new data item
    // will be created.
    static HRESULT GetInstance(
        DkmStackContext* pContext,
        CHelloWorldDataItem** ppStateObject
        );

    // Returns the instance of CHelloWorldDataItem associated with the input DkmStackContext
    // object. If there is not currently an associated CHelloWorldDataItem, this method will
    // fail.
    static HRESULT GetExistingInstance(
        DkmStackContext* pContext,
        CHelloWorldDataItem** ppStateObject
        );

protected:
    HRESULT _InternalQueryInterface(REFIID riid, void **ppvObject)
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
};
