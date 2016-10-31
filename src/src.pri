!isEmpty(QTVSOOLS_SRC_PRI) {
    error("src.pri already included")
}
QTVSTOOLS_SRC_PRI = 1

!static {
    error("Please use a static Qt to build the tools.")
}
