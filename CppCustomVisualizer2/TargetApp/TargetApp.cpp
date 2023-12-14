// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// TargetApp.cpp : Example application which will be debuggeed

#include <iostream>
#include <windows.h>
#include "TargetApp.h"

int wmain(int argc, WCHAR* argv[])
{
    Sample sample;
    sample.a = { 1, 2, 3, 4, 5 };
    sample.b = { 5, 4, 3, 2, 1 };

    __debugbreak(); // program will stop here. Evaluate 'sample' in the locals or watch window.
    std::cout << "Test complete\n";

    return 0;
}
