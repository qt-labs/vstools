/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;

using Task = System.Threading.Tasks.Task;
using static Microsoft.VisualStudio.Shell.PackageAutoLoadFlags;

namespace QtVsTools
{
    using Core;
    using Core.Options;
    using Package;
    using Package.CMake;
    using Qml.Debug;
    using QtVsTools.Core.Common;
    using VisualStudio;

    using static SyntaxAnalysis.RegExpr;

    public static partial class Instances
    {
        public static QtVsToolsPackage Package => QtVsToolsPackage.Instance;
    }

    [Guid(QtMenus.Package.GuidString)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Version.PRODUCT_VERSION)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.EmptySolution, BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.SolutionHasSingleProject, BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.SolutionHasMultipleProjects, BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.CodeWindow, BackgroundLoad)]

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

    // Test Adapter page
    [ProvideOptionPage(typeof(TestAdapter.QtTestPage),
        "Qt", "Test Adapter", 0, 0, true, Sort = 1)]

    // Qt Versions page
    [ProvideOptionPage(typeof(Core.Options.QtVersionsPage),
        "Qt", "Versions", 0, 0, true, Sort = 2)]

    [ProvideLaunchHook(typeof(QmlDebugLaunchHook))]

    public sealed class QtVsToolsPackage : AsyncPackage, IVsServiceProvider
    {
        public DTE Dte { get; private set; }

        public Editors.QtDesigner QtDesigner { get; private set; }
        public Editors.QtLinguist QtLinguist { get; private set; }
        private Editors.QtResourceEditor QtResourceEditor { get; set; }

        public static EventWaitHandle Initialized { get; } = new(false, EventResetMode.ManualReset);
        private static bool InitializationAwaited { get; set; } = false;

        public static QtVsToolsPackage Instance { get; private set; }

        private DteEventsHandler EventHandler { get; set; }
        private string VisualizersPath { get; set; }

        private Guid LegacyPackageId = new("6E7FA583-5FAA-4EC9-9E90-4A0AE5FD61EE");
        private const string LegacyPackageName = "QtVsToolsLegacyPackage";

        ConcurrentStopwatch InitTimer { get; set; }
        ConcurrentStopwatch UiTimer { get; set; }

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            try {
                InitTimer = ConcurrentStopwatch.StartNew();
                VsServiceProvider.Instance = Instance = this;

                var packages = await GetServiceAsync<
                    SVsPackageInfoQueryService, IVsPackageInfoQueryService>();

                ///////////////////////////////////////////////////////////////////////////////////
                // Switch to main (UI) thread
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                UiTimer = ConcurrentStopwatch.StartNew();

                if (packages?.GetPackageInfo(ref LegacyPackageId) is { Name: LegacyPackageName } )
                    throw new InvalidOperationException("Legacy extension detected.");

                if ((Dte = await VsServiceProvider.GetServiceAsync<DTE>()) == null)
                    throw new InvalidOperationException("Unable to get service: DTE");

                if (Dte.CommandLineArguments?.Contains("/Command QtVSTools.ClearSettings") == true) {
                    Registry.CurrentUser.DeleteSubKeyTree(Resources.ObsoleteRootPath, false);
                    Registry.CurrentUser.DeleteSubKeyTree(Resources.CurrentRootPath, false);
                }

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
                    QMakeImport.VcPath = Path.Combine(VsShell.InstallRootDir, "VC");

                SetVisualizersPathProperty();

                ///////////////////////////////////////////////////////////////////////////////////
                // Switch to background thread
                await TaskScheduler.Default;
                UiTimer.Stop();

                ///////////
                // Install Qt/MSBuild files from package folder to standard location
                //  -> %LOCALAPPDATA%\QtMsBuild
                //
                var qtMsBuildDefault = Path.Combine(
                    Environment.GetEnvironmentVariable("LocalAppData") ?? "", "QtMsBuild");
                try {
                    var qtMsBuildDefaultUri = new Uri(qtMsBuildDefault + Path.DirectorySeparatorChar);
                    var qtMsBuildVsixPath = Path.Combine(Utils.PackageInstallPath, "QtMsBuild");
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
                                    .Equals("Qt.props", Utils.IgnoreCase)
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
                                Utils.DeleteFile(targetPathTemp);
                            }
                        }
                    }
                } catch {
                    /////////
                    // Error copying files to standard location.
                    //  -> FAIL-SAFE: use source folder (within package) as the standard location
                    qtMsBuildDefault = Path.Combine(Utils.PackageInstallPath, "QtMsBuild");
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
                InitTimer.Stop();

            } catch (Exception ex) {
                var activityLog = await GetServiceAsync<SVsActivityLog, IVsActivityLog>();
                activityLog?.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, ToString(),
                    $"Failed to load QtVsTools package. Exception details:\n"
                        + $" Message: {ex.Message}\n"
                        + $" Source: {ex.Source}\n"
                        + $" Stack Trace: {ex.StackTrace}\n"
                        + $" Target Site: {ex.TargetSite}\n"
                        + (ex.InnerException != null
                            ? $"Inner Exception Message: {ex.InnerException.Message}\n"
                                + $" Inner Exception Stack Trace: {ex.InnerException.StackTrace}\n"
                            : "")
                );
                throw; // VS will catch the exception and mark the extension as failed to load.
            }
        }

        protected override async Task OnAfterPackageLoadedAsync(CancellationToken cancellationToken)
        {
            QtVersionManager.MoveRegisteredQtVersions();

            await Task.WhenAll(

                /////////
                // Initialize Qt versions information
                //
                CheckVersionsAsync(),

                /////////
                // Copy natvis files
                //
                CopyVisualizersFilesAsync());

            if (QtVersionManager.GetInstallPath("$(DefaultQtVersion)") is {} path) {
                if (!new[] { "SSH:", "WSL:" }.Any(path.StartsWith)) {
                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("QTDIR"))) {
                        Environment.SetEnvironmentVariable("QTDIR", path,
                            EnvironmentVariableTarget.Process);
                    }
                }
            }

            /////////
            // Show banner
            //
            Messages.Print(trim: false, text: @$"


    ################################################################
        == Qt Visual Studio Tools version {Version.USER_VERSION} ==
            Extension package initialized in:
             * Total: {InitTimer.Elapsed.TotalMilliseconds:0.##} msecs
             * UI thread: {UiTimer.Elapsed.TotalMilliseconds:0.##} msecs
    ################################################################");

            /////////
            // If configured, show link to dev release, if any
            //
            if (QtOptionsPage.SearchDevRelease) {
                var result = await GetLatestDevelopmentReleaseAsync();
                if (result != null) {
                    Messages.Print(
                        trim: false, text: $@"

    ################################################################
      Qt Visual Studio Tools version {result.Value.Version} PREVIEW available at:
      {result.Value.Uri}
    ################################################################");
                }
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
            // Enable output messages and activate output pane.
            //
            Messages.Initialized = true;
            Messages.ActivateMessagePane();

            /////////
            // Signal package initialization complete.
            //
            Initialized.Set();

            await base.OnAfterPackageLoadedAsync(cancellationToken);
        }

        public static async Task WaitUntilInitializedAsync()
        {
            InitializationAwaited = true;
            await Initialized;
        }

        public static bool IsInitialized => Initialized.WaitOne(0);

        private async Task CheckVersionsAsync()
        {
            await VsShell.UiThreadAsync(() =>
                StatusBar.SetText("Checking installed Qt versions..."));

            Messages.Print($"--- Checking default Qt version...{Environment.NewLine}"
                + (QMake.Exists(QtVersionManager.GetDefaultVersionInstallPath() ?? "")
                  ? "--- default Qt version check OK" : "--> default Qt version missing."));

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
                var qt = VersionInformation.GetOrAddByName(version);
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

            // always copy .pri/.pro TextMate Language Grammar file
            Utils.CopyDirectory(Path.Combine(Utils.PackageInstallPath, "qttmlanguage"),
                qtTmLanguagePath);

            //Remove TextMate-based QML syntax highlighting
            Utils.DeleteDirectory(Path.Combine(qtTmLanguagePath, "qml"), Utils.Option.Recursive);
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
                var text = await Utils.ReadAllTextAsync(Path.Combine(Utils.PackageInstallPath,
                    filename));

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

                await Utils.WriteAllTextAsync(Path.Combine(VisualizersPath, visualizerFile), text);
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

        private static async Task<(string Version, string Uri)?> GetLatestDevelopmentReleaseAsync()
        {
            const string urlDownloadQtIo = "https://download.qt.io/development_releases/vsaddin/";

            var currentVersion = new System.Version(Version.PRODUCT_VERSION);
            try {
                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(QtOptionsPage.SearchDevReleaseTimeout);
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

                var requestUri = $"{urlDownloadQtIo}{devVersion}/";
                response = await http.GetAsync(requestUri);
                return response.IsSuccessStatusCode ? (devVersion.ToString(), requestUri) : null;
            } catch {
                return null;
            }
        }
    }
}
