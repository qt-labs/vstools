# Qt Visual Studio Tools

The Qt Visual Studio Tools integrate the Qt development tools into Microsoft Visual Studio. This
enables developers to use the standard Windows development environment without having to worry
about Qt-related build steps or tools.

<!--TOC-->
  - [Sources](#sources)
  - [Qt installation](#qt-installation)
    - [Building Qt from sources](#building-qt-from-sources)
    - [32-bit or 64-bit](#32-bit-or-64-bit)
  - [Build](#build)
    - [Requirements](#requirements)
    - [Environment variables](#environment-variables)
    - [Initialization](#initialization)
    - [Target platform](#target-platform)
  - [Debug](#debug)
  - [Documentation](#documentation)
<!--/TOC-->

## Sources

Use Git to check out the
[Qt Visual Studio Tools sources](https://code.qt.io/cgit/qt-labs/vstools.git), using one of the
following options:

    git clone git://code.qt.io/qt-labs/vstools.git

    git clone https://code.qt.io/qt-labs/vstools.git

Contributions to the Qt Visual Studio Tools project must be submitted to the
[`qt-labs/vstools`](https://codereview.qt-project.org/admin/repos/qt-labs/vstools) Gerrit
repository. For instructions on how to set up a Gerrit account and contribute to Qt projects, refer
to the wiki page ["Setting up Gerrit"](https://wiki.qt.io/Setting_up_Gerrit).

## Qt installation

To build the Qt Visual Studio Tools, an installation of Qt is required. The version of Qt that is
currently supported is 5.12.9. Either build Qt from the sources available in the
[Qt Project Git Repository Browser](https://code.qt.io/cgit/qt/qt5.git/tag/?h=v5.12.9)
or install a [pre-built binary package](https://download.qt.io/official_releases/qt/5.12/5.12.9/).

### Building Qt from sources

See the [Qt documentation](https://wiki.qt.io/Building_Qt_5_from_Git#Windows) for the prerequisites
and steps to build Qt from sources.

Recommended options for the `configure` tool:

    configure -static -opensource -confirm-license -nomake examples -nomake tests -opengl desktop

Recommended options for [jom](https://wiki.qt.io/Jom):

    jom module-qtbase module-qtdeclarative

### 32-bit or 64-bit

Visual Studio 2022 is a 64-bit application, whereas VS 2019 and 2017 are 32-bit applications. The
target platform for which Qt is built must reflect this:

- For Visual Studio 2022, use Qt built for the x64 platform.

- For Visual Studio 2019, use Qt built for the x86 platform.

- For Visual Studio 2017, use Qt built for the x86 platform.

## Build

After cloning the repository, follow the instructions below to build the Qt Visual Studio Tools.

### Requirements

The following is required in order to build the Qt Visual Studio solution:

- Visual Studio 2017, 2019 or 2022, with the following workloads (A .vsconfig file per VS version can be found in the source tree):
    - Desktop development with C++
    - .NET desktop development
    - [Visual Studio extension development](https://docs.microsoft.com/en-us/visualstudio/extensibility/installing-the-visual-studio-sdk)
    - [Linux development with C++](https://devblogs.microsoft.com/cppblog/linux-development-with-c-in-visual-studio/)

- [`vswhere` tool](https://github.com/microsoft/vswhere) (usually installed with Visual Studio):
    - [Version 2.7.1](https://github.com/microsoft/vswhere/releases/tag/2.7.1) or greater.

- `git` must be installed and included in the `PATH` environment variable.

### Environment variables

Set environment variables `QTBUILD_STATIC_VS`_`nnnn`_ according to the installed VS versions, i.e.:
- `QTBUILD_STATIC_VS2017` = _path to Qt installation built with msvc2017_
- `QTBUILD_STATIC_VS2019` = _path to Qt installation built with msvc2019_
- `QTBUILD_STATIC_VS2022` = _path to Qt installation built with msvc2022_

For example, assuming Qt is installed in the following directory tree:

    C:
    +--- build
         +--- qt_5.12.9_msvc2019_x86
         |    +--- bin
         |    +--- include
         |    +--- lib
         |    (etc.)
         |
         +--- qt_5.12.9_msvc2022_x64
              +--- bin
              +--- include
              +--- lib
              (etc.)

In this case, the following environment variables must be set:

    QTBUILD_STATIC_VS2019=C:\build\qt_5.12.9_msvc2019_x86
    QTBUILD_STATIC_VS2022=C:\build\qt_5.12.9_msvc2022_x64

### Initialization

In a command prompt (a "regular" one, *not* a VS Developer/Native Tools prompt), `CD` to the
root of the repository and run `vstools.bat` to initialize the solution and open it in Visual
Studio, with the following arguments:

    C:\...\vstools> vstools -init -startvs

This will:
- Delete all output files;
- Restore NuGet packages;
- Run an initial text template generation;
- Open the solution in the VS IDE, ready to build/debug.

This procedure must be repeated when opening the solution on another
version of VS. For example, assuming VS 2022 and VS 2019 are
installed, to open the solution in VS 2019 after it has already been
initialized and used in VS 2022, run the following:

    C:\...\vstools> vstools -vs2019 -init -startvs

By default, if no VS version is specified, the most recent version is selected.

### Target platform

The solution platform must be set to `'x64'` for VS 2022, and `'x86'`
or `'Any CPU'` for VS 2019 and VS 2017.

## Debug

To debug the Qt Visual Studio tools extension, the
`QtVsTools.Package` project must be set as the
startup project. Also, the target binary for the debug session must
be set to the Visual Studio executable (`devenv.exe`), with the
option to start an
[experimental instance](https://docs.microsoft.com/en-us/visualstudio/extensibility/the-experimental-instance).

Follow these instructions to configure the solution for debug:

- In the solution explorer: right-click QtVsTools.Package > Set as
startup
- In the solution explorer: right-click QtVsTools.Package > Properties
- In the properties dialog: select the Debug page
- In the debug properties page, set the following options:
    - _Start external program_ = path to the Visual Studio executable
    (`devenv.exe`).
    - _Command line arguments_ = `/rootSuffix Exp`.

## Documentation

To build the Qt Visual Studio Tools documentation, run
`qmake && jom docs` from the root directory of the `vstools`
repository. You need to have `qdoc` and friends built already.

See the
[Qt documentation](https://wiki.qt.io/Building_Qt_Documentation) for
the prerequisites and steps to build Qt documentation from sources.
