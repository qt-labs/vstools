#pragma once

#include <QtCore/qglobal.h>

#ifndef BUILD_STATIC
# if defined($pro_lib_define$)
#  define $pro_lib_export$ Q_DECL_EXPORT
# else
#  define $pro_lib_export$ Q_DECL_IMPORT
# endif
#else
# define $pro_lib_export$
#endif
