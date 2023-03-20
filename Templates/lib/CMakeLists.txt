cmake_minimum_required(VERSION 3.16)
project($projectname$ LANGUAGES CXX)

include(qt.cmake)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

find_package(QT NAMES Qt6 Qt5 REQUIRED COMPONENTS Core)
find_package(Qt${QT_VERSION_MAJOR}
    COMPONENTS
        $cmake_qt_modules$
)
qt_standard_project_setup()

set(PROJECT_SOURCES
    $saveglobal$_global.h
    $headerfilename$
    $sourcefilename$
)

add_library($projectname$ $cmake_static$ ${PROJECT_SOURCES})

target_link_libraries($projectname$
    PRIVATE
        $cmake_qt_libs$
)

target_compile_definitions($projectname$
    PRIVATE
        $pro_lib_define$
)
