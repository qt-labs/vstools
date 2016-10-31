TEMPLATE = app
QT += gui widgets
TARGET = QrcEditor
DESTDIR = $$PWD/bin

include(../src.pri)
include(../../vstools.pri)
include(./shared/qrceditor.pri)

SOURCES += main.cpp \
    mainwindow.cpp
    
HEADERS += mainwindow.h

RC_FILE = qrceditor.rc
