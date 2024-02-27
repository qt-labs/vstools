/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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
            Debug
        }

        public static Task<OutputWindowPane> GetVSOutputWindowPaneAsync(VSOutputWindowPane pane)
        {
            return pane switch
            {
                VSOutputWindowPane.General => GetAsync(
                    VSConstants.OutputWindowPaneGuid.GeneralPane_guid),
                VSOutputWindowPane.Build => GetAsync(
                    VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid),
                VSOutputWindowPane.Debug => GetAsync(
                    VSConstants.OutputWindowPaneGuid.DebugPane_guid),
                _ => throw new InvalidOperationException("Unsupported Visual Studio output pane")
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
        private IVsOutputWindowPane Pane { get; set; }

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
            ThreadHelper.JoinableTaskFactory.Run(async () => { await PrintAsync(); });
        }

        public void Print(string value)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () => { await PrintAsync(value); });
        }

        public async Task PrintAsync(string value = "")
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (Pane is IVsOutputWindowPaneNoPump noPumpPane)
                noPumpPane.OutputStringNoPump(value + Environment.NewLine);
            else
                ErrorHandler.ThrowOnFailure(Pane.OutputStringThreadSafe(value + Environment.NewLine));
        }
    }
}
