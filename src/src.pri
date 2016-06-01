!isEmpty(QT_VS_TOOLS_PRI) {
    error("src.pri already included")
}
QT_VS_TOOLS_PRI = 1

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

!static {
    error("Please use a static Qt to build the tools.")
}

!minQtVersion(5, 6, 0) {
    message("Cannot build Qt VS Tools with Qt version $${QT_VERSION}.")
    error("Use at least Qt 5.6.0.")
}
