TEMPLATE = app
QT += gui

include(./shared/qrceditor.pri)
SOURCES += main.cpp mainwindow.cpp
HEADERS += mainwindow.h

win32 {
    RC_FILE = qrceditor.rc
}

CONFIG(debug, debug|release) {
    DESTDIR  = ../../Qt4VS2003/Qt4VSAddin/Debug
}

CONFIG(release, debug|release) {
    DESTDIR  = ../../Qt4VS2003/Qt4VSAddin/Release
}

