/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Navigation;

namespace QtVsTools.Wizards.Common
{
    using Core;

    public partial class WizardWindow : NavigationWindow, IEnumerable<WizardPage>
    {

        public WizardWindow(IEnumerable<WizardPage> pages = null, string title = null)
        {
            InitializeComponent();
            SourceInitialized += OnSourceInitialized;

            if (title != null)
                Title = title;

            Pages = new List<WizardPage>();

            if (pages != null) {
                foreach (var page in pages)
                    Add(page);
            }
        }

        public void Add(WizardPage page)
        {
            bool isFirstPage = Pages.Count == 0;
            page.Wizard = this;
            page.NavigateForward += OnNavigateForward;
            page.NavigatedBackward += OnNavigatedBackwards;
            Pages.Add(page);

            if (isFirstPage) {
                NextPage.ReturnEx += OnPageReturn;
                Navigate(NextPage); // put on navigation stack
            }
        }

        public WizardPage NextPage => Pages[currentPage];

        private List<WizardPage> Pages
        {
            get;
        }

        public IEnumerator<WizardPage> GetEnumerator()
        {
            return ((IEnumerable<WizardPage>)Pages).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<WizardPage>)Pages).GetEnumerator();
        }

        private int currentPage;

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            try {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                var windowStyles = NativeAPI.GetWindowLong(hwnd, NativeAPI.GWL_STYLE);
                NativeAPI.SetWindowLong(hwnd, NativeAPI.GWL_STYLE,
                    windowStyles & ~(NativeAPI.WS_MAXIMIZEBOX | NativeAPI.WS_MINIMIZEBOX));
            } catch {
                // Ignore if we can't remove the minimize and maximize buttons.
                SourceInitialized -= OnSourceInitialized;
            }
        }

        private void OnNavigateForward(object sender, EventArgs e)
        {
            var tmp = currentPage + 1;
            if (tmp >= Pages.Count) {
                throw new InvalidOperationException(@"Current wizard page "
                    + @"cannot be equal or greater than pages count.");
            }
            currentPage++;
        }

        private void OnPageReturn(object sender, ReturnEventArgs<WizardResult> e)
        {
            DialogResult ??= e.Result == WizardResult.Finished;
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
