/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/
#pragma once

#ifndef BUILD_STATIC
# if defined(VSQML_LIB)
#  define VSQML_EXPORT __declspec(dllexport)
# else
#  define VSQML_EXPORT __declspec(dllimport)
# endif
#else
# define VSQML_EXPORT
#endif
