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

using Microsoft.VisualStudio.VCProjectEngine;
using Nokia.QtProjectLib;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Qt4VSAddin
{
	/// <summary>
	/// Summary description for Translation.
	/// </summary>
	public class Translation
	{
        public static bool RunlRelease(VCFile vcFile)
        {
            bool success = true;
            try
            {
                VCProject vcProject = vcFile.project as VCProject;
                EnvDTE.Project project = vcProject.Object as EnvDTE.Project;
                Messages.PaneMessage(project.DTE,
                    "--- (lrelease) file: " + vcFile.FullPath);

                HelperFunctions.StartExternalQtApplication(Resources.lreleaseCommand, vcFile.RelativePath,
                    vcProject.ProjectDirectory, HelperFunctions.GetSelectedQtProject(project.DTE), true,
                    null);
            }
            catch (Qt4VS2003Exception e)
            {
                success = false;
                Messages.DisplayErrorMessage(e.Message);
            }

            return success;
        }

        public static void RunlRelease(VCFile[] vcFiles)
        {
            foreach (VCFile vcFile in vcFiles)
            {
                if (vcFile == null)
                    continue;
                if (HelperFunctions.IsTranslationFile(vcFile))
                {
                    if (!RunlRelease(vcFile))
                        return;
                }
            }
        }

        public static void RunlRelease(EnvDTE.Project project)
        {
            QtProject qtPro = QtProject.Create(project);
            if (qtPro == null)
                return;

            FakeFilter ts = Filters.TranslationFiles();
            VCFilter tsFilter = qtPro.FindFilterFromGuid(ts.UniqueIdentifier);
            if (tsFilter == null)
                return;

            IVCCollection files = tsFilter.Files as IVCCollection;
            foreach (VCFile file in files)
            {
                VCFile vcFile = file as VCFile;
                if (HelperFunctions.IsTranslationFile(vcFile))
                {
                    if (!RunlRelease(vcFile))
                        return;
                }
            }
        }

        public static void RunlRelease(EnvDTE.Solution solution)
        {
            foreach (EnvDTE.Project project in HelperFunctions.ProjectsInSolution(solution.DTE))
                RunlRelease(project);
        }

        public static bool RunlUpdate(VCFile vcFile, EnvDTE.Project pro)
        {
            if (!HelperFunctions.IsQtProject(pro))
                return false;

            string cmdLine = "";
            List<string> headers = HelperFunctions.GetProjectFiles(pro, FilesToList.FL_HFiles);
            List<string> sources = HelperFunctions.GetProjectFiles(pro, FilesToList.FL_CppFiles);
            List<string> uifiles = HelperFunctions.GetProjectFiles(pro, FilesToList.FL_UiFiles);

            foreach (string file in headers)
                cmdLine += file + " ";

            foreach (string file in sources)
                cmdLine += file + " ";

            foreach (string file in uifiles)
                cmdLine += file + " ";

            cmdLine += "-ts " + vcFile.RelativePath;

            int cmdLineLength = cmdLine.Length + Resources.lupdateCommand.Length + 1;
            string temporaryProFile = null;
            if (cmdLineLength > HelperFunctions.GetMaximumCommandLineLength())
            {
                VCProject vcPro = (VCProject) pro.Object;
                temporaryProFile = System.IO.Path.GetTempFileName();
                temporaryProFile = System.IO.Path.GetDirectoryName(temporaryProFile) + "\\" +
                                   System.IO.Path.GetFileNameWithoutExtension(temporaryProFile) + ".pro";
                if (System.IO.File.Exists(temporaryProFile))
                    System.IO.File.Delete(temporaryProFile);
                System.IO.StreamWriter sw = new System.IO.StreamWriter(temporaryProFile);
                writeFilesToPro(sw, "HEADERS",
                    ProjectExporter.ConvertFilesToFullPath(headers, vcPro.ProjectDirectory));
                writeFilesToPro(sw, "SOURCES",
                    ProjectExporter.ConvertFilesToFullPath(sources, vcPro.ProjectDirectory));
                writeFilesToPro(sw, "FORMS",
                    ProjectExporter.ConvertFilesToFullPath(uifiles, vcPro.ProjectDirectory));

                List<string> tsFiles = new List<string>(1);
                tsFiles.Add(vcFile.FullPath);
                writeFilesToPro(sw, "TRANSLATIONS", tsFiles);
                sw.Close();

                cmdLine = "\"" + temporaryProFile + "\"";
            }

            bool success = true;
			try
			{
                Messages.PaneMessage(pro.DTE, "--- (lupdate) file: " + vcFile.FullPath);

                HelperFunctions.StartExternalQtApplication(Resources.lupdateCommand, cmdLine,
                    ((VCProject)vcFile.project).ProjectDirectory, pro, true, null);
			}
			catch (Qt4VS2003Exception e)
			{
                success = false;
                Messages.DisplayErrorMessage(e.Message);
			}

            if (temporaryProFile != null && System.IO.File.Exists(temporaryProFile))
            {
                System.IO.File.Delete(temporaryProFile);
                temporaryProFile = temporaryProFile.Substring(0, temporaryProFile.Length - 3);
                temporaryProFile += "TMP";
                if (System.IO.File.Exists(temporaryProFile))
                    System.IO.File.Delete(temporaryProFile);
            }

            return success;
        }

        private static void writeFilesToPro(System.IO.StreamWriter pro, string section, List<string> files)
        {
            if (files.Count > 0)
            {
                pro.Write(section + " = ");
                foreach (string file in files)
                {
                    pro.WriteLine("\\");
                    pro.Write("\"" + file + "\"");
                }
                pro.WriteLine();
            }
        }

        public static void RunlUpdate(VCFile[] vcFiles, EnvDTE.Project pro)
        {
            foreach (VCFile vcFile in vcFiles)
            {
                if (vcFile == null)
                    continue;
                if (HelperFunctions.IsTranslationFile(vcFile))
                {
                    if (!RunlUpdate(vcFile, pro))
                        return;
                }
            }
        }

        public static void RunlUpdate(EnvDTE.Project project)
        {
            QtProject qtPro = QtProject.Create(project);
            if (qtPro == null)
                return;

            FakeFilter ts = Filters.TranslationFiles();
            VCFilter tsFilter = qtPro.FindFilterFromGuid(ts.UniqueIdentifier);
            if (tsFilter == null)
                return;

            IVCCollection files = tsFilter.Files as IVCCollection;
            foreach (VCFile file in files)
            {
                VCFile vcFile = file as VCFile;
                if (HelperFunctions.IsTranslationFile(vcFile))
                {
                    if (!RunlUpdate(vcFile, project))
                        return;
                }
            }
        }

        public static void RunlUpdate(EnvDTE.Solution solution)
        {
            foreach (EnvDTE.Project project in HelperFunctions.ProjectsInSolution(solution.DTE))
                RunlUpdate(project);
        }

        public static void CreateNewTranslationFile(EnvDTE.Project project)
        {
            if (project == null)
                return;

            AddTranslationDialog transDlg = new AddTranslationDialog(project);
            if (transDlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    QtProject qtPro = QtProject.Create(project);
                    VCFile file = qtPro.AddFileInFilter(Filters.TranslationFiles(), transDlg.TranslationFile, true);
                    Translation.RunlUpdate(file, project);
                }
                catch (Qt4VS2003Exception e)
                {
                    Messages.DisplayErrorMessage(e.Message);
                }
                catch (System.Exception ex)
                {
                    Messages.DisplayErrorMessage(ex.Message);
                }
            }
        }
    }
}
