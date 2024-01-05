/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.VisualStudio.Workspace.VSIntegration.Contracts;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools.VisualStudio
{
    using Common;

    public static class VsShell
    {
        static LazyFactory Lazy { get; } = new LazyFactory();

        static IComponentModel ComponentModel => Lazy.Get(() => ComponentModel,
            () => VsServiceProvider.GetGlobalService<SComponentModel, IComponentModel>());

        public static I GetComponentService<I>() where I : class
        {
            return ComponentModel.GetService<I>();
        }

        public static IVsFolderWorkspaceService FolderWorkspace => Lazy.Get(() => FolderWorkspace,
            () => GetComponentService<IVsFolderWorkspaceService>());

        public static string InstallRootDir
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                Initialize();
                return _InstallRootDir;
            }
        }

        private static IVsShell vsShell;
        private static string _InstallRootDir;

        private static void Initialize()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (vsShell != null)
                return;
            vsShell = VsServiceProvider.GetService<IVsShell>();

            int res = vsShell.GetProperty((int)__VSSPROPID2.VSSPROPID_InstallRootDir, out object o);
            if (res == VSConstants.S_OK && o is string property)
                _InstallRootDir = property;
        }

        public static VCProject GetProject(IVsHierarchy context)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            int res = context.GetProperty(
                (uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ExtObject, out object value);
            if (res == VSConstants.S_OK && value is EnvDTE.Project project)
                return project.Object as VCProject;
            return null;
        }

        public static EnvDTE.ProjectItem GetProjectItem(IVsHierarchy context, uint itemid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            int res = context.GetProperty(
                itemid, (int)__VSHPROPID.VSHPROPID_ExtObject, out object value);
            if (res == VSConstants.S_OK && value is EnvDTE.ProjectItem item)
                return item;
            return null;
        }

        public static EnvDTE.Document GetDocument(IVsHierarchy context, uint itemid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetProjectItem(context, itemid)?.Document;
        }

        private static IVsInfoBarHost _InfoBarHost;
        public static IVsInfoBarHost InfoBarHost
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (_InfoBarHost != null)
                    return _InfoBarHost;
                Initialize();
                object host = null;
                vsShell?.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out host);
                return _InfoBarHost = host as IVsInfoBarHost;
            }
        }

        public static async Task UiThreadAsync(Action action)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            action();
        }
    }
}
