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
using Microsoft.VisualStudio.Shell;
using QtProjectLib;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace QtVsTools
{
    [Guid(Vsix.PackageGuid)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids.SolutionExists)]
    public sealed class Vsix : Package
    {
        #region public

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

        public ExtLoader ExtLoader
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

        /// <summary>
        /// Gets the instance of the package.
        /// </summary>
        public static Vsix Instance
        {
            get;
            private set;
        }

        private string appWrapperPath = null;
        public string AppWrapperPath
        {
            get
            {
                if (appWrapperPath == null)
                    appWrapperPath = locateHelperExecutable("QtAppWrapper.exe");
                return appWrapperPath;
            }
        }

        private string qmakeFileReaderPath = null;
        public string QMakeFileReaderPath
        {
            get
            {
                if (qmakeFileReaderPath == null)
                    qmakeFileReaderPath = locateHelperExecutable("QMakeFileReader.exe");
                return qmakeFileReaderPath;
            }
        }

        #endregion public

        #region protected

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited,
        /// so this is the place where you can put all the initialization code that rely on services
        /// provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Instance = this;
            base.Initialize();

            Dte = (this as IServiceProvider).GetService(typeof(DTE)) as DTE;

            // determine the package installation directory
            var uri = new Uri(System.Reflection.Assembly.GetExecutingAssembly().EscapedCodeBase);
            PkgInstallPath = Path.GetDirectoryName(Uri.UnescapeDataString(uri.AbsolutePath)) + @"\";

            var vm = QtVersionManager.The();
            string error = null;
            if (vm.HasInvalidVersions(out error))
                Messages.DisplayErrorMessage(error);
            eventHandler = new AddInEventHandler(Dte);
            ExtLoader = new ExtLoader();

            QtMainMenu.Initialize(this);
            QtSolutionContextMenu.Initialize(this);
            QtProjectContextMenu.Initialize(this);
            QtItemContextMenu.Initialize(this);

            try {
                UpdateDefaultEditors(Mode.Startup);
            } catch (System.Exception e) {
                MessageBox.Show(e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace);
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
            eventHandler.Disconnect();
            try {
                UpdateDefaultEditors(Mode.Shutdown);
            } catch (System.Exception e) {
                MessageBox.Show(e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace);
            }
            return base.QueryClose(out canClose);
        }

        #endregion protected

        #region private

        private enum Mode
        {
            Startup = 0,
            Shutdown
        }

        private AddInEventHandler eventHandler = null;

        private string locateHelperExecutable(string exeName)
        {
            string filePath = PkgInstallPath + exeName;
            if (!File.Exists(filePath)) {
                filePath = PkgInstallPath;
                if (filePath.EndsWith("\\"))
                    filePath = filePath.Remove(filePath.Length - 1);
                var idx = filePath.LastIndexOf('\\');
                if (idx >= 0 && idx < filePath.Length - 1)
                    filePath = filePath.Remove(idx + 1);
                filePath += exeName;
                if (!File.Exists(filePath))
                    filePath = null;
            }
            return filePath;
        }

        /// <summary>
        /// This is to support VS2013, both VSIX and Qt4 or Qt5 Add-In installed. Default editor
        /// values in the registry are changed so that Qt4 or Qt5 Add-in values are default and
        /// Qt5 VSIX values are set only when the VSIX is loaded. On startup, Qt5 related registry
        /// values for *.qrc, *.ts and *.ui extensions are written, while on shutdown possible
        /// existing Add-in values are written back.
        /// </summary>
        private static void UpdateDefaultEditors(Mode mode)
        {
            if (mode == Mode.Shutdown) {
                var qt5 = new Qt5DefaultEditors();
                qt5.WriteAddinRegistryValues();
                var qt4 = new Qt4DefaultEditors();
                qt4.WriteAddinRegistryValues();
            } else {
                var vsix = new QtVsToolsDefaultEditors();
                vsix.WriteVsixRegistryValues();
            }
        }

        #endregion private
    }
}
