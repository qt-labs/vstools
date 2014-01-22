/****************************************************************************
**
** Copyright (C) 2012 Digia Plc and/or its subsidiary(-ies).
** Contact: http://www.qt-project.org/legal
**
** This file is part of the Qt VS Add-in.
**
** $QT_BEGIN_LICENSE:LGPL$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Digia.  For licensing terms and
** conditions see http://qt.digia.com/licensing.  For further information
** use the contact form at http://qt.digia.com/contact-us.
**
** GNU Lesser General Public License Usage
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** In addition, as a special exception, Digia gives you certain additional
** rights.  These rights are described in the Digia Qt LGPL Exception
** version 1.1, included in the file LGPL_EXCEPTION.txt in this package.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3.0 as published by the Free Software
** Foundation and appearing in the file LICENSE.GPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU General Public License version 3.0 requirements will be
** met: http://www.gnu.org/copyleft/gpl.html.
**
**
** $QT_END_LICENSE$
**
****************************************************************************/

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Digia.Qt5ProjectLib
{
    public class ProjectImporter
    {
        private EnvDTE.DTE dteObject = null;

#if (VS2010 || VS2012 || VS2013)
        const string projectFileExtension = ".vcxproj";
#else
        const string projectFileExtension = ".vcproj";
#endif


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
            VersionInformation versionInfo = QtVersionManager.The().GetVersionInfo(qtVersion);
            FileInfo VCInfo = RunQmake(mainInfo, ".sln", true, versionInfo);
            if (null == VCInfo)
                return;
            ReplaceAbsoluteQtDirInSolution(VCInfo);

            try
            {
                if (CheckQtVersion(versionInfo))
                {
                    dteObject.Solution.Open(VCInfo.FullName);
                    if (qtVersion != null)
                    {
                        QtVersionManager.The().SaveSolutionQtVersion(dteObject.Solution, qtVersion);
                        foreach (Project prj in HelperFunctions.ProjectsInSolution(dteObject))
                        {
                            QtVersionManager.The().SaveProjectQtVersion(prj, qtVersion);
                            QtProject qtPro = QtProject.Create(prj);
                            qtPro.SetQtEnvironment();
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
            VersionInformation versionInfo = QtVersionManager.The().GetVersionInfo(qtVersion);
            FileInfo VCInfo = RunQmake(mainInfo, projectFileExtension, false, versionInfo);
            if (null == VCInfo)
                return;

            ReplaceAbsoluteQtDirInProject(VCInfo);

            try
            {
                if (CheckQtVersion(versionInfo))
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
                        qtPro.SetQtEnvironment();
                        string platformName = versionInfo.GetVSPlatformName();

                        if (qtVersion != null)
                        {
                            QtVersionManager.The().SaveProjectQtVersion(pro, qtVersion, platformName);
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

        private void ReplaceAbsoluteQtDirInSolution(FileInfo solutionFile)
        {
            List<string> projects = ParseProjectsFromSolution(solutionFile);
            foreach (string project in projects)
            {
                FileInfo projectInfo = new FileInfo(project);
                ReplaceAbsoluteQtDirInProject(projectInfo);
            }
        }

        private static List<string> ParseProjectsFromSolution(FileInfo solutionFile)
        {
            StreamReader sr = solutionFile.OpenText();
            string content = sr.ReadToEnd();
            sr.Close();

            List<string> projects = new List<string>();
            int index = content.IndexOf(projectFileExtension);
            while (index != -1)
            {
                int startIndex = content.LastIndexOf('\"', index, index) + 1;
                int endIndex = content.IndexOf('\"', index);
                projects.Add(content.Substring(startIndex, endIndex - startIndex));
                content = content.Substring(endIndex);
                index = content.IndexOf(projectFileExtension);
            }
            return projects;
        }

        private void ReplaceAbsoluteQtDirInProject(FileInfo projectFile)
        {
            StreamReader sr = projectFile.OpenText();
            string content = sr.ReadToEnd();
            sr.Close();

            string qtDir = ParseQtDirFromFileContent(content);
            if (!string.IsNullOrEmpty(qtDir))
            {
                content = HelperFunctions.ReplaceCaseInsensitive(content, qtDir, "$(QTDIR)\\");
                StreamWriter sw = projectFile.CreateText();
                sw.Write(content);
                sw.Flush();
                sw.Close();
            }
            else
            {
                Messages.PaneMessage(dteObject, SR.GetString("ImportProject_CannotFindQtDirectory", projectFile.Name));
            }
        }

        private static string ParseQtDirFromFileContent(string vcFileContent)
        {
            string uicQtDir = FindQtDirFromExtension(vcFileContent, "bin\\uic.exe");
            string rccQtDir = FindQtDirFromExtension(vcFileContent, "bin\\rcc.exe");
            string mkspecQtDir = FindQtDirFromExtension(vcFileContent, "mkspecs\\default");
            if (!string.IsNullOrEmpty(mkspecQtDir))
            {
                if (!string.IsNullOrEmpty(uicQtDir) && uicQtDir.ToLower() != mkspecQtDir.ToLower())
                {
                    return "";
                }
                if (!string.IsNullOrEmpty(rccQtDir) && rccQtDir.ToLower() != mkspecQtDir.ToLower())
                {
                    return "";
                }
                return mkspecQtDir;
            }
            if (!string.IsNullOrEmpty(uicQtDir))
            {
                if (!string.IsNullOrEmpty(rccQtDir) && rccQtDir.ToLower() != uicQtDir.ToLower())
                {
                    return "";
                }
                return uicQtDir;
            }
            if (!string.IsNullOrEmpty(rccQtDir))
                return rccQtDir;
            return "";
        }

        private static string FindQtDirFromExtension(string content, string extension)
        {
            string s = "";
            int index = -1;
            index = content.ToLower().IndexOf(extension.ToLower());
            if (index != -1)
            {
                s = content.Remove(index);
                index = s.LastIndexOf("CommandLine=");
                if (s.LastIndexOf("AdditionalDependencies=") > index)
                    index = s.LastIndexOf("AdditionalDependencies=");
                if (index != -1)
                {
                    s = s.Substring(index);
                    s = s.Substring(s.IndexOf('=') + 1);
                }

                index = s.LastIndexOf(';');
                if (index != -1)
                    s = s.Substring(index + 1);
            }
            if (!string.IsNullOrEmpty(s))
            {
                s = s.Trim(new char[] { ' ', '\"' , ','});
#if (VS2010 || VS2012 || VS2013)
                if (s.StartsWith(">"))
                    s = s.Substring(1);
#endif
                if (!Path.IsPathRooted(s))
                    s = null;
            }
            return s;
        }

        private static void ApplyPostImportSteps(QtProject qtProject)
        {
            foreach (VCConfiguration cfg in (IVCCollection)qtProject.VCProject.Configurations)
            {
#if (VS2010 || VS2012 || VS2013)
                cfg.IntermediateDirectory = @"$(Platform)\$(Configuration)\";
#else
                cfg.IntermediateDirectory = @"$(PlatformName)\$(ConfigurationName)";
#endif
                CompilerToolWrapper compilerTool = CompilerToolWrapper.Create(cfg);
                if (compilerTool != null)
                {
#if (VS2010 || VS2012 || VS2013)
                    compilerTool.ObjectFile = @"$(IntDir)";
                    compilerTool.ProgramDataBaseFileName = @"$(IntDir)vc$(PlatformToolsetVersion).pdb";
#else
                    compilerTool.ObjectFile = @"$(IntDir)\";
                    compilerTool.ProgramDataBaseFileName = @"$(IntDir)\vc90.pdb";
#endif
                }
            }

            qtProject.RemoveResFilesFromGeneratedFilesFilter();
            qtProject.RepairGeneratedFilesStructure();
            qtProject.TranslateFilterNames();

            QtVSIPSettings.SaveUicDirectory(qtProject.Project, QtVSIPSettings.GetUicDirectory());
            qtProject.UpdateUicSteps(".", false); // false is to not remove given path from includes
            QtVSIPSettings.SaveRccDirectory(qtProject.Project, QtVSIPSettings.GetRccDirectory());
            qtProject.RefreshRccSteps();

            // collapse the generated files/resources filters afterwards
            qtProject.CollapseFilter(Filters.ResourceFiles().Name);
            qtProject.CollapseFilter(Filters.GeneratedFiles().Name);

            try
            {
                // save the project after modification
                qtProject.Project.Save(null);
            }
            catch { /* ignore */ }
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
                            if (firstLoop)
                                firstLoop = false;
                            else
                                commandLineToSet += "\r\n";
                            commandLineToSet += commandLine;
                        }
                        tool.CommandLine = commandLineToSet;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private FileInfo RunQmake(FileInfo mainInfo, string ext, bool recursive, VersionInformation vi)
        {
            string name = mainInfo.Name.Remove(mainInfo.Name.IndexOf('.'));

            FileInfo VCInfo = new FileInfo(mainInfo.DirectoryName + "\\" + name + ext);

            if (!VCInfo.Exists || DialogResult.Yes == MessageBox.Show(SR.GetString("ExportProject_ProjectExistsRegenerateOrReuse", VCInfo.Name),
                SR.GetString("ProjectExists"), MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                Messages.PaneMessage(dteObject, "--- (Import): Generating new project of " + mainInfo.Name + " file");

                InfoDialog dialog = new InfoDialog(mainInfo.Name);
                QMake qmake = new QMake(dteObject, mainInfo.FullName, recursive, vi);

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

        private static bool CheckQtVersion(VersionInformation vi)
        {
            if (!vi.qt5Version)
            {
                Messages.DisplayWarningMessage(SR.GetString("ExportProject_EditProjectFileManually"));
                return false;
            }
            return true;
        }

    }
}
