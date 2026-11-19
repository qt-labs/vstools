cmake_minimum_required(VERSION 3.16)
project(QtWidgetsApplication LANGUAGES CXX)

include(qt.cmake)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

find_package(QT NAMES Qt6 Qt5 REQUIRED COMPONENTS Core)
find_package(Qt${QT_VERSION_MAJOR}
    COMPONENTS
        Core
        Gui
        Widgets
)
qt_standard_project_setup()

set(PROJECT_SOURCES
    main.cpp
    QtWidgetsApplication.ui
    QtWidgetsApplication.h
    QtWidgetsApplication.cpp
)

if(QT_VERSION VERSION_LESS 5.15)
    qt5_add_resources(PROJECT_SOURCES QtWidgetsApplication.qrc)
else()
    qt_add_resources(PROJECT_SOURCES QtWidgetsApplication.qrc)
endif()

qt_add_executable(QtWidgetsApplication ${PROJECT_SOURCES})

set_target_properties(QtWidgetsApplication
    PROPERTIES
        WIN32_EXECUTABLE TRUE
)

target_link_libraries(QtWidgetsApplication
    PUBLIC
        Qt::Core
        Qt::Gui
        Qt::Widgets
)
