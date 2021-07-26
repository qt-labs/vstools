QT += network testlib

INCLUDEPATH += $(LOCALAPPDATA)\qtvstest
DEFINES += "\"QT_CONF_PATH=\\\"$$QMAKESPEC/qt.conf\\\"\""

RESOURCES += \
    Macros.qrc

SOURCES += \
    main.cpp
