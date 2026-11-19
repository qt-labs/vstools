HELPGENERATOR = $$shell_path($$[QT_INSTALL_BINS]/qhelpgenerator) -platform minimal
QDOC_BIN = $$shell_path($$[QT_INSTALL_BINS]/qdoc)
QDOC_GLOBAL = QT_INSTALL_DOCS=$$[QT_INSTALL_DOCS/src] QDOC_INDEX_DIR=$$[QT_INSTALL_DOCS]
COMPAT =

# unset the installdir for qdoc, so we force generation
# of URLs for the links to the Qt documentation
QMAKE_DOCS_INSTALLDIR =

defineReplace(cmdEnv) {
    !equals(QMAKE_DIR_SEP, /): 1 ~= s,^(.*)$,(set \\1) &&,g
    return("$$1")
}

defineReplace(qdoc) {
    return("$$cmdEnv(OUTDIR=$$1 QTVSTOOLS_VERSION=$$QTVSTOOLS_VERSION QTVSTOOLS_VERSION_TAG=$$QTVSTOOLS_VERSION_TAG $$QDOC_GLOBAL) $$QDOC_BIN")
}

QHP_FILE = $$OUT_PWD/doc/html/qtvstools.qhp
QCH_FILE = $$OUT_PWD/doc/qtvstools.qch

HELP_DEP_FILES = $$PWD/src/qtvstools.qdoc $$PWD/src/externallinks.qdoc

html_docs.commands = $$qdoc($$OUT_PWD/doc/html) $$PWD/qtvstools.qdocconf
html_docs.depends += $$HELP_DEP_FILES
html_docs.files = $$QHP_FILE

html_docs_online.commands = $$qdoc($$OUT_PWD/doc/html) $$PWD/qtvstools-online.qdocconf
html_docs_online.depends += $$HELP_DEP_FILES

qch_docs.commands = $$HELPGENERATOR -o \"$$QCH_FILE\" $$QHP_FILE
qch_docs.depends += html_docs

docs_online.depends = html_docs_online
docs.depends = qch_docs
QMAKE_EXTRA_TARGETS += qch_docs html_docs html_docs_online docs docs_online

DISTFILES += \
    $$HELP_DEP_FILES \
    $$PWD/config/qtvstools-project.qdocconf \
    $$PWD/qtvstools.qdocconf \
    $$PWD/qtvstools-online.qdocconf \
