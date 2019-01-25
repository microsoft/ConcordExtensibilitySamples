// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once

#ifndef STRICT
#define STRICT
#endif

#include "targetver.h"

#define _ATL_FREE_THREADED
#define _ATL_NO_AUTOMATIC_NAMESPACE

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS	// some CString constructors will be explicit

#include "resource.h"
#include <atlbase.h>
#include <atlcom.h>
#include <atlctl.h>

#include <vsdebugeng.h>
#include <vsdebugeng.templates.h>

using namespace ATL;
using namespace Microsoft::VisualStudio::Debugger;
using namespace Microsoft::VisualStudio::Debugger::Evaluation;

inline _Ret_range_(0x8000000, 0xffffffff) HRESULT WIN32_ERROR(LONG lError)
{
    HRESULT hr = HRESULT_FROM_WIN32(lError);
    if (SUCCEEDED(hr))
        hr = E_FAIL;
    return hr;
}

inline _Ret_range_(0x8000000, 0xffffffff) HRESULT WIN32_LAST_ERROR()
{
    return WIN32_ERROR(GetLastError());
}
