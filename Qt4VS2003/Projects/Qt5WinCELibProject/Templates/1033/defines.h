#ifndef %PRE_DEF%
#define %PRE_DEF%

#include <QtCore/qglobal.h>

#ifdef %PRO_LIB_DEFINE%
# define %PRO_LIB_EXPORT% Q_DECL_EXPORT
#else
# define %PRO_LIB_EXPORT% Q_DECL_IMPORT
#endif

#endif // %PRE_DEF%
