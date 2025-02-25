// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

#include "shared.h"

AnotherFancyStringClass::AnotherFancyStringClass(const char *str)
{
    if (str) {
        size = std::strlen(str);
        data = new char[size + 1];
        std::strcpy(data, str);
    }
}

AnotherFancyStringClass::~AnotherFancyStringClass()
{
    delete[] data;
}

AnotherFancyStringClass::AnotherFancyStringClass(const AnotherFancyStringClass &other)
{
    size = other.size;
    data = new char[size + 1];
    std::strcpy(data, other.data);
}

AnotherFancyStringClass::AnotherFancyStringClass(AnotherFancyStringClass &&other) noexcept
    : data(other.data)
    , size(other.size)
{
    other.data = nullptr;
    other.size = 0;
}

AnotherFancyStringClass &AnotherFancyStringClass::operator=(const AnotherFancyStringClass &other)
{
    if (this != &other) {
        delete[] data;
        size = other.size;
        data = new char[size + 1];
        std::strcpy(data, other.data);
    }
    return *this;
}

AnotherFancyStringClass &AnotherFancyStringClass::operator=(AnotherFancyStringClass &&other) noexcept
{
    if (this != &other) {
        delete[] data;
        data = other.data;
        size = other.size;
        other.data = nullptr;
        other.size = 0;
    }
    return *this;
}

AnotherFancyStringClass::operator QString() const
{
    return QString::fromUtf8(data ? data : "");
}
