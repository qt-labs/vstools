QT -= gui
CONFIG += console
DESTDIR = $$PWD/bin
TARGET = QMakeFileReader

include(../src.pri)
include(./evaluator/evaluator.pri)

SOURCES += main.cpp \
    evalhandler.cpp \
    qmakedataprovider.cpp \

HEADERS += evalhandler.h \
    qmakedataprovider.h \

RC_FILE = qmakefilereader.rc
