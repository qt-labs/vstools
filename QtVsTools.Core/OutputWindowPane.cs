/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Core
{
    using VisualStudio;

    public class OutputWindowPane
    {
        public enum VSOutputWindowPane
        {
            General,
            Build,
            Debug,
        }

        public static Task<OutputWindowPane> GetVSOutputWindowPaneAsync(VSOutputWindowPane pane)
        {
            switch (pane) {
            case VSOutputWindowPane.General:
                return GetAsync(VSConstants.OutputWindowPaneGuid.GeneralPane_guid);
            case VSOutputWindowPane.Build:
                return GetAsync(VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid);
            case VSOutputWindowPane.Debug:
                return GetAsync(VSConstants.OutputWindowPaneGuid.DebugPane_guid);
            default:
                throw new InvalidOperationException("Unsupported Visual Studio output pane");
            };
        }

        public static async Task<OutputWindowPane> GetAsync(Guid guid)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try {
                IVsOutputWindow w = null;
                if (guid == VSConstants.OutputWindowPaneGuid.GeneralPane_guid) {
                    w = await VsServiceProvider.GetServiceAsync<SVsGeneralOutputWindowPane,
                        IVsOutputWindow>();
                } else {
                    w = await VsServiceProvider.GetServiceAsync<SVsOutputWindow, IVsOutputWindow>();
                }
                ErrorHandler.ThrowOnFailure(w.GetPane(guid, out IVsOutputWindowPane pane));

                return new OutputWindowPane(guid, pane);
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex);
                return null;
            }
        }

        public static async Task<OutputWindowPane> CreateAsync(string name, Guid guid)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException($"{ nameof(name) } cannot be null");

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try {
                var w = await VsServiceProvider.GetServiceAsync<SVsOutputWindow, IVsOutputWindow>();

                const int visible = 1, clear = 1;
                ErrorHandler.ThrowOnFailure(w.CreatePane(guid, name, visible, clear));
                ErrorHandler.ThrowOnFailure(w.GetPane(guid, out IVsOutputWindowPane pane));

                return new OutputWindowPane(guid, pane);
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex);
                return null;
            }
        }

        public static async Task<OutputWindowPane> GetActiveAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try {
                var w2 = await VsServiceProvider
                    .GetServiceAsync<SVsOutputWindow, IVsOutputWindow>() as IVsOutputWindow2;
                ErrorHandler.ThrowOnFailure(w2.GetActivePaneGUID(out Guid guid));

                IVsOutputWindow w = w2 as IVsOutputWindow;
                ErrorHandler.ThrowOnFailure(w.GetPane(guid, out IVsOutputWindowPane pane));

                return new OutputWindowPane(guid, pane);
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex);
                return null;
            }
        }

        private Guid Guid { get; }
        private IVsOutputWindowPane Pane { get; set; } = null;

        private OutputWindowPane(Guid guid, IVsOutputWindowPane pane)
        {
            Guid = guid;
            Pane = pane;
        }

        public async Task ActivateAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (Pane == null)
                throw new InvalidOperationException($"{ nameof(Pane) } cannot be null");
            Pane.Activate();
        }

        public async Task HideAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (Pane == null)
                throw new InvalidOperationException($"{ nameof(Pane) } cannot be null");
            Pane.Hide();
        }

        public async Task ClearAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (Pane == null)
                throw new InvalidOperationException($"{ nameof(Pane) } cannot be null");
            Pane.Clear();
        }

        public void Print()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () => { await PrintAsync(""); });
        }

        public void Print(string value)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () => { await PrintAsync(value); });
        }

        public Task PrintAsync()
        {
            return PrintAsync("");
        }

        public async Task PrintAsync(string value)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (Pane is IVsOutputWindowPaneNoPump noPumpPane)
                noPumpPane.OutputStringNoPump(value + Environment.NewLine);
            else
                ErrorHandler.ThrowOnFailure(Pane.OutputStringThreadSafe(value + Environment.NewLine));
        }
    }
}
