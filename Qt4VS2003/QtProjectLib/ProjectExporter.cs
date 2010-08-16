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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Collections;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Nokia.QtProjectLib
{
    #region Storage Classes
    internal class ProSolution
    {
        public ProSolution(EnvDTE.Solution sln)
        {
            prosln = sln;
            proFiles = new List<ProFileContent>();
        }

        public List<ProFileContent> ProFiles
        {
            get
            {
                return proFiles;
            }
        }

        public EnvDTE.Solution ProjectSolution
        {
            get
            {
                return prosln;
            }
        }

        private List<ProFileContent> proFiles;
        private EnvDTE.Solution prosln;
    }

    internal class ProFileOption
    {
        public ProFileOption(string optname)
        {
            name = optname;
            astype = AssignType.AT_PlusEquals;
            comment = null;
            shortComment = "Default";
            incComment = false;
            newOpt = " \\\r\n    ";
            list = new List<string>();
        }

        public override string ToString()
        {
            return shortComment;
        }

        public string Comment
        {
            get
            {
                return comment;
            }
            set
            {
                comment = value;
            }
        }

        public string ShortComment
        {
            get
            {
                return shortComment;
            }
            set
            {
                shortComment = value;
            }
        }

        public AssignType AssignSymbol
        {
            get
            {
                return astype;
            }
            set
            {
                astype = value;
            }
        }

        public string NewOption
        {
            get
            {
                return newOpt;
            }
            set
            {
                newOpt = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public List<string> List
        {
            get
            {
                return list;
            }
        }

        public bool IncludeComment
        {
            get
            {
                return incComment;
            }
            set
            {
                incComment = value;
            }
        }

        public enum AssignType
        {
            AT_Equals = 1,
            AT_PlusEquals = 2, // default
            AT_MinusEquals = 3,
            AT_Function = 4
        }

        private AssignType astype;
        private string shortComment;
        private bool incComment;
        private string comment;
        private string newOpt;
        private string name;
        private List<string> list;
    }

    internal class ProFileContent
    {
        public ProFileContent(VCProject proj)
        {
            export = true;
            vcproj = proj;
            options = new List<ProFileOption>();
        }

        public override string ToString()
        {
            return vcproj.Name;
        }

        public VCProject Project
        {
            get
            {
                return vcproj;
            }
        }

        public bool Export
        {
            get
            {
                return export;
            }
            set
            {
                export = value;
            }
        }


        public List<ProFileOption> Options
        {
            get
            {
                return options;
            }
        }

        private VCProject vcproj;
        private bool export;
        private List<ProFileOption> options;
    }
    #endregion

    /// <summary>
    /// Summary description for Export.
    /// </summary>

    public class ProjectExporter
    {
        private EnvDTE.DTE dteObject = null;

        public ProjectExporter(EnvDTE.DTE dte)
        {
            dteObject = dte;
        }

        #region Helper Functions
        private static void MakeFilesRelativePath(VCProject vcproj, List<string> files, string path)
        {
            for(int i=0; i<files.Count; i++)
            {
                string relPath;
                if(files[i].IndexOf(":") != 1)
                    relPath = HelperFunctions.GetRelativePath(path,
                        vcproj.ProjectDirectory + "\\" + (string)files[i]);
                else
                    relPath = HelperFunctions.GetRelativePath(path, (string)files[i]);
                files[i] = HelperFunctions.ChangePathFormat(relPath);
            }
        }

        private static bool ContainsFilesWithSpaces(List<string> files)
        {
            for (int i=0; i<files.Count; i++)
            {
                if(files[i].IndexOf(' ') != -1)
                    return true;
            }

            return false;
        }

        public static List<string> ConvertFilesToFullPath(List<string> files, string path)
        {
            List<string> ret = new List<string>(files.Count);
            for (int i=0; i<files.Count; i++)
            {
                FileInfo fi;
                if(files[i].IndexOf(":") != 1)
                    fi = new FileInfo(path + "\\" + (string)files[i]);
                else
                    fi = new FileInfo((string)files[i]);

                ret[i] = fi.FullName;
            }
            return ret;
        }
        #endregion

        #region Export pri/pro Files Helper Functions
        private ProSolution CreateProFileSolution(EnvDTE.Solution sln)
        {
            ProFileContent content;
            ProSolution prosln = new ProSolution(sln);

            foreach(EnvDTE.Project proj in HelperFunctions.ProjectsInSolution(sln.DTE))
            {
                try
                {
                    // only add qt projects
                    if (HelperFunctions.IsQtProject(proj))
                    {
                        content = CreateProFileContent(proj, null);
                        prosln.ProFiles.Add(content);
                    }
                    else if (proj.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                    {
                        addProjectsInFolder(proj, prosln);
                    }
                }
                catch
                {
                    // Catch all exceptions. Try to add as many projects as possible.
                }
            }

            return prosln;
        }

        private void addProjectsInFolder(EnvDTE.Project solutionFolder, ProSolution sln)
        {
            foreach (ProjectItem pi in solutionFolder.ProjectItems)
            {
                Project containedProject = pi.Object as Project;
                if (HelperFunctions.IsQtProject(containedProject))
                {
                    ProFileContent content = CreateProFileContent(containedProject, null);
                    sln.ProFiles.Add(content);
                }
                else if (containedProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    addProjectsInFolder(containedProject, sln);
                }
            }
        }

        private static ProFileContent CreateProFileContent(EnvDTE.Project project, FileInfo priFile)
        {
            ProFileOption option;
            QtProject qtPro = QtProject.Create(project);
            ProFileContent content = new ProFileContent(qtPro.VCProject);

            if (priFile == null)
            {
                // hack to get active config
                string activeConfig = project.ConfigurationManager.ActiveConfiguration.ConfigurationName;
                string activePlatform = project.ConfigurationManager.ActiveConfiguration.PlatformName;
                VCConfiguration config = (VCConfiguration)((IVCCollection)qtPro.VCProject.Configurations).Item(activeConfig);
                CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
                VCLinkerTool linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");
                VCLibrarianTool libTool = (VCLibrarianTool)((IVCCollection)config.Tools).Item("VCLibrarianTool");

                string outPut = config.PrimaryOutput;
                FileInfo fi = new FileInfo(outPut);
                string destdir = HelperFunctions.GetRelativePath(qtPro.VCProject.ProjectDirectory, fi.DirectoryName);
                destdir = HelperFunctions.ChangePathFormat(destdir);
                string target = qtPro.VCProject.Name;

                option = new ProFileOption("TEMPLATE");
                option.Comment = Resources.ec_Template;
                option.ShortComment = "Template";
                option.NewOption = null; // just one option...
                option.AssignSymbol = ProFileOption.AssignType.AT_Equals;
                content.Options.Add(option);
                if (config.ConfigurationType == ConfigurationTypes.typeApplication)
                    option.List.Add("app");
                else
                    option.List.Add("lib");

                option = new ProFileOption("TARGET");
                option.Comment = Resources.ec_Target;
                option.ShortComment = "Target Name";
                option.NewOption = null; // just one option...
                option.AssignSymbol = ProFileOption.AssignType.AT_Equals;
                content.Options.Add(option);
                option.List.Add(target);

                option = new ProFileOption("DESTDIR");
                option.Comment = Resources.ec_DestDir;
                option.ShortComment = "Destination Directory";
                option.NewOption = null; // just one option...
                option.AssignSymbol = ProFileOption.AssignType.AT_Equals;
                content.Options.Add(option);
                option.List.Add(destdir);

                // add the qt option
                option = new ProFileOption("QT");
                ProFileOption optionQT = option;
                option.Comment = Resources.ec_Qt;
                option.ShortComment = "Qt Options";
                option.NewOption = " "; // just space between the options...
                content.Options.Add(option);

                // add the config option
                option = new ProFileOption("CONFIG");
                ProFileOption optionCONFIG = option;
                option.Comment = Resources.ec_Config;
                option.ShortComment = "Config Options";
                option.NewOption = " "; // just space between the options...
                content.Options.Add(option);

                AddModules(qtPro, optionQT, optionCONFIG);

                if (config.ConfigurationType == ConfigurationTypes.typeStaticLibrary)
                    option.List.Add("staticlib");
                if (linker != null)
                {
                    if (linker.GenerateDebugInformation)
                        option.List.Add("debug");
                    else
                        option.List.Add("release");

                    if (linker.SubSystem == subSystemOption.subSystemConsole)
                        option.List.Add("console");

                    if (linker.AdditionalDependencies != null)
                    {
                        if (linker.AdditionalDependencies.IndexOf("QAxServer") > -1)
                            option.List.Add("qaxserver");
                        else if (linker.AdditionalDependencies.IndexOf("QAxContainer") > -1)
                            option.List.Add("qaxcontainer");
                        else if (linker.AdditionalDependencies.IndexOf("QtHelp") > -1)
                            option.List.Add("help");
                    }
                }

                if (qtPro.IsDesignerPluginProject())
                {
                    option.List.Add("designer");
                    option.List.Add("plugin");
                }

                // add defines
                option = new ProFileOption("DEFINES");
                option.Comment = Resources.ec_Defines;
                option.ShortComment = "Defines";
                option.NewOption = " ";
                option.AssignSymbol = ProFileOption.AssignType.AT_PlusEquals;
                content.Options.Add(option);
                AddPreprocessorDefinitions(option, compiler.GetPreprocessorDefinitions());

                // add the include path option
                option = new ProFileOption("INCLUDEPATH");
                option.Comment = Resources.ec_IncludePath;
                option.ShortComment = "Include Path";
                content.Options.Add(option);
                AddIncludePaths(project, option, compiler.GetAdditionalIncludeDirectories());

                option = new ProFileOption("LIBS");
                option.Comment = Resources.ec_Libs;
                option.ShortComment = "Additional Libraries";
                content.Options.Add(option);
                if (linker != null)
                {
                    AddLibraries(project, option, linker.AdditionalLibraryDirectories,
                        linker.AdditionalDependencies);
                }
                else if (libTool != null)
                {
                    AddLibraries(project, option, libTool.AdditionalLibraryDirectories,
                        libTool.AdditionalDependencies);
                }

                option = new ProFileOption("PRECOMPILED_HEADER");
                option.Comment = Resources.ec_PrecompiledHeader;
                option.ShortComment = "Using Precompiled Headers";
                option.AssignSymbol = ProFileOption.AssignType.AT_Equals;
                content.Options.Add(option);
                if (compiler.GetPrecompiledHeaderFile().Length > 0)
                    option.List.Add(compiler.GetPrecompiledHeaderThrough());

                // add the depend path option
                option = new ProFileOption("DEPENDPATH");
                option.Comment = Resources.ec_DependPath;
                option.ShortComment = "Depend Path";
                content.Options.Add(option);
                option.List.Add(".");

                string mocDir = QtVSIPSettings.GetMocDirectory(project, activeConfig.ToLower(), activePlatform.ToLower());
                mocDir = mocDir.Replace('\\', '/');
                option = new ProFileOption("MOC_DIR");
                option.Comment = Resources.ec_MocDir;
                option.ShortComment = "Moc Directory";
                option.NewOption = null; // just one option...
                content.Options.Add(option);
                option.List.Add(mocDir);

                option = new ProFileOption("OBJECTS_DIR");
                option.Comment = Resources.ec_ObjDir;
                option.ShortComment = "Objects Directory";
                option.NewOption = null; // just one option...
                content.Options.Add(option);
                option.List.Add(config.ConfigurationName.ToLower());

                string uiDir = QtVSIPSettings.GetUicDirectory(project);
                uiDir = uiDir.Replace('\\', '/');
                option = new ProFileOption("UI_DIR");
                option.Comment = Resources.ec_UiDir;
                option.ShortComment = "UI Directory";
                option.NewOption = null; // just one option...
                content.Options.Add(option);
                option.List.Add(uiDir);

                string rccDir = QtVSIPSettings.GetRccDirectory(project);
                rccDir = rccDir.Replace('\\', '/');
                option = new ProFileOption("RCC_DIR");
                option.Comment = Resources.ec_RccDir;
                option.ShortComment = "RCC Directory";
                option.NewOption = null; // just one option...
                content.Options.Add(option);
                option.List.Add(rccDir);

                // add the include path option
                option = new ProFileOption("include");
                option.Comment = Resources.ec_Include;
                option.ShortComment = "Include file(s)";
                option.IncludeComment = false; // print the comment in the output file
                option.AssignSymbol = ProFileOption.AssignType.AT_Function;
                content.Options.Add(option);
                option.List.Add(project.Name + ".pri");

                // add the translation files
                option = new ProFileOption("TRANSLATIONS");
                option.Comment = Resources.ec_Translations;
                option.ShortComment = "Translation files";
                option.IncludeComment = false;
                content.Options.Add(option);
                option.List.AddRange(HelperFunctions.GetProjectFiles(project, FilesToList.FL_Translation));

                // add the rc file
                if (File.Exists(qtPro.VCProject.ProjectDirectory + "\\" + project.Name + ".rc")) 
                {
                    option = new ProFileOption("win32:RC_FILE");
                    option.Comment = Resources.ec_rcFile;
                    option.ShortComment = "Windows resource file";
                    option.IncludeComment = false;
                    option.AssignSymbol = ProFileOption.AssignType.AT_Equals;
                    content.Options.Add(option);
                    option.List.Add(project.Name + ".rc");
                }

                if (qtPro.IsDesignerPluginProject())
                {
                    option = new ProFileOption("target.path");
                    option.ShortComment = "Install the plugin in the designer plugins directory.";
                    option.IncludeComment = true;
                    option.AssignSymbol = ProFileOption.AssignType.AT_Equals;
                    option.List.Add("$$[QT_INSTALL_PLUGINS]/designer");
                    content.Options.Add(option);

                    option = new ProFileOption("INSTALLS");
                    option.IncludeComment = false;
                    option.AssignSymbol = ProFileOption.AssignType.AT_PlusEquals;
                    option.List.Add("target");
                    content.Options.Add(option);
                }
            }
            else
            {
                bool hasSpaces = false;

                // add the header files
                option = new ProFileOption("HEADERS");
                option.ShortComment = "Header files";
                option.IncludeComment = false;
                content.Options.Add(option);
                option.List.AddRange(HelperFunctions.GetProjectFiles(project, FilesToList.FL_HFiles));
                MakeFilesRelativePath(qtPro.VCProject, option.List, priFile.DirectoryName);
                if(ContainsFilesWithSpaces(option.List))
                    hasSpaces = true;

                // add the source files
                option = new ProFileOption("SOURCES");
                option.ShortComment = "Source files";
                option.IncludeComment = false;
                content.Options.Add(option);
                option.List.AddRange(HelperFunctions.GetProjectFiles(project, FilesToList.FL_CppFiles));
                MakeFilesRelativePath(qtPro.VCProject, option.List, priFile.DirectoryName);
                if(ContainsFilesWithSpaces(option.List))
                    hasSpaces = true;

                // add the form files
                option = new ProFileOption("FORMS");
                option.ShortComment = "Forms";
                option.IncludeComment = false;
                content.Options.Add(option);
                option.List.AddRange(HelperFunctions.GetProjectFiles(project, FilesToList.FL_UiFiles));
                MakeFilesRelativePath(qtPro.VCProject, option.List, priFile.DirectoryName);
                if(ContainsFilesWithSpaces(option.List))
                    hasSpaces = true;

                // add the translation files
                option = new ProFileOption("TRANSLATIONS");
                option.Comment = Resources.ec_Translations;
                option.ShortComment = "Translation file(s)";
                option.IncludeComment = false;
                option.List.AddRange(HelperFunctions.GetProjectFiles(project, FilesToList.FL_Translation));
                MakeFilesRelativePath(qtPro.VCProject, option.List, priFile.DirectoryName);
                if (ContainsFilesWithSpaces(option.List))
                    hasSpaces = true;
                content.Options.Add(option);

                // add the resource files
                option = new ProFileOption("RESOURCES");
                option.Comment = Resources.ec_Resources;
                option.ShortComment = "Resource file(s)";
                option.IncludeComment = false;
                content.Options.Add(option);

                foreach (VCFile resFile in qtPro.GetResourceFiles())
                    option.List.Add(resFile.RelativePath.Replace('\\', '/'));

                if (hasSpaces)
                    Messages.DisplayWarningMessage(SR.GetString("ExportProject_PriFileContainsSpaces"));
            }

            return content;
        }

        private static void AddPreprocessorDefinitions(ProFileOption option, string preprocessorDefinitions)
        {
            if (preprocessorDefinitions == null)
                return;

            string excludeList = "UNICODE WIN32 NDEBUG QDESIGNER_EXPORT_WIDGETS ";
            excludeList += "QT_THREAD_SUPPORT QT_PLUGIN QT_NO_DEBUG QT_CORE_LIB QT_GUI_LIB";

            foreach (string define in preprocessorDefinitions.Split(new char[] {';', ','}))
            {
                if (excludeList.IndexOf(define.ToUpper()) == -1)
                    option.List.Add(define);
            }
        }

        private static void AddIncludePaths(EnvDTE.Project project, ProFileOption option, string includePaths)
        {
            if (includePaths == null)
                return;

            QtVersionManager versionManager = QtVersionManager.The();
            string qtDir = versionManager.GetInstallPath(project);
            if (qtDir == null)
                qtDir = System.Environment.GetEnvironmentVariable("QTDIR");
            if (qtDir == null)
                qtDir = "";

            qtDir = HelperFunctions.NormalizeRelativeFilePath(qtDir);

            foreach (string s in includePaths.Split(new char[] {';', ','}))
            {
                string d = HelperFunctions.NormalizeRelativeFilePath(s);
                if (!d.ToLower().StartsWith("$(qtdir)\\include") &&
                    !d.ToLower().StartsWith(qtDir + "\\include") &&
                    !d.ToLower().EndsWith("win32-msvc2005"))
                {
                    d = d.Replace("$(ConfigurationName)", project.ConfigurationManager.ActiveConfiguration.ConfigurationName);
                    d = d.Replace("$(PlatformName)", project.ConfigurationManager.ActiveConfiguration.PlatformName);
                    if (HelperFunctions.IsAbsoluteFilePath(d))
                        d = HelperFunctions.GetRelativePath(project.FullName, d);
                    if (!HelperFunctions.IsAbsoluteFilePath(d))
                        option.List.Add(HelperFunctions.ChangePathFormat(d));
                }
            }
        }

        private static void AddLibraries(EnvDTE.Project project, ProFileOption option, string paths, string deps)
        {
            QtVersionManager versionManager = QtVersionManager.The();
            string qtDir = versionManager.GetInstallPath(project);
            if (qtDir == null)
                qtDir = System.Environment.GetEnvironmentVariable("QTDIR");
            if (qtDir == null)
                qtDir = "";

            qtDir = HelperFunctions.NormalizeRelativeFilePath(qtDir);

            if (paths != null)
            {
                foreach (string s in paths.Split(new char[] {';', ','}))
                {
                    string d = HelperFunctions.NormalizeRelativeFilePath(s);
                    if (!d.ToLower().StartsWith("$(qtdir)\\lib") &&
                        !d.ToLower().StartsWith(qtDir + "\\lib"))
                    {
                        if (HelperFunctions.IsAbsoluteFilePath(d))
                            d = HelperFunctions.GetRelativePath(project.FullName, d);
                        if (!HelperFunctions.IsAbsoluteFilePath(d))
                            option.List.Add("-L\"" + HelperFunctions.ChangePathFormat(d) + "\"");
                    }
                }
            }

            if (deps != null)
            {
                foreach (string s in deps.Split(new char[] {' '}))
                {
                    string d = s.ToLower();
                    if (d.Length > 0 &&
                        !d.StartsWith("$(qtdir)\\lib") &&
                        !d.StartsWith(qtDir + "\\lib") &&
                        !d.StartsWith("qt") && !d.StartsWith(".\\qt") && d != ".")
                        option.List.Add("-l" + HelperFunctions.ChangePathFormat(s).Replace(".lib", ""));
                }
            }
        }

        private static void AddModules(QtProject qtPrj, ProFileOption optionQT, ProFileOption optionCONFIG)
        {
            foreach (QtModuleInfo moduleInfo in QtModules.Instance.GetAvailableModuleInformation())
            {
                if (!qtPrj.HasModule(moduleInfo.ModuleId))
                    continue;

                if (moduleInfo.proVarQT != null)
                    optionQT.List.Add(moduleInfo.proVarQT);
                if (moduleInfo.proVarCONFIG != null)
                    optionCONFIG.List.Add(moduleInfo.proVarCONFIG);
            }
        }

        private void WriteProSolution(ProSolution prosln, bool openFile)
        {
            EnvDTE.Solution sln = prosln.ProjectSolution;
            if (string.IsNullOrEmpty(sln.FileName))
                return;

            FileInfo fi = new FileInfo(sln.FullName);
            DirectoryInfo slnDir = fi.Directory;
            bool createSlnFile = false;

            if ((slnDir != null) && (prosln.ProFiles.Count > 1))
            {
                if (MessageBox.Show(SR.GetString("ExportProject_SolutionProFileBuildIn", slnDir.FullName),
                    SR.GetString("ExportSolution"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    createSlnFile = true;
            }

            if (createSlnFile)
            {
                StreamWriter sw;
                string slnName = HelperFunctions.RemoveFileNameExtension(fi);
                string slnFileName = slnDir.FullName + "\\" + slnName + ".pro";

                if(File.Exists(slnFileName))
                    if (MessageBox.Show(SR.GetString("ExportProject_ExistsOverwriteQuestion", slnFileName),
                        SR.GetString("ExportSolution"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        return;

                try
                {
                    sw = new StreamWriter(File.Create(slnFileName));
                }
                catch (System.Exception e)
                {
                    Messages.DisplayErrorMessage(e);
                    return;
                }

                ProFileContent content = new ProFileContent(null);

                ProFileOption option = new ProFileOption("TEMPLATE");
                option.NewOption = null; // just one option...
                option.AssignSymbol = ProFileOption.AssignType.AT_Equals;
                content.Options.Add(option);
                option.List.Add("subdirs");

                option = new ProFileOption("SUBDIRS");
                option.ShortComment = "#Projects";
                content.Options.Add(option);

                string proFullName, relativePath;
                char [] trimChars = {'\\'};
                foreach (ProFileContent profile in prosln.ProFiles)
                {
                    FileInfo fiProject = new FileInfo(profile.Project.ProjectFile);
                    string projectBaseName = HelperFunctions.RemoveFileNameExtension(fiProject);
                    proFullName = profile.Project.ProjectDirectory + projectBaseName + ".pro";
                    relativePath = HelperFunctions.GetRelativePath(slnDir.FullName, proFullName);
                    relativePath = relativePath.TrimEnd(trimChars);
                    relativePath = HelperFunctions.ChangePathFormat(relativePath.Remove(0,2));
                    option.List.Add(relativePath);
                }

                using (sw)
                {
                    sw.WriteLine(Resources.exportSolutionHeader);
                    for (int i=0; i<content.Options.Count; i++)
                    {
                        WriteProFileOption(sw, (ProFileOption)content.Options[i]);
                    }
                }

                if (openFile)
                    dteObject.OpenFile(EnvDTE.Constants.vsViewKindTextView, slnFileName).Activate();
            }
        }

        private void WriteProFile(ProFileContent content, FileInfo priFile, bool openFile)
        {
            StreamWriter sw;
            string proFileName = content.Project.ProjectDirectory + content.Project.Name;

            if(priFile == null)
            {
                proFileName += ".pro";
                if(File.Exists(proFileName))
                    if(MessageBox.Show(SR.GetString("ExportProject_ExistsOverwriteQuestion", proFileName),
                        SR.GetString("ExportSolution"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        return;
            }
            else
            {
                proFileName = priFile.FullName;
            }

            try
            {
                sw = new StreamWriter(File.Create(proFileName));
            }
            catch (System.Exception e)
            {
                Messages.DisplayErrorMessage(e);
                return;
            }

            using (sw)
            {
                if (priFile == null)
                    sw.WriteLine(Resources.exportProHeader);
                else
                    sw.WriteLine(Resources.exportPriHeader);
                // write options
                for (int i=0; i<content.Options.Count; i++)
                {
                    WriteProFileOption(sw, (ProFileOption)content.Options[i]);
                }
            }

            // open the file in vs
            if (openFile)
                dteObject.OpenFile(EnvDTE.Constants.vsViewKindTextView, proFileName).Activate();
        }

        private static void WriteProFileOption(StreamWriter sw, ProFileOption option)
        {
            if (option.List.Count <= 0)
                return;

            if (option.IncludeComment)
                sw.WriteLine(sw.NewLine + "#" + option.ShortComment);

            if (option.AssignSymbol != ProFileOption.AssignType.AT_Function)
            {
                sw.Write(option.Name);

                switch(option.AssignSymbol)
                {
                    case ProFileOption.AssignType.AT_Equals:
                        sw.Write(" = "); break;
                    case ProFileOption.AssignType.AT_MinusEquals:
                        sw.Write(" -= "); break;
                    case ProFileOption.AssignType.AT_PlusEquals:
                        sw.Write(" += "); break;
                }

                for (int i=0; i<option.List.Count-1; i++)
                {
                    sw.Write((string)option.List[i] + option.NewOption);
                }
                sw.Write((string)option.List[option.List.Count-1] + sw.NewLine);
            }
            else
            {
                for (int i=0; i<option.List.Count; i++)
                {
                    sw.WriteLine(option.Name + "(" + (string)option.List[i] + ")");
                }
            }
        }
        #endregion

        #region Import .pri Helper Functions
        private static List<string> GetFilesInPriFile(FileInfo priFileInfo, FilesToList ftl)
        {
            StreamReader sr;
            List<string> fileList = new List<string>();

            try
            {
                if (!priFileInfo.Exists)
                    return null;

                sr = new StreamReader(priFileInfo.FullName);
            }
            catch (System.Exception e)
            {
                Messages.DisplayWarningMessage(e);
                return null;
            }

            switch(ftl)
            {
                case FilesToList.FL_CppFiles:
                    ParseTag(sr, "SOURCES", fileList); break;
                case FilesToList.FL_HFiles:
                    ParseTag(sr, "HEADERS", fileList); break;
                case FilesToList.FL_UiFiles:
                    ParseTag(sr, "FORMS", fileList); break;
            }

            // the filelist should contain the entire path since we can select
            // any .pri file..
            List<string> ret = new List<string>();
            try
            {
                ret = ConvertFilesToFullPath(ret, priFileInfo.DirectoryName);
            }
            catch (System.Exception e)
            {
                Messages.DisplayErrorMessage(SR.GetString("ExportProject_ErrorParsingPriFile", e.Message),
                    SR.GetString("ExportProject_CheckFileAndSyntax"));
                return null;
            }

            sr.Close();
            return ret;
        }

        // a very simple and bad parser for .pri files...
        private static void ParseTag(StreamReader sr, string tag, List<string> arList)
        {
            // start parsing from the beginning...
            sr.BaseStream.Position = 0;

            string line;
            bool parsing = false;
            char [] trimChars = {'/', '\\', ' ', '\t', '=', '+', '-'};
            char [] sepChars = {'\t', ' '};

            while ((line = sr.ReadLine()) != null)
            {
                if (parsing && (line.IndexOf('=') != -1))
                    break;

                line = line.Trim();
                if(line.StartsWith(tag))
                {
                    line = line.Remove(0, tag.Length);
                    parsing = true;
                }

                if (parsing)
                {
                    // remove pwd, as we build the full path ourself
                    string pwd = "$$PWD";
                    if (line.IndexOf(pwd) > -1)
                        line = line.Remove(line.IndexOf(pwd), pwd.Length);

                    line = line.TrimStart(trimChars);
                    line = line.TrimEnd(trimChars);

                    // remove comments
                    int comStart = line.IndexOf('#');
                    if (comStart != -1)
                        line = line.Remove(comStart, line.Length-comStart);

                    if (line.Length > 0)
                        arList.AddRange(line.Split(sepChars));
                }
            }
        }

        private static VCFilter BestMatch(string path, Hashtable pathFilterTable)
        {
            string bestMatch = ".";
            string inPath = path;
            if (inPath.StartsWith(".\\"))
                inPath = inPath.Substring(2);
            foreach (string p in pathFilterTable.Keys)
            {
                int best = 0;
                for (int i = 0; i < inPath.Length; ++i)
                {
                    if (i < p.Length && inPath[i] == p[i])
                        ++best;
                    else
                        break;
                }
                if (best > bestMatch.Length && p.Length == best)
                    bestMatch = p;
            }
            return pathFilterTable[bestMatch] as VCFilter;
        }

        private static void CollectFilters(VCFilter filter, string path, ref Hashtable filterPathTable,
            ref Hashtable pathFilterTable)
        {
            string newPath = ".";
            if (path != null)
                newPath = path + "\\" +  filter.Name;
            newPath = newPath.ToLower().Trim();
            newPath = Regex.Replace(newPath, @"\\+\.?\\+", "\\");
            newPath = Regex.Replace(newPath, @"\\\.?$", "");
            if (newPath.StartsWith(".\\"))
                newPath = newPath.Substring(2);
            filterPathTable.Add(filter, newPath);
            pathFilterTable.Add(newPath, filter);
            foreach (VCFilter f in (IVCCollection)filter.Filters)
            {
                CollectFilters(f, newPath, ref filterPathTable, ref pathFilterTable);
            }
        }

        public static void SyncIncludeFiles(VCProject vcproj, List<string> priFiles,
            List<string> projFiles, EnvDTE.DTE dte)
        {
            SyncIncludeFiles(vcproj, priFiles, projFiles, dte, false, null);
        }

        public static void SyncIncludeFiles(VCProject vcproj, List<string> priFiles,
            List<string> projFiles, EnvDTE.DTE dte, bool flat, FakeFilter fakeFilter)
        {
            List<string> cmpPriFiles = new List<string>(priFiles.Count);
            foreach (string s in priFiles)
                cmpPriFiles.Add(HelperFunctions.NormalizeFilePath(s).ToLower());
            cmpPriFiles.Sort();

            List<string> cmpProjFiles = new List<string>(projFiles.Count);
            foreach (string s in projFiles)
                cmpProjFiles.Add(HelperFunctions.NormalizeFilePath(s).ToLower());

            QtProject qtPro = QtProject.Create(vcproj);
            Hashtable filterPathTable = new Hashtable(17);
            Hashtable pathFilterTable = new Hashtable(17);
            if (!flat && fakeFilter != null)
            {
                VCFilter rootFilter = qtPro.FindFilterFromGuid(fakeFilter.UniqueIdentifier);
                if (rootFilter == null)
                    qtPro.AddFilterToProject(Filters.SourceFiles());

                CollectFilters(rootFilter, null, ref filterPathTable, ref pathFilterTable);
            }

            // first check for new files
            foreach(string file in cmpPriFiles)
            {
                if (cmpProjFiles.IndexOf(file) > -1)
                    continue;

                if (flat)
                {
                    vcproj.AddFile(file); // the file is not in the project
                }
                else
                {
                    string path = HelperFunctions.GetRelativePath(vcproj.ProjectDirectory, file);
                    if (path.StartsWith(".\\"))
                        path =  path.Substring(2);

                    int i = path.LastIndexOf('\\');
                    if (i > -1)
                        path = path.Substring(0, i);
                    else
                        path = ".";

                    if (pathFilterTable.Contains(path))
                    {
                        VCFilter f = pathFilterTable[path] as VCFilter;
                        f.AddFile(file);
                        continue;
                    }

                    VCFilter filter = BestMatch(path, pathFilterTable);

                    string filterDir = filterPathTable[filter] as string;
                    string name = path;
                    if (!name.StartsWith("..") && name.StartsWith(filterDir))
                        name = name.Substring(filterDir.Length + 1);

                    VCFilter newFilter = filter.AddFilter(name) as VCFilter;
                    newFilter.AddFile(file);

                    filterPathTable.Add(newFilter, path);
                    pathFilterTable.Add(path, newFilter);
                }
            }

            // then check for deleted files
            foreach (string file in cmpProjFiles)
            {
                if (cmpPriFiles.IndexOf(file) == -1)
                {
                    // the file is not in the pri file
                    // (only removes it from the project, does not del. the file)
                    FileInfo info = new FileInfo(file);
                    HelperFunctions.RemoveFileInProject(vcproj, file);
                    Messages.PaneMessage(dte, "--- (Importing .pri file) file: " + info.Name + 
                        " does not exist in .pri file, move to " + vcproj.ProjectDirectory + "Deleted");
                }
            }
        }
        #endregion

        public void ExportToProFile()
        {
            ExportProjectDialog expDlg = new ExportProjectDialog();

            EnvDTE.Solution sln = dteObject.Solution;
            ProSolution prosln = CreateProFileSolution(sln);

            if (prosln.ProFiles.Count <= 0)
            {
                Messages.DisplayWarningMessage(SR.GetString("ExportProject_NoProjectsToExport"));
                return;
            }

            expDlg.ProFileSolution = prosln;
            expDlg.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            MainWinWrapper ww = new MainWinWrapper(dteObject);
            if (expDlg.ShowDialog(ww) == DialogResult.OK)
            {
                WriteProSolution(prosln, expDlg.OpenFiles);

                // create all the project .pro files
                foreach (ProFileContent profile in prosln.ProFiles)
                {
                    if (profile.Export)
                    {
                        WriteProFile(profile, null, expDlg.OpenFiles);
                        if (expDlg.CreatePriFile)
                            ExportToPriFile(HelperFunctions.VCProjectToProject(profile.Project));
                    }
                }
            }
        }

        public void ImportPriFile(EnvDTE.Project proj)
        {
            VCProject vcproj;

            if (HelperFunctions.IsQtProject(proj))
            {
                try
                {
                    vcproj = (VCProject)proj.Object;
                }
                catch (System.Exception e)
                {
                    Messages.DisplayWarningMessage(e);
                    return;
                }

                // make the user able to choose .pri file
                OpenFileDialog fd = new OpenFileDialog();
                fd.Multiselect = false;
                fd.CheckFileExists = true;
                fd.Title = SR.GetString("ExportProject_ImportPriFile");
                fd.Filter = "Project Include Files (*.pri)|*.pri";
                fd.InitialDirectory = vcproj.ProjectDirectory;
                fd.FileName = vcproj.Name + ".pri";

                if (fd.ShowDialog() != DialogResult.OK)
                    return;

                ImportPriFile(proj, fd.FileName);
            }
        }

        public void ImportPriFile(EnvDTE.Project proj, string fileName)
        {
            VCProject vcproj;

            List<string> priFiles;
            List<string> projFiles;

            try
            {
                vcproj = (VCProject)proj.Object;
            }
            catch (System.Exception e)
            {
                Messages.DisplayWarningMessage(e);
                return;
            }

            FileInfo priFileInfo = new FileInfo(fileName);

            // source files
            if ((priFiles = GetFilesInPriFile(priFileInfo, FilesToList.FL_CppFiles)) == null)
                return;
            projFiles = HelperFunctions.GetProjectFiles(proj, FilesToList.FL_CppFiles);
            projFiles = ConvertFilesToFullPath(projFiles, vcproj.ProjectDirectory);
            ProjectExporter.SyncIncludeFiles(vcproj, priFiles, projFiles, dteObject);

            // header files
            if ((priFiles = GetFilesInPriFile(priFileInfo, FilesToList.FL_HFiles)) == null)
                return;
            projFiles = HelperFunctions.GetProjectFiles(proj, FilesToList.FL_HFiles);
            projFiles = ConvertFilesToFullPath(projFiles, vcproj.ProjectDirectory);
            ProjectExporter.SyncIncludeFiles(vcproj, priFiles, projFiles, dteObject);

            // form files
            if ((priFiles = GetFilesInPriFile(priFileInfo, FilesToList.FL_UiFiles)) == null)
                return;
            projFiles = HelperFunctions.GetProjectFiles(proj, FilesToList.FL_UiFiles);
            projFiles = ConvertFilesToFullPath(projFiles, vcproj.ProjectDirectory);
            ProjectExporter.SyncIncludeFiles(vcproj, priFiles, projFiles, dteObject);
        }

        public void ExportToPriFile(EnvDTE.Project proj)
        {
            VCProject vcproj;

            if(HelperFunctions.IsQtProject(proj))
            {
                try
                {
                    vcproj = (VCProject)proj.Object;
                }
                catch(System.Exception e)
                {
                    Messages.DisplayErrorMessage(e);
                    return;
                }

                // make the user able to choose .pri file
                SaveFileDialog fd = new SaveFileDialog();
                fd.OverwritePrompt = true;
                fd.CheckPathExists = true;
                fd.Title = SR.GetString("ExportProject_ExportPriFile");
                fd.Filter = "Project Include Files (*.pri)|*.pri";
                fd.InitialDirectory = vcproj.ProjectDirectory;
                fd.FileName = vcproj.Name + ".pri";

                if (fd.ShowDialog() != DialogResult.OK)
                    return;

                ExportToPriFile(proj, fd.FileName);
            }
        }

        public void ExportToPriFile(EnvDTE.Project proj, string fileName)
        {
            ProFileContent content;

            FileInfo priFile = new FileInfo(fileName);

            content = CreateProFileContent(proj, priFile);
            WriteProFile(content, priFile, false);
        }
    }
}
