/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.ComponentModel.Design;
using System.IO;
using Microsoft.VisualStudio.Shell;

namespace QtVsTools
{
    using Core;
    using Core.MsBuild;
    using QtVsTools.Core.Common;
    using VisualStudio;

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class QtProjectContextMenu
    {
        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        private static QtProjectContextMenu Instance
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        public static void Initialize()
        {
            Instance = new QtProjectContextMenu();
        }

        /// <summary>
        /// Command ID.
        /// TODO: Remove, take form QtMenus.Package
        /// </summary>
        private enum CommandId
        {
            ImportPriFileProject = QtMenus.Package.ImportPriFileProject,
            lUpdateOnProject = QtMenus.Package.lUpdateOnProject,
            lReleaseOnProject = QtMenus.Package.lReleaseOnProject,
            ProjectConvertToQtMsBuild = QtMenus.Package.ProjectConvertToQtMsBuild,
            ProjectRefreshIntelliSense = QtMenus.Package.ProjectRefreshIntelliSense,
            QtProjectSettingsProject = QtMenus.Package.QtProjectSettingsProject
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QtMainMenu"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        private QtProjectContextMenu()
        {
            var commandService = VsServiceProvider
                .GetService<IMenuCommandService, OleMenuCommandService>();
            if (commandService == null)
                return;

            foreach (int id in Enum.GetValues(typeof(CommandId))) {
                var command = new OleMenuCommand(execHandler,
                    new CommandID(QtMenus.Package.Guid, id));
                command.BeforeQueryStatus += beforeQueryStatus;
                commandService.AddCommand(command);
            }
        }

        private void execHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is not OleMenuCommand command)
                return;

            var dte = QtVsToolsPackage.Instance.Dte;
            switch (command.CommandID.ID) {
            case QtMenus.Package.ImportPriFileProject:
                ProjectImporter.ImportPriFile(dte, Utils.PackageInstallPath);
                break;
            case QtMenus.Package.lUpdateOnProject:
                Translation.RunlUpdate(HelperFunctions.GetSelectedQtProject(dte));
                break;
            case QtMenus.Package.lReleaseOnProject:
                Translation.RunlRelease(HelperFunctions.GetSelectedQtProject(dte));
                break;
            case QtMenus.Package.QtProjectSettingsProject:
                QtVsToolsPackage.Instance.Dte.ExecuteCommand("Project.Properties");
                break;
            case QtMenus.Package.ProjectConvertToQtMsBuild:
                if (HelperFunctions.GetSelectedProject(Instances.Package.Dte) is not { } vcProject)
                    break;
                switch (MsBuildProjectFormat.GetVersion(vcProject)) {
                case MsBuildProjectFormat.Version.Latest:
                    VsEditor.Open(Path.ChangeExtension(vcProject.ProjectFile, ".qtvscr"));
                    break;
                default:
                    MsBuildProjectConverter.ProjectToQtMsBuild();
                    break;
                }
                break;
            case QtMenus.Package.ProjectRefreshIntelliSense:
                if (HelperFunctions.GetSelectedQtProject(dte) is not {} project)
                    break;
                project.Refresh();
                break;
            }
        }

        private void beforeQueryStatus(object sender, EventArgs e)
        {
            if (sender is not OleMenuCommand command)
                return;

            command.Visible = command.Enabled = false;
            var vcProject = HelperFunctions.GetSelectedProject(QtVsToolsPackage.Instance.Dte);

            // Filter out non-VC projects
            if (vcProject is null)
                return;

            switch (command.CommandID.ID) {
            case QtMenus.Package.ImportPriFileProject:
            case QtMenus.Package.QtProjectSettingsProject:
            case QtMenus.Package.ProjectRefreshIntelliSense:
                command.Visible = command.Enabled = MsBuildProject.GetOrAdd(vcProject) is {};
                break;
            case QtMenus.Package.lUpdateOnProject:
            case QtMenus.Package.lReleaseOnProject:
                if (MsBuildProject.GetOrAdd(vcProject) is not {} project)
                    break;
                command.Visible = true;
                command.Enabled = Translation.ToolsAvailable(project);
                break;
            case QtMenus.Package.ProjectConvertToQtMsBuild:
                switch (MsBuildProjectFormat.GetVersion(vcProject)) {
                case MsBuildProjectFormat.Version.V1:
                case MsBuildProjectFormat.Version.V2:
                    command.Visible = command.Enabled = true;
                    return;
                case >= MsBuildProjectFormat.Version.V3 and < MsBuildProjectFormat.Version.Latest:
                    command.Visible = command.Enabled = true;
                    command.Text = "Upgrade project to latest Qt project format version";
                    return;
                case MsBuildProjectFormat.Version.Unknown:
                    command.Visible = command.Enabled = true;
                    command.Text = "Convert to Qt/MSBuild project";
                    return;
                case MsBuildProjectFormat.Version.Latest:
                    if (File.Exists(Path.ChangeExtension(vcProject.ProjectFile, ".qtvscr"))) {
                        command.Visible = command.Enabled = true;
                        command.Text = "View conversion report";
                    }
                    return;
                }
                break;
            }
        }
    }
}
