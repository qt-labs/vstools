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
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Windows.Forms;

namespace QtProjectLib
{
    public class QtProjectEngine
    {
        private EnvDTE.Project pro = null;
        private QtProject qtPro = null;
        private const string commonError =
            "You have to call CreateXProject(...) or UseSelectedProject(...) before calling this function";

        #region helper functions

        private uint GetBuildConfigFromName(string configName)
        {
            if (configName == "RELEASE")
                return BuildConfig.Release;
            else if (configName == "DEBUG")
                return BuildConfig.Debug;
            else if (configName == "BOTH")
                return BuildConfig.Both;
            return BuildConfig.Both; // fall back to both
        }

        private FakeFilter GetFakeFilterFromName(string filterName)
        {
            if (filterName == "QT_SOURCE_FILTER")
                return Filters.SourceFiles();
            else if (filterName == "QT_HEADER_FILTER")
                return Filters.HeaderFiles();
            else if (filterName == "QT_FORM_FILTER")
                return Filters.FormFiles();
            else if (filterName == "QT_RESOURCE_FILTER")
                return Filters.ResourceFiles();
            else if (filterName == "QT_TRANSLATION_FILTER")
                return Filters.TranslationFiles();
            else if (filterName == "QT_OTHER_FILTER")
                return Filters.OtherFiles();

            return null;
        }

        private QtModule GetQtModuleFromName(string module)
        {
            return QtModules.Instance.ModuleIdByName(module);
        }

        private void CreateProject(EnvDTE.DTE app, string proName,
            string proPath, string slnName, bool exclusive, FakeFilter[] filters,
            string qtVersion, string platformName)
        {
            QtVersionManager versionManager = QtVersionManager.The();
            if (qtVersion == null)
                qtVersion = versionManager.GetDefaultVersion();

            if (qtVersion == null)
                throw new QtVSException("Unable to find a Qt build!\r\n"
                    + "To solve this problem specify a Qt build");

            string solutionPath = "";
            Solution newSolution = app.Solution;

            if (platformName == null) {
                string tmpQtVersion = versionManager.GetSolutionQtVersion(newSolution);
                qtVersion = tmpQtVersion != null ? tmpQtVersion : qtVersion;
                try {
                    VersionInformation vi = new VersionInformation(versionManager.GetInstallPath(qtVersion));
                    if (vi.is64Bit())
                        platformName = "x64";
                    else
                        platformName = "Win32";
                } catch (Exception e) {
                    Messages.DisplayErrorMessage(e);
                }
            }

            if (!string.IsNullOrEmpty(slnName) && (exclusive == true)) {
                solutionPath = proPath.Substring(0, proPath.LastIndexOf("\\"));
                newSolution.Create(solutionPath, slnName);
            }

            System.IO.Directory.CreateDirectory(proPath);
            string templatePath = HelperFunctions.CreateProjectTemplateFile(filters, true, platformName);

            pro = newSolution.AddFromTemplate(templatePath, proPath, proName, exclusive);

            HelperFunctions.ReleaseProjectTemplateFile();

            qtPro = QtProject.Create(pro);
            QtVSIPSettings.SaveUicDirectory(pro, null);
            QtVSIPSettings.SaveMocDirectory(pro, null);
            QtVSIPSettings.SaveMocOptions(pro, null);
            QtVSIPSettings.SaveRccDirectory(pro, null);
            QtVSIPSettings.SaveLUpdateOnBuild(pro);
            QtVSIPSettings.SaveLUpdateOptions(pro, null);
            QtVSIPSettings.SaveLReleaseOptions(pro, null);

            if (platformName != "Win32")
                qtPro.SelectSolutionPlatform(platformName);
            versionManager.SaveProjectQtVersion(pro, qtVersion);

            qtPro.MarkAsQtProject("v1.0");
            qtPro.AddDirectories();

            if (!string.IsNullOrEmpty(slnName) && (exclusive == true))
                newSolution.SaveAs(solutionPath + "\\" + slnName + ".sln");
        }
        #endregion

        #region functions for creating projects
        /// <summary>
        /// Creates an initializes a new qt library project. Call this function before calling other functions in this class.
        /// </summary>
        /// <param name="app">The DTE object</param>
        /// <param name="proName">Name of the project to create</param>
        /// <param name="proPath">The path to the new project</param>
        /// <param name="slnName">Name of solution to create (If this is empty it will create a solution with
        /// the same name as the project)</param>
        /// <param name="exclusive">true if the project should be opened in a new solution</param>
        /// <param name="staticLib">true if the project should be created as a static library</param>
        public void CreateLibraryProject(EnvDTE.DTE app, string proName,
            string proPath, string slnName, bool exclusive, bool staticLib, bool usePrecompiledHeaders)
        {
            FakeFilter[] filters = {Filters.SourceFiles(), Filters.HeaderFiles(),
                                       Filters.FormFiles(), Filters.ResourceFiles(), Filters.GeneratedFiles()};
            uint projType;
            if (staticLib)
                projType = TemplateType.StaticLibrary | TemplateType.GUISystem;
            else
                projType = TemplateType.DynamicLibrary | TemplateType.GUISystem;

            CreateProject(app, proName, proPath, slnName, exclusive, filters, null, null);
            qtPro.WriteProjectBasicConfigurations(projType, usePrecompiledHeaders);
            qtPro.AddModule(QtModule.Main);
        }

        /// <summary>
        /// Creates an initializes a new qt console application project. Call this function before calling other functions in this class.
        /// </summary>
        /// <param name="app">The DTE object</param>
        /// <param name="proName">Name of the project to create</param>
        /// <param name="proPath">The path to the new project</param>
        /// <param name="slnName">Name of solution to create (If this is empty it will create a solution with
        /// the same name as the project)</param>
        /// <param name="exclusive">true if the project should be opened in a new solution</param>
        public void CreateConsoleProject(EnvDTE.DTE app, string proName,
            string proPath, string slnName, bool exclusive, bool usePrecompiledHeaders)
        {
            FakeFilter[] filters = {Filters.SourceFiles(), Filters.HeaderFiles(),
                                       Filters.ResourceFiles(), Filters.GeneratedFiles()};
            const uint projType = TemplateType.Application | TemplateType.ConsoleSystem;
            CreateProject(app, proName, proPath, slnName, exclusive, filters, null, null);
            qtPro.WriteProjectBasicConfigurations(projType, usePrecompiledHeaders);
            qtPro.AddModule(QtModule.Main);
        }

        /// <summary>
        /// Creates an initializes a new qt application project. Call this function before calling other functions in this class.
        /// </summary>
        /// <param name="app">The DTE object</param>
        /// <param name="proName">Name of the project to create</param>
        /// <param name="proPath">The path to the new project</param>
        /// <param name="slnName">Name of solution to create (If this is empty it will create a solution with
        /// the same name as the project)</param>
        /// <param name="exclusive">true if the project should be opened in a new solution</param>
        public void CreateApplicationProject(EnvDTE.DTE app, string proName,
            string proPath, string slnName, bool exclusive, bool usePrecompiledHeaders)
        {
            FakeFilter[] filters = {Filters.SourceFiles(), Filters.HeaderFiles(),
                                       Filters.FormFiles(), Filters.ResourceFiles(), Filters.GeneratedFiles()};
            const uint projType = TemplateType.Application | TemplateType.GUISystem;
            CreateProject(app, proName, proPath, slnName, exclusive, filters, null, null);
            qtPro.WriteProjectBasicConfigurations(projType, usePrecompiledHeaders);
            qtPro.AddModule(QtModule.Main);
        }

        public void CreatePluginProject(EnvDTE.DTE app, string proName,
            string proPath, string slnName, bool exclusive, bool usePrecompiledHeaders)
        {
            FakeFilter[] filters = { Filters.SourceFiles(), Filters.HeaderFiles(), Filters.GeneratedFiles() };
            const uint projType = TemplateType.PluginProject | TemplateType.DynamicLibrary | TemplateType.GUISystem;
            CreateProject(app, proName, proPath, slnName, exclusive, filters, null, null);
            qtPro.WriteProjectBasicConfigurations(projType, usePrecompiledHeaders);
            qtPro.AddModule(QtModule.Main);
            qtPro.AddModule(QtModule.Designer);
        }
        #endregion

        /// <summary>
        /// Adds a file to the project
        /// </summary>
        /// <param name="file">file (result from CopyFileToProjectFolder)</param>
        /// <param name="filter">the filter
        /// can be one of the following: QT_SOURCE_FILTER, QT_HEADER_FILTER,
        /// QT_FORM_FILTER, QT_RESOURCE_FILTER, QT_TRANSLATION_FILTER, QT_OTHER_FILTER</param>
        public VCFile AddFileToProject(string file, string filter)
        {
            if (qtPro == null)
                throw new QtVSException(commonError);
            qtPro.AdjustWhitespace(file);
            return qtPro.AddFileToProject(file, GetFakeFilterFromName(filter));
        }

        /// <summary>
        /// Copy a file to the project folder.
        /// </summary>
        /// <param name="srcFile">full path to source file</param>
        /// <param name="destName">destination file (relative to the location of the project)</param>
        /// <returns>full path to the destination file</returns>
        public string CopyFileToProjectFolder(string srcFile, string destName)
        {
            if (qtPro == null)
                throw new QtVSException(commonError);

            return qtPro.CopyFileToProject(srcFile, destName);
        }

        public string CopyFileToFolder(string srcFile, string destFolder, string destName)
        {
            return QtProject.CopyFileToFolder(srcFile, destFolder, destName);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="className"></param>
        /// <param name="destName"></param>
        /// <returns></returns>
        public string CreateQrcFile(string className, string destName)
        {
            if (qtPro == null)
                throw new QtVSException(commonError);
            return qtPro.CreateQrcFile(className, destName);
        }

        /// <summary>
        /// Adds a qt module to the project
        /// </summary>
        /// <param name="module">the module to add
        /// see QtModules.ModuleIdByName()
        /// </param>
        public void AddModule(string module)
        {
            if (qtPro == null)
                throw new QtVSException(commonError);
            qtPro.AddModule(GetQtModuleFromName(module));
        }

        /// <summary>
        /// Removes a qt module from the project
        /// </summary>
        /// <param name="module">the module to remove
        /// see QtModules.ModuleIdByName()
        /// </param>
        public void RemoveModule(string module)
        {
            if (qtPro == null)
                throw new QtVSException(commonError);
            qtPro.RemoveModule(GetQtModuleFromName(module));
        }

        /// <summary>
        /// Checks if an add-on qt module is installed
        /// </summary>
        /// <param name="moduleName">the module to find
        /// </param>
        public bool IsModuleInstalled(string moduleName)
        {
            QtVersionManager versionManager = QtVersionManager.The();
            string qtVersion = versionManager.GetDefaultVersion();
            if (qtVersion == null) {
                throw new QtVSException("Unable to find a Qt build!\r\n"
                    + "To solve this problem specify a Qt build");
            }
            string install_path = versionManager.GetInstallPath(qtVersion);

            if (moduleName.StartsWith("Qt", StringComparison.Ordinal))
                moduleName = "Qt5" + moduleName.Substring(2);

            string full_path = install_path + "\\lib\\" + moduleName + ".lib";

            System.IO.FileInfo fi = new System.IO.FileInfo(full_path);

            return fi.Exists;
        }

        /// <summary>
        /// Adds a icon resource to the project
        /// </summary>
        /// <param name="iconFileName">the icon file to add</param>
        /// <returns>true if everything is ok</returns>
        public bool AddApplicationIcon(string iconFileName)
        {
            if (qtPro == null)
                throw new QtVSException(commonError);
            return qtPro.AddApplicationIcon(iconFileName);
        }

        /// <summary>
        /// Creates a new GUID and returns it as a string.
        /// </summary>
        /// <returns>the new GUID</returns>
        public string CreateNewGUID()
        {
            return System.Guid.NewGuid().ToString().ToUpper();
        }

        /// <summary>
        /// Returns the file name of a given file path.
        /// </summary>
        /// <param name="filePath">can be relative or absolute</param>
        /// <returns>the file name</returns>
        public string GetFileName(string filePath)
        {
            // TODO: Use Path instead of FileInfo.
            System.IO.FileInfo fi = new System.IO.FileInfo(filePath);
            return fi.Name;
        }

        /// <summary>
        /// Replaces a token in a VCFile. The tokens are usually class names and file names.
        /// </summary>
        /// <param name="file">the file in which to replace tokens</param>
        /// <param name="token">the token to replace</param>
        /// <param name="replacement">the replacement value</param>
        public void ReplaceTokenInFile(string file, string token, string replacement)
        {
            QtProject.ReplaceTokenInFile(file, token, replacement);
        }

        public void EnableSection(string file, string sectionName, bool enable)
        {
            QtProject.EnableSection(file, sectionName, enable);
        }

        /// <summary>
        /// Adds a ActiveQt build step to the project.
        /// </summary>
        /// <param name="version">the version of the library</param>
        public void AddActiveQtBuildStep(string version)
        {
            if (qtPro == null)
                throw new QtVSException(commonError);
            qtPro.AddActiveQtBuildStep(version);
        }

        /// <summary>
        /// Adds a define to the config.
        /// </summary>
        /// <param name="define">the define to add</param>
        /// <param name="config">the config (can be RELEASE, DEBUG or BOTH)</param>
        public void AddDefine(string define, string config)
        {
            if (qtPro == null)
                throw new QtVSException(commonError);
            qtPro.AddDefine(define, GetBuildConfigFromName(config));
        }

        /// <summary>
        /// The created project.
        /// </summary>
        public EnvDTE.Project Project()
        {
            if (qtPro == null)
                throw new QtVSException(commonError);
            return pro;
        }

        /// <summary>
        /// Finishes the creation of the qt project
        /// </summary>
        public void Finish()
        {
            if (qtPro == null)
                throw new QtVSException(commonError);
            qtPro.Finish();
        }

        public void UseSelectedProject(EnvDTE.DTE app)
        {
            pro = HelperFunctions.GetSelectedQtProject(app);
            if (pro == null)
                throw new QtVSException("Can't find a selected project");

            qtPro = QtProject.Create(pro);
        }

        public bool IsSelectedProjectQt(EnvDTE.DTE app)
        {
            pro = HelperFunctions.GetSelectedQtProject(app);
            if (pro == null)
                return false;
            return true;
        }

        public string ShowOpenFolderDialog(string directory)
        {
            FolderBrowserDialog dia = new FolderBrowserDialog();
            dia.Description = "Select a directory:";
            dia.SelectedPath = directory;
            if (dia.ShowDialog() == DialogResult.OK)
                return dia.SelectedPath;
            return directory;
        }

        public bool UsesPrecompiledHeaders()
        {
            if (qtPro == null)
                throw new QtVSException(commonError);
            return qtPro.UsesPrecompiledHeaders();
        }

        public string GetPrecompiledHeaderThrough()
        {
            if (qtPro == null)
                throw new QtVSException(commonError);
            return qtPro.GetPrecompiledHeaderThrough();
        }
    }
}
