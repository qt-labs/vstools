// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

#pragma once

#include <QString>

class AnotherFancyStringClass
{
public:
    AnotherFancyStringClass() = default;
    explicit AnotherFancyStringClass(const char *str);
    ~AnotherFancyStringClass();

    AnotherFancyStringClass(const AnotherFancyStringClass &other);
    AnotherFancyStringClass(AnotherFancyStringClass &&other) noexcept;

    AnotherFancyStringClass &operator=(const AnotherFancyStringClass &other);
    AnotherFancyStringClass &operator=(AnotherFancyStringClass &&other) noexcept;

    operator QString() const;

private:
    char *data = nullptr;
    std::size_t size = 0;
};
