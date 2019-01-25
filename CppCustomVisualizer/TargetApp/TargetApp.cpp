// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// TargetApp.cpp : Example application which will be debuggeed

#include <iostream>
#include <windows.h>

class MyClass
{
    const FILETIME m_fileTime;
    const int m_anotherField;

public:
    MyClass(const FILETIME& ft, int anotherField) :
        m_fileTime(ft),
        m_anotherField(anotherField)
    {
    }
};

int wmain(int argc, WCHAR* argv[])
{
    FILETIME creationTime;
    HANDLE hFile = CreateFile(argv[0], GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_EXISTING, 0, nullptr);
    if (hFile == INVALID_HANDLE_VALUE)
        return -1;

    if (!GetFileTime(hFile, &creationTime, nullptr, nullptr))
        return -1;

    FILETIME* pPointerTest1 = &creationTime;
    FILETIME* pPointerTest2 = nullptr;
    MyClass c(creationTime, 12);

    FILETIME FTZero = {};

    __debugbreak(); // program will stop here. Evaluate 'creationTime' and 'pPointerTest' in the locals or watch window.
    std::cout << "Test complete\n";

    return 0;
}
