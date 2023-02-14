/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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

    [Guid(QtVsToolsPackage.PackageGuidString)]
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

    public sealed class QtVsToolsPackage : AsyncPackage, IVsServiceProvider, IProjectTracker
    {
        private const string PackageGuidString = "15021976-647e-4876-9040-2507afde45d2";

        public DTE Dte { get; private set; }
        public string PkgInstallPath { get; private set; }
        public Options.QtOptionsPage Options
            => GetDialogPage(typeof(Options.QtOptionsPage)) as Options.QtOptionsPage;
        public Editors.QtDesigner QtDesigner { get; private set; }
        public Editors.QtLinguist QtLinguist { get; private set; }
        private Editors.QtResourceEditor QtResourceEditor { get; set; }

        private static readonly EventWaitHandle InitDone = new(false, EventResetMode.ManualReset);

        private static QtVsToolsPackage _instance;
        public static QtVsToolsPackage Instance
        {
            get
            {
                InitDone.WaitOne();
                return _instance;
            }
        }

        private static readonly Stopwatch initTimer = Stopwatch.StartNew();
        private static readonly HttpClient http = new();
        private const string urlDownloadQtIo = "https://download.qt.io/development_releases/vsaddin/";

        private DteEventsHandler eventHandler;
        private string VisualizersPath { get; set; }


        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            try {
                var timeInitBegin = initTimer.Elapsed;
                VsServiceProvider.Instance = _instance = this;
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

                SetVisualizersPathProperty();

                ///////////////////////////////////////////////////////////////////////////////////
                // Switch to background thread
                await TaskScheduler.Default;
                var timeUiThreadEnd = initTimer.Elapsed;

                var vm = QtVersionManager.The(InitDone);
                if (vm.HasInvalidVersions(out var error, out var defaultInvalid)) {
                    if (defaultInvalid)
                        vm.SetLatestQtVersionAsDefault();
                    Messages.Print(error);
                }

                ///////////
                // Install Qt/MSBuild files from package folder to standard location
                //  -> %LOCALAPPDATA%\QtMsBuild
                //
                var qtMsBuildDefault = Path.Combine(
                    Environment.GetEnvironmentVariable("LocalAppData") ?? "", "QtMsBuild");
                try {
                    var qtMsBuildDefaultUri = new Uri(qtMsBuildDefault + Path.DirectorySeparatorChar);
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
                            Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? "");
                            File.Copy(sourcePath, targetPathTemp, overwrite: true);
                            ////////
                            // Copy Qt/MSBuild files to standard location, taking care not to
                            // overwrite the updated Qt props file, possibly containing user-defined
                            // build settings (written by the VS Property Manager). This file is
                            // recognized as being named "Qt.props" and containing the import
                            // statement for qt_private.props.
                            //
                            const string qtPrivateImport
                                = @"<Import Project=""$(MSBuildThisFileDirectory)\qt_private.props""";
                            Func<string, bool> isUpdateQtProps = _ => Path.GetFileName(targetPath)
                                    .Equals("Qt.props", StringComparison.OrdinalIgnoreCase)
                                && File.ReadAllText(targetPath).Contains(qtPrivateImport);

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
                    qtMsBuildDefault = Path.Combine(PkgInstallPath, "QtMsBuild");
                }

                ///////
                // Set %QTMSBUILD% by default to point to standard location of Qt/MSBuild
                //
                var QtMsBuildPath = Environment.GetEnvironmentVariable("QtMsBuild");
                if (string.IsNullOrEmpty(QtMsBuildPath)) {

                    Environment.SetEnvironmentVariable(
                        "QtMsBuild", qtMsBuildDefault,
                        EnvironmentVariableTarget.User);

                    Environment.SetEnvironmentVariable(
                        "QtMsBuild", qtMsBuildDefault,
                        EnvironmentVariableTarget.Process);
                }

                CopyTextMateLanguageFiles();
                CopyVisualizersFiles();

                Messages.Print($"\r\n== Qt Visual Studio Tools version {Version.USER_VERSION}\r\n"
                    + "\r\n   Initialized in: "
                    + $"{(initTimer.Elapsed - timeInitBegin).TotalMilliseconds:0.##} msecs"
                    + "\r\n   Main (UI) thread: "
                    + $"{(timeUiThreadEnd - timeUiThreadBegin).TotalMilliseconds:0.##} msecs\r\n");

                var devRelease = await GetLatestDevelopmentReleaseAsync();
                if (devRelease != null) {
                    Messages.Print($@"
    ================================================================
      Qt Visual Studio Tools version {devRelease} PREVIEW available at:
      {urlDownloadQtIo}{devRelease}/
    ================================================================");
                }
            } catch (Exception exception) {
                exception.Log();
            } finally {
                InitDone.Set();
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

        private bool TestVersionInstalled()
        {
            var newVersion = true;
            var versionFile = Path.Combine(PkgInstallPath, "lastversion.txt");
            if (File.Exists(versionFile)) {
                var lastVersion = File.ReadAllText(versionFile);
                newVersion = lastVersion!= Version.PRODUCT_VERSION;
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

            eventHandler?.Disconnect();
            return base.QueryClose(out canClose);
        }

        private void CopyTextMateLanguageFiles()
        {
            var qtTmLanguagePath = Environment.
                ExpandEnvironmentVariables("%USERPROFILE%\\.vs\\Extensions\\qttmlanguage");
            HelperFunctions.CopyDirectory(Path.Combine(PkgInstallPath, "qttmlanguage"),
                qtTmLanguagePath); // always copy .pri/.pro TextMate Language Grammar file

            try { //Remove TextMate-based QML syntax highlighting
                var qmlTextMate = Path.Combine(qtTmLanguagePath, "qml");
                if (Directory.Exists(qmlTextMate))
                    Directory.Delete(qmlTextMate, true);
            } catch {}
        }

        private void SetVisualizersPathProperty()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try {
                using var vsRootKey = Registry.CurrentUser.OpenSubKey(Dte.RegistryRoot);
                if (vsRootKey?.GetValue("VisualStudioLocation") is string vsLocation)
                    VisualizersPath = Path.Combine(vsLocation, "Visualizers");
            } catch {
            }

            if (string.IsNullOrEmpty(VisualizersPath)) {
                VisualizersPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
#if VS2022
                    @"Visual Studio 2022\Visualizers\");
#elif VS2019
                    @"Visual Studio 2019\Visualizers\");
#elif VS2017
                    @"Visual Studio 2017\Visualizers\");
#endif
            }
        }

        public void CopyVisualizersFiles(string qtNamespace = null)
        {
            string[] files = { "qt5.natvis.xml", "qt6.natvis.xml" };
            foreach (var file in files)
                CopyVisualizersFile(file, qtNamespace);
        }

        private void CopyVisualizersFile(string filename, string qtNamespace)
        {
            try {
                var text = File.ReadAllText(Path.Combine(PkgInstallPath, filename));

                string visualizerFile;
                if (string.IsNullOrEmpty(qtNamespace)) {
                    text = text.Replace("##NAMESPACE##::", string.Empty);
                    visualizerFile = Path.GetFileNameWithoutExtension(filename);
                } else {
                    text = text.Replace("##NAMESPACE##", qtNamespace);
                    visualizerFile = filename.Substring(0, filename.IndexOf('.'))
                        + $"_{qtNamespace.Replace("::", "_")}.natvis";
                }

                if (!Directory.Exists(VisualizersPath))
                    Directory.CreateDirectory(VisualizersPath);

                File.WriteAllText(Path.Combine(VisualizersPath, visualizerFile),
                    text, System.Text.Encoding.UTF8);
            } catch (Exception exception) {
                exception.Log();
            }
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

        private static async Task<string> GetLatestDevelopmentReleaseAsync()
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

                response = await http.GetAsync($"{urlDownloadQtIo}{devVersion}/");
                return response.IsSuccessStatusCode ? devVersion.ToString() : null;
            } catch {
                return null;
            }
        }
    }
}
