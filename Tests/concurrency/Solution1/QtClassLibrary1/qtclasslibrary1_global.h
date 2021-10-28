#pragma once

#include <QtCore/qglobal.h>

#ifndef BUILD_STATIC
# if defined(QTCLASSLIBRARY1_LIB)
#  define QTCLASSLIBRARY1_EXPORT Q_DECL_EXPORT
# else
#  define QTCLASSLIBRARY1_EXPORT Q_DECL_IMPORT
# endif
#else
# define QTCLASSLIBRARY1_EXPORT
#endif
