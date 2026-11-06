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

    git clone --recurse-submodules git://code.qt.io/qt-labs/vstools.git

    git clone --recurse-submodules https://code.qt.io/qt-labs/vstools.git

Contributions to the Qt Visual Studio Tools project must be submitted to the
[`qt-labs/vstools`](https://codereview.qt-project.org/admin/repos/qt-labs/vstools) Gerrit
repository. For instructions on how to set up a Gerrit account and contribute to Qt projects, refer
to the wiki page ["Setting up Gerrit"](https://wiki.qt.io/Setting_up_Gerrit).

## Qt installation

To build the Qt Visual Studio Tools, a statically-linked installation of Qt is required. The version
that is currently supported is [5.15.9](https://code.qt.io/cgit/qt/qt5.git/tag/?h=v5.15.9-lts-lgpl).

### Building Qt from sources

See the [Qt documentation](https://wiki.qt.io/Building_Qt_5_from_Git#Windows) for the prerequisites
and steps to build Qt from sources.

Recommended options for the `configure` tool:

    configure -static -opensource -confirm-license -nomake examples -nomake tests -no-opengl

Recommended options for [jom](https://wiki.qt.io/Jom):

    jom module-qtbase module-qtdeclarative

### Toolset and platform

Build Qt with the Visual Studio 2022 toolset (MSVC v143), the toolset the extension is built
with, for the architecture of Visual Studio the machine builds the extension for: x64, or arm64
for Visual Studio on ARM (a native build on an ARM64 machine, configured with
`-platform win32-msvc`). A static Qt built with a newer toolset cannot be linked.

## Build

After cloning the repository, follow the instructions below to build the Qt Visual Studio Tools.

### Requirements

The following is required in order to build the Qt Visual Studio solution:

- Visual Studio 2022 version 17.14 or later, with the following workloads:
    - Desktop development with C++
    - .NET desktop development
    - [Visual Studio extension development](https://docs.microsoft.com/en-us/visualstudio/extensibility/installing-the-visual-studio-sdk)
    - [Linux development with C++](https://devblogs.microsoft.com/cppblog/linux-development-with-c-in-visual-studio/)

- [`vswhere` tool](https://github.com/microsoft/vswhere) (usually installed with Visual Studio):
    - [Version 2.7.1](https://github.com/microsoft/vswhere/releases/tag/2.7.1) or greater.

- `git` must be installed and included in the `PATH` environment variable.

### Environment variables

Set the environment variable `QTBUILD_STATIC_VS2022` to the path of the static Qt installation
built with msvc2022 for the architecture this machine builds the extension for. For example,
assuming Qt is installed in the following directory tree:

    C:
    +--- build
         +--- qt_5.15.9_msvc2022_x64
              +--- bin
              +--- include
              +--- lib
              (etc.)

In this case, the following environment variable must be set:

    QTBUILD_STATIC_VS2022=C:\build\qt_5.15.9_msvc2022_x64

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

By default, Visual Studio 2022 17.14 or later is selected. The extension built with it supports
Visual Studio 2022 (17.14 or later) and Visual Studio 2026; there are no separate builds per
Visual Studio version.

### Target platform

The solution platform selects the architecture of Visual Studio the extension is built for:
`'x64'` or `'arm64'`.

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
