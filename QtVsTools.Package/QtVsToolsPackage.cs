/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Tools.
**
** $QT_BEGIN_LICENSE:GPL-EXCEPT$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see https://www.qt.io/terms-conditions. For further
** information use the contact form at https://www.qt.io/contact-us.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3 as published by the Free Software
** Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
** included in the packaging of this file. Please review the following
** information to ensure the GNU General Public License requirements will
** be met: https://www.gnu.org/licenses/gpl-3.0.html.
**
** $QT_END_LICENSE$
**
****************************************************************************/

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;
using EnvDTE;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools
{
    using Core;
    using QtMsBuild;
    using VisualStudio;

    using static SyntaxAnalysis.RegExpr;

    [Guid(QtVsToolsLegacyPackage.PackageGuidString)]
    [InstalledProductRegistration("#110", "#112", Version.PRODUCT_VERSION, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]

    // Custom editor: Qt Designer
    [ProvideEditorExtension(typeof(Editors.QtDesigner),
        extension: ".ui",
        priority: 999,
        DefaultName = Editors.QtDesigner.Title)]
    [ProvideEditorLogicalView(typeof(Editors.QtDesigner),
        logicalViewGuid: VSConstants.LOGVIEWID.TextView_string)]

    // Custom editor: Qt Linguist
    [ProvideEditorExtension(typeof(Editors.QtLinguist),
        extension: ".ts",
        priority: 999,
        DefaultName = Editors.QtLinguist.Title)]
    [ProvideEditorLogicalView(typeof(Editors.QtLinguist),
        logicalViewGuid: VSConstants.LOGVIEWID.TextView_string)]

    // Custom editor: Qt Resource Editor
    [ProvideEditorExtension(typeof(Editors.QtResourceEditor),
        extension: ".qrc",
        priority: 999,
        DefaultName = Editors.QtResourceEditor.Title)]
    [ProvideEditorLogicalView(typeof(Editors.QtResourceEditor),
        logicalViewGuid: VSConstants.LOGVIEWID.TextView_string)]

    // Options page
    [ProvideOptionPage(typeof(Options.QtOptionsPage),
        "Qt", "General", 0, 0, true, Sort = 0)]

    // Qt Versions page
    [ProvideOptionPage(typeof(Options.QtVersionsPage),
        "Qt", "Versions", 0, 0, true, Sort = 1)]

    // Legacy options page
    [ProvideOptionPage(typeof(Legacy.QtOptionsPage),
        "Qt", "Legacy Project Format", 0, 0, true, Sort = 2)]

    public sealed class QtVsToolsLegacyPackage : AsyncPackage, IVsServiceProvider, IProjectTracker
    {
        public const string PackageGuidString = "6E7FA583-5FAA-4EC9-9E90-4A0AE5FD61EE";
        const StringComparison IGNORE_CASE = StringComparison.InvariantCultureIgnoreCase;

        public DTE Dte { get; private set; }
        public string PkgInstallPath { get; private set; }
        public Options.QtOptionsPage Options
            => GetDialogPage(typeof(Options.QtOptionsPage)) as Options.QtOptionsPage;
        public Legacy.QtOptionsPage LegacyOptions
            => GetDialogPage(typeof(Legacy.QtOptionsPage)) as Legacy.QtOptionsPage;
        public Editors.QtDesigner QtDesigner { get; private set; }
        public Editors.QtLinguist QtLinguist { get; private set; }
        private Editors.QtResourceEditor QtResourceEditor { get; set; }

        static readonly EventWaitHandle initDone = new EventWaitHandle(false, EventResetMode.ManualReset);

        static QtVsToolsLegacyPackage instance = null;
        public static QtVsToolsLegacyPackage Instance
        {
            get
            {
                initDone.WaitOne();
                return instance;
            }
        }

        private string locateHelperExecutable(string exeName)
        {
            if (!string.IsNullOrEmpty(PkgInstallPath) && File.Exists(PkgInstallPath + exeName))
                return PkgInstallPath + exeName;
            return null;
        }

        private string _QMakeFileReaderPath;
        public string QMakeFileReaderPath
        {
            get
            {
                if (_QMakeFileReaderPath == null)
                    _QMakeFileReaderPath = locateHelperExecutable("QMakeFileReader.exe");
                return _QMakeFileReaderPath;
            }
        }

        static readonly Stopwatch initTimer = Stopwatch.StartNew();
        static readonly HttpClient http = new HttpClient();
        const string urlDownloadQtIo = "https://download.qt.io/development_releases/vsaddin/";

        private DteEventsHandler eventHandler;
        private bool useQtTmLanguage;
        private string qtTmLanguagePath;
        private string visualizersPath;


        public QtVsToolsLegacyPackage()
        {
        }

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            try {
                var timeInitBegin = initTimer.Elapsed;
                VsServiceProvider.Instance = instance = this;
                QtProject.ProjectTracker = this;

                // determine the package installation directory
                var uri = new Uri(System.Reflection.Assembly
                    .GetExecutingAssembly().EscapedCodeBase);
                PkgInstallPath = Path.GetDirectoryName(
                    Uri.UnescapeDataString(uri.AbsolutePath)) + @"\";

                ///////////////////////////////////////////////////////////////////////////////////
                // Switch to main (UI) thread
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                var timeUiThreadBegin = initTimer.Elapsed;

                if ((Dte = await VsServiceProvider.GetServiceAsync<DTE>()) == null)
                    throw new Exception("Unable to get service: DTE");

                QtVSIPSettings.Options = Options;

                eventHandler = new DteEventsHandler(Dte);

                Qml.Debug.Launcher.Initialize();
                QtMainMenu.Initialize();
                QtSolutionContextMenu.Initialize();
                QtProjectContextMenu.Initialize();
                QtItemContextMenu.Initialize();
                RegisterEditorFactory(QtDesigner = new Editors.QtDesigner());
                RegisterEditorFactory(QtLinguist = new Editors.QtLinguist());
                RegisterEditorFactory(QtResourceEditor = new Editors.QtResourceEditor());
                QtHelp.Initialize();

                if (!string.IsNullOrEmpty(VsShell.InstallRootDir))
                    HelperFunctions.VCPath = Path.Combine(VsShell.InstallRootDir, "VC");

                GetTextMateLanguagePath();
                GetNatvisPath();

                ///////////////////////////////////////////////////////////////////////////////////
                // Switch to background thread
                await TaskScheduler.Default;
                var timeUiThreadEnd = initTimer.Elapsed;

                var vm = QtVersionManager.The(initDone);
                if (vm.HasInvalidVersions(out string error, out bool defaultInvalid)) {
                    if (defaultInvalid)
                        vm.SetLatestQtVersionAsDefault();
                    Messages.Print(error);
                }

                ///////////
                // Install Qt/MSBuild files from package folder to standard location
                //  -> %LOCALAPPDATA%\QtMsBuild
                //
                var QtMsBuildDefault = Path.Combine(
                    Environment.GetEnvironmentVariable("LocalAppData"), "QtMsBuild");
                try {
                    var qtMsBuildDefaultUri = new Uri(QtMsBuildDefault + Path.DirectorySeparatorChar);
                    var qtMsBuildVsixPath = Path.Combine(PkgInstallPath, "QtMsBuild");
                    var qtMsBuildVsixUri = new Uri(qtMsBuildVsixPath + Path.DirectorySeparatorChar);
                    if (qtMsBuildVsixUri != qtMsBuildDefaultUri) {
                        var qtMsBuildVsixFiles = Directory
                            .GetFiles(qtMsBuildVsixPath, "*", SearchOption.AllDirectories)
                            .Select(x => qtMsBuildVsixUri.MakeRelativeUri(new Uri(x)));
                        foreach (var qtMsBuildFile in qtMsBuildVsixFiles) {
                            var sourcePath = new Uri(qtMsBuildVsixUri, qtMsBuildFile).LocalPath;
                            var targetPath = new Uri(qtMsBuildDefaultUri, qtMsBuildFile).LocalPath;
                            var targetPathTemp = targetPath + ".tmp";
                            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                            File.Copy(sourcePath, targetPathTemp, overwrite: true);
                            ////////
                            // Copy Qt/MSBuild files to standard location, taking care not to
                            // overwrite the updated Qt props file, possibly containing user-defined
                            // build settings (written by the VS Property Manager). This file is
                            // recognized as being named "Qt.props" and containing the import
                            // statement for qt_private.props.
                            //
                            string qtPrivateImport =
                                @"<Import Project=""$(MSBuildThisFileDirectory)\qt_private.props""";
                            Func<string, bool> isUpdateQtProps = (string filePath) =>
                            {
                                return Path.GetFileName(targetPath).Equals("Qt.props", IGNORE_CASE)
                                    && File.ReadAllText(targetPath).Contains(qtPrivateImport);
                            };
                            if (!File.Exists(targetPath)) {
                                // Target file does not exist
                                //  -> Create new
                                File.Move(targetPathTemp, targetPath);
                            } else if (!isUpdateQtProps(targetPath)) {
                                // Target file is not the updated Qt.props
                                //  -> Overwrite
                                File.Replace(targetPathTemp, targetPath, null);
                            } else {
                                // Target file *is* the updated Qt.props; skip!
                                //  -> Remove temp file
                                File.Delete(targetPathTemp);
                            }
                        }
                    }
                } catch {
                    /////////
                    // Error copying files to standard location.
                    //  -> FAIL-SAFE: use source folder (within package) as the standard location
                    QtMsBuildDefault = Path.Combine(PkgInstallPath, "QtMsBuild");
                }

                ///////
                // Set %QTMSBUILD% by default to point to standard location of Qt/MSBuild
                //
                var QtMsBuildPath = Environment.GetEnvironmentVariable("QtMsBuild");
                if (string.IsNullOrEmpty(QtMsBuildPath)) {

                    Environment.SetEnvironmentVariable(
                        "QtMsBuild", QtMsBuildDefault,
                        EnvironmentVariableTarget.User);

                    Environment.SetEnvironmentVariable(
                        "QtMsBuild", QtMsBuildDefault,
                        EnvironmentVariableTarget.Process);
                }

                CopyTextMateLanguageFiles();
                CopyNatvisFiles();

                Messages.Print(string.Format("\r\n"
                    + "== LEGACY Qt Visual Studio Tools version {0}\r\n"
                    + "\r\n"
                    + "   Initialized in: {1:0.##} msecs\r\n"
                    + "   Main (UI) thread: {2:0.##} msecs\r\n"
                    , Version.USER_VERSION
                    , (initTimer.Elapsed - timeInitBegin).TotalMilliseconds
                    , (timeUiThreadEnd - timeUiThreadBegin).TotalMilliseconds
                    ));

            } catch (Exception exception) {
                exception.Log();
            } finally {
                initDone.Set();
                initTimer.Stop();
            }

            ///////////////////////////////////////////////////////////////////////////////////
            // Switch to main (UI) thread
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            /////////
            // Check if a solution was opened during initialization.
            // If so, fire solution open event.
            //
            if (Dte?.Solution?.IsOpen == true)
                eventHandler.SolutionEvents_Opened();
        }

        bool TestVersionInstalled()
        {
            bool newVersion = false;
            string versionFile = Path.Combine(PkgInstallPath, "lastversion.txt");
            if (File.Exists(versionFile)) {
                string lastVersion = File.ReadAllText(versionFile);
                newVersion = (lastVersion!= Version.PRODUCT_VERSION);
            } else {
                newVersion = true;
            }
            if (newVersion)
                File.WriteAllText(versionFile, Version.PRODUCT_VERSION);
            return newVersion;
        }

        public void VsMainWindowActivated()
        {
            if (QtVersionManager.The().GetVersions()?.Length == 0)
                Notifications.NoQtVersion.Show();
            if (Options.NotifyInstalled && TestVersionInstalled())
                Notifications.NotifyInstall.Show();
        }

        protected override int QueryClose(out bool canClose)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (eventHandler != null)
                eventHandler.Disconnect();
            return base.QueryClose(out canClose);
        }

        private void GetTextMateLanguagePath()
        {
            var settingsManager = new ShellSettingsManager(this as System.IServiceProvider);
            var store = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
            useQtTmLanguage = store.GetBoolean(@"QtVsTools\Qml\TextMate", @"Enable", true);
            qtTmLanguagePath = Environment.
                ExpandEnvironmentVariables("%USERPROFILE%\\.vs\\Extensions\\qttmlanguage");
        }

        private void CopyTextMateLanguageFiles()
        {
            if (useQtTmLanguage) {
                HelperFunctions.CopyDirectory(Path.Combine(PkgInstallPath, "qttmlanguage"),
                    qtTmLanguagePath);
            } else {
                Directory.Delete(qtTmLanguagePath, true);
            }

            //Remove textmate-based QML syntax highlighting
            var qmlTextmate = Path.Combine(qtTmLanguagePath, "qml");
            if (Directory.Exists(qmlTextmate)) {
                try {
                    Directory.Delete(qmlTextmate, true);
                } catch { }
            }
        }

        private void CopyNatvisFile(string filename, string qtNamespace)
        {
            try {
                string natvis = File.ReadAllText(Path.Combine(PkgInstallPath, filename));

                string natvisFile;
                if (string.IsNullOrEmpty(qtNamespace)) {
                    natvis = natvis.Replace("##NAMESPACE##::", string.Empty);
                    natvisFile = Path.GetFileNameWithoutExtension(filename);
                } else {
                    natvis = natvis.Replace("##NAMESPACE##", qtNamespace);
                    natvisFile = string.Format(filename.Substring(0, filename.IndexOf('.'))
                        + "_{0}.natvis", qtNamespace.Replace("::", "_"));
                }

                if (!Directory.Exists(visualizersPath))
                    Directory.CreateDirectory(visualizersPath);

                File.WriteAllText(Path.Combine(visualizersPath, natvisFile),
                    natvis, System.Text.Encoding.UTF8);
            } catch (Exception exception) {
                exception.Log();
            }
        }

        public string GetNatvisPath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try {
                using (var vsRootKey = Registry.CurrentUser.OpenSubKey(Dte.RegistryRoot)) {
                    if (vsRootKey.GetValue("VisualStudioLocation") is string vsLocation)
                        visualizersPath = Path.Combine(vsLocation, "Visualizers");
                }
            } catch {
            }
            if (string.IsNullOrEmpty(visualizersPath)) {
                visualizersPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
#if VS2022
                    @"Visual Studio 2022\Visualizers\");
#elif VS2019
                    @"Visual Studio 2019\Visualizers\");
#elif VS2017
                    @"Visual Studio 2017\Visualizers\");
#endif
            }
            return visualizersPath;
        }

        public void CopyNatvisFiles(string qtNamespace = null)
        {
            string[] files = { "qt5.natvis.xml", "qt6.natvis.xml" };
            foreach (var file in files)
                CopyNatvisFile(file, qtNamespace);
        }

        public I GetService<T, I>()
            where T : class
            where I : class
        {
            return GetService(typeof(T)) as I;
        }

        public async Task<I> GetServiceAsync<T, I>()
            where T : class
            where I : class
        {
            return await GetServiceAsync(typeof(T)) as I;
        }

        void IProjectTracker.AddProject(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            QtProjectTracker.Add(project);
        }

        async Task<string> GetLatestDevelopmentReleaseAsync()
        {
            var currentVersion = new System.Version(Version.PRODUCT_VERSION);
            try {
                var response = await http.GetAsync(urlDownloadQtIo);
                if (!response.IsSuccessStatusCode)
                    return null;

                var tokenVersion = new Token("VERSION", Number & "." & Number & "." & Number)
                {
                    new Rule<System.Version> { Capture(value => new System.Version(value)) }
                };
                var regexHrefVersion = "href=\"" & tokenVersion & Chars["/"].Optional() & "\"";
                var regexResponse = (regexHrefVersion | AnyChar | VertSpace).Repeat();
                var parserResponse = regexResponse.Render();

                var responseData = await response.Content.ReadAsStringAsync();
                var devVersion = parserResponse.Parse(responseData)
                    .GetValues<System.Version>("VERSION")
                    .Where(v => currentVersion < v)
                    .Max();
                if (devVersion == null)
                    return null;

                response = await http.GetAsync(
                    string.Format("{0}{1}/", urlDownloadQtIo, devVersion));
                if (!response.IsSuccessStatusCode)
                    return null;
                return devVersion.ToString();
            } catch {
                return null;
            }
        }
    }
}
