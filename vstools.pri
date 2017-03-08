!isEmpty(QTVSOOLS_PRI) {
    error("vstools.pri already included")
}
QTVSTOOLS_PRI = 1
QTVSTOOLS_VERSION = 2.1.1
QTVSTOOLS_VERSION_TAG = 211

defineTest(minQtVersion) {
    maj = $$1
    min = $$2
    patch = $$3
    isEqual(QT_MAJOR_VERSION, $$maj) {
        isEqual(QT_MINOR_VERSION, $$min) {
            isEqual(QT_PATCH_VERSION, $$patch) {
                return(true)
            }
            greaterThan(QT_PATCH_VERSION, $$patch) {
                return(true)
            }
        }
        greaterThan(QT_MINOR_VERSION, $$min) {
            return(true)
        }
    }
    greaterThan(QT_MAJOR_VERSION, $$maj) {
        return(true)
    }
    return(false)
}

!minQtVersion(5, 6, 0) {
    message("Cannot build Qt VS Tools with Qt version $${QT_VERSION}.")
    error("Use at least Qt 5.6.0.")
}
