QMAKE_SOURCE_DIR=$$QT_SOURCE_TREE/qmake

INCLUDEPATH += \
    $${QMAKE_SOURCE_DIR} \
    $${QMAKE_SOURCE_DIR}/generators/symbian \
    $$QT_SOURCE_TREE/tools/shared \
    $$PWD

DEFINES += \
    QT_BUILD_QMAKE\
    QT_BUILD_QMAKE_LIBRARY QT_QMAKE_PARSER_ONLY

HEADERS += \
    $$PWD/qmakedataprovider.h

SOURCES += \
    $${QMAKE_SOURCE_DIR}/option.cpp \
    $${QMAKE_SOURCE_DIR}/project.cpp \
    $${QMAKE_SOURCE_DIR}/property.cpp \
    $${QMAKE_SOURCE_DIR}/generators/metamakefile.cpp \
    $${QMAKE_SOURCE_DIR}/generators/symbian/initprojectdeploy_symbian.cpp \
    $$QT_SOURCE_TREE/tools/shared/symbian/epocroot.cpp \
	$$QT_SOURCE_TREE/tools/shared/windows/registry.cpp \
    \
    $$PWD/qmakedataprovider.cpp

LIBS += ole32.lib advapi32.lib
