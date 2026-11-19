// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.VisualStudio
{
    using Core;

    public class DebuggerEvents : IVsDebuggerEvents
    {
        private readonly EnvDTE.DTE dte;

        public DebuggerEvents(EnvDTE.DTE dte)
        {
            this.dte = dte;
        }

        public int OnModeChange(DBGMODE dbgmodeNew)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (dbgmodeNew != DBGMODE.DBGMODE_Run)
                return VSConstants.S_OK;
            if (HelperFunctions.GetSelectedQtProject(dte) is not { } project)
                return VSConstants.S_OK;

            var @namespace = project.VersionInfo?.Namespace;
            if (!string.IsNullOrEmpty(@namespace))
                NatvisHelper.CopyVisualizersFiles(@namespace);

            return VSConstants.S_OK;
        }
    }
}
