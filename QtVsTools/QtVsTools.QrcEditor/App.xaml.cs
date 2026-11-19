// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace QtVsTools.QrcEditor
{
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var commandLine = CommandLine.Parse(e.Args);
            if (commandLine.DarkTheme) {
                Resources.MergedDictionaries[0] = new ResourceDictionary
                {
                    Source = new Uri("Themes/Dark.xaml", UriKind.Relative)
                };
            }

            var window = new MainWindow();
            MainWindow = window;

            // Keep the editor, and with it unsaved edits, alive after an unexpected error.
            DispatcherUnhandledException += (_, args) =>
            {
                window.ShowUnexpectedError(args.Exception);
                args.Handled = true;
            };

            // The extension starts the editor hidden and then moves its window into a document
            // tab of Visual Studio. WPF ignores the show state requested at process start, so
            // keep the window out of sight until it is embedded.
            if (IsStartedHidden())
                window.ShowOffScreen();
            else
                window.Show();

            if (!string.IsNullOrEmpty(commandLine.FilePath))
                window.OpenFile(commandLine.FilePath);
        }

        private static bool IsStartedHidden()
        {
            const int STARTF_USESHOWWINDOW = 0x00000001;
            const short SW_HIDE = 0;

            var startupInfo = new STARTUPINFO { cb = Marshal.SizeOf<STARTUPINFO>() };
            GetStartupInfo(ref startupInfo);
            return (startupInfo.dwFlags & STARTF_USESHOWWINDOW) != 0
                && startupInfo.wShowWindow == SW_HIDE;
        }

        // String members are declared as IntPtr: the strings belong to the process, so the
        // marshaler must not free them.
        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public IntPtr lpReserved;
            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [DllImport("kernel32.dll", EntryPoint = "GetStartupInfoW")]
        private static extern void GetStartupInfo(ref STARTUPINFO lpStartupInfo);
    }
}
