isEmpty(QT_BUILD_TREE):QT_BUILD_TREE=$(QTDIR)
isEmpty(QT_SOURCE_TREE):QT_SOURCE_TREE=$$fromfile($$QT_BUILD_TREE/.qmake.cache, QT_SOURCE_TREE)

ASSISTANTDIR = $${QT_SOURCE_TREE}/tools/assistant

TEMPLATE = app

CONFIG += console
QT += xml

SOURCES = $${ASSISTANTDIR}/lib/qhelpprojectdata.cpp \
          $${ASSISTANTDIR}/lib/qhelpdatainterface.cpp \
          vshelpbuilder.cpp \
          main.cpp
HEADERS = $${ASSISTANTDIR}/lib/qhelpprojectdata_p.h \
          $${ASSISTANTDIR}/lib/qhelpdatainterface_p.h \
          $${ASSISTANTDIR}/lib/qhelpprojectdata_p.h \
          $${ASSISTANTDIR}/lib/qhelp_global.h \
          vshelpbuilder.h
		  
INCLUDEPATH += $${ASSISTANTDIR}/lib
DEFINES += QHELP_LIB
