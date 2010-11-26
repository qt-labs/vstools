!win32 {
    error("This lib can only be built under Windows!")
}

isEmpty(QT_BUILD_TREE):QT_BUILD_TREE=$(QTDIR)
isEmpty(QT_SOURCE_TREE):QT_SOURCE_TREE=$$fromfile($$QT_BUILD_TREE/.qmake.cache, QT_SOURCE_TREE)

TEMPLATE = lib

CONFIG += qt dll qaxserver release debug_and_release
TARGET = qmakewrapper
VERSION = 1.0.0
DEF_FILE = $$QT_SOURCE_TREE/src/activeqt/control/qaxserver.def
RC_FILE	 = $$QT_SOURCE_TREE/src/activeqt/control/qaxserver.rc

include($$QT_SOURCE_TREE/mkspecs/features/win32/qaxserver.prf)
QMAKE_POST_LINK += $$escape_expand(\\n\\t)

CONFIG(debug, release|debug) {
    DEFINES += DEBUG
    QMAKE_POST_LINK += $$quote(aximp debug\\qmakewrapper1.dll)
}

CONFIG(release, release|debug) {
    QMAKE_POST_LINK += $$quote(aximp release\\qmakewrapper1.dll)
}

HEADERS = qmakewrapper.h
           
SOURCES = qmakewrapper.cpp \
          axfactory.cpp

include(../qmake/qmake.pri)
