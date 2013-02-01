QT -= gui
CONFIG += console
TARGET = qmakefilereader

CONFIG(debug, debug|release) {
    DESTDIR  = ../../Qt4VS2003/Qt4VSAddin/Debug
}

CONFIG(release, debug|release) {
    DESTDIR  = ../../Qt4VS2003/Qt4VSAddin/Release
}

SOURCES += \
    main.cpp

include(qmakefilereader.pri)
