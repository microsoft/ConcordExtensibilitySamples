// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

// This file defines the CHelloWorldService class, which is the one and only
// COM object exported from the sample dll.

#include "HelloWorld.Contract.h"

class ATL_NO_VTABLE CHelloWorldService :
    // Inherit from CHelloWorldServiceContract to provide the list of interfaces that
    // this class implements (interface list comes from HelloWorld.vsdconfigxml)
    public CHelloWorldServiceContract,

    // Inherit from CComObjectRootEx to provide ATL support for reference counting and
    // object creation.
	public CComObjectRootEx<CComMultiThreadModel>,

    // Inherit from CComCoClass to provide ATL support for exporting this class from
    // DllGetClassObject
    public CComCoClass<CHelloWorldService, &CHelloWorldServiceContract::ClassId>
{
protected:
    CHelloWorldService()
    {
    }
    ~CHelloWorldService()
    {
    }

public:
    DECLARE_NO_REGISTRY();
    DECLARE_NOT_AGGREGATABLE(CHelloWorldService);

// IDkmCallStackFilter methods
// For documentation of this interface, open <Visual Studio Install Directory>\VSSDK\VisualStudioIntegration\Common\inc\vsdebugeng.h
// Then search for "IDkmCallStackFilter"
public:
    HRESULT STDMETHODCALLTYPE FilterNextFrame(
        DkmStackContext* pStackContext,
        DkmStackWalkFrame* pInput,
        DkmArray<DkmStackWalkFrame*>* pResult
        );
};

OBJECT_ENTRY_AUTO(CHelloWorldService::ClassId, CHelloWorldService)
