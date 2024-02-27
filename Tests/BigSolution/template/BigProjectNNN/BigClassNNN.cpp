/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

#include "BigClassNNN.h"

namespace biglib
{
    template<int N>
    struct value
    {
        static constexpr int get()
        {
            return 1 + value<N - 1>::get();
        }
    };

    template<>
    struct value<0>
    {
        static constexpr int get()
        {
            int n = 0;
            for (int i = 0; i < 20000; ++i) {
                constexpr double x = 42;
                n += (int)x;
            }
            return 0;
        }
    };

    template<int X, int Y>
    struct add
    {
        static constexpr int get()
        {
            return 1 + add<X, Y - 1>::get();
        }
    };

    template <int X>
    struct add<X, 0>
    {
        static constexpr int get()
        {
            return value<X>::get();
        }
    };

    template<int X, int Y>
    struct mult
    {
        static constexpr int get()
        {
            return add<value<X>::get(), mult<X, Y - 1>::get()>::get();
        }
    };

    template <int X>
    struct mult<X, 0>
    {
        static constexpr int get()
        {
            return value<0>::get();
        }
    };

    template<int X, int Y>
    struct pow
    {
        static constexpr int get()
        {
            return mult<value<X>::get(), pow<X, Y - 1>::get()>::get();
        }
    };

    template <int X>
    struct pow<X, 0>
    {
        static constexpr int get()
        {
            return value<1>::get();
        }
    };
}

BigClassNNN::BigClassNNN()
{
}

int BigClassNNN::BigMethod()
{
    return biglib::pow<2, 3>::get();
}
