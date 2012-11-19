isEmpty(QT_BUILD_TREE):QT_BUILD_TREE=$(QTDIR)
isEmpty(QT_SOURCE_TREE):QT_SOURCE_TREE=$$fromfile($$QT_BUILD_TREE/.qmake.cache, QT_SOURCE_TREE)

ASSISTANTDIR = $$(QT_SOURCE_TREE)/qttools/src/assistant

TEMPLATE = app

CONFIG += console
QT += xml

SOURCES = $${ASSISTANTDIR}/help/qhelpprojectdata.cpp \
          $${ASSISTANTDIR}/help/qhelpdatainterface.cpp \
          vshelpbuilder.cpp \
          main.cpp
HEADERS = $${ASSISTANTDIR}/help/qhelpprojectdata_p.h \
          $${ASSISTANTDIR}/help/qhelpdatainterface_p.h \
          $${ASSISTANTDIR}/help/qhelpprojectdata_p.h \
          $${ASSISTANTDIR}/help/qhelp_global.h \
          vshelpbuilder.h
		  
INCLUDEPATH += $${ASSISTANTDIR}/help
DEFINES += QHELP_LIB
