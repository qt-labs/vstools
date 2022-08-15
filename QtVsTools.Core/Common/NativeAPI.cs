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
using System.Runtime.InteropServices;
using System.Text;

namespace QtVsTools.Core
{
    public static class NativeAPI
    {
        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;
        public const int WS_VISIBLE = 0x10000000;
        public const int WM_CLOSE = 0x10;
        public const int WM_STYLECHANGED = 0x007D;
        public const int WM_GETICON = 0x007F;
        public const int WM_SETICON = 0x0080;
        public const int ICON_SMALL = 0;
        public const int GCL_HICON = -14;
        public const int GCL_HICONSM = -34;
        public const int SW_HIDE = 0;
        public const int SW_SHOWMINNOACTIVE = 7;
        public const int SW_RESTORE = 9;
        public const int WS_MAXIMIZEBOX = 0x00010000;
        public const int WS_MINIMIZEBOX = 0x00020000;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumThreadWindows(
            uint dwThreadId,
            EnumThreadWindowsCallback lpfn,
            IntPtr lParam);

        public delegate bool EnumThreadWindowsCallback(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetWindowTextW")]
        public static extern int GetWindowText(IntPtr hwnd, StringBuilder lpString, int maxCount);

        public static string GetWindowCaption(IntPtr hwnd)
        {
            var caption = new StringBuilder(256);
            if (GetWindowText(hwnd, caption, caption.Capacity) > 0)
                return caption.ToString();
            else
                return string.Empty;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW", CharSet = CharSet.Unicode)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetClassLongW", CharSet = CharSet.Unicode)]
        public static extern int GetClassLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(
            IntPtr Handle,
            int x, int y,
            int w, int h,
            bool repaint);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
