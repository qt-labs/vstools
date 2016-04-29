/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Add-in.
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
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Digia.Qt5ProjectLib;
using EnvDTE80;

namespace Qt5VSAddin
{
    [Guid(Connect.PackageGuid)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules",
        "SA1650:ElementDocumentationMustBeSpelledCorrectly",
        Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [InstalledProductRegistration("#110", "#112", "1.0.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids.SolutionExists)]
    public sealed class Connect : Package
    {
        /// <summary>
        /// The package GUID string.
        /// </summary>
        public const string PackageGuid = "15021976-647e-4876-9040-2507afde45d2";

        public static DTE _applicationObject = null;
        public static Connect _addInInstance = null;
        public static ExtLoader extLoader = null;

        private AddInEventHandler eventHandler = null;
        private FormChangeQtVersion formChangeQtVersion = null;
        private FormVSQtSettings formQtVersions = null;
        private FormProjectQtSettings formProjectQtSettings = null;
        private string installationDir = null;
        private string appWrapperPath = null;
        private string qmakeFileReaderPath = null;

        public static Connect Instance()
        {
            return _addInInstance;
        }

        /// <summary>Implements the constructor for the Add-in object. Place your initialization code within this method.</summary>
        public Connect()
        {
        }

        public string InstallationDir
        {
            get
            {
                return installationDir;
            }
        }

        public string AppWrapperPath
        {
            get
            {
                if (appWrapperPath == null)
                    appWrapperPath = locateHelperExecutable("qt5appwrapper.exe");
                return appWrapperPath;
            }
        }

        public string QMakeFileReaderPath
        {
            get
            {
                if (qmakeFileReaderPath == null)
                    qmakeFileReaderPath = locateHelperExecutable("qmakefilereader.exe");
                return qmakeFileReaderPath;
            }
        }

        private string locateHelperExecutable(string exeName)
        {
            string filePath = installationDir + exeName;
            if (!File.Exists(filePath))
            {
                filePath = installationDir;
                if (filePath.EndsWith("\\"))
                    filePath = filePath.Remove(filePath.Length - 1);
                int idx = filePath.LastIndexOf('\\');
                if (idx >= 0 && idx < filePath.Length - 1)
                    filePath = filePath.Remove(idx + 1);
                filePath += exeName;
                if (!File.Exists(filePath))
                    filePath = null;
            }
            return filePath;
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited,
        /// so this is the place where you can put all the initialization code that rely on services
        /// provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            _addInInstance = this;
            _applicationObject = (this as IServiceProvider).GetService(typeof(DTE)) as DTE;

            // determine the installation directory
            System.Uri uri = new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().EscapedCodeBase);
            installationDir = Path.GetDirectoryName(System.Uri.UnescapeDataString(uri.AbsolutePath));
            installationDir += "\\";

            {
                QtVersionManager vm = QtVersionManager.The();
                string error = null;
                if (vm.HasInvalidVersions(out error))
                    Messages.DisplayErrorMessage(error);
                eventHandler = new AddInEventHandler(_applicationObject);
                extLoader = new ExtLoader();

                QtMainMenu.Initialize(this);
                QtSolutionContextMenu.Initialize(this);
                QtProjectContextMenu.Initialize(this);
                QtItemContextMenu.Initialize(this);

                {
                    try
                    {
                        // TODO: Do we need this?
                        // UpdateDefaultEditors(true);
                    }
                    catch (System.Exception e)
                    {
                        MessageBox.Show(e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace);
                    }
                }
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
                // TODO: Do we need this?
                // UpdateDefaultEditors(false);
            } catch (System.Exception e) {
                MessageBox.Show(e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace);
            }
            return base.QueryClose(out canClose);
        }

        public void QueryStatus(string commandName,
                 EnvDTE.vsCommandStatusTextWanted neededText,
                 ref EnvDTE.vsCommandStatus status,
                 ref object commandText)
        {
            try
            {
                if (neededText == EnvDTE.vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
                {
                    if ((commandName == Res.LaunchDesignerFullCommand) ||
                        (commandName == Res.LaunchLinguistFullCommand) ||
                        (commandName == Res.VSQtOptionsFullCommand) ||
                        (commandName == Res.ImportProFileFullCommand))
                    {
                        status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported
                            | vsCommandStatus.vsCommandStatusEnabled;
                    }
                    else if ((commandName == Res.ImportPriFileFullCommand) ||
                        (commandName == Res.ExportPriFileFullCommand) ||
                        (commandName == Res.ExportProFileFullCommand) ||
                        (commandName == Res.CreateNewTranslationFileFullCommand) ||
                        (commandName == Res.lupdateProjectFullCommand) ||
                        (commandName == Res.lreleaseProjectFullCommand))
                    {
                        Project prj = HelperFunctions.GetSelectedProject(_applicationObject);
                        if (prj != null && HelperFunctions.IsQtProject(prj))
                            status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported
                                                    | vsCommandStatus.vsCommandStatusEnabled;
                        else
                            status = vsCommandStatus.vsCommandStatusSupported;
                    }
                    else if (commandName == Res.ProjectQtSettingsFullCommand ||
                        commandName == Res.ConvertToQMakeFullCommand)
                    {
                        Project prj = HelperFunctions.GetSelectedProject(_applicationObject);
                        if (prj == null)
                            status = vsCommandStatus.vsCommandStatusSupported;
                        else if (HelperFunctions.IsQtProject(prj))
                            status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported
                                                    | vsCommandStatus.vsCommandStatusEnabled;
                        else if (HelperFunctions.IsQMakeProject(prj))
                            status = vsCommandStatus.vsCommandStatusInvisible;
                        else
                            status = vsCommandStatus.vsCommandStatusSupported;
                    }
                    else if (commandName == Res.ChangeProjectQtVersionFullCommand ||
                        commandName == Res.ConvertToQtFullCommand)
                    {
                        Project prj = HelperFunctions.GetSelectedProject(_applicationObject);
                        if (prj == null || HelperFunctions.IsQtProject(prj))
                            status = vsCommandStatus.vsCommandStatusInvisible;
                        else if (HelperFunctions.IsQMakeProject(prj))
                            status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported
                                                    | vsCommandStatus.vsCommandStatusEnabled;
                        else
                            status = vsCommandStatus.vsCommandStatusInvisible;
                    }
                    else if ((commandName == Res.ChangeSolutionQtVersionFullCommand) ||
                        (commandName == Res.lupdateSolutionFullCommand) ||
                        (commandName == Res.lreleaseSolutionFullCommand))
                    {
                        if (_applicationObject.Solution.IsOpen)
                            status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported
                            | vsCommandStatus.vsCommandStatusEnabled;
                        else
                            status = vsCommandStatus.vsCommandStatusSupported;
                    }
                    else if (commandName == Res.CommandBarName + ".Connect.lupdate" ||
                        commandName == Res.CommandBarName + ".Connect.lrelease")
                    {
                        Project prj = HelperFunctions.GetSelectedProject(_applicationObject);
                        if (prj == null || !HelperFunctions.IsQtProject(prj) ||
                            _applicationObject.SelectedItems.Count == 0)
                        {
                            status = vsCommandStatus.vsCommandStatusInvisible;
                        }
                        else
                        {
                            status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported
                                | vsCommandStatus.vsCommandStatusEnabled;
                        }

                        if (status != vsCommandStatus.vsCommandStatusInvisible)
                        {
                            // Don't display commands if one of the selected files is not a .ts file.
                            foreach (SelectedItem si in _applicationObject.SelectedItems)
                            {
                                if (!si.Name.ToLower().EndsWith(".ts"))
                                {
                                    status = vsCommandStatus.vsCommandStatusInvisible;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                MessageBox.Show(e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace);
            }
        }

        public void Exec(string commandName,
                  EnvDTE.vsCommandExecOption executeOption,
                  ref object varIn,
                  ref object varOut,
                  ref bool handled)
        {
            try
            {
                handled = false;
                if (executeOption == EnvDTE.vsCommandExecOption.vsCommandExecOptionDoDefault)
                {
                    switch (commandName)
                    {
                        case Res.LaunchDesignerFullCommand:
                            handled = true;
                            extLoader.loadDesigner(null);
                            break;
                        case Res.LaunchLinguistFullCommand:
                            handled = true;
                            ExtLoader.loadLinguist(null);
                            break;
                        case Res.ImportProFileFullCommand:
                            handled = true;
                            ExtLoader.ImportProFile();
                            break;
                        case Res.ImportPriFileFullCommand:
                            handled = true;
                            ExtLoader.ImportPriFile(HelperFunctions.GetSelectedQtProject(_applicationObject));
                            break;
                        case Res.ExportPriFileFullCommand:
                            handled = true;
                            ExtLoader.ExportPriFile();
                            break;
                        case Res.ExportProFileFullCommand:
                            handled = true;
                            ExtLoader.ExportProFile();
                            break;
                        case Res.ChangeSolutionQtVersionFullCommand:
                            QtVersionManager vManager = QtVersionManager.The();
                            if (formChangeQtVersion == null)
                                formChangeQtVersion = new FormChangeQtVersion();
                            formChangeQtVersion.UpdateContent(ChangeFor.Solution);
                            if (formChangeQtVersion.ShowDialog() == DialogResult.OK)
                            {
                                string newQtVersion = formChangeQtVersion.GetSelectedQtVersion();
                                if (newQtVersion != null)
                                {
                                    string currentPlatform = null;
                                    try
                                    {
                                        SolutionConfiguration config = _applicationObject.Solution.SolutionBuild.ActiveConfiguration;
                                        SolutionConfiguration2 config2 = config as SolutionConfiguration2;
                                        currentPlatform = config2.PlatformName;
                                    }
                                    catch
                                    {
                                    }
                                    if (string.IsNullOrEmpty(currentPlatform))
                                        return;

                                    foreach (Project project in HelperFunctions.ProjectsInSolution(_applicationObject))
                                    {
                                        if (HelperFunctions.IsQtProject(project))
                                        {
                                            string OldQtVersion = vManager.GetProjectQtVersion(project, currentPlatform);
                                            if (OldQtVersion == null)
                                                OldQtVersion = vManager.GetDefaultVersion();

                                            QtProject qtProject = QtProject.Create(project);
                                            bool newProjectCreated = false;
                                            qtProject.ChangeQtVersion(OldQtVersion, newQtVersion, ref newProjectCreated);
                                        }
                                    }
                                    vManager.SaveSolutionQtVersion(_applicationObject.Solution, newQtVersion);
                                }
                            }
                            break;
                        case Res.ProjectQtSettingsFullCommand:
                            handled = true;
                            EnvDTE.DTE dte = _applicationObject;
                            Project pro = HelperFunctions.GetSelectedQtProject(dte);
                            if (pro != null)
                            {
                                if (formProjectQtSettings == null)
                                    formProjectQtSettings = new FormProjectQtSettings();
                                formProjectQtSettings.SetProject(pro);
                                formProjectQtSettings.StartPosition = FormStartPosition.CenterParent;
                                MainWinWrapper ww = new MainWinWrapper(dte);
                                formProjectQtSettings.ShowDialog(ww);
                            }
                            else
                                MessageBox.Show(SR.GetString("NoProjectOpened"));
                            break;
                        case Res.ChangeProjectQtVersionFullCommand:
                            handled = true;
                            dte = _applicationObject;
                            pro = HelperFunctions.GetSelectedProject(dte);
                            if (pro != null && HelperFunctions.IsQMakeProject(pro))
                            {
                                if (formChangeQtVersion == null)
                                    formChangeQtVersion = new FormChangeQtVersion();
                                formChangeQtVersion.UpdateContent(ChangeFor.Project);
                                MainWinWrapper ww = new MainWinWrapper(dte);
                                if (formChangeQtVersion.ShowDialog(ww) == DialogResult.OK)
                                {
                                    string qtVersion = formChangeQtVersion.GetSelectedQtVersion();
                                    QtVersionManager vm = QtVersionManager.The();
                                    string qtPath = vm.GetInstallPath(qtVersion);
                                    HelperFunctions.SetDebuggingEnvironment(pro, "PATH=" + qtPath + "\\bin;$(PATH)", true);
                                }
                            }
                            break;
                        case Res.VSQtOptionsFullCommand:
                            handled = true;
                            if (formQtVersions == null)
                            {
                                formQtVersions = new FormVSQtSettings();
                                formQtVersions.LoadSettings();
                            }
                            formQtVersions.StartPosition = FormStartPosition.CenterParent;
                            MainWinWrapper mww = new MainWinWrapper(_applicationObject);
                            if (formQtVersions.ShowDialog(mww) == DialogResult.OK)
                                formQtVersions.SaveSettings();
                            break;
                        case Res.CreateNewTranslationFileFullCommand:
                            handled = true;
                            pro = HelperFunctions.GetSelectedQtProject(_applicationObject);
                            Translation.CreateNewTranslationFile(pro);
                            break;
                        case Res.CommandBarName + ".Connect.lupdate":
                            handled = true;
                            Translation.RunlUpdate(HelperFunctions.GetSelectedFiles(_applicationObject), 
                                HelperFunctions.GetSelectedQtProject(_applicationObject));
                            break;
                        case Res.CommandBarName + ".Connect.lrelease":
                            handled = true;
                            Translation.RunlRelease(HelperFunctions.GetSelectedFiles(_applicationObject));
                            break;
                        case Res.lupdateProjectFullCommand:
                            handled = true;
                            pro = HelperFunctions.GetSelectedQtProject(Connect._applicationObject);
                            Translation.RunlUpdate(pro);
                            break;
                        case Res.lreleaseProjectFullCommand:
                            handled = true;
                            pro = HelperFunctions.GetSelectedQtProject(Connect._applicationObject);
                            Translation.RunlRelease(pro);
                            break;
                        case Res.lupdateSolutionFullCommand:
                            handled = true;
                            Translation.RunlUpdate(Connect._applicationObject.Solution);
                            break;
                        case Res.lreleaseSolutionFullCommand:
                            handled = true;
                            Translation.RunlRelease(Connect._applicationObject.Solution);
                            break;
                        case Res.ConvertToQtFullCommand:
                        case Res.ConvertToQMakeFullCommand:
                            if (MessageBox.Show(SR.GetString("ConvertConfirmation"), SR.GetString("ConvertTitle"), MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                handled = true;
                                dte = _applicationObject;
                                pro = HelperFunctions.GetSelectedProject(dte);
                                HelperFunctions.ToggleProjectKind(pro);
                            }
                            break;
                    }
                }
            }
            catch (System.Exception e)
            {
                MessageBox.Show(e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace);
            }
        }

        /// <summary>This is to support Qt5 and Qt5 development at the same time. Default editor values in registry are changed so that Qt4 add-in is default and Qt5 values are set only when this add-in is loaded.</summary>
        /// <param term='startup'>Is this Qt5 add-in starting (true) or closing (false).</param>
        private void UpdateDefaultEditors(bool startup)
        {
            // This add-in starts so just setting correct default editor
            // registry values for .qrc, .ts and .ui extensions.
            if (startup)
            {
                Qt5DefaultEditors qt5 = new Qt5DefaultEditors();
                qt5.WriteRegistryValues();
            }
            // This add-in closing so checking...
            else
            {
                // ...if also Qt4 addin is installed and setting correct
                // registry values for it
                Qt4DefaultEditors qt4 = new Qt4DefaultEditors();
                if (qt4.IsAddinInstalled())
                {
                    qt4.WriteRegistryValues();
                }
            }
        }
    }
}
