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
using System.Xml;
using Extensibility;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.VCProjectEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using CompilerToolSpace;
using System.Collections.Generic;

namespace AddInAutoTest
{
    /// <summary>The object for implementing an Add-in.</summary>
    /// <seealso class='IDTExtensibility2' />
    public class Connect : IDTExtensibility2
    {
        enum ProjectDirectory
        {
            MocDir,
            RccDir,
            UicDir
        };
        System.Threading.Thread svthread;
        UdpClient server;
#if VS2005
        string templatePath = "TemplateProjects2005" + Path.DirectorySeparatorChar;
#elif VS2008
        string templatePath = "TemplateProjects2008" + Path.DirectorySeparatorChar;
#elif VS2010
        string templatePath = "TemplateProjects2010" + Path.DirectorySeparatorChar;
#endif

        private StreamWriter logger = null;
        private string testPath = null;
        private List<String> extensions = new List<String>();
        private List<string> mocDirectories = new List<string>();

        private String BackupSolution(string path, string name)
        {
            try
            {
                if (_applicationObject.Solution != null)
                    _applicationObject.Solution.Close(false);

                System.Threading.Thread.Sleep(500);
                char sep = Path.DirectorySeparatorChar;
                if (Directory.Exists(path + "tmp"))
                    Directory.Delete(path + "tmp", true);
                System.Threading.Thread.Sleep(500);
                CopyDirectory(path + name, path + "tmp");
                return path + "tmp" + sep;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        private Project GetProject(string projectName, Solution solution)
        {
            foreach (Project p in solution.Projects)
                if (p.Name.ToLower() == projectName.ToLower())
                    return p;
            return null;
        }

        private void CopyDirectory(string Src, string Dst)
        {
            String[] Files;

            if (Dst[Dst.Length - 1] != Path.DirectorySeparatorChar)
                Dst += Path.DirectorySeparatorChar;
            if (!Directory.Exists(Dst)) Directory.CreateDirectory(Dst);
            Files = Directory.GetFileSystemEntries(Src);
            foreach (string Element in Files)
            {
                if (Directory.Exists(Element))
                    CopyDirectory(Element, Dst + Path.GetFileName(Element));
                else
                    File.Copy(Element, Dst + Path.GetFileName(Element), true);
            }
        }

        private bool ProjectItemContainsString(ProjectItem item, string str)
        {
            item.Open(EnvDTE.Constants.vsViewKindPrimary);
            if (item.Document == null)
                return false;

            item.Document.Activate();
            return File.ReadAllText(item.Document.FullName).Contains(str);
        }

        private Exception ReplaceStringInProjectItem(ProjectItem item, string oldValue, string newValue)
        {
            item.Open(EnvDTE.Constants.vsViewKindPrimary);
            item.Document.Activate();
            if (!ProjectItemContainsString(item, oldValue))
                return new Exception(item.Document.Name + " does not contain \"" + oldValue + "\"");
            item.Document.ReplaceText(oldValue, newValue, (int)vsFindOptions.vsFindOptionsNone);
            /*if (item.Document.Save(item.Document.FullName) == vsSaveStatus.vsSaveCancelled)
            {
                return new Exception("File Dialog");
            }*/
#if !VS2010            
            System.Threading.Thread.Sleep(7500);
#endif
            item.Document.Save(item.Document.FullName);
#if !VS2010
            System.Threading.Thread.Sleep(500);
#endif
            item.Document.Close(vsSaveChanges.vsSaveChangesNo);
            return null;
        }

        private Exception RebuildSolution()
        {
            _applicationObject.Solution.SolutionBuild.Clean(true);

            _applicationObject.Solution.SolutionBuild.Build(true);
            while (_applicationObject.Solution.SolutionBuild.BuildState == vsBuildState.vsBuildStateInProgress)
                System.Threading.Thread.Sleep(100);
            if (_applicationObject.Solution.SolutionBuild.LastBuildInfo != 0)
                return new Exception("Build process failed");
            return null;
        }

        private Exception ChangeIncludeAndUpdateMoc(ProjectItem header, ProjectItem source,
            string oldInclude, string newInclude)
        {
            Exception e = null;
            e = ReplaceStringInProjectItem(header, "Q_OBJECT", "//Q_OBJECT_HERE");
            if (e != null)
                return e;
            e = ReplaceStringInProjectItem(source, oldInclude, newInclude);
            if (e != null)
                return e;
            e = ReplaceStringInProjectItem(header, "//Q_OBJECT_HERE", "Q_OBJECT");
            if (e != null)
                return e;
            return null;
        }

        private bool FileExistsAndExcludedFromBuild(VCProject vcProject, string filename)
        {
            foreach (VCFile file in (IVCCollection)vcProject.Files)
            {
                foreach (VCFileConfiguration fileConfig in (IVCCollection)file.FileConfigurations)
                {
                    if (file.Name == filename && !fileConfig.ExcludedFromBuild)
                            return false;
                }
            }
            return true;            
        }

        public void AutoTestCase1()
        {
            foreach (String extension in extensions)
            {
                foreach (string mocDirectory in mocDirectories)
                {
                    bool success = true;
                    Exception currentException = null;
                    try
                    {
                        logger.WriteLine(DateTime.Now.ToString() + ": Case1 (adding Q_OBJECT macro) begins");
                        logger.WriteLine("\textension: " + extension);
                        logger.WriteLine("\tmoc directory: " + mocDirectory);
                        String solutionRootDir = BackupSolution(testPath + templatePath, "Test1" + extension);
                        _applicationObject.Solution.Open(solutionRootDir + "Test1" + extension + ".sln");
                        Solution solution = _applicationObject.Solution;
                        if (solution == null)
                            throw new Exception(DateTime.Now.ToString() + "Case1: Could not open solution");

                        Project project = GetProject("Test1", solution);
                        SetProjectDirectory(ref project, ProjectDirectory.MocDir, mocDirectory);

                        ProjectItem piSource = project.ProjectItems.Item("Source Files");
                        ProjectItem main = piSource.ProjectItems.Item("main.cpp");

                        currentException = ReplaceStringInProjectItem(main, "//Q_OBJECT_HERE", "Q_OBJECT");
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString() + "Case1: "
                                + currentException.Message);
                        currentException = ReplaceStringInProjectItem(main, "//#include \"main.moc\"", "#include \"main.moc\"");
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString() + "Case1: "
                                + currentException.Message);

                        ProjectItem sub = piSource.ProjectItems.Item("sub.cpp");

                        currentException = ReplaceStringInProjectItem(sub, "//Q_OBJECT_HERE", "Q_OBJECT");
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString() + "Case1: "
                                + currentException.Message);
                        currentException = ReplaceStringInProjectItem(sub, "//#include \"sub.moc\"", "#include \"sub.moc\"");
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString() + "Case1: "
                                + currentException.Message);

                        ProjectItem piHeader = project.ProjectItems.Item("Header Files");
                        ProjectItem foo = piHeader.ProjectItems.Item("foo.h");

                        currentException = ReplaceStringInProjectItem(foo, "//Q_OBJECT_HERE", "Q_OBJECT");
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString() + "Case1: "
                                + currentException.Message);


                        bool excludedCorrectly = true;
                        VCProject vcProject = project.Object as VCProject;
                        foreach (VCFile file in (IVCCollection)vcProject.Files)
                        {
                            if (file.Name == "moc_foo.cpp" || file.Name == "main.moc"
                                        || file.Name == "sub.moc")
                            {

                                ProjectItem fileItem = file.Object as ProjectItem;
                                VCProjectItem vcFileItem = fileItem.Object as VCProjectItem;
                                VCFilter itemFilter = vcFileItem.Parent as VCFilter;
                                if (itemFilter == null)
                                {
                                    success = false;
                                    break;
                                }
                                foreach (VCFileConfiguration fileConfig in (IVCCollection)file.FileConfigurations)
                                {
                                    foreach (VCConfiguration projectConfig in vcProject.Configurations as IVCCollection)
                                    {
                                        if (fileConfig.ExcludedFromBuild == fileConfig.Name.StartsWith(itemFilter.Name))
                                        {
                                            excludedCorrectly = false;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        if (!excludedCorrectly)
                            throw new Exception(DateTime.Now.ToString()
                                + ": Case1: Moc Files were not generated (correctly)");
                        else
                            logger.WriteLine(DateTime.Now.ToString()
                                + ": Case1: Moc Files were generated correctly");


                        currentException = RebuildSolution();
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString()
                                + ": Case1: " + currentException.Message);
                        else
                            logger.WriteLine(DateTime.Now.ToString()
                                + ": Case1: Build process succeeded");
                    }
                    catch (Exception e)
                    {
                        success = false;
                        logger.WriteLine(e.Message);
                    }
                    if (success)
                        logger.WriteLine(DateTime.Now.ToString() + ": Case1 succeeded");
                    else
                        logger.WriteLine(DateTime.Now.ToString() + ": Case1 failed");
                    logger.WriteLine("");
                }
            }
        }

        private void AutoTestCase2()
        {
            foreach (String extension in extensions)
            {
                foreach (string mocDirectory in mocDirectories)
                {
                    bool success = true;
                    Exception currentException = null;
                    try
                    {
                        success = true;
                        logger.WriteLine(DateTime.Now.ToString()
                            + ": Case2 (remove the Q_OBJECT macro from header) begins");
                        logger.WriteLine("\textension: " + extension);
                        logger.WriteLine("\tmoc directory: " + mocDirectory);

                        String solutionRootDir = BackupSolution(testPath + templatePath, "Test2" + extension);
                        _applicationObject.Solution.Open(solutionRootDir + "Test2" + extension + ".sln");
                        Solution solution = _applicationObject.Solution;
                        if (solution == null)
                            throw new Exception(DateTime.Now.ToString() + ": Case2: Could not open solution");

                        Project pro = GetProject("Test2", solution);
                        SetProjectDirectory(ref pro, ProjectDirectory.MocDir, mocDirectory);

                        ProjectItem piSource = pro.ProjectItems.Item("Source Files");
                        ProjectItem main = piSource.ProjectItems.Item("main.cpp");

                        main.Open(EnvDTE.Constants.vsViewKindPrimary);
                        if (ProjectItemContainsString(main, "//Q_OBJECT"))
                            throw new Exception(DateTime.Now.ToString()
                                + ": Case2: " + "\"Q_OBJECT\" is commented out in " + main.Document.Name);
                        System.Threading.Thread.Sleep(5000);
                        main.Document.Close(vsSaveChanges.vsSaveChangesYes);
                        currentException = ReplaceStringInProjectItem(main, "Q_OBJECT", "//Q_OBJECT_HERE");
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString() + ": Case2: "
                                + currentException.Message);
                        currentException = ReplaceStringInProjectItem(main, "#include \"main.moc\"", "//#include \"main.moc\"");
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString() + "Case1: "
                                + currentException.Message);

                        ProjectItem sub = piSource.ProjectItems.Item("sub.cpp");

                        currentException = ReplaceStringInProjectItem(sub, "Q_OBJECT", "//Q_OBJECT_HERE");
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString() + "Case1: "
                                + currentException.Message);
                        currentException = ReplaceStringInProjectItem(sub, "#include \"sub.moc\"", "//#include \"sub.moc\"");
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString() + "Case1: "
                                + currentException.Message);

                        ProjectItem piHeader = pro.ProjectItems.Item("Header Files");
                        ProjectItem foo = piHeader.ProjectItems.Item("foo.h");

                        foo.Open(EnvDTE.Constants.vsViewKindPrimary);
                        if (ProjectItemContainsString(foo, "//Q_OBJECT_HERE"))
                            throw new Exception(DateTime.Now.ToString()
                                + ": Case2: " + "\"Q_OBJECT\" is commented out in " + foo.Document.Name);
                        currentException = ReplaceStringInProjectItem(foo, "Q_OBJECT", "//Q_OBJECT_HERE");
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString() + ": Case2: "
                                + currentException.Message);

                        bool mocFound = false;
                        VCProject vcProject = (VCProject)pro.Object;
                        foreach (VCFile file in (IVCCollection)vcProject.Files)
                        {
                            if (file.Name == "main.moc" || file.Name == "moc_foo.cpp" || file.Name == "sub.moc")
                            {
                                mocFound = true;
                                break;
                            }
                        }
                        if (mocFound)
                            throw new Exception(DateTime.Now.ToString()
                                + ": Case2: Moc Files were not deleted");
                        else
                            logger.WriteLine(DateTime.Now.ToString()
                                + ": Case2: Moc Files were deleted as supposed");

                        currentException = RebuildSolution();
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString()
                                + ": Case2: " + currentException.Message);
                        else
                            logger.WriteLine(DateTime.Now.ToString()
                                + ": Case2: Build process succeeded");
                    }
                    catch (Exception e)
                    {
                        success = false;
                        logger.WriteLine(e.Message);
                    }
                    if (success)
                        logger.WriteLine(DateTime.Now.ToString() + ": Case2 succeeded");
                    else
                        logger.WriteLine(DateTime.Now.ToString() + ": Case2 failed");
                    logger.WriteLine("");
                }
            }
        }

        private void AutoTestCase3()
        {
            foreach (String extension in extensions)
            {
                foreach (string mocDirectory in mocDirectories)
                {
                    bool success = true;
                    bool subsuccess = true;
                    Exception currentException = null;
                    try
                    {
                        logger.WriteLine(DateTime.Now.ToString()
                            + ": Case3 (directly include the moc file and save the header file) begins");
                        logger.WriteLine("\textension: " + extension);
                        logger.WriteLine("\tmoc directory: " + mocDirectory);

                        String solutionRootDir = BackupSolution(testPath + templatePath, "Test1" + extension);
                        _applicationObject.Solution.Open(solutionRootDir + "Test1" + extension + ".sln");
                        Solution solution = _applicationObject.Solution;
                        if (solution == null)
                            throw new Exception(DateTime.Now.ToString() + "Case3: Could not open solution");

                        Project project = GetProject("Test1", solution);
                        SetProjectDirectory(ref project, ProjectDirectory.MocDir, mocDirectory);

                        ProjectItem piHeader = project.ProjectItems.Item("Header Files");
                        ProjectItem foo = piHeader.ProjectItems.Item("foo.h");

                        ProjectItem piSource = project.ProjectItems.Item("Source Files");
                        ProjectItem foocpp = piSource.ProjectItems.Item("foo.cpp");

                        foo.Open(EnvDTE.Constants.vsViewKindPrimary);
                        if (ProjectItemContainsString(foo, "Q_OBJECT")
                            && !ProjectItemContainsString(foo, "//Q_OBJECT"))
                            throw new Exception(DateTime.Now.ToString()
                                + ": Case3: \"" + foo.Document.Name + "\" contains Q_OBJECT");
                        foo.Document.Close(vsSaveChanges.vsSaveChangesNo);
                        if (foo.IsDirty)
                            throw new Exception(DateTime.Now.ToString()
                                + ": Case3: " + foo.Document.Name + " is dirty");

                        string currentInclude = "#include \"moc_foo.cpp\"";
                        currentException = ReplaceStringInProjectItem(foocpp,
                            "////#CUSTOMINCLUDE", currentInclude);
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString()
                                + ": Case3: " + currentException.Message);
                        currentException = ReplaceStringInProjectItem(foo, "//Q_OBJECT_HERE", "Q_OBJECT");

                        VCProject vcProject = (VCProject)project.Object;
                        if (!FileExistsAndExcludedFromBuild(vcProject, "moc_foo.cpp"))
                        {
                            logger.WriteLine(DateTime.Now.ToString() + ": Case3: Moc Files were not "
                                + "found or not excluded from build (using " + currentInclude + ")");
                            success = false;
                            subsuccess = false;
                        }
                        else
                            logger.WriteLine(DateTime.Now.ToString() + ": Case3: Moc Files were "
                                + "excluded from build (using " + currentInclude + ")");

                        if (subsuccess)
                        {
                            currentException = RebuildSolution();
                            if (currentException != null)
                            {
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case3: " + currentException.Message);
                                success = false;
                            }
                            else
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case3: Build process succeeded (using : " + currentInclude + ")");
                        }
                        else
                        {
                            currentInclude = "////#CUSTOMINCLUDE";
                            solutionRootDir = BackupSolution(testPath + templatePath, "Test1" + extension);
                            _applicationObject.Solution.Open(solutionRootDir + "Test1" + extension + ".sln");
                            solution = _applicationObject.Solution;
                        }

                        subsuccess = true;
                        currentException = ChangeIncludeAndUpdateMoc(foo, foocpp, currentInclude, "/*schnusel*/ #include \"moc_foo.cpp\" // bla bla bla");
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString()
                                + ": Case3: " + currentException.Message);
                        currentInclude = "/*schnusel*/ #include \"moc_foo.cpp\" // bla bla bla";

                        if (!FileExistsAndExcludedFromBuild(vcProject, "moc_foo.cpp"))
                        {
                            logger.WriteLine(DateTime.Now.ToString() + ": Case3: Moc Files were not "
                                + "found or not excluded from build (using " + currentInclude + ")");
                            subsuccess = false;
                            success = false;
                        }
                        else
                            logger.WriteLine(DateTime.Now.ToString() + ": Case3: Moc Files were "
                                + "excluded from build (using " + currentInclude + ")");

                        if (subsuccess)
                        {
                            currentException = RebuildSolution();
                            if (currentException != null)
                            {
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case3: " + currentException.Message);
                                success = false;
                            }
                            else
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case3: Build process succeeded (using : " + currentInclude + ")");
                        }
                        else
                        {
                            currentInclude = "////#CUSTOMINCLUDE";
                            solutionRootDir = BackupSolution(testPath + templatePath, "Test1" + extension);
                            _applicationObject.Solution.Open(solutionRootDir + "Test1" + extension + ".sln");
                            solution = _applicationObject.Solution;
                        }

                        subsuccess = true;
                        currentException = ChangeIncludeAndUpdateMoc(foo, foocpp, currentInclude, "#include <moc_foo.cpp>");
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString()
                                + ": Case3: " + currentException.Message);
                        currentInclude = "#include <moc_foo.cpp>";

                        if (!FileExistsAndExcludedFromBuild(vcProject, "moc_foo.cpp"))
                        {
                            logger.WriteLine(DateTime.Now.ToString() + ": Case3: Moc Files were not "
                                + "found or not excluded from build (using " + currentInclude + ")");
                            subsuccess = false;
                            success = false;
                        }
                        else
                            logger.WriteLine(DateTime.Now.ToString() + ": Case3: Moc Files were "
                                + "excluded from build (using " + currentInclude + ")");

                        if (subsuccess)
                        {
                            currentException = RebuildSolution();
                            if (currentException != null)
                            {
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case3: " + currentException.Message);
                                success = false;
                            }
                            else
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case3: Build process succeeded (using : " + currentInclude + ")");
                        }
                    }
                    catch (Exception e)
                    {
                        success = false;
                        logger.WriteLine(e.Message);
                    }
                    if (success)
                        logger.WriteLine(DateTime.Now.ToString() + ": Case3 succeeded");
                    else
                        logger.WriteLine(DateTime.Now.ToString() + ": Case3 failed");
                    logger.WriteLine("");
                }
            }
        }

        private void AutoTestCase4()
        {
            foreach (String extension in extensions)
            {
                foreach (string mocDirectory in mocDirectories)
                {
                    bool success = true;
                    bool subsuccess = true;
                    Exception currentException = null;
                    try
                    {
                        logger.WriteLine(DateTime.Now.ToString()
                            + ": Case4 (directly include the moc file and save the source file) begins");
                        logger.WriteLine("\textension: " + extension);
                        logger.WriteLine("\tmoc directory: " + mocDirectory);

                        String solutionRootDir = BackupSolution(testPath + templatePath, "Test2" + extension);
                        _applicationObject.Solution.Open(solutionRootDir + "Test2" + extension + ".sln");
                        Solution solution = _applicationObject.Solution;
                        if (solution == null)
                            throw new Exception(DateTime.Now.ToString() + "Case4: Could not open solution");

                        Project project = GetProject("Test2", solution);
                        SetProjectDirectory(ref project, ProjectDirectory.MocDir, mocDirectory);
                        ProjectItem piHeader = project.ProjectItems.Item("Header Files");
                        ProjectItem foo = piHeader.ProjectItems.Item("foo.h");

                        ProjectItem piSource = project.ProjectItems.Item("Source Files");
                        ProjectItem foocpp = piSource.ProjectItems.Item("foo.cpp");

                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString() + ": Case4: "
                                + currentException.Message);

                        foo.Open(EnvDTE.Constants.vsViewKindPrimary);
                        if (!ProjectItemContainsString(foo, "Q_OBJECT")
                            || ProjectItemContainsString(foo, "//Q_OBJECT"))
                            throw new Exception(DateTime.Now.ToString()
                                + ": Case4: \"" + foo.Document.Name + "\" does not contain Q_OBJECT");
                        foo.Document.Close(vsSaveChanges.vsSaveChangesNo);
                        if (foo.IsDirty)
                            throw new Exception(DateTime.Now.ToString()
                                + ": Case4: " + foo.Document.Name + " is dirty");

                        string currentInclude = "#include \"moc_foo.cpp\"";
                        currentException = ReplaceStringInProjectItem(foocpp,
                            "////#CUSTOMINCLUDE", currentInclude);
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString()
                                + ": Case4: " + currentException.Message);

                        VCProject vcProject = (VCProject)project.Object;
                        if (!FileExistsAndExcludedFromBuild(vcProject, "moc_foo.cpp"))
                        {
                            logger.WriteLine(DateTime.Now.ToString() + ": Case4: Moc Files were not "
                                + "found or not excluded from build (using " + currentInclude + ")");
                            subsuccess = false;
                            success = false;
                        }
                        else
                            logger.WriteLine(DateTime.Now.ToString() + ": Case4: Moc Files were "
                                + "excluded from build (using " + currentInclude + ")");

                        if (subsuccess)
                        {
                            currentException = RebuildSolution();
                            if (currentException != null)
                            {
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case4: " + currentException.Message);
                                success = false;
                            }
                            else
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case4: Build process succeeded (using : " + currentInclude + ")");
                        }
                        else
                        {
                            currentInclude = "////#CUSTOMINCLUDE";
                            solutionRootDir = BackupSolution(testPath + templatePath, "Test2" + extension);
                            _applicationObject.Solution.Open(solutionRootDir + "Test2" + extension + ".sln");
                            solution = _applicationObject.Solution;
                        }

                        subsuccess = true;
                        currentException = ReplaceStringInProjectItem(foocpp, currentInclude, "/*schnusel*/ #include \"moc_foo.cpp\" // bla bla bla");
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString()
                                + ": Case4: " + currentException.Message);
                        currentInclude = "/*schnusel*/ #include \"moc_foo.cpp\" // bla bla bla";

                        if (!FileExistsAndExcludedFromBuild(vcProject, "moc_foo.cpp"))
                        {
                            logger.WriteLine(DateTime.Now.ToString() + ": Case4: Moc Files were not "
                                + "found or not excluded from build (using " + currentInclude + ")");
                            subsuccess = false;
                            success = false;
                        }
                        else
                            logger.WriteLine(DateTime.Now.ToString() + ": Case4: Moc Files were "
                                + "excluded from build (using " + currentInclude + ")");

                        if (subsuccess)
                        {
                            currentException = RebuildSolution();
                            if (currentException != null)
                            {
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case4: " + currentException.Message);
                                success = false;
                            }
                            else
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case4: Build process succeeded (using : " + currentInclude + ")");
                        }
                        else
                        {
                            currentInclude = "////#CUSTOMINCLUDE";
                            solutionRootDir = BackupSolution(testPath + templatePath, "Test2" + extension);
                            _applicationObject.Solution.Open(solutionRootDir + "Test2" + extension + ".sln");
                            solution = _applicationObject.Solution;
                        }

                        subsuccess = true;
                        currentException = ReplaceStringInProjectItem(foocpp, currentInclude, "#include <moc_foo.cpp>");
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString()
                                + ": Case4: " + currentException.Message);
                        currentInclude = "#include <moc_foo.cpp>";

                        if (!FileExistsAndExcludedFromBuild(vcProject, "moc_foo.cpp"))
                        {
                            logger.WriteLine(DateTime.Now.ToString() + ": Case4: Moc Files were not "
                                + "found or not excluded from build (using " + currentInclude + ")");
                            subsuccess = false;
                            success = false;
                        }
                        else
                            logger.WriteLine(DateTime.Now.ToString() + ": Case4: Moc Files were "
                                + "excluded from build (using " + currentInclude + ")");

                        if (subsuccess)
                        {
                            currentException = RebuildSolution();
                            if (currentException != null)
                            {
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case4: " + currentException.Message);
                                success = false;
                            }
                            else
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case4: Build process succeeded (using : " + currentInclude + ")");
                        }
                    }
                    catch (Exception e)
                    {
                        success = false;
                        logger.WriteLine(e.Message);
                    }
                    if (success)
                        logger.WriteLine(DateTime.Now.ToString() + ": Case4 succeeded");
                    else
                        logger.WriteLine(DateTime.Now.ToString() + ": Case4 failed");
                    logger.WriteLine("");
                }
            }
        }

        private void AutoTestCase5()
        {
            foreach (String extension in extensions)
            {
                foreach (string mocDirectory in mocDirectories)
                {
                    bool success = true;
                    Exception currentException = null;
                    try
                    {
                        logger.WriteLine(DateTime.Now.ToString()
                            + ": Case5 (Change Preprocessor Definitions) begins");
                        logger.WriteLine("\textension: " + extension);
                        logger.WriteLine("\tmoc directory: " + mocDirectory);

                        String solutionRootDir = BackupSolution(testPath + templatePath, "Test2" + extension);
                        _applicationObject.Solution.Open(solutionRootDir + "Test2" + extension + ".sln");
                        Solution solution = _applicationObject.Solution;
                        if (solution == null)
                            throw new Exception(DateTime.Now.ToString() + ": Case5: Could not open solution");

                        Project project = GetProject("Test2", solution);
                        SetProjectDirectory(ref project, ProjectDirectory.MocDir, mocDirectory);
                        VCProject vcProject = (VCProject)project.Object;
                        foreach (VCConfiguration proConfig in (IVCCollection)vcProject.Configurations)
                        {
                            string define = proConfig.Name.Remove(proConfig.Name.IndexOf('|'));
                            CompilerToolWrapper compiler = new CompilerToolWrapper(proConfig);
                            compiler.AddPreprocessorDefinitions(define);
                        }

                        vcProject.Save();

                        currentException = RebuildSolution();
                        if (currentException != null)
                        {
                            logger.WriteLine(DateTime.Now.ToString()
                                + ": Case5: " + currentException.Message);
                            success = false;
                        }
                        else
                            logger.WriteLine(DateTime.Now.ToString()
                                + ": Case5: Build process succeeded");

                        if (success)
                        {
                            foreach (VCFile file in (IVCCollection)vcProject.Files)
                            {
                                if (file.Name == "foo.h")
                                {
                                    IVCCollection fileConfigs = (IVCCollection)file.FileConfigurations;
                                    foreach (VCFileConfiguration fileConfig in fileConfigs)
                                    {
                                        string define = fileConfig.Name.Remove(fileConfig.Name.IndexOf('|'));
                                        VCCustomBuildTool buildTool = (VCCustomBuildTool)fileConfig.Tool;
                                        if (buildTool != null)
                                        {
                                            if (!buildTool.CommandLine.Contains(define))
                                            {
                                                logger.WriteLine(DateTime.Now.ToString() + ": Case5: Preprocessor definition was not "
                                                    + "added to the custom build step (" + fileConfig.Name + ")");
                                                success = false;
                                            }
                                            else
                                                logger.WriteLine(DateTime.Now.ToString() + ": Case5: Preprocessor definition was "
                                                    + "added to the custom build step (" + fileConfig.Name + ")");
                                        }
                                        else
                                            success = false;
                                    }
                                }
                            }

                            if (!success)
                            {
                                logger.WriteLine(DateTime.Now.ToString() + ": Case5: Preprocessor definition was not "
                                    + "added to all custom build steps");
                            }
                            else
                            {
                                logger.WriteLine(DateTime.Now.ToString() + ": Case5: Preprocessor definition was "
                                    + "added to all custom build steps");

                            }
                        }
                    }
                    catch (Exception e)
                    {
                        success = false;
                        logger.WriteLine(e.Message);
                    }
                    if (success)
                        logger.WriteLine(DateTime.Now.ToString() + ": Case5 succeeded");
                    else
                        logger.WriteLine(DateTime.Now.ToString() + ": Case5 failed");
                    logger.WriteLine("");
                }
            }
        }

        private void AutoTestCase6()
        {
            foreach (String extension in extensions)
            {
                foreach (string mocDirectory in mocDirectories)
                {
                    bool success = true;
                    Exception currentException = null;
                    try
                    {
                        logger.WriteLine(DateTime.Now.ToString()
                            + ": Case6 (Change Additional Include Directories) begins");
                        logger.WriteLine("\textension: " + extension);
                        logger.WriteLine("\tmoc directory: " + mocDirectory);

                        String solutionRootDir = BackupSolution(testPath + templatePath, "Test2" + extension);
                        _applicationObject.Solution.Open(solutionRootDir + "Test2" + extension + ".sln");
                        Solution solution = _applicationObject.Solution;
                        if (solution == null)
                            throw new Exception(DateTime.Now.ToString() + ": Case6: "
                                + currentException.Message);

                        Project project = GetProject("Test2", solution);
                        SetProjectDirectory(ref project, ProjectDirectory.MocDir, mocDirectory);
                        VCProject vcProject = (VCProject)project.Object;

                        foreach (VCConfiguration config in (IVCCollection)vcProject.Configurations)
                        {
                            string include = config.Name.Remove(config.Name.IndexOf('|'));
                            CompilerToolWrapper compiler = new CompilerToolWrapper(config);
                            compiler.AddAdditionalIncludeDirectories(include);
                        }
                        vcProject.Save();

                        currentException = RebuildSolution();
                        if (currentException != null)
                        {
                            logger.WriteLine(DateTime.Now.ToString()
                                + ": Case6: " + currentException.Message);
                            success = false;
                        }
                        else
                            logger.WriteLine(DateTime.Now.ToString()
                                + ": Case6: Build process succeeded");

                        if (success)
                        {
                            foreach (VCFile file in (IVCCollection)vcProject.Files)
                            {
                                if (file.Name == "foo.h")
                                {
                                    foreach (VCFileConfiguration fileConfig in (IVCCollection)file.FileConfigurations)
                                    {
                                        string include = fileConfig.Name.Remove(fileConfig.Name.IndexOf('|'));
                                        VCCustomBuildTool buildTool = (VCCustomBuildTool)fileConfig.Tool;
                                        if (buildTool != null)
                                        {
                                            if (!buildTool.CommandLine.Contains("\"-I.\\" + include + "\""))
                                            {
                                                logger.WriteLine(DateTime.Now.ToString() + ": Case6: Additional include lib "
                                                    + include + " was not added to the custom build step (" + fileConfig.Name + ")");
                                                success = false;
                                            }
                                            else
                                                logger.WriteLine(DateTime.Now.ToString() + ": Case6: Additional include lib "
                                                    + include + " was added to the custom build step (" + fileConfig.Name + ")");
                                        }
                                    }
                                }
                            }

                            if (!success)
                            {
                                logger.WriteLine(DateTime.Now.ToString() + ": Case6: Additional include lib was not "
                                    + "added to all custom build steps");
                            }
                            else
                            {
                                logger.WriteLine(DateTime.Now.ToString() + ": Case6: Additional include lib was "
                                    + "added to all custom build steps");

                            }
                        }
                    }
                    catch (Exception e)
                    {
                        success = false;
                        logger.WriteLine(e.Message);
                    }
                    if (!success)
                    {
                        logger.WriteLine(DateTime.Now.ToString() + ": Case6 failed");
                        return;
                    }
                    try
                    {
                        string include = "$(SolutionDir)";
                        String solutionRootDir = BackupSolution(testPath + templatePath, "Test2" + extension);
                        _applicationObject.Solution.Open(solutionRootDir + "Test2" + extension + ".sln");
                        Solution solution = _applicationObject.Solution;
                        if (solution == null)
                            throw new Exception(DateTime.Now.ToString() + ": Case6: "
                                + currentException.Message);

                        Project project = GetProject("Test2", solution);
                        SetProjectDirectory(ref project, ProjectDirectory.MocDir, mocDirectory);
                        VCProject vcProject = (VCProject)project.Object;

                        foreach (VCConfiguration config in (IVCCollection)vcProject.Configurations)
                        {
                            CompilerToolWrapper compiler = new CompilerToolWrapper(config);
                            compiler.AddAdditionalIncludeDirectories(include);
                        }
                        vcProject.Save();

                        currentException = RebuildSolution();
                        if (currentException != null)
                        {
                            logger.WriteLine(DateTime.Now.ToString()
                                + ": Case6: " + currentException.Message);
                            success = false;
                        }
                        else
                            logger.WriteLine(DateTime.Now.ToString()
                                + ": Case6: Build process succeeded");

                        if (success)
                        {
                            foreach (VCFile file in (IVCCollection)vcProject.Files)
                            {
                                if (file.Name == "foo.h")
                                {
                                    foreach (VCFileConfiguration fileConfig in (IVCCollection)file.FileConfigurations)
                                    {
                                        VCCustomBuildTool buildTool = (VCCustomBuildTool)fileConfig.Tool;
                                        if (buildTool != null)
                                        {
                                            if (!buildTool.CommandLine.Contains("\"-I" + include + "\\.\""))
                                            {
                                                logger.WriteLine(DateTime.Now.ToString() + ": Case6: Additional include lib "
                                                    + include + " was not added to the custom build step (" + fileConfig.Name + ")");
                                                success = false;
                                            }
                                            else
                                                logger.WriteLine(DateTime.Now.ToString() + ": Case6: Additional include lib "
                                                    + include + " was added to the custom build step (" + fileConfig.Name + ")");
                                        }
                                    }
                                }
                            }

                            if (!success)
                            {
                                logger.WriteLine(DateTime.Now.ToString() + ": Case6: Additional include lib was not "
                                    + "added to all custom build steps");
                            }
                            else
                            {
                                logger.WriteLine(DateTime.Now.ToString() + ": Case6: Additional include lib was "
                                    + "added to all custom build steps");

                            }
                        }
                    }
                    catch (Exception e)
                    {
                        success = false;
                        logger.WriteLine(e.Message);
                    }
                    if (!success)
                    {
                        logger.WriteLine(DateTime.Now.ToString() + ": Case6 failed");
                        return;
                    }
                    try
                    {
                        string include = "C:\\FOO\\";
                        String solutionRootDir = BackupSolution(testPath + templatePath, "Test2" + extension);
                        _applicationObject.Solution.Open(solutionRootDir + "Test2" + extension + ".sln");
                        Solution solution = _applicationObject.Solution;
                        if (solution == null)
                            throw new Exception(DateTime.Now.ToString() + ": Case6: "
                                + currentException.Message);

                        Project project = GetProject("Test2", solution);
                        SetProjectDirectory(ref project, ProjectDirectory.MocDir, mocDirectory);
                        VCProject vcProject = (VCProject)project.Object;

                        foreach (VCConfiguration config in (IVCCollection)vcProject.Configurations)
                        {
                            CompilerToolWrapper compiler = new CompilerToolWrapper(config);
                            compiler.AddAdditionalIncludeDirectories(include);
                        }
                        vcProject.Save();

                        currentException = RebuildSolution();
                        if (currentException != null)
                        {
                            logger.WriteLine(DateTime.Now.ToString()
                                + ": Case6: " + currentException.Message);
                            success = false;
                        }
                        else
                            logger.WriteLine(DateTime.Now.ToString()
                                + ": Case6: Build process succeeded");

                        if (success)
                        {
                            foreach (VCFile file in (IVCCollection)vcProject.Files)
                            {
                                if (file.Name == "foo.h")
                                {
                                    foreach (VCFileConfiguration fileConfig in (IVCCollection)file.FileConfigurations)
                                    {
                                        VCCustomBuildTool buildTool = (VCCustomBuildTool)fileConfig.Tool;
                                        if (buildTool != null)
                                        {
                                            if (!buildTool.CommandLine.Contains("\"-IC:\\FOO\""))
                                            {
                                                logger.WriteLine(DateTime.Now.ToString() + ": Case6: Additional include lib "
                                                    + include + " was not added to the custom build step (" + fileConfig.Name + ")");
                                                success = false;
                                            }
                                            else
                                                logger.WriteLine(DateTime.Now.ToString() + ": Case6: Additional include lib "
                                                    + include + " was added to the custom build step (" + fileConfig.Name + ")");
                                        }
                                    }
                                }
                            }

                            if (!success)
                            {
                                logger.WriteLine(DateTime.Now.ToString() + ": Case6: Additional include lib was not "
                                    + "added to all custom build steps");
                            }
                            else
                            {
                                logger.WriteLine(DateTime.Now.ToString() + ": Case6: Additional include lib was "
                                    + "added to all custom build steps");

                            }
                        }
                    }
                    catch (Exception e)
                    {
                        success = false;
                        logger.WriteLine(e.Message);
                    }
                    if (success)
                        logger.WriteLine(DateTime.Now.ToString() + ": Case6 succeeded");
                    else
                        logger.WriteLine(DateTime.Now.ToString() + ": Case6 failed");
                    logger.WriteLine("");
                }
            }
        }

        private void AutoTestCase7()
        {
            foreach (String extension in extensions)
            {
                foreach (string mocDirectory in mocDirectories)
                {
                    bool success = true;
                    bool subsuccess = true;
                    Exception currentException = null;
                    try
                    {
                        logger.WriteLine(DateTime.Now.ToString()
                            + ": Case7 (Add user defined custom build steps) begins");
                        logger.WriteLine("\textension: " + extension);
                        logger.WriteLine("\tmoc directory: " + mocDirectory);

                        String solutionRootDir = BackupSolution(testPath + templatePath, "Test1" + extension);
                        _applicationObject.Solution.Open(solutionRootDir + "Test1" + extension + ".sln");
                        Solution solution = _applicationObject.Solution;
                        if (solution == null)
                            throw new Exception(DateTime.Now.ToString() + ": Case7: "
                                + currentException.Message);

                        Project project = GetProject("Test1", solution);
                        SetProjectDirectory(ref project, ProjectDirectory.MocDir, mocDirectory);
                        VCProject vcProject = (VCProject)project.Object;
                        foreach (VCFile file in (IVCCollection)vcProject.Files)
                        {
                            if (file.Name == "foo.h")
                            {
                                IVCCollection fileConfigs = (IVCCollection)file.FileConfigurations;
                                foreach (VCFileConfiguration fileConfig in fileConfigs)
                                {
                                    VCCustomBuildTool buildTool = (VCCustomBuildTool)fileConfig.Tool;
                                    buildTool.CommandLine = buildTool.CommandLine + "T3$T$TR1NG";
                                }
                            }
                        }
                        vcProject.Save();

                        ProjectItem piHeader = project.ProjectItems.Item("Header Files");
                        ProjectItem foo = piHeader.ProjectItems.Item("foo.h");

                        currentException = ReplaceStringInProjectItem(foo, "//Q_OBJECT_HERE", "Q_OBJECT");
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString() + "Case7: "
                                + currentException.Message);


                        foreach (VCFile file in (IVCCollection)vcProject.Files)
                        {
                            if (file.Name == "foo.h")
                            {
                                IVCCollection fileConfigs = (IVCCollection)file.FileConfigurations;
                                foreach (VCFileConfiguration fileConfig in fileConfigs)
                                {
                                    VCCustomBuildTool buildTool = (VCCustomBuildTool)fileConfig.Tool;
                                    if (buildTool != null)
                                        if (!buildTool.CommandLine.Contains("T3$T$TR1NG"))
                                        {
                                            subsuccess = false;
                                            break;
                                        }
                                }
                            }
                        }

                        if (!subsuccess)
                        {
                            logger.WriteLine(DateTime.Now.ToString() + ": Case7: Custom build step was not kept "
                                + "when the Q_OBJECT macro was added");
                            success = false;
                        }
                        else
                        {
                            logger.WriteLine(DateTime.Now.ToString() + ": Case7: Custom build step was kept "
                                + "when the Q_OBJECT macro was added");
                            currentException = RebuildSolution();
                            if (currentException != null)
                            {
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case7: " + currentException.Message);
                                success = false;
                                subsuccess = false;
                            }
                            else
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case7: Build process succeeded");
                        }

                        if (!subsuccess)
                        {
                            solutionRootDir = BackupSolution(testPath + templatePath, "Test2" + extension);
                            _applicationObject.Solution.Open(solutionRootDir + "Test2" + extension + ".sln");
                            solution = _applicationObject.Solution;
                            if (solution == null)
                                throw new Exception(DateTime.Now.ToString() + ": Case7: "
                                    + currentException.Message);

                            foreach (VCFile file in (IVCCollection)vcProject.Files)
                            {
                                if (file.Name == "foo.h")
                                {
                                    IVCCollection fileConfigs = (IVCCollection)file.FileConfigurations;
                                    foreach (VCFileConfiguration fileConfig in fileConfigs)
                                    {
                                        VCCustomBuildTool buildTool = (VCCustomBuildTool)fileConfig.Tool;
                                        buildTool.CommandLine = "T3$T$TR1NG " + buildTool.CommandLine;
                                    }
                                }
                            }
                            vcProject.Save();
                        }

                        currentException = ReplaceStringInProjectItem(foo, "Q_OBJECT", "//Q_OBJECT_HERE");
                        if (currentException != null)
                            throw new Exception(DateTime.Now.ToString() + "Case7: "
                                + currentException.Message);


                        foreach (VCFile file in (IVCCollection)vcProject.Files)
                        {
                            if (file.Name == "foo.h")
                            {
                                IVCCollection fileConfigs = (IVCCollection)file.FileConfigurations;
                                foreach (VCFileConfiguration fileConfig in fileConfigs)
                                {
                                    VCCustomBuildTool buildTool = (VCCustomBuildTool)fileConfig.Tool;
                                    if (buildTool != null)
                                        if (!buildTool.CommandLine.Contains("T3$T$TR1NG"))
                                        {
                                            subsuccess = false;
                                            break;
                                        }
                                }
                            }
                        }

                        if (!subsuccess)
                        {
                            logger.WriteLine(DateTime.Now.ToString() + ": Case7: Custom build step was not kept "
                                + "when the Q_OBJECT macro was removed");
                            success = false;
                        }
                        else
                        {
                            logger.WriteLine(DateTime.Now.ToString() + ": Case7: Custom build step was kept "
                                + "when the Q_OBJECT macro was removed");
                            currentException = RebuildSolution();
                            if (currentException != null)
                            {
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case7: " + currentException.Message);
                                success = false;
                            }
                            else
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case7: Build process succeeded");
                        }
                    }
                    catch (Exception e)
                    {
                        success = false;
                        logger.WriteLine(e.Message);
                    }
                    if (success)
                        logger.WriteLine(DateTime.Now.ToString() + ": Case7 succeeded");
                    else
                        logger.WriteLine(DateTime.Now.ToString() + ": Case7 failed");
                    logger.WriteLine("");
                }
            }
        }

        private void AutoTestCase8()
        {
            foreach (String extension in extensions)
            {
                foreach (string mocDirectory in mocDirectories)
                {
                    bool success = true;
                    Exception currentException = null;
                    try
                    {
                        logger.WriteLine(DateTime.Now.ToString()
                            + ": Case8 (Exclusion of mocced files) begins");
                        logger.WriteLine("\textension: " + extension);
                        logger.WriteLine("\tmoc directory: " + mocDirectory);

                        String solutionRootDir = BackupSolution(testPath + templatePath, "Test3" + extension);
                        _applicationObject.Solution.Open(solutionRootDir + "Test3" + extension + ".sln");
                        Solution solution = _applicationObject.Solution;
                        if (solution == null)
                            throw new Exception(DateTime.Now.ToString() + ": Case8: "
                                + currentException.Message);

                        Project project = GetProject("Test3", solution);
                        SetProjectDirectory(ref project, ProjectDirectory.MocDir, mocDirectory);
                        VCProject vcProject = (VCProject)project.Object;
                        foreach (VCFile file in (IVCCollection)vcProject.Files)
                        {
                            if (file.Name == "foo.h" || file.Name == "foo.cpp" || file.Name == "bar.cpp")
                            {
                                IVCCollection fileConfigs = (IVCCollection)file.FileConfigurations;
                                foreach (VCFileConfiguration fileConfig in fileConfigs)
                                {
                                    if (fileConfig.Name.ToLower().Contains("debug"))
                                        fileConfig.ExcludedFromBuild = true;
                                }
                            }
                        }
                        vcProject.Save();
                        currentException = RebuildSolution();
                        if (currentException != null)
                        {
                            logger.WriteLine(DateTime.Now.ToString()
                                + ": Case8: " + currentException.Message);
                            success = false;
                        }
                        else
                            logger.WriteLine(DateTime.Now.ToString()
                                + ": Case8: Build process succeeded");

                        if (success)
                        {
                            foreach (VCFile file in (IVCCollection)vcProject.Files)
                            {
                                if (file.Name == "moc_foo.cpp" || file.Name == "bar.moc")
                                {
                                    IVCCollection fileConfigs = (IVCCollection)file.FileConfigurations;
                                    foreach (VCFileConfiguration fileConfig in fileConfigs)
                                    {
                                        if (fileConfig.Name.ToLower().Contains("debug") && !fileConfig.ExcludedFromBuild)
                                        {
                                            success = false;
                                            break;
                                        }
                                    }
                                }
                                if (!success)
                                    break;
                            }
                        }

                        if (!success)
                        {
                            logger.WriteLine(DateTime.Now.ToString() + ": Case8: mocced Files were not excluded from build "
                                + "when sources were excluded (excluded in debug configuration)");
                        }
                        else
                        {
                            logger.WriteLine(DateTime.Now.ToString() + ": Case8: mocced Files were excluded from build "
                                + "when sources were excluded (excluded in debug configuration)");
                        }

                        if (success)
                        {
                            foreach (VCFile file in (IVCCollection)vcProject.Files)
                            {
                                if (file.Name == "foo.h" || file.Name == "foo.cpp" || file.Name == "bar.cpp")
                                {
                                    IVCCollection fileConfigs = (IVCCollection)file.FileConfigurations;
                                    foreach (VCFileConfiguration fileConfig in fileConfigs)
                                    {
                                        if (fileConfig.Name.ToLower().Contains("foobar"))
                                            fileConfig.ExcludedFromBuild = true;
                                    }
                                }
                            }
                            vcProject.Save();
                            currentException = RebuildSolution();
                            if (currentException != null)
                            {
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case8: " + currentException.Message);
                                success = false;
                            }
                            else
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case8: Build process succeeded");

                            if (success)
                            {
                                foreach (VCFile file in (IVCCollection)vcProject.Files)
                                {
                                    if (file.Name == "moc_foo.cpp" || file.Name == "bar.moc")
                                    {
                                        IVCCollection fileConfigs = (IVCCollection)file.FileConfigurations;
                                        foreach (VCFileConfiguration fileConfig in fileConfigs)
                                        {
                                            if (fileConfig.Name.ToLower().Contains("foobar") && !fileConfig.ExcludedFromBuild)
                                            {
                                                logger.WriteLine(DateTime.Now.ToString()
                                                    + ": Case8: mocced file " + file.Name + "was not excluded from build in config " + fileConfig.Name);
                                                success = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (!success)
                                        break;
                                }

                                if (!success)
                                {
                                    logger.WriteLine(DateTime.Now.ToString() + ": Case8: mocced Files were not excluded from build "
                                        + "when sources were excluded (excluded in debug & foobar configuration)");
                                    success = false;
                                }
                                else
                                {
                                    logger.WriteLine(DateTime.Now.ToString() + ": Case8: mocced Files were excluded from build "
                                        + "when sources were excluded (excluded in debug & foobar configuration)");
                                }
                            }
                        }

                        if (success)
                        {
                            foreach (VCFile file in (IVCCollection)vcProject.Files)
                            {
                                if (file.Name == "foo.h" || file.Name == "foo.cpp" || file.Name == "bar.cpp")
                                {
                                    IVCCollection fileConfigs = (IVCCollection)file.FileConfigurations;
                                    foreach (VCFileConfiguration fileConfig in fileConfigs)
                                    {
                                        if (fileConfig.Name.ToLower().Contains("foobar"))
                                            fileConfig.ExcludedFromBuild = false;
                                    }
                                }
                            }
                            vcProject.Save();
                            currentException = RebuildSolution();
                            if (currentException != null)
                            {
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case8: " + currentException.Message);
                                success = false;
                            }
                            else
                                logger.WriteLine(DateTime.Now.ToString()
                                    + ": Case8: Build process succeeded");

                            if (success)
                            {
                                foreach (VCFile file in (IVCCollection)vcProject.Files)
                                {
                                    if (file.Name == "moc_foo.cpp" || file.Name == "foo.moc")
                                    {
                                        VCFilter filter = file.Parent as VCFilter;
                                        if (filter == null || filter.Name != "foobar")
                                            continue;
                                        IVCCollection fileConfigs = (IVCCollection)file.FileConfigurations;
                                        foreach (VCFileConfiguration fileConfig in fileConfigs)
                                        {
                                            if (fileConfig.Name.ToLower().Contains("foobar") && fileConfig.ExcludedFromBuild)
                                            {
                                                success = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (!success)
                                        break;
                                }
                                if (!success)
                                {
                                    logger.WriteLine(DateTime.Now.ToString() + ": Case8: mocced Files were not included correctly"
                                        + "when sources were included (excluded in debug configuration)");
                                }
                                else
                                {
                                    logger.WriteLine(DateTime.Now.ToString() + ": Case8: mocced Files were included in build "
                                        + "when sources were included (excluded in debug configuration)");
                                }
                            }
                            if (success)
                            {
                                foreach (VCFile file in (IVCCollection)vcProject.Files)
                                {
                                    if (file.Name == "foo.h" || file.Name == "foo.cpp" || file.Name == "bar.cpp")
                                    {
                                        IVCCollection fileConfigs = (IVCCollection)file.FileConfigurations;
                                        foreach (VCFileConfiguration fileConfig in fileConfigs)
                                        {
                                            if (fileConfig.Name.ToLower().Contains("debug"))
                                                fileConfig.ExcludedFromBuild = false;
                                        }
                                    }
                                }
                                vcProject.Save();
                                currentException = RebuildSolution();
                                if (currentException != null)
                                {
                                    logger.WriteLine(DateTime.Now.ToString()
                                        + ": Case8: " + currentException.Message);
                                    success = false;
                                }
                                else
                                    logger.WriteLine(DateTime.Now.ToString()
                                        + ": Case8: Build process succeeded");

                                if (success)
                                {
                                    foreach (VCFile file in (IVCCollection)vcProject.Files)
                                    {
                                        if (file.Name == "moc_foo.cpp" || file.Name == "foo.moc")
                                        {
                                            VCFilter filter = file.Parent as VCFilter;
                                            if (filter == null || filter.Name != "debug")
                                                continue;
                                            IVCCollection fileConfigs = (IVCCollection)file.FileConfigurations;
                                            foreach (VCFileConfiguration fileConfig in fileConfigs)
                                            {
                                                if (fileConfig.Name.ToLower().Contains("debug") && fileConfig.ExcludedFromBuild)
                                                {
                                                    string configName = fileConfig.Name;
                                                    bool excluded = fileConfig.ExcludedFromBuild;
                                                    success = false;
                                                    break;
                                                }
                                            }
                                        }
                                        if (!success)
                                            break;
                                    }
                                    if (!success)
                                    {
                                        logger.WriteLine(DateTime.Now.ToString() + ": Case8: mocced Files were not included in build "
                                            + "when sources were included");
                                    }
                                    else
                                    {
                                        logger.WriteLine(DateTime.Now.ToString() + ": Case8: mocced Files were included in build "
                                            + "when sources were included");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        success = false;
                        logger.WriteLine(e.Message);
                    }
                    if (success)
                        logger.WriteLine(DateTime.Now.ToString() + ": Case8 succeeded");
                    else
                        logger.WriteLine(DateTime.Now.ToString() + ": Case8 failed");
                    logger.WriteLine("");
                }
            }
        }

        private void AutoTestCase9()
        {
            bool success = true;
            Exception currentException = null;
            try
            {
                logger.WriteLine(DateTime.Now.ToString()
                    + ": Case9 (Handling of precompiled headers) begins");
                logger.WriteLine(DateTime.Now.ToString()
                    + ": Case9: Adding precompiled header data");

                String solutionRootDir = BackupSolution(testPath + templatePath, "Test2");
                _applicationObject.Solution.Open(solutionRootDir + "Test2.sln");
                Solution solution = _applicationObject.Solution;
                if (solution == null)
                    throw new Exception(DateTime.Now.ToString() + ": Case9: "
                        + currentException.Message);

                Project project = GetProject("Test2", solution);
                VCProject vcProject = (VCProject)project.Object;

                foreach (VCConfiguration vcConfig in vcProject.Configurations as IVCCollection)
                {
                    CompilerToolWrapper wrapper = new CompilerToolWrapper(vcConfig);
                    if (wrapper == null)
                        continue;

                    wrapper.SetUsePrecompiledHeader(pchOption.pchUseUsingSpecific);
                    wrapper.SetPrecompiledHeaderFile("test.pch");
                    wrapper.SetPrecompiledHeaderThrough("test.h");
                }
                foreach (VCFile file in (IVCCollection)vcProject.Files)
                {
                    if (file.Name.EndsWith(".cpp"))
                    {
                        IVCCollection fileConfigs = (IVCCollection)file.FileConfigurations;
                        foreach (VCFileConfiguration fileConfig in fileConfigs)
                        {
                            CompilerToolWrapper wrapper = new CompilerToolWrapper(fileConfig);
                            if (wrapper == null)
                                continue;

                            wrapper.SetPrecompiledHeaderFile("test.pch");
                            wrapper.SetPrecompiledHeaderThrough("test.h");
                            if (file.Name == "test.cpp")
                                wrapper.SetUsePrecompiledHeader(pchOption.pchCreateUsingSpecific);
                            else
                                wrapper.SetUsePrecompiledHeader(pchOption.pchUseUsingSpecific);
                        }
                    }
                }
                ProjectItem piSource = project.ProjectItems.Item("Source Files");
                foreach (ProjectItem item in piSource.ProjectItems)
                {
                    if (ProjectItemContainsString(item, "//#include \"test.h\""))
                    {
                        currentException = ReplaceStringInProjectItem(item, "//#include \"test.h\"", "#include \"test.h\"");
                    }
                }
                vcProject.Save();
                currentException = RebuildSolution();
                if (currentException != null)
                {
                    logger.WriteLine(DateTime.Now.ToString()
                        + ": Case9: " + currentException.Message);
                    success = false;
                }
                else
                    logger.WriteLine(DateTime.Now.ToString()
                        + ": Case9: Build process succeeded");

                logger.WriteLine(DateTime.Now.ToString()
                    + ": Case9: Changing precompiled header data");

                solutionRootDir = BackupSolution(testPath + templatePath, "Test4");
                _applicationObject.Solution.Open(solutionRootDir + "Test4.sln");
                solution = _applicationObject.Solution;
                if (solution == null)
                    throw new Exception(DateTime.Now.ToString() + ": Case9: "
                        + currentException.Message);

                project = GetProject("Test4", solution);
                vcProject = (VCProject)project.Object;

                foreach (VCConfiguration vcConfig in vcProject.Configurations as IVCCollection)
                {
                    CompilerToolWrapper wrapper = new CompilerToolWrapper(vcConfig);
                    if (wrapper == null)
                        continue;

                    wrapper.SetUsePrecompiledHeader(pchOption.pchUseUsingSpecific);
                    wrapper.SetPrecompiledHeaderFile("stdafx.pch");
                    wrapper.SetPrecompiledHeaderThrough("stdafx.h");
                }
                foreach (VCFile file in (IVCCollection)vcProject.Files)
                {
                    if (file.Name.EndsWith(".cpp"))
                    {
                        IVCCollection fileConfigs = (IVCCollection)file.FileConfigurations;
                        foreach (VCFileConfiguration fileConfig in fileConfigs)
                        {
                            CompilerToolWrapper wrapper = new CompilerToolWrapper(fileConfig);
                            if (wrapper == null)
                                continue;

                            wrapper.SetPrecompiledHeaderFile("stdafx.pch");
                            wrapper.SetPrecompiledHeaderThrough("stdafx.h");
                            if (file.Name == "test.cpp")
                                wrapper.SetUsePrecompiledHeader(pchOption.pchCreateUsingSpecific);
                            else
                                wrapper.SetUsePrecompiledHeader(pchOption.pchUseUsingSpecific);
                        }
                    }
                }
                piSource = project.ProjectItems.Item("Source Files");
                foreach (ProjectItem item in piSource.ProjectItems)
                {
                    if (ProjectItemContainsString(item, "#include \"test.h\""))
                    {
                        currentException = ReplaceStringInProjectItem(item, "#include \"test.h\"", "#include \"stdafx.h\"");
                    }
                }
                vcProject.Save();
                currentException = RebuildSolution();
                if (currentException != null)
                {
                    logger.WriteLine(DateTime.Now.ToString()
                        + ": Case9: " + currentException.Message);
                    success = false;
                }
                else
                    logger.WriteLine(DateTime.Now.ToString()
                        + ": Case9: Build process succeeded");

                logger.WriteLine(DateTime.Now.ToString()
                    + ": Case9: Removing precompiled header data");

                solutionRootDir = BackupSolution(testPath + templatePath, "Test4");
                _applicationObject.Solution.Open(solutionRootDir + "Test4.sln");
                solution = _applicationObject.Solution;
                if (solution == null)
                    throw new Exception(DateTime.Now.ToString() + ": Case9: "
                        + currentException.Message);
                project = GetProject("Test4", solution);
                vcProject = (VCProject)project.Object;

                foreach (VCConfiguration vcConfig in vcProject.Configurations as IVCCollection)
                {
                    CompilerToolWrapper wrapper = new CompilerToolWrapper(vcConfig);
                    if (wrapper == null)
                        continue;

                    wrapper.SetUsePrecompiledHeader(pchOption.pchNone);
                    wrapper.SetPrecompiledHeaderFile("");
                    wrapper.SetPrecompiledHeaderThrough("");
                }
                foreach (VCFile file in (IVCCollection)vcProject.Files)
                {
                    if (file.Name.EndsWith(".cpp"))
                    {
                        IVCCollection fileConfigs = (IVCCollection)file.FileConfigurations;
                        foreach (VCFileConfiguration fileConfig in fileConfigs)
                        {
                            CompilerToolWrapper wrapper = new CompilerToolWrapper(fileConfig);
                            if (wrapper == null)
                                continue;

                            wrapper.SetPrecompiledHeaderFile("");
                            wrapper.SetPrecompiledHeaderThrough("");
                            wrapper.SetUsePrecompiledHeader(pchOption.pchNone);
                        }
                    }
                }
                piSource = project.ProjectItems.Item("Source Files");
                foreach (ProjectItem item in piSource.ProjectItems)
                {
                    if (ProjectItemContainsString(item, "#include \"test.h\""))
                    {
                        currentException = ReplaceStringInProjectItem(item, "#include \"test.h\"", "//#include \"test.h\"");
                    }
                }
                ProjectItem piHeader = project.ProjectItems.Item("Header Files");
                ProjectItems headerItems = piHeader.ProjectItems;
                for (int i = headerItems.Count; i > 0; i--)
                {
                    ProjectItem item = headerItems.Item(i);
                    if (item.Name == "test.h")
                    {
                        item.Delete();
                    }
                }
                vcProject.Save();
                currentException = RebuildSolution();
                if (currentException != null)
                {
                    logger.WriteLine(DateTime.Now.ToString()
                        + ": Case9: " + currentException.Message);
                    success = false;
                }
                else
                    logger.WriteLine(DateTime.Now.ToString()
                        + ": Case9: Build process succeeded");
            }
            catch (Exception e)
            {
                success = false;
                logger.WriteLine(e.Message);
            }
            if (success)
                logger.WriteLine(DateTime.Now.ToString() + ": Case9 succeeded");
            else
                logger.WriteLine(DateTime.Now.ToString() + ": Case9 failed");
            logger.WriteLine("");
        }

        private void FullAutoTest()
        {
            try
            {
                if (!File.Exists(testPath + "log.txt"))
                    File.Create(testPath + "log.txt");
                logger = new StreamWriter(testPath + "log.txt");

                AutoTestCase1();    //- adding Q_OBJECT macro

                AutoTestCase2();    //- remove Q_OBJECT macro from header

                AutoTestCase3();    //- directly include the moc file and save the header file

                AutoTestCase4();    //- directly include the moc file and save the source file

                AutoTestCase5();    //- Change Preprocessor Definitions

                AutoTestCase6();    //- Change Additional Include Directories

                AutoTestCase7();    //- Add user defined custom build steps

                AutoTestCase8();    //- Exclusion of mocced files

                AutoTestCase9();    //- Handling of precompiled headers

                if (_applicationObject.Solution.IsOpen)
                    _applicationObject.Solution.Close(false);
                DirectoryInfo di = new DirectoryInfo(testPath + templatePath + "tmp");
                if (di.Exists)
                    di.Delete(true);
                logger.WriteLine(DateTime.Now.ToString() + ": Tests finished");
                logger.Close();
            }
            catch (Exception)
            {
                logger.WriteLine(DateTime.Now.ToString() + ": Tests were not completed");
                logger.Close();
            }
        }

        private void ExecuteTest(int testNr)
        {
            switch (testNr)
            {
                case 1:
                    AutoTestCase1();
                    break;
                case 2:
                    AutoTestCase2();
                    break;
                case 3:
                    AutoTestCase3();
                    break;
                case 4:
                    AutoTestCase4();
                    break;
                case 5:
                    AutoTestCase5();
                    break;
                case 6:
                    AutoTestCase6();
                    break;
                case 7:
                    AutoTestCase7();
                    break;
                case 8:
                    AutoTestCase8();
                    break;
                case 9:
                    AutoTestCase9();
                    break;
            }
        }


        public int GetNumberOfTests()
        {
            Type t = typeof(Connect);
            System.Reflection.MemberInfo[] members = t.GetMembers();
            int memberCount = 0;
            foreach (System.Reflection.MemberInfo member in members)
                if (member.Name.ToLower().StartsWith("autotestcase"))
                    ++memberCount;
            return memberCount;
        }

        public string GetTestDescription(int testNr)
        {
            switch (testNr)
            {
                case 1:
                    return "Adding Q_OBJECT macro";
                case 2:
                    return "Remove Q_OBJECT macro from header";
                case 3:
                    return "Directly include the moc file and save the header file";
                case 4:
                    return "Directly include the moc file and save the source file";
                case 5:
                    return "Change Preprocessor Definitions";
                case 6:
                    return "Change Additional Include Directories";
                case 7:
                    return "Add user defined custom build steps";
                default:
                    return "";
            }
        }

                        

        public void Startserver()
        {
            extensions.Add("");
            extensions.Add("pch");
            mocDirectories.Add("GeneratedFiles");
            mocDirectories.Add("GeneratedFiles\\$(ConfigurationName)");
            mocDirectories.Add("GeneratedFiles\\$(PlatformName)");
            mocDirectories.Add("GeneratedFiles\\$(ConfigurationName)-$(PlatformName)");
            server = new UdpClient(200);
            IPEndPoint recvpt = new IPEndPoint(IPAddress.Any, 0);
            byte[] data;
            string str, from;
            int index;
            while (true)
            {
                data = server.Receive(ref recvpt);
                try
                {
                    str = Encoding.ASCII.GetString(data);
                    index = str.LastIndexOf("@");
                    from = str.Substring(index + 1);
                    if (index >= 0)
                        str = str.Remove(index, str.Length - index);
                    if (str.CompareTo("FullTest") == 0)
                    {
                        FullAutoTest();
                    }
                    else if (str.StartsWith("Tests:"))
                    {
                        if (!File.Exists(testPath + "log.txt"))
                            File.Create(testPath + "log.txt");
                        logger = new StreamWriter(testPath + "log.txt");

                        string tests = str.Substring(6);
                        string[] testArray = tests.Split(';');
                        foreach (string s in testArray)
                        {
                            try
                            {
                                int nr = Convert.ToInt32(s);
                                ExecuteTest(nr);
                            }
                            catch (Exception) { }
                        }

                        if (_applicationObject.Solution.IsOpen)
                            _applicationObject.Solution.Close(false);
                        DirectoryInfo di = new DirectoryInfo(testPath + templatePath + "tmp");
                        if (di.Exists)
                            di.Delete(true);
                        logger.WriteLine(DateTime.Now.ToString() + ": Tests finished");
                        logger.Close();
                    }
                }
                catch
                { }
            }
        }

        private bool SetProjectDirectory(ref Project project, ProjectDirectory directory, string value)
        {
#if !VS2010
            Solution solution = _applicationObject.Solution;
            string projectPath = project.FullName;
            solution.Remove(project);
            XmlDocument document = new XmlDocument();
            document.Load(projectPath);
            XmlNodeList nodes = document.GetElementsByTagName("Global");
            foreach (XmlNode node in nodes)
            {
                XmlAttributeCollection attributes = node.Attributes;
                if (attributes["Name"].Value == directory.ToString())
                {
                    attributes["Value"].Value = value;
                    document.Save(projectPath);
                    project = solution.AddFromFile(projectPath, false);
                    return true;
                }
            }
            project = solution.AddFromFile(projectPath, false);
#else
#endif
            return false;
        }

        /// <summary>Implements the constructor for the Add-in object. Place your initialization code within this method.</summary>
        public Connect()
        {
        }

        /// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being loaded.</summary>
        /// <param term='application'>Root object of the host application.</param>
        /// <param term='connectMode'>Describes how the Add-in is being loaded.</param>
        /// <param term='addInInst'>Object representing this Add-in.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            _applicationObject = (DTE2)application;
            _addInInstance = (AddIn)addInInst;

            System.Uri uri = new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().EscapedCodeBase);
            testPath = Path.GetDirectoryName(System.Uri.UnescapeDataString(uri.AbsolutePath));
            testPath = testPath.Remove(testPath.LastIndexOf(Path.DirectorySeparatorChar));
            testPath += Path.DirectorySeparatorChar;

            svthread = new System.Threading.Thread(new System.Threading.ThreadStart(Startserver));
            svthread.IsBackground = true;
            svthread.Start();
        }

        /// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
        /// <param term='disconnectMode'>Describes how the Add-in is being unloaded.</param>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
        {
            svthread.Abort();
            server.Close();
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
            svthread.Abort();
            server.Close();
        }

        private DTE2 _applicationObject;
        private AddIn _addInInstance;
    }
}