TEMPLATE = app
INCLUDEPATH += .
TARGET = AddressBook
QT += core gui widgets

HEADERS += adddialog.h \
           addressbook.h
SOURCES += main.cpp \
           adddialog.cpp \
           addressbook.cpp

RC_FILE += AddressBook.rc
FORMS += adddialog.ui addressbook.ui
