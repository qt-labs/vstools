// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

using static Microsoft.VisualStudio.Shell.PackageAutoLoadFlags;
using Task = System.Threading.Tasks.Task;

namespace QtVsTools
{
    using Core;
    using Package;
    using Package.CMake;
    using Qml.Debug;
    using QtVsTools.Core.Common;
    using VisualStudio;

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
    [ProvideEditorExtension(typeof(Package.Editors.QtDesigner),
        extension: ".ui",
        priority: 999,
        DefaultName = Package.Editors.QtDesigner.Title)]
    [ProvideEditorLogicalView(typeof(Package.Editors.QtDesigner),
        logicalViewGuid: VSConstants.LOGVIEWID.TextView_string)]

    // Custom editor: Qt Linguist
    [ProvideEditorExtension(typeof(Package.Editors.QtLinguist),
        extension: ".ts",
        priority: 999,
        DefaultName = Package.Editors.QtLinguist.Title)]
    [ProvideEditorLogicalView(typeof(Package.Editors.QtLinguist),
        logicalViewGuid: VSConstants.LOGVIEWID.TextView_string)]

    // Custom editor: Qt Resource Editor
    [ProvideEditorExtension(typeof(Package.Editors.QtResourceEditor),
        extension: ".qrc",
        priority: 999,
        DefaultName = Package.Editors.QtResourceEditor.Title)]
    [ProvideEditorLogicalView(typeof(Package.Editors.QtResourceEditor),
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

    [ProvideService((typeof(SIdleTaskManager)), IsAsyncQueryable = true)]

    public sealed class QtVsToolsPackage : AsyncPackage, IVsServiceProvider
    {
        public DTE Dte { get; private set; }

        public Package.Editors.QtDesigner QtDesigner { get; private set; }
        public Package.Editors.QtLinguist QtLinguist { get; private set; }
        private Package.Editors.QtResourceEditor QtResourceEditor { get; set; }

        public static EventWaitHandle Initialized { get; } = new(false, EventResetMode.ManualReset);
        private static bool InitializationAwaited { get; set; } = false;

        public static QtVsToolsPackage Instance { get; private set; }

        private DteEventsHandler EventHandler { get; set; }

        private Guid legacyPackageId = new("6E7FA583-5FAA-4EC9-9E90-4A0AE5FD61EE");
        private const string LegacyPackageName = "QtVsToolsLegacyPackage";

        private ConcurrentStopwatch InitTimer { get; set; }
        private ConcurrentStopwatch UiTimer { get; set; }

        private uint debuggerEventsCookie;
        private DebuggerEvents debuggerEventsHandler;

        private uint buildEventsCookie;
        private UpdateSolutionEvents buildEventsHandler;

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            try {
                InitTimer = ConcurrentStopwatch.StartNew();

                VsServiceProvider.Instance = Instance = this;
                AddService(typeof(SIdleTaskManager), CreateServiceAsync);
                await VsShell.InitializeAsync();

                var packages = await GetServiceAsync<
                    SVsPackageInfoQueryService, IVsPackageInfoQueryService>();

                ///////////////////////////////////////////////////////////////////////////////////
                // Switch to main (UI) thread
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                UiTimer = ConcurrentStopwatch.StartNew();

                if (packages?.GetPackageInfo(ref legacyPackageId) is { Name: LegacyPackageName })
                    throw new InvalidOperationException("Legacy extension detected.");

                if ((Dte = await VsServiceProvider.GetServiceAsync<DTE>()) == null)
                    throw new InvalidOperationException("Unable to get service: DTE");

                if (await VsServiceProvider.GetServiceAsync<IVsAppCommandLine>() is { } cmd) {
                    var result = cmd.GetOption("rootSuffix", out var exists, out var suffix);
                    if (result == VSConstants.S_OK && exists > 0)
                        Resources.RegistrySuffix = @"\" + suffix;
                } else {
                    Messages.Print("Unable to get service: IVsAppCommandLine");
                }

                if (Dte.CommandLineArguments?.Contains("/Command QtVSTools.ClearSettings") == true)
                    ClearSettingsRegistry();

                if (await VsServiceProvider.GetServiceAsync<IVsDebugger>() is { } service) {
                    debuggerEventsHandler = new DebuggerEvents(Dte);
                    service.AdviseDebuggerEvents(debuggerEventsHandler, out debuggerEventsCookie);
                }

                // TODO: At some point, maybe version 4.0 or later, we can remove this call again.
                Utils.SetRegistryKeyOnce(Resources.SettingsRegistryPath, "QmlLsp_Enable", 1,
                    RegistryValueKind.DWord, "QmlLsp_Enable_Changed");

                Qml.Debug.Launcher.Initialize();
                QtMainMenu.Initialize();
                AddCMakeItem.Initialize();
                QtSolutionContextMenu.Initialize();
                QtProjectContextMenu.Initialize();
                QtItemContextMenu.Initialize();
                RegisterEditorFactory(QtDesigner = new Package.Editors.QtDesigner());
                RegisterEditorFactory(QtLinguist = new Package.Editors.QtLinguist());
                RegisterEditorFactory(QtResourceEditor = new Package.Editors.QtResourceEditor());
                RegisterEditorFactory(new Package.MsBuild.ConversionReportViewer());
                QtHelp.Initialize();

                if (!string.IsNullOrEmpty(VsShell.InstallRootDir))
                    QMakeImport.VcPath = Path.Combine(VsShell.InstallRootDir, "VC");

                ///////////////////////////////////////////////////////////////////////////////////
                // Switch to background thread
                await TaskScheduler.Default;
                UiTimer.Stop();

                ///////////
                // Install Qt/MSBuild files from package folder to standard location
                //  -> %LOCALAPPDATA%\QtMsBuild
                //
                var qtMsBuildDefault = await InstallQtMsBuildFilesAsync();

                ///////
                // Set %QTMSBUILD% by default to point to standard location of Qt/MSBuild
                //
                var qtMsBuildPath = Environment.GetEnvironmentVariable("QtMsBuild");
                if (string.IsNullOrEmpty(qtMsBuildPath)) {

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
                var properties = new Dictionary<string, string>
                {
                    {"Operation", GetType().FullName + ".InitializeAsync"}
                };
                Telemetry.TrackException(ex, properties);
                var activityLog = await GetServiceAsync<SVsActivityLog, IVsActivityLog>();
                activityLog?.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, ToString(),
                    "Failed to load QtVsTools package. Exception details:\n"
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
            await Task.WhenAll(
                /////////
                // Move registered Qt versions
                //
                Task.Run(QtVersionManager.MoveRegisteredQtVersions, cancellationToken),

                /////////
                // Initialize Qt versions information
                //
                CheckVersionsAsync(),

                /////////
                // Copy natvis files
                //
                NatvisHelper.CopyVisualizersFilesAsync(),

                /////////
                // Setup QTDIR environment variable
                //
                Task.Run(() =>
                {
                    if (QtVersionManager.GetInstallPath("$(DefaultQtVersion)") is not { } path)
                        return;
                    if (new[] { "SSH:", "WSL:" }.Any(path.StartsWith))
                        return;
                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("QTDIR"))) {
                        Environment.SetEnvironmentVariable("QTDIR", path,
                            EnvironmentVariableTarget.Process);
                    }
                }, cancellationToken),

                /////////
                // Force download and install of local QML Language Server, otherwise it's
                // periodically checked if Visual Studio is idling.
                //
                RunQmlLanguageServerMonitorTaskOnceAsync(cancellationToken)
            );


            /////////
            // Show banner
            //
            Messages.Print(trim: false, text: @$"

    ################################################################
        == Qt Visual Studio Tools version {Version.USER_VERSION} ==
            Extension package initialized in:
             * Total: {InitTimer.Elapsed.TotalMilliseconds:0.##} ms
             * UI thread: {UiTimer.Elapsed.TotalMilliseconds:0.##} ms
    ################################################################");

            /////////
            // Switch to main (UI) thread
            //
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            /////////
            // Send telemetry
            //
            var properties = new Dictionary<string, string>
            {
                {"VSVersion", await VsShell.GetReleaseStringAsync()},
                {"VSToolsVersion", Version.PRODUCT_VERSION},
                {"QtVersions", string.Join(";", QtVersionManager.GetVersions())}
            };
            var metrics = new Dictionary<string, double>
            {
                {"InitTimer", InitTimer.Elapsed.TotalMilliseconds},
                {"UiTimer", UiTimer.Elapsed.TotalMilliseconds}
            };
            Telemetry.TrackEvent(GetType().FullName + ".OnAfterPackageLoadedAsync", properties,
                metrics);

            /////////
            // Initialize DTE event handlers.
            //
            EventHandler = new DteEventsHandler(Dte);

            /////////
            // Wire up solution change events. Currently, targets project configuration
            // changed events only. Used in conjunction with the QML Language Server.
            //
            var manager = await VsServiceProvider.GetServiceAsync<IVsSolutionBuildManager>();
            if (manager != null) {
                buildEventsHandler = new UpdateSolutionEvents();
                manager.AdviseUpdateSolutionEvents(buildEventsHandler, out buildEventsCookie);
            }

            /////////
            // Check if a solution was opened during initialization.
            // If so, fire solution open event.
            //
            if (VsShell.FolderWorkspace?.CurrentWorkspace is not null)
                EventHandler.OnActiveWorkspaceChanged();
            else if (Dte.Solution?.IsOpen == true)
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


            if (await GetServiceAsync<SIdleTaskManager, IIdleTaskManager>() is { } service) {
                service.Add(new DevReleaseMonitorTask());
                service.Add(new QmlLanguageServerMonitorTask());
            }

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

        internal static void ClearSettingsRegistry()
        {
            Registry.CurrentUser.DeleteSubKeyTree(Resources.ObsoleteRegistryPath, false);
            Registry.CurrentUser.DeleteSubKeyTree(Resources.RegistryPath, false);
        }

        /// <summary>
        /// Returns true when the target is the migrated/user-managed Qt.props variant (identified
        /// by importing qt_private.props), which must not be overwritten.
        /// </summary>
        private static bool IsUpdatedQtProps(string path)
        {
            const string qtPrivateImport
                = @"<Import Project=""$(MSBuildThisFileDirectory)\qt_private.props""";
            try {
                return string.Equals(Path.GetFileName(path), "Qt.props", Utils.IgnoreCase)
                    && File.ReadAllText(path).Contains(qtPrivateImport);
            } catch (Exception) {
                return false;
            }
        }

        /// <summary>
        /// Copies one Qt/MSBuild file to the target path. Skips files that are preserved
        /// Qt.props or already identical, otherwise stages to a temp file and commits via
        /// replace/move with handling for target creation races after existence checks.
        /// </summary>
        private static async Task CopyQtMsBuildFileAsync(string sourcePath, string targetPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? "");

            if (IsUpdatedQtProps(targetPath))
                return;
            if (await Utils.CompareFilesAsync(sourcePath, targetPath))
                return;

            var targetPathTemp = targetPath + ".tmp." + Guid.NewGuid().ToString("N");
            try {
                File.Copy(sourcePath, targetPathTemp, overwrite: true);
                if (!File.Exists(targetPath)) {
                    // Target file does not exist. -> Create new
                    try {
                        File.Move(targetPathTemp, targetPath);
                    } catch (IOException) when (File.Exists(targetPath)) {
                        // Race after the existence check: target appeared before commit.
                        // -> Replace existing target instead. target instead.
                        File.Replace(targetPathTemp, targetPath, null);
                    }
                } else {
                    // Target exists and is not updated Qt.props -> Overwrite
                    File.Replace(targetPathTemp, targetPath, null);
                }
            } finally {
                Utils.DeleteFile(targetPathTemp);
            }
        }

        /// <summary>
        /// Installs Qt/MSBuild files from the package folder to the standard location under
        /// %LOCALAPPDATA%\QtMsBuild.
        /// </summary>
        /// <remarks>
        /// Uses a named local mutex to serialize this copy step across concurrent VS instances,
        /// so shared qt*.props/qt*.targets files are not replaced in parallel. The lock scope is
        /// limited to the copy block and uses a bounded wait to avoid hanging startup.
        /// </remarks>>
        private static async Task<string> InstallQtMsBuildFilesAsync()
        {
            var qtMsBuildDefault = Path.Combine(
                Environment.GetEnvironmentVariable("LocalAppData") ?? "", "QtMsBuild");
            try {
                var defaultUri = new Uri(qtMsBuildDefault + Path.DirectorySeparatorChar);
                var vsixPath = Path.Combine(Utils.PackageInstallPath, "QtMsBuild");
                var vsixUri = new Uri(vsixPath + Path.DirectorySeparatorChar);
                if (vsixUri == defaultUri)
                    return qtMsBuildDefault;

                using var mutex = new Mutex(false, @"Local\QtVsTools.QtMsBuild.Copy");
                var locked = false;
                try {
                    try {
                        locked = mutex.WaitOne(TimeSpan.FromSeconds(30));
                    } catch (AbandonedMutexException) {
                        locked = true;
                    }

                    if (!locked)
                        throw new TimeoutException("Timed out waiting for Qt/MSBuild copy lock.");

                    var qtMsBuildFiles = Directory
                        .GetFiles(vsixPath, "*", SearchOption.AllDirectories)
                        .Select(x => vsixUri.MakeRelativeUri(new Uri(x)));
                    foreach (var qtMsBuildFile in qtMsBuildFiles) {
                        var sourcePath = new Uri(vsixUri, qtMsBuildFile).LocalPath;
                        var targetPath = new Uri(defaultUri, qtMsBuildFile).LocalPath;
                        await CopyQtMsBuildFileAsync(sourcePath, targetPath);
                    }
                } finally {
                    if (locked)
                        mutex.ReleaseMutex();
                }
            } catch {
                /////////
                // Error copying files to standard location.
                //  -> FAIL-SAFE: use source folder (within package) as the standard location
                qtMsBuildDefault = Path.Combine(Utils.PackageInstallPath, "QtMsBuild");
            }
            return qtMsBuildDefault;
        }

        private async Task CheckVersionsAsync()
        {
            await VsShell.UiThreadAsync(() =>
                StatusBar.SetText("Checking installed Qt versions..."));

            var defaultPath = QtVersionManager.GetDefaultVersionInstallPath();
            Messages.Print($"--- Checking default Qt version...{Environment.NewLine}"
                + (QtPaths.Exists(defaultPath) || QMake.Exists(defaultPath)
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
            status?.RegisterTask(new(() => throw new InvalidOperationException()));
            status?.Progress.Report(new TaskProgressData
            {
                ProgressText = $"{versions.Length} version(s)",
                CanBeCanceled = false,
                PercentComplete = 0
            });

            var tasks = versions.Select((version, idx) => JoinableTaskFactory.RunAsync(async () =>
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

            await Task.WhenAll(tasks);

            if (InitializationAwaited)
                await VsShell.UiThreadAsync(StatusBar.ResetProgress);
            await VsShell.UiThreadAsync(StatusBar.Clear);
            status?.Dismiss();
        }

        protected override int QueryClose(out bool canClose)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Telemetry.Flush();
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

        public T2 GetService<T1, T2>()
            where T1 : class
            where T2 : class
        {
            return GetService(typeof(T1)) as T2;
        }

        public async Task<T2> GetServiceAsync<T1, T2>()
            where T1 : class
            where T2 : class
        {
            return await GetServiceAsync(typeof(T1)) as T2;
        }

        private SIdleTaskManager idleTaskManager;
        private async Task<object> CreateServiceAsync(IAsyncServiceContainer container,
            CancellationToken cancellationToken, Type serviceType)
        {
            if (container != this || serviceType != typeof(SIdleTaskManager))
                return null;

            if (idleTaskManager != null)
                return idleTaskManager;

            var service = new IdleTaskManager(ThreadHelper.JoinableTaskContext);
            await service.InitializeAsync(this, cancellationToken);

            return idleTaskManager ??= service;
        }

        private static async Task RunQmlLanguageServerMonitorTaskOnceAsync(CancellationToken token)
        {
            if (Directory.Exists(QmlLanguageServerManager.InstallDir))
                return;
            try {
                await new QmlLanguageServerMonitorTask().RunAsync(token);
            } catch {
                Utils.DeleteDirectory(QmlLanguageServerManager.InstallDir, Utils.Option.Recursive);
            }
        }

        protected override void Dispose(bool disposing)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (disposing) {
                var debugger = GetService<IVsDebugger, IVsDebugger>();
                if (debugger != null && debuggerEventsCookie != 0) {
                    debugger.UnadviseDebuggerEvents(debuggerEventsCookie);
                    debuggerEventsCookie = 0;
                }

                var buildManager = GetService<SVsSolutionBuildManager, IVsSolutionBuildManager>();
                if (buildManager != null && buildEventsCookie != 0) {
                    buildManager.UnadviseUpdateSolutionEvents(buildEventsCookie);
                    buildEventsCookie = 0;
                }
            }
            base.Dispose(disposing);
        }
    }
}
