cmake_minimum_required(VERSION 3.16)
project(QtClassLibrary LANGUAGES CXX)

include(qt.cmake)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

find_package(QT NAMES Qt6 Qt5 REQUIRED COMPONENTS Core)
find_package(Qt${QT_VERSION_MAJOR}
    COMPONENTS
        Core
)
qt_standard_project_setup()

set(PROJECT_SOURCES
    qtclasslibrary_global.h
    QtClassLibrary.h
    QtClassLibrary.cpp
)

add_library(QtClassLibrary SHARED ${PROJECT_SOURCES})

target_link_libraries(QtClassLibrary
    PRIVATE
        Qt::Core
)

target_compile_definitions(QtClassLibrary
    PRIVATE
        QTCLASSLIBRARY_LIB
)
