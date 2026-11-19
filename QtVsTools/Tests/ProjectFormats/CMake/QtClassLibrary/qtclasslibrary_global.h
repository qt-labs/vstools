#pragma once

#include <QtCore/qglobal.h>

#ifndef BUILD_STATIC
# if defined(QTCLASSLIBRARY_LIB)
#  define QTCLASSLIBRARY_EXPORT Q_DECL_EXPORT
# else
#  define QTCLASSLIBRARY_EXPORT Q_DECL_IMPORT
# endif
#else
# define QTCLASSLIBRARY_EXPORT
#endif
