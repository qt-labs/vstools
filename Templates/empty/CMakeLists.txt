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

qt_add_executable($projectname$ ${PROJECT_SOURCES})

set_target_properties($projectname$
    PROPERTIES
        WIN32_EXECUTABLE TRUE
)

target_link_libraries($projectname$
    PUBLIC
        $cmake_qt_libs$
)
