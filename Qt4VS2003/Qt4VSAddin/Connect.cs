/**************************************************************************
**
** This file is part of the Qt VS Add-in
**
** Copyright (c) 2011 Nokia Corporation and/or its subsidiary(-ies).
**
** Contact: Nokia Corporation (qt-info@nokia.com)
**
** Commercial Usage
**
** Licensees holding valid Qt Commercial licenses may use this file in
** accordance with the Qt Commercial License Agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Nokia.
**
** GNU Lesser General Public License Usage
**
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** If you are unsure which license is appropriate for your use, please
** contact the sales department at http://qt.nokia.com/contact.
**
**************************************************************************/

using System;
using Extensibility;
using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Nokia.QtProjectLib;
using EnvDTE80;

namespace Qt4VSAddin
{
    [GuidAttribute("6A7385B4-1D62-46e0-A4E3-AED4475371F0"), ProgId("Qt4VSAddin")]
	/// <summary>The object for implementing an Add-in.</summary>
	/// <seealso class='IDTExtensibility2' />
    public class Connect : IDTExtensibility2, IDTCommandTarget
	{
		public static DTE _applicationObject = null;
		public static AddIn _addInInstance = null;
        public static ExtLoader extLoader = null;
        public static bool menuVisible = false;

        private AddinInit initializer = null;
        private AddInEventHandler eventHandler = null;
        private FormChangeQtVersion formChangeQtVersion = null;
        private FormVSQtSettings formQtVersions = null;
        private FormProjectQtSettings formProjectQtSettings = null;
        private string installationDir = null;
        private string appWrapperPath = null;
        private bool commandLine = false;

        public static Connect Instance()
        {
            return _addInInstance.Object as Connect;
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
                {
                    string exeName = "qtappwrapper.exe";
                    appWrapperPath = installationDir + exeName;
                    if (!File.Exists(appWrapperPath))
                    {
                        appWrapperPath = installationDir;
                        if (appWrapperPath.EndsWith("\\"))
                            appWrapperPath = appWrapperPath.Remove(appWrapperPath.Length - 1);
                        int idx = appWrapperPath.LastIndexOf('\\');
                        if (idx >= 0 && idx < appWrapperPath.Length - 1)
                            appWrapperPath = appWrapperPath.Remove(idx + 1);
                        appWrapperPath += exeName;
                        if (!File.Exists(appWrapperPath))
                            appWrapperPath = null;
                    }
                }
                return appWrapperPath;
            }
        }

		/// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being loaded.</summary>
		/// <param term='application'>Root object of the host application.</param>
		/// <param term='connectMode'>Describes how the Add-in is being loaded.</param>
		/// <param term='addInInst'>Object representing this Add-in.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
		{
            _applicationObject = (DTE)application;
            _addInInstance = (AddIn)addInInst;

            // determine the installation directory
            System.Uri uri = new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().EscapedCodeBase);
            installationDir = Path.GetDirectoryName(System.Uri.UnescapeDataString(uri.AbsolutePath));
            installationDir += "\\";

            // General startup code
            if ((ext_ConnectMode.ext_cm_AfterStartup == connectMode) ||
                (ext_ConnectMode.ext_cm_Startup == connectMode) ||
                (ext_ConnectMode.ext_cm_CommandLine == connectMode))
            {
                QtVersionManager vm = QtVersionManager.The();
                string error = null;
                if (vm.HasInvalidVersions(out error))
                    Messages.DisplayErrorMessage(error);
                eventHandler = new AddInEventHandler(_applicationObject);
                extLoader = new ExtLoader();
                if (ext_ConnectMode.ext_cm_CommandLine != connectMode)
                {
                    try
                    {
                        initializer = new AddinInit(_applicationObject);
                        initializer.removeCommands();
                        initializer.registerCommands();
                    }
                    catch (System.Exception e)
                    {
                        MessageBox.Show(e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace);
                    }
                }
                else
                    commandLine = true;
            }
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
                                    catch (Exception e)
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

		/// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
		/// <param term='disconnectMode'>Describes how the Add-in is being unloaded.</param>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
		{
            eventHandler.Disconnect();
            if (!commandLine)
            {
                try
                {
                    initializer.removeCommands();
                }
                catch (System.Exception e)
                {
                    MessageBox.Show(e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace);
                }
            }
		}

		/// <summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection of Add-ins has changed.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />		
		public void OnAddInsUpdate(ref Array custom)
		{
		}

		/// <summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the host application has completed loading.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnStartupComplete(ref Array custom)
		{
		}

		/// <summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the host application is being unloaded.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnBeginShutdown(ref Array custom)
		{
		}
    }
}
