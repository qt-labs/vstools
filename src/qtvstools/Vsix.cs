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
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using QtProjectLib;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace QtVsTools
{
    [Guid(PackageGuid)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Version.PRODUCT_VERSION, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids.SolutionExists)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids.NoSolution)]
    public sealed class Vsix : Package
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

        private string appWrapperPath;
        public string AppWrapperPath
        {
            get
            {
                if (appWrapperPath == null)
                    appWrapperPath = locateHelperExecutable("QtAppWrapper.exe");
                return appWrapperPath;
            }
        }

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

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited,
        /// so this is the place where you can put all the initialization code that rely on services
        /// provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            Qml.Debug.Launcher.Initialize();
            Action init = () =>
            {
                try {
                    instance = this;

                    Dte = (this as IServiceProvider).GetService(typeof(DTE)) as DTE;

                    // determine the package installation directory
                    var uri = new Uri(System.Reflection.Assembly
                        .GetExecutingAssembly().EscapedCodeBase);
                    PkgInstallPath = Path.GetDirectoryName(
                        Uri.UnescapeDataString(uri.AbsolutePath)) + @"\";

                    var QtMsBuildDefault = Path.Combine(
                        Environment.GetEnvironmentVariable("LocalAppData"), "QtMsBuild");
                    try {
                        if (!Directory.Exists(QtMsBuildDefault))
                            Directory.CreateDirectory(QtMsBuildDefault);
                        var qtMsBuildFiles = Directory.GetFiles(
                            Path.Combine(PkgInstallPath, "QtMsBuild"));
                        foreach (var qtMsBuildFile in qtMsBuildFiles) {
                            File.Copy(qtMsBuildFile, Path.Combine(
                                QtMsBuildDefault, Path.GetFileName(qtMsBuildFile)), true);
                        }
                    } catch {
                        QtMsBuildDefault = Path.Combine(PkgInstallPath, "QtMsBuild");
                    }

                    var QtMsBuildPath = Environment.GetEnvironmentVariable("QtMsBuild");
                    if (string.IsNullOrEmpty(QtMsBuildPath)) {

                        Environment.SetEnvironmentVariable(
                            "QtMsBuild", QtMsBuildDefault,
                            EnvironmentVariableTarget.User);

                        Environment.SetEnvironmentVariable(
                            "QtMsBuild", QtMsBuildDefault,
                            EnvironmentVariableTarget.Process);
                    }

                    var vm = QtVersionManager.The(initDone);
                    var error = string.Empty;
                    if (vm.HasInvalidVersions(out error))
                        Messages.PaneMessageSafe(Dte, error, 5000);
                    eventHandler = new DteEventsHandler(Dte);

                    QtMainMenu.Initialize(this);
                    QtSolutionContextMenu.Initialize(this);
                    QtProjectContextMenu.Initialize(this);
                    QtItemContextMenu.Initialize(this);
                    DefaultEditorsHandler.Initialize(Dte);
                    QtHelpMenu.Initialize(this);

                    try {
                        CopyTextMateLanguageFiles();
                        UpdateDefaultEditors(Mode.Startup);
                    } catch (Exception e) {
                        Messages.PaneMessageSafe(Dte,
                            e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace, 5000);
                    }

                    var modules = QtModules.Instance.GetAvailableModuleInformation();
                    foreach (var module in modules) {
                        if (!string.IsNullOrEmpty(module.ResourceName)) {
                            var translatedName = SR.GetString(module.ResourceName, this);
                            if (!string.IsNullOrEmpty(translatedName))
                                module.Name = translatedName;
                        }
                    }

                    CopyNatvisFile();

                } catch (Exception e) {
                    Messages.PaneMessageSafe(Dte,
                        e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace, 5000);

                } finally {
                    initDone.Set();

                }
            };

            var QtVs_Init = Environment.GetEnvironmentVariable("QtVs_Init");
            if (string.Equals(QtVs_Init, "fast", StringComparison.InvariantCultureIgnoreCase))
                System.Threading.Tasks.Task.Run(init);
            else
                init();
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
                eventHandler.Disconnect();

            try {
                UpdateDefaultEditors(Mode.Shutdown);
            } catch (Exception e) {
                MessageBox.Show(e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace);
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

        /// <summary>
        /// This is to support VS2013, both VSIX and Qt4 or Qt5 Add-In installed. Default editor
        /// values in the registry are changed so that Qt4 or Qt5 Add-in values are default and
        /// Qt5 VSIX values are set only when the VSIX is loaded. On startup, Qt5 related registry
        /// values for *.qrc, *.ts and *.ui extensions are written, while on shutdown possible
        /// existing Add-in values are written back.
        /// </summary>
        private void UpdateDefaultEditors(Mode mode)
        {
            if (mode == Mode.Shutdown) {
                var qt5 = new Qt5DefaultEditors();
                qt5.WriteAddinRegistryValues();
                var qt4 = new Qt4DefaultEditors();
                qt4.WriteAddinRegistryValues();
            } else {
                var vsix = new QtVsToolsDefaultEditors();
                vsix.WriteVsixRegistryValues(this);
            }
        }

        private void CopyTextMateLanguageFiles()
        {
#if (!VS2013)
            var settingsManager = new ShellSettingsManager(instance);
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

                File.WriteAllText(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
#if VS2017
                @"Visual Studio 2017\Visualizers\",
#elif VS2015
                @"Visual Studio 2015\Visualizers\",
#elif VS2013
                @"Visual Studio 2013\Visualizers\",
#endif
                natvisFile), natvis, System.Text.Encoding.UTF8);
            } catch (Exception e) {
                Messages.PaneMessageSafe(Dte,
                    e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace, 5000);
            }
        }
    }
}
