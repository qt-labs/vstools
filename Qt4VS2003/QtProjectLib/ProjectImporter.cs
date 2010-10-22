/**************************************************************************
**
** This file is part of the Qt VS Add-in
**
** Copyright (c) 2010 Nokia Corporation and/or its subsidiary(-ies).
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
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Nokia.QtProjectLib
{
    public class ProjectImporter
    {
        private EnvDTE.DTE dteObject = null;

        public ProjectImporter(EnvDTE.DTE dte)
        {
            dteObject = dte;
        }

        public void ImportProFile(string qtVersion)
        {
            FileDialog toOpen = new OpenFileDialog();
            toOpen.FilterIndex = 1;
            toOpen.CheckFileExists = true;
            toOpen.Title = SR.GetString("ExportProject_SelectQtProjectToAdd");
            toOpen.Filter = "Qt Project files (*.pro)|*.pro|All files (*.*)|*.*";

            if (DialogResult.OK != toOpen.ShowDialog())
                return;

            FileInfo mainInfo = new FileInfo(toOpen.FileName);
            if (HelperFunctions.IsSubDirsFile(mainInfo.FullName))
            {
                // we use the safe way. Make the user close the existing solution manually
                if ((!string.IsNullOrEmpty(dteObject.Solution.FullName)) || (HelperFunctions.ProjectsInSolution(dteObject).Count > 0))
                {
                    if (MessageBox.Show(SR.GetString("ExportProject_SubdirsProfileSolutionClose"),
                        SR.GetString("OpenSolution"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question)
                        == DialogResult.OK)
                        dteObject.Solution.Close(true);
                    else
                        return;
                }

                ImportSolution(mainInfo, qtVersion);
            }
            else
            {
                ImportProject(mainInfo, qtVersion);
            }
        }

        private void ImportSolution(FileInfo mainInfo, string qtVersion)
        {
            FileInfo VCInfo = RunQmake(mainInfo, ".sln", true);
            if (null == VCInfo)
                return;

            try
            {
                if (CheckVersionInfo())
                {
                    dteObject.Solution.Open(VCInfo.FullName);
                    if (qtVersion != null)
                    {
                        QtVersionManager.The().SaveSolutionQtVersion(dteObject.Solution, qtVersion);
                        foreach (Project prj in HelperFunctions.ProjectsInSolution(dteObject))
                        {
                            QtVersionManager.The().SaveProjectQtVersion(prj, qtVersion);
                            QtProject qtPro = QtProject.Create(prj);
#if VS2010
                            string newQtDir = QtVersionManager.The().GetInstallPath(qtVersion);
                            qtPro.UpdateQtDirPropertySheet(qtVersion);
#endif
                            ApplyPostImportSteps(qtPro);
                        }
                    }
                }

                Messages.PaneMessage(dteObject, "--- (Import): Finished opening " + VCInfo.Name);
            }
            catch (Exception e)
            {
                Messages.DisplayCriticalErrorMessage(e);
            }
        }

        public void ImportProject(FileInfo mainInfo, string qtVersion)
        {
#if VS2010
            const string projectFileExtension = ".vcxproj";
#else
            const string projectFileExtension = ".vcproj";
#endif
            FileInfo VCInfo = RunQmake(mainInfo, projectFileExtension, false);
            if (null == VCInfo)
                return;

            try
            {
                if (CheckVersionInfo())
                {
                    // no need to add the project again if it's already there...
                    if (!HelperFunctions.IsProjectInSolution(dteObject, VCInfo.FullName))
                    {
                        try
                        {
                            dteObject.Solution.AddFromFile(VCInfo.FullName, false);
                        }
                        catch (Exception /*exception*/)
                        {
                            Messages.PaneMessage(dteObject, "--- (Import): Generated project could not be loaded.");
                            Messages.PaneMessage(dteObject, "--- (Import): Please look in the output above for errors and warnings.");
                            return;
                        }
                        Messages.PaneMessage(dteObject, "--- (Import): Added " + VCInfo.Name + " to Solution");
                    }
                    else
                    {
                        Messages.PaneMessage(dteObject, "Project already in Solution");
                    }

                    EnvDTE.Project pro = null;
                    foreach (EnvDTE.Project p in HelperFunctions.ProjectsInSolution(dteObject))
                    {
                        if (p.FullName.ToLower() == VCInfo.FullName.ToLower())
                        {
                            pro = p;
                            break;
                        }
                    }
                    if (pro != null)
                    {
                        QtProject qtPro = QtProject.Create(pro);
                        VersionInformation versionInfo = new VersionInformation(null);
                        string platformName = versionInfo.GetVSPlatformName();

                        if (qtVersion != null)
                        {
                            QtVersionManager.The().SaveProjectQtVersion(pro, qtVersion, platformName);
#if VS2010
                            string newQtDir = QtVersionManager.The().GetInstallPath(qtVersion);
                            qtPro.UpdateQtDirPropertySheet(qtVersion);
#endif
                        }
                        if (!qtPro.SelectSolutionPlatform(platformName) || !qtPro.HasPlatform(platformName))
                        {
                            bool newProject = false;
                            qtPro.CreatePlatform("Win32", platformName, null, versionInfo, ref newProject);
                            if (!qtPro.SelectSolutionPlatform(platformName))
                            {
                                Messages.PaneMessage(dteObject, "Can't select the platform " + platformName + ".");
                            }
                        }

                        // try to figure out if the project is a plugin project
                        try
                        {
                            string activeConfig = pro.ConfigurationManager.ActiveConfiguration.ConfigurationName;
                            VCConfiguration config = (VCConfiguration)((IVCCollection)qtPro.VCProject.Configurations).Item(activeConfig);
                            if (config.ConfigurationType == ConfigurationTypes.typeDynamicLibrary)
                            {
                                CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
                                VCLinkerTool linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");
                                if (compiler.GetPreprocessorDefinitions().IndexOf("QT_PLUGIN") > -1
                                    && compiler.GetPreprocessorDefinitions().IndexOf("QDESIGNER_EXPORT_WIDGETS") > -1
                                    && compiler.GetAdditionalIncludeDirectories().IndexOf("QtDesigner") > -1
                                    && linker.AdditionalDependencies.IndexOf("QtDesigner") > -1)
                                {
                                    qtPro.MarkAsDesignerPluginProject();
                                }
                            }
                        }
                        catch (Exception)
                        { }

                        ApplyPostImportSteps(qtPro);
                    }
                }
            }
            catch (Exception e)
            {
                Messages.DisplayCriticalErrorMessage(SR.GetString("ExportProject_ProjectOrSolutionCorrupt", e.ToString()));
            }
        }

        private static void ApplyPostImportSteps(QtProject qtProject)
        {
            RepairMocSteps(qtProject.Project);
            HelperFunctions.CleanupQMakeDependencies(qtProject.Project);
            qtProject.RemoveResFilesFromGeneratedFilesFilter();
            qtProject.RepairGeneratedFilesStructure();
            qtProject.TranslateFilterNames();

            QtVSIPSettings.SaveRccDirectory(qtProject.Project, QtVSIPSettings.GetRccDirectory());
            qtProject.RefreshRccSteps();

            // collapse the generated files/resources filters afterwards
            qtProject.CollapseFilter(Filters.ResourceFiles().Name);
            qtProject.CollapseFilter(Filters.GeneratedFiles().Name);
        }

        private static void RepairMocSteps(EnvDTE.Project project)
        {
            VCProject vcProject = project.Object as VCProject;
            if (vcProject == null)
                return;

            foreach (VCFile vcfile in (IVCCollection)vcProject.Files)
            {
                foreach (VCFileConfiguration config in (IVCCollection)vcfile.FileConfigurations)
                {
                    try
                    {
                        VCCustomBuildTool tool = HelperFunctions.GetCustomBuildTool(config);
                        if (tool == null)
                            continue;
                        string[] commandLines = tool.CommandLine.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        string commandLineToSet = "";
                        bool firstLoop = true;
                        for (int i = 0; i < commandLines.Length; i++)
                        {
                            string commandLine = commandLines[i];
                            // If CONFIG contains silent in the pro file, there is an @echo at the beginning of the
                            // command line which we remove.
                            if (commandLine.Contains("moc.exe") && commandLine.StartsWith("@echo"))
                                commandLine = commandLine.Substring(commandLine.IndexOf("&&") + 3);
                            commandLine = RepairMocStepString(commandLine);
                            if (firstLoop)
                                firstLoop = false;
                            else
                                commandLineToSet += "\r\n";
                            commandLineToSet += commandLine;
                        }
                        tool.CommandLine = commandLineToSet;
                        tool.Description = RepairMocStepString(tool.Description);
                        tool.Outputs = RepairMocStepString(tool.Outputs);
                        tool.AdditionalDependencies = RepairMocStepString(tool.AdditionalDependencies);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private static string RepairMocStepString(string str)
        {
            if (str != null)
            {
                str = str.Replace("_(QTDIR)", "$(QTDIR)");
            }
            return str;
        }

        private FileInfo RunQmake(FileInfo mainInfo, string ext, bool recursive)
        {
            string name = mainInfo.Name.Remove(mainInfo.Name.IndexOf('.'));

            FileInfo VCInfo = new FileInfo(mainInfo.DirectoryName + "\\" + name + ext);

            if (!VCInfo.Exists || DialogResult.Yes == MessageBox.Show(SR.GetString("ExportProject_ProjectExistsRegenerateOrReuse", VCInfo.Name),
                SR.GetString("ProjectExists"), MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                Messages.PaneMessage(dteObject, "--- (Import): Generating new project of " + mainInfo.Name + " file");

                InfoDialog dialog = new InfoDialog(mainInfo.Name);
                QMake qmake = new QMake(dteObject, mainInfo.FullName, recursive);

                qmake.CloseEvent += new QMake.ProcessEventHandler(dialog.CloseEventHandler);
                qmake.PaneMessageDataEvent += new QMake.ProcessEventHandlerArg(this.PaneMessageDataReceived);

                System.Threading.Thread qmakeThread = new System.Threading.Thread(new ThreadStart(qmake.RunQMake));
                qmakeThread.Start();
                dialog.ShowDialog();
                qmakeThread.Join();

                if (qmake.ErrorValue == 0)
                    return VCInfo;
            }

            return null;
        }

        private void PaneMessageDataReceived(string data)
        {
            Messages.PaneMessage(dteObject, data);
        }

        private static bool CheckVersionInfo()
        {
            VersionInformation vi = new VersionInformation(null);
            if (!vi.qt4Version)
            {
                Messages.DisplayWarningMessage(SR.GetString("ExportProject_EditProjectFileManually"));
                return false;
            }
            return true;
        }

    }
}
