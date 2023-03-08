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
    main.cpp
)

if(QT_VERSION VERSION_LESS 6.2)
    list(APPEND PROJECT_SOURCES qml.qrc)
endif()

qt_add_executable($projectname$ ${PROJECT_SOURCES})

if(QT_VERSION VERSION_GREATER_EQUAL 6.2)
    qt_add_qml_module($projectname$
        URI $qml_prefix$
        VERSION 1.0
        RESOURCE_PREFIX "/qt/qml/"
        QML_FILES
            main.qml
    )
endif()

set_target_properties($projectname$
    PROPERTIES
        WIN32_EXECUTABLE TRUE
)

target_compile_definitions($projectname$
  PRIVATE
    $<$<OR:$<CONFIG:Debug>,$<CONFIG:RelWithDebInfo>>:QT_QML_DEBUG>
)

target_link_libraries($projectname$
    PUBLIC
        $cmake_qt_libs$
)

if(Qt6_FOUND)
    qt_import_qml_plugins($projectname$)
endif()
