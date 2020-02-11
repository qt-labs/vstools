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
using System.Windows;
using System.Windows.Navigation;

namespace QtVsTools.Wizards
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
            if (ReturnEx != null)
                ReturnEx.Invoke(this, e);
        }

        protected virtual void OnPreviousButtonClick(object sender, RoutedEventArgs e)
        {
            if (NavigationService == null || !NavigationService.CanGoBack)
                return;

            NavigationService.GoBack();
            OnNavigatedBackward(new EventArgs());
        }

        protected virtual void OnNextButtonClick(object sender, RoutedEventArgs e)
        {
            if (NavigationService == null || Wizard == null)
                return;

            try {
                OnNavigateForward(new EventArgs());
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
            if (NavigateForward != null)
                NavigateForward.Invoke(this, e);
        }

        protected virtual void OnNavigatedBackward(EventArgs e)
        {
            if (NavigatedBackward != null)
                NavigatedBackward.Invoke(this, e);
        }

        private void OnPageReturn(object sender, ReturnEventArgs<WizardResult> e)
        {
            OnReturnEx(e);
            OnReturn(null);
        }
    }
}
