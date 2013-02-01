QMAKE_PARSER_DIR=$$PWD/evaluator

INCLUDEPATH += \
    $$QMAKE_PARSER_DIR

HEADERS += \
    $$PWD/evalhandler.h \
    $$PWD/qmakedataprovider.h \
    $$QMAKE_PARSER_DIR/ioutils.h \
    $$QMAKE_PARSER_DIR/proitems.h \
    $$QMAKE_PARSER_DIR/qmakeevaluator.h \
    $$QMAKE_PARSER_DIR/qmakeevaluator_p.h \
    $$QMAKE_PARSER_DIR/qmakeglobals.h \
    $$QMAKE_PARSER_DIR/qmakeparser.h \
    $$QMAKE_PARSER_DIR/qmake_global.h

SOURCES += \
    $$PWD/evalhandler.cpp \
    $$PWD/qmakedataprovider.cpp \
    $$QMAKE_PARSER_DIR/ioutils.cpp \
    $$QMAKE_PARSER_DIR/proitems.cpp \
    $$QMAKE_PARSER_DIR/qmakebuiltins.cpp \
    $$QMAKE_PARSER_DIR/qmakeevaluator.cpp \
    $$QMAKE_PARSER_DIR/qmakeglobals.cpp \
    $$QMAKE_PARSER_DIR/qmakeparser.cpp
