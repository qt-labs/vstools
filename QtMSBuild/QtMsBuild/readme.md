## Qt/MSBuild
Special build rules called Qt/MSBuild let you run Qt build tools and set build options for them. You can also use the tools independently from Qt VS Tools with MSBuild or Visual Studio.

### Using with MSBuild
- Set the `QTMSBUILD` environment variable to this folder.
- Call msbuild with the `-p:QtInstall=[PATH]` property to set the Qt installation to build with.
  - Example: `msbuild -t:Rebuild -p:Configuration=Release -p:QtInstall=C:\Qt\6.8.0\msvc2022_64`.

### Using with Visual Studio
- Set `Menu > Tools > Options > Qt > General > Qt/MSBuild > Path to Qt/MSBuild files` to this folder.

Note that Qt VS Tools already contains the latest Qt/MSBuild, hence this is only necessary if you need a specific version of Qt/MSBuild.

### Support
If you experience any problems, please open an issue at [https://qt-project.atlassian.net/jira/software/c/projects/QTVSADDINBUG/issues]().
