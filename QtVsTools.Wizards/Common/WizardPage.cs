/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Windows;
using System.Windows.Navigation;

namespace QtVsTools.Wizards.Common
{
    public class WizardPage : PageFunction<WizardResult>
    {
        public string Header { get; set; }
        public string Message { get; set; }

        public WizardData Data { get; set; }
        public WizardWindow Wizard { get; set; }

        public bool PreviousButtonEnabled { get; set; }
        public bool NextButtonEnabled { get; set; }
        public bool FinishButtonEnabled { get; set; }
        public bool CancelButtonEnabled { get; set; }

        public event EventHandler NavigateForward;
        public event EventHandler NavigatedBackward;

        public event ReturnEventHandler<WizardResult> ReturnEx;
        public void OnReturnEx(ReturnEventArgs<WizardResult> e)
        {
            ReturnEx?.Invoke(this, e);
        }

        protected virtual void OnPreviousButtonClick(object sender, RoutedEventArgs e)
        {
            if (NavigationService == null || !NavigationService.CanGoBack)
                return;

            NavigationService.GoBack();
            OnNavigatedBackward(EventArgs.Empty);
        }

        protected virtual void OnNextButtonClick(object sender, RoutedEventArgs e)
        {
            if (NavigationService == null || Wizard == null)
                return;

            try {
                OnNavigateForward(EventArgs.Empty);
            } catch (InvalidOperationException) {
                return; // we can't navigate any further
            }

            if (!NavigationService.CanGoForward) {
                Wizard.NextPage.ReturnEx += OnPageReturn;
                NavigationService.Navigate(Wizard.NextPage);
            } else {
                NavigationService.GoForward();
            }
        }

        protected virtual void OnFinishButtonClick(object sender, RoutedEventArgs e)
        {
            OnReturnEx(new ReturnEventArgs<WizardResult>(WizardResult.Finished));
        }

        protected virtual void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            OnReturnEx(new ReturnEventArgs<WizardResult>(WizardResult.Canceled));
            OnReturn(null);
        }

        protected virtual void OnNavigateForward(EventArgs e)
        {
            NavigateForward?.Invoke(this, e);
        }

        protected virtual void OnNavigatedBackward(EventArgs e)
        {
            NavigatedBackward?.Invoke(this, e);
        }

        private void OnPageReturn(object sender, ReturnEventArgs<WizardResult> e)
        {
            OnReturnEx(e);
            OnReturn(null);
        }
    }
}
