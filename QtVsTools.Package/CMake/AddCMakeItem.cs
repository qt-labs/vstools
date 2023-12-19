/**************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
**************************************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Workspace.VSIntegration.UI;

using Constants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace QtVsTools.Package.CMake
{
    using QtVsTools.Core.CMake;
    using QtVsTools.Core.Common;
    using VisualStudio;

    using CommandTable = QtMenus.Package;

    internal sealed class AddCMakeItem
    {
        private static AddCMakeItem instance;

        public static void Initialize()
        {
            instance = new AddCMakeItem();
        }

        private AddCMakeItem()
        {
            var commandService = VsServiceProvider
                .GetService<IMenuCommandService, OleMenuCommandService>();
            if (commandService == null)
                return;

            var command = new OleMenuCommand(null,
                new CommandID(CommandTable.Guid, CommandTable.AddNewQtCMakeItem));
            command.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(command);
        }

        private static void BeforeQueryStatus(object sender, EventArgs e)
        {
            if (sender is not OleMenuCommand command)
                return;
            command.Visible = command.Enabled = AddCMakeItemCommandHandler.IsQtCMakeProject;
        }
    }

    [Export(typeof(INodeExtender))]
    internal class AddCMakeItemNodeExtender : INodeExtender
    {
        private readonly AddCMakeItemCommandHandler handler = new();

        public IChildrenSource ProvideChildren(WorkspaceVisualNodeBase parentNode) => null;

        public IWorkspaceCommandHandler ProvideCommandHandler(WorkspaceVisualNodeBase parentNode)
        {
            return parentNode switch
            {
                IFileNode => handler,
                IFolderNode => handler,
                _ => null
            };
        }
    }

    internal class AddCMakeItemCommandHandler : IWorkspaceCommandHandler
    {
        public int Priority => 100;

        public bool IgnoreOnMultiselect => true;

        public static bool IsQtCMakeProject { get; private set; }

        public int Exec(List<WorkspaceVisualNodeBase> selection, Guid commandGroupGuid,
            uint cmdId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (commandGroupGuid != CommandTable.Guid || cmdId != CommandTable.AddNewQtCMakeItem)
                return (int)Constants.OLECMDERR_E_NOTSUPPORTED;

            if (selection.FirstOrDefault() is not IFileSystemNode node)
                return (int)Constants.OLECMDERR_E_NOTSUPPORTED;

            var projectTypeGuid = new Guid(Utils.ProjectTypes.C_PLUS_PLUS);
            const uint uiFlags = (uint)(__VSADDITEMFLAGS.VSADDITEM_AddNewItems
                | __VSADDITEMFLAGS.VSADDITEM_SuggestTemplateName
                | __VSADDITEMFLAGS.VSADDITEM_AllowHiddenTreeView);
            string directoryPath = node.FullPath, filter = "";

            var service = VsServiceProvider.GetService<SVsAddProjectItemDlg, IVsAddProjectItemDlg>();
            service?.AddProjectItemDlg(1u, ref projectTypeGuid, new CMakeVsProject(node),
                uiFlags, "Qt", "Qt Class", ref directoryPath, ref filter, out _);

            return VSConstants.S_OK;
        }

        public bool QueryStatus(List<WorkspaceVisualNodeBase> selection, Guid commandGroupGuid,
            uint cmdId, ref uint commandFlags, ref string customTitle)
        {
            if (commandGroupGuid != CommandTable.Guid || cmdId != CommandTable.AddNewQtCMakeItem)
                return false;

            if (selection.FirstOrDefault()?.Workspace is not {} workspace)
                return false;

            if (CMakeProject.ActiveProject is not { Status: CMakeProject.QtStatus.True })
                return false;

            if (!CMakeProject.ActiveProject.RootPath.Equals(workspace.Location, Utils.IgnoreCase))
                return false;

            IsQtCMakeProject = true;
            commandFlags = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
            return true;
        }
    }
}
