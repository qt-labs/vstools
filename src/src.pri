!isEmpty(QTVSOOLS_SRC_PRI) {
    error("src.pri already included")
}
QTVSTOOLS_SRC_PRI = 1

!static {
    !build_pass: message("Using dynamic build. Remember to call \"nmake windeployqt\" after building")
    CONFIG += windeployqt
}
