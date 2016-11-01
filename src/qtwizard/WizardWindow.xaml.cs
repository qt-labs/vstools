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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Navigation;

namespace QtProjectWizard
{
    internal static class NativeMethods
    {
        [ResourceExposure(ResourceScope.None)]
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern int GetWindowLong(IntPtr hwnd, int index);
    }

    internal static class UnsafeNativeMethods
    {
        [ResourceExposure(ResourceScope.None)]
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern int SetWindowLong(IntPtr hwnd, int index, int value);
    }

    public partial class WizardWindow : NavigationWindow
    {

        public WizardWindow(List<WizardPage> pages)
        {
            InitializeComponent();
            SourceInitialized += onSourceInitialized;

            Pages = pages ?? new List<WizardPage>();
            foreach (var page in Pages) {
                page.Wizard = this;
                page.NavigateForward += OnNavigateForward;
                page.NavigatedBackward += OnNavigatedBackwards;
            }

            if (Pages.Count > 0) {
                NextPage.ReturnEx += OnPageReturn;
                Navigate(NextPage); // put on navigation stack
            }
        }

        public WizardPage NextPage
        {
            get
            {
                return Pages[currentPage];
            }
        }

        public List<WizardPage> Pages
        {
            get;
            private set;
        }

        private int currentPage;

        private void onSourceInitialized(object sender, EventArgs e)
        {
            try {
                var STYLE = -16; // see winuser.h
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                UnsafeNativeMethods.SetWindowLong(hwnd, STYLE,
                    NativeMethods.GetWindowLong(hwnd, STYLE) & ~(0x10000 | 0x20000));
            } catch {
                // Ignore if we can't remove the buttons.
                SourceInitialized -= onSourceInitialized;
            }
        }

        private void OnNavigateForward(object sender, EventArgs e)
        {
            var tmp = currentPage + 1;
            if (tmp >= Pages.Count) {
                throw new InvalidOperationException(@"Current wizard page "
                    + @"cannot be equal or greather then pages count.");
            }
            currentPage++;
        }

        private void OnPageReturn(object sender, ReturnEventArgs<WizardResult> e)
        {
            if (DialogResult == null)
                DialogResult = (e.Result == WizardResult.Finished);
        }

        private void OnNavigatedBackwards(object sender, EventArgs e)
        {
            var tmp = currentPage - 1;
            if (tmp < 0)
                throw new InvalidOperationException(@"Current wizard page cannot be less then 0.");
            currentPage--;
        }
    }
}
