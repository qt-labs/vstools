cmake_minimum_required(VERSION 3.16)
project($projectname$ LANGUAGES CXX)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTORCC ON)
set(CMAKE_AUTOUIC ON)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

set(CMAKE_PREFIX_PATH "$ENV{QTDIR};$ENV{QTDIR}/lib/cmake/Qt5")

find_package(QT NAMES Qt6 Qt5 REQUIRED COMPONENTS Core)
set(Qt${QT_VERSION_MAJOR} "${QT_VERSION_MAJOR}.${QT_VERSION_MINOR}")
find_package(Qt${QT_VERSION_MAJOR} REQUIRED COMPONENTS ${QT_MODULES})
list(TRANSFORM QT_MODULES PREPEND "Qt::" OUTPUT_VARIABLE QT_LIBS)

set(PROJECT_SOURCES
    main.cpp
    qml.qrc
)

if(Qt6)
    qt_add_executable($projectname$ MANUAL_FINALIZATION ${PROJECT_SOURCES})
else()
    add_executable($projectname$ ${PROJECT_SOURCES})
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
        ${QT_LIBS}
)

if(Qt6)
    qt_import_qml_plugins($projectname$)
    qt_finalize_executable($projectname$)
endif()
