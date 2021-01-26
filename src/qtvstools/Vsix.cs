/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;
using QtVsTools.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools
{
    using VisualStudio;
    using QtMsBuild;

#if VS2013
    using AsyncPackage = Package;
#endif

    [Guid(PackageGuid)]
    [InstalledProductRegistration("#110", "#112", Version.PRODUCT_VERSION, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
#if VS2013
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    [ProvideAutoLoad(UIContextGuids.NoSolution)]
#else
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
#endif

    // Options page
    [ProvideOptionPage(typeof(Options.QtOptionPage),
        "Qt", "General", 0, 0, true)]

    // Qt Versions page
    [ProvideOptionPage(typeof(Options.QtVersionsPage),
        "Qt", "Versions", 0, 0, true)]

    public sealed class Vsix : AsyncPackage, IVsServiceProvider, IProjectTracker
    {
        /// <summary>
        /// The package GUID string.
        /// </summary>
        public const string PackageGuid = "15021976-647e-4876-9040-2507afde45d2";

        /// <summary>
        /// Gets the Visual Studio application object that hosts the package.
        /// </summary>
        public DTE Dte
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the installation path of the package.
        /// </summary>
        public string PkgInstallPath
        {
            get;
            private set;
        }

        static EventWaitHandle initDone = new EventWaitHandle(false, EventResetMode.ManualReset);
        static Vsix instance = null;

        const StringComparison IGNORE_CASE = StringComparison.InvariantCultureIgnoreCase;

        /// <summary>
        /// Gets the instance of the package.
        /// </summary>
        public static Vsix Instance
        {
            get
            {
                initDone.WaitOne();
                return instance;
            }
        }

        public Options.QtOptionPage Options
            => GetDialogPage(typeof(Options.QtOptionPage)) as Options.QtOptionPage;

        private string qmakeFileReaderPath;
        public string QMakeFileReaderPath
        {
            get
            {
                if (qmakeFileReaderPath == null)
                    qmakeFileReaderPath = locateHelperExecutable("QMakeFileReader.exe");
                return qmakeFileReaderPath;
            }
        }

        public Editors.QtDesigner QtDesigner { get; private set; }
        public Editors.QtLinguist QtLinguist { get; private set; }
        public Editors.QtResourceEditor QtResourceEditor { get; private set; }

        static readonly Stopwatch initTimer = Stopwatch.StartNew();

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited,
        /// so this is the place where you can put all the initialization code that rely on services
        /// provided by VisualStudio.
        /// </summary>
#if VS2013
        protected override void Initialize()
#else
        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
#endif
        {
            try
            {
                var timeInitBegin = initTimer.Elapsed;
                VsServiceProvider.Instance = instance = this;
                QtProject.ProjectTracker = this;
                Messages.JoinableTaskFactory = JoinableTaskFactory;
#if !VS2013
                ///////////////////////////////////////////////////////////////////////////////////
                // Switch to main (UI) thread
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                var timeUiThreadBegin = initTimer.Elapsed;
#endif
                if ((Dte = VsServiceProvider.GetService<DTE>()) == null)
                    throw new Exception("Unable to get service: DTE");

                VsShellSettings.Manager = new ShellSettingsManager(this as IServiceProvider);

                eventHandler = new DteEventsHandler(Dte);

                Qml.Debug.Launcher.Initialize();
                QtMainMenu.Initialize(this);
                QtSolutionContextMenu.Initialize(this);
                QtProjectContextMenu.Initialize(this);
                QtItemContextMenu.Initialize(this);
                RegisterEditorFactory(QtDesigner = new Editors.QtDesigner());
                RegisterEditorFactory(QtLinguist = new Editors.QtLinguist());
                RegisterEditorFactory(QtResourceEditor = new Editors.QtResourceEditor());
                QtHelpMenu.Initialize(this);

                if (!string.IsNullOrEmpty(VsShell.InstallRootDir))
                    HelperFunctions.VCPath = Path.Combine(VsShell.InstallRootDir, "VC");

#if !VS2013
                ///////////////////////////////////////////////////////////////////////////////////
                // Switch to background thread
                await TaskScheduler.Default;
                var timeUiThreadEnd = initTimer.Elapsed;
#endif

                var vm = QtVersionManager.The(initDone);
                var error = string.Empty;
                if (vm.HasInvalidVersions(out error))
                    Messages.Print(error);

                // determine the package installation directory
                var uri = new Uri(System.Reflection.Assembly
                    .GetExecutingAssembly().EscapedCodeBase);
                PkgInstallPath = Path.GetDirectoryName(
                    Uri.UnescapeDataString(uri.AbsolutePath)) + @"\";

                ///////////
                // Install Qt/MSBuild files from package folder to standard location
                //  -> %LOCALAPPDATA%\QtMsBuild
                //
                var QtMsBuildDefault = Path.Combine(
                    Environment.GetEnvironmentVariable("LocalAppData"), "QtMsBuild");
                try {
                    var qtMsBuildDefaultUri = new Uri(QtMsBuildDefault + "\\");
                    var qtMsBuildVsixPath = Path.Combine(PkgInstallPath, "QtMsBuild");
                    var qtMsBuildVsixUri = new Uri(qtMsBuildVsixPath + "\\");
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
                if (string.IsNullOrEmpty(QtMsBuildPath))
                {

                    Environment.SetEnvironmentVariable(
                        "QtMsBuild", QtMsBuildDefault,
                        EnvironmentVariableTarget.User);

                    Environment.SetEnvironmentVariable(
                        "QtMsBuild", QtMsBuildDefault,
                        EnvironmentVariableTarget.Process);
                }

                CopyTextMateLanguageFiles();
                CopyNatvisFile();

                var modules = QtModules.Instance.GetAvailableModuleInformation();
                foreach (var module in modules)
                {
                    if (!string.IsNullOrEmpty(module.ResourceName))
                    {
                        var translatedName = SR.GetString(module.ResourceName, this);
                        if (!string.IsNullOrEmpty(translatedName))
                            module.Name = translatedName;
                    }
                }

                Messages.Print(string.Format("\r\n"
                    + "== Qt Visual Studio Tools version {0}\r\n"
                    + "\r\n"
                    + "   Initialized in: {1:0.##} msecs\r\n"
#if !VS2013
                    + "   Main (UI) thread: {2:0.##} msecs\r\n"
#endif
                    , Version.USER_VERSION
                    , (initTimer.Elapsed - timeInitBegin).TotalMilliseconds
#if !VS2013
                    , (timeUiThreadEnd - timeUiThreadBegin).TotalMilliseconds
#endif
                    ));
            }
            catch (Exception e)
            {
                Messages.Print(
                    e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace);
            }
            finally
            {
                initDone.Set();
                initTimer.Stop();
            }
        }

        /// <summary>
        /// Called to ask the package if the shell can be closed.
        /// </summary>
        /// <param term='canClose'>Returns true if the shell can be closed, otherwise false.</param>
        /// <returns>
        /// Microsoft.VisualStudio.VSConstants.S_OK if the method succeeded, otherwise an error code.
        /// </returns>
        protected override int QueryClose(out bool canClose)
        {
            if (eventHandler != null)
            {
                eventHandler.Disconnect();
            }

            return base.QueryClose(out canClose);
        }

        private enum Mode
        {
            Startup = 0,
            Shutdown
        }

        private DteEventsHandler eventHandler;

        private string locateHelperExecutable(string exeName)
        {
            if (!string.IsNullOrEmpty(PkgInstallPath) && File.Exists(PkgInstallPath + exeName))
                return PkgInstallPath + exeName;
            return null;
        }

        private void CopyTextMateLanguageFiles()
        {
#if (!VS2013)
            var settingsManager = VsShellSettings.Manager;
            var store = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

            var qttmlanguage = Environment.
                ExpandEnvironmentVariables("%USERPROFILE%\\.vs\\Extensions\\qttmlanguage");
            if (store.GetBoolean(Statics.QmlTextMatePath, Statics.QmlTextMateKey, true)) {
                HelperFunctions.CopyDirectory(Path.Combine(PkgInstallPath, "qttmlanguage"),
                    qttmlanguage);
            } else {
                Directory.Delete(qttmlanguage, true);
            }

            //Remove textmate-based QML syntax highlighting
            var qmlTextmate = Path.Combine(qttmlanguage, "qml");
            if (Directory.Exists(qmlTextmate)) {
                try {
                    Directory.Delete(qmlTextmate, true);
                } catch { }
            }
#endif
        }

        public void CopyNatvisFile(string qtNamespace = null)
        {
            try {
                string natvis = File.ReadAllText(
                    Path.Combine(PkgInstallPath, "qt5.natvis.xml"));

                string natvisFile;
                if (string.IsNullOrEmpty(qtNamespace)) {
                    natvis = natvis.Replace("##NAMESPACE##::", string.Empty);
                    natvisFile = "qt5.natvis";
                } else {
                    natvis = natvis.Replace("##NAMESPACE##", qtNamespace);
                    natvisFile = string.Format("qt5_{0}.natvis", qtNamespace.Replace("::", "_"));
                }

                string visualizersPath = string.Empty;
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
#if VS2019
                    @"Visual Studio 2019\Visualizers\");
#elif VS2017
                    @"Visual Studio 2017\Visualizers\");
#elif VS2015
                    @"Visual Studio 2015\Visualizers\");
#elif VS2013
                    @"Visual Studio 2013\Visualizers\");
#endif
                }

                if (!Directory.Exists(visualizersPath))
                    Directory.CreateDirectory(visualizersPath);

                File.WriteAllText(Path.Combine(visualizersPath, natvisFile),
                    natvis, System.Text.Encoding.UTF8);
            } catch (Exception e) {
                Messages.Print(
                    e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace);
            }
        }

        public I GetService<T, I>()
            where T : class
            where I : class
        {
            return GetService(typeof(T)) as I;
        }

#if !VS2013
        public async Task<I> GetServiceAsync<T, I>()
            where T : class
            where I : class
        {
            return await GetServiceAsync(typeof(T)) as I;
        }
#endif

        void IProjectTracker.AddProject(Project project)
        {
            QtProjectTracker.AddProject(project, runQtTools: false);
        }
    }
}
