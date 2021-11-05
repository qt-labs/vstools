/****************************************************************************
**
** Copyright (C) 2021 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Tools.
**
** $QT_BEGIN_LICENSE:GPL-EXCEPT$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see https://www.qt.io/terms-conditions. For further
** information use the contact form at https://www.qt.io/contact-us.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3 as published by the Free Software
** Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
** included in the packaging of this file. Please review the following
** information to ensure the GNU General Public License requirements will
** be met: https://www.gnu.org/licenses/gpl-3.0.html.
**
** $QT_END_LICENSE$
**
****************************************************************************/

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
