TEMPLATE = lib

QT += axserver
CONFIG += qaxserver_no_postlink release
TARGET = q5makewrapper
DESTDIR = ./
VERSION = 1.0.0
DEF_FILE = qmakewrapper.def
RC_FILE = qmakewrapper.rc

NEWLINE = $$escape_expand(\\n\\t)

!isEmpty(QMAKE_POST_LINK):QMAKE_POST_LINK += $$NEWLINE
QMAKE_POST_LINK += $$quote(idc q5makewrapper1.dll /idl q5makewrapper.idl -version 1.0.0$$NEWLINE)
QMAKE_POST_LINK += $$quote(midl q5makewrapper.idl /nologo /tlb q5makewrapper.tlb$$NEWLINE)
QMAKE_POST_LINK += $$quote(idc q5makewrapper1.dll /tlb q5makewrapper.tlb$$NEWLINE)
QMAKE_POST_LINK += $$quote(idc q5makewrapper1.dll /regserver$$NEWLINE)
QMAKE_POST_LINK += $$quote(aximp q5makewrapper1.dll)
QMAKE_CLEAN += q5makewrapper.idl q5makewrapper.tlb

INCLUDEPATH += ../qmake

HEADERS = qmakewrapper.h
           
SOURCES = qmakewrapper.cpp \
          axfactory.cpp

include(../qmake/qmake.pri)
