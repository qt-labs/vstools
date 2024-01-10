/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
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
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;
using EnvDTE;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools
{
    using Common;
    using Core;
    using Core.Options;
    using Package;
    using Package.CMake;
    using Qml.Debug;
    using VisualStudio;

    using static QtVsTools.Core.Common.Utils;
    using static SyntaxAnalysis.RegExpr;

    public static partial class Instances
    {
        public static QtVsToolsPackage Package => QtVsToolsPackage.Instance;
    }

    [Guid(QtMenus.Package.GuidString)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Version.PRODUCT_VERSION)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]

    [ProvideEditorExtension(typeof(Package.MsBuild.ConversionReportViewer),
        extension: ".qtvscr",
        priority: 999,
        DefaultName = "Qt/MSBuild Project Format Conversion Report")]
    [ProvideEditorLogicalView(typeof(Package.MsBuild.ConversionReportViewer),
        logicalViewGuid: VSConstants.LOGVIEWID.TextView_string)]

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
    [ProvideOptionPage(typeof(Core.Options.QtOptionsPage),
        "Qt", "General", 0, 0, true, Sort = 0)]

    // Qt Versions page
    [ProvideOptionPage(typeof(Core.Options.QtVersionsPage),
        "Qt", "Versions", 0, 0, true, Sort = 1)]

    [ProvideLaunchHook(typeof(QmlDebugLaunchHook))]

    public sealed class QtVsToolsPackage : AsyncPackage, IVsServiceProvider
    {
        LazyFactory Lazy { get; } = new();

        public DTE Dte { get; private set; }
        public string PkgInstallPath { get; private set; }
        public QtOptionsPage Options => Lazy.Get(() => Options, () =>
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return GetDialogPage(typeof(QtOptionsPage)) as QtOptionsPage;
            }));

        public Editors.QtDesigner QtDesigner { get; private set; }
        public Editors.QtLinguist QtLinguist { get; private set; }
        private Editors.QtResourceEditor QtResourceEditor { get; set; }

        public EventWaitHandle Initialized { get; } = new(false, EventResetMode.ManualReset);
        private bool InitializationAwaited { get; set; } = false;

        private static QtVsToolsPackage instance;
        public static QtVsToolsPackage Instance
        {
            get
            {
                return instance;
            }
        }

        private static readonly HttpClient http = new();
        private const string urlDownloadQtIo = "https://download.qt.io/development_releases/vsaddin/";

        private DteEventsHandler EventHandler { get; set; }
        private string VisualizersPath { get; set; }

        private Guid LegacyPackageId = new("6E7FA583-5FAA-4EC9-9E90-4A0AE5FD61EE");
        private const string LegacyPackageName = "QtVsToolsLegacyPackage";

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            try {
                var initTimer = Stopwatch.StartNew();
                VsServiceProvider.Instance = instance = this;

                var packages = await GetServiceAsync<
                    SVsPackageInfoQueryService, IVsPackageInfoQueryService>();

                // determine the package installation directory
                var uri = new Uri(System.Reflection.Assembly
                    .GetExecutingAssembly().EscapedCodeBase);
                PkgInstallPath = Path.GetDirectoryName(
                    Uri.UnescapeDataString(uri.AbsolutePath)) + @"\";

                ///////////////////////////////////////////////////////////////////////////////////
                // Switch to main (UI) thread
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                var uiTimer = Stopwatch.StartNew();

                if (packages?.GetPackageInfo(ref LegacyPackageId) is { Name: LegacyPackageName } )
                    throw new QtVSException("Legacy extension detected.");

                if ((Dte = await VsServiceProvider.GetServiceAsync<DTE>()) == null)
                    throw new Exception("Unable to get service: DTE");

                Qml.Debug.Launcher.Initialize();
                QtMainMenu.Initialize();
                AddCMakeItem.Initialize();
                QtSolutionContextMenu.Initialize();
                QtProjectContextMenu.Initialize();
                QtItemContextMenu.Initialize();
                RegisterEditorFactory(QtDesigner = new Editors.QtDesigner());
                RegisterEditorFactory(QtLinguist = new Editors.QtLinguist());
                RegisterEditorFactory(QtResourceEditor = new Editors.QtResourceEditor());
                RegisterEditorFactory(new Package.MsBuild.ConversionReportViewer());
                QtHelp.Initialize();

                if (!string.IsNullOrEmpty(VsShell.InstallRootDir))
                    HelperFunctions.VcPath = Path.Combine(VsShell.InstallRootDir, "VC");

                SetVisualizersPathProperty();

                ///////////////////////////////////////////////////////////////////////////////////
                // Switch to background thread
                await TaskScheduler.Default;
                uiTimer.Stop();

                MoveRegistryKeys("SOFTWARE\\Digia", Resources.RegistryRootPath);
                MoveRegistryKeys(Resources.RegistryRootPath + "\\Qt5VS2017",
                    Resources.RegistryPackagePath);

                if (QtVersionManager.HasInvalidVersions(out var error, out var defaultInvalid)) {
                    if (defaultInvalid)
                        QtVersionManager.SetLatestQtVersionAsDefault();
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
                                    .Equals("Qt.props", IgnoreCase)
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

                var defaultVersion = QtVersionManager.GetInstallPath("$(DefaultQtVersion)");
                if (defaultVersion != null) {
                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("QTDIR"))) {
                        Environment.SetEnvironmentVariable("QTDIR", defaultVersion,
                            EnvironmentVariableTarget.Process);
                    }
                }

                CopyTextMateLanguageFiles();
                initTimer.Stop();
                var initMsecs = initTimer.Elapsed.TotalMilliseconds;
                var uiMsecs = uiTimer.Elapsed.TotalMilliseconds;

                /////////
                // Continue initialization in background task
                //
                await Task.WhenAny(
                    Task.Run(Task.Yield, cancellationToken),
                    Task.Run(async () => await CopyVisualizersFilesAsync(), cancellationToken),
                    Task.Run(async () =>
                        await FinalizeInitializationAsync(initMsecs, uiMsecs), cancellationToken)
                );

            } catch (Exception exception) {
                exception.Log();
            }
        }

        private async Task FinalizeInitializationAsync(double initMsecs, double uiMsecs)
        {
            /////////
            // Initialize Qt versions information
            //
            await CheckVersionsAsync();

            /////////
            // Show banner
            //
            Messages.Print(trim: false, text: @$"


    ################################################################
        == Qt Visual Studio Tools version {Version.USER_VERSION} ==
            Extension package initialized in:
             * Total: {initMsecs:0.##} msecs
             * UI thread: {uiMsecs:0.##} msecs
    ################################################################");

            /////////
            // Show link to dev release, if any
            //
            var devRelease = await GetLatestDevelopmentReleaseAsync();
            if (devRelease != null) {
                Messages.Print(trim: false, text: $@"

    ################################################################
      Qt Visual Studio Tools version {devRelease} PREVIEW available at:
      {urlDownloadQtIo}{devRelease}/
    ################################################################");
            }

            /////////
            // Switch to main (UI) thread
            //
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            /////////
            // Initialize DTE event handlers.
            //
            EventHandler = new DteEventsHandler(Dte);

            /////////
            // Check if a solution was opened during initialization.
            // If so, fire solution open event.
            //
            if (VsShell.FolderWorkspace?.CurrentWorkspace is not null)
                EventHandler.OnActiveWorkspaceChanged();
            else if (Dte?.Solution?.IsOpen == true)
                EventHandler.SolutionEvents_Opened();

            if (Dte.Debugger.CurrentMode != dbgDebugMode.dbgDesignMode) {
                foreach (EnvDTE.Process proc in Dte.Debugger.DebuggedProcesses) {
                    if (Qml.Debug.Launcher.TryAttachToProcess((uint)proc.ProcessID))
                        break;
                }
            }

            /////////
            // Eable output messages and activate output pane.
            //
            Messages.Initialized = true;
            Messages.ActivateMessagePane();

            /////////
            // Signal package initialization complete.
            //
            Initialized.Set();
        }

        public bool WaitUntilInitialized(int timeout = -1)
        {
            InitializationAwaited = true;
            return Initialized.WaitOne(timeout);
        }

        public bool IsInitialized => WaitUntilInitialized(0);

        private async Task CheckVersionsAsync()
        {
            await VsShell.UiThreadAsync(() =>
                StatusBar.SetText("Checking installed Qt versions..."));

            var versions = QtVersionManager.GetVersions();
            var statusCenter = await VsServiceProvider
                .GetServiceAsync<SVsTaskStatusCenterService, IVsTaskStatusCenterService>();
            var status = statusCenter?.PreRegister(
                    new TaskHandlerOptions
                    {
                        Title = "Qt VS Tools: Checking installed Qt versions..."
                    },
                    new TaskProgressData
                    {
                        ProgressText = $"{versions.Length} version(s)",
                        CanBeCanceled = false,
                        PercentComplete = 0
                    })
                    as ITaskHandler2;
            status?.RegisterTask(new (() => throw new InvalidOperationException()));
            status?.Progress.Report(new TaskProgressData
            {
                ProgressText = $"{versions.Length} version(s)",
                CanBeCanceled = false,
                PercentComplete = 0
            });

            var tasks = versions.Select((version, idx) => ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await Task.Yield();
                Messages.Print($@"
--- Checking {version} ...");
                var timer = Stopwatch.StartNew();
                var qt = QtVersionManager.GetVersionInfo(version);
                if (Directory.Exists(qt?.InstallPrefix ?? string.Empty)) {
                    Messages.Print($@"
--- {version} check OK ({timer.Elapsed.TotalSeconds:0.##} secs)");
                } else {
                    Messages.Print($@"
--> {version} Missing or cross-platform installation; skipped.");
                }
                if (InitializationAwaited) {
                    await VsShell.UiThreadAsync(() => StatusBar.Progress(
                        $"Checking Qt version: {version}", versions.Length, idx));
                }
                status?.Progress.Report(new TaskProgressData
                {
                    ProgressText = $"{version} ({versions.Length - idx - 1} remaining)",
                    CanBeCanceled = false,
                    PercentComplete = (100 * (idx + 1)) / versions.Length
                });
            }).Task);

            await Task.WhenAll(tasks.ToArray());

            if (InitializationAwaited)
                await VsShell.UiThreadAsync(StatusBar.ResetProgress);
            await VsShell.UiThreadAsync(StatusBar.Clear);
            status?.Dismiss();
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
            if (QtVersionManager.GetVersions().Length == 0)
                Notifications.NoQtVersion.Show();
            if (Options.NotifyInstalled && TestVersionInstalled())
                Notifications.NotifyInstall.Show();
        }

        protected override int QueryClose(out bool canClose)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EventHandler?.Disconnect();
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
            } catch { }
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
#endif
            }
        }

        public async Task CopyVisualizersFilesAsync(string qtNamespace = null)
        {
            string[] files = { "qt5.natvis.xml", "qt6.natvis.xml" };
            foreach (var file in files)
                await CopyVisualizersFileAsync(file, qtNamespace);
        }

        private async Task CopyVisualizersFileAsync(string filename, string qtNamespace)
        {
            try {
                var text = await ReadAllTextAsync(Path.Combine(PkgInstallPath, filename));

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

                await WriteAllTextAsync(Path.Combine(VisualizersPath, visualizerFile), text);
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
