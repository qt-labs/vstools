// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.VisualStudio
{
    using Core;
    using Core.MsBuild;

    public class UpdateSolutionEvents : IVsUpdateSolutionEvents
    {
        public delegate void ProjectConfigurationDelegate(ProjectConfigurationEventArgs args);
        public event ProjectConfigurationDelegate ProjectConfigurationChanged;

        // Called when the solution update (build) is about to begin.
        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        // Called when the solution update (build) has finished.
        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            return VSConstants.S_OK;
        }

        // Called if the solution update (build) is canceled.
        public int UpdateSolution_Cancel()
        {
            return VSConstants.S_OK;
        }

        // Called when the solution update (build) starts updating.
        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called when the active project configuration changes.
        /// </summary>
        /// <param name="pHierarchy">
        /// The hierarchy of the project whose configuration has changed.
        /// </param>
        public int OnActiveProjectCfgChange(IVsHierarchy pHierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (ProjectConfigurationChanged == null || pHierarchy == null)
                return VSConstants.S_OK;
            if (MsBuildProject.GetOrAdd(VsShell.GetProject(pHierarchy)) is not { } project)
                return VSConstants.S_OK;

            ProjectConfigurationChanged.Invoke(new ProjectConfigurationEventArgs
            {
                ProjectPath = project.VcProjectPath,
                ConfigurationName = project.VcProject?.ActiveConfiguration?.Name
            });
            return VSConstants.S_OK;
        }
    }
}
