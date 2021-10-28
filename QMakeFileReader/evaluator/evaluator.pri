INCLUDEPATH *= $$PWD
DEPENDPATH *= $$PWD

INCLUDEPATH += \
    $$QMAKE_PARSER_DIR

HEADERS += \
    $$PWD/ioutils.h \
    $$PWD/proitems.h \
    $$PWD/qmakeevaluator.h \
    $$PWD/qmakeevaluator_p.h \
    $$PWD/qmakeglobals.h \
    $$PWD/qmakeparser.h \
    $$PWD/qmake_global.h

SOURCES += \
    $$PWD/ioutils.cpp \
    $$PWD/proitems.cpp \
    $$PWD/qmakebuiltins.cpp \
    $$PWD/qmakeevaluator.cpp \
    $$PWD/qmakeglobals.cpp \
    $$PWD/qmakeparser.cpp
