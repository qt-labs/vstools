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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.Wizards.ItemWizard
{
    using QtVsTools.VisualStudio;
    using Wizards.Common;

    public partial class TranslationPage : WizardPage
    {
        private string SearchText { get; set; }

        public TranslationPage()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += OnTranslationPageLoaded;
        }

        private void OnTranslationPageLoaded(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var view = CollectionViewSource.GetDefaultView(LanguageListBox.ItemsSource);
            view.Filter = obj =>
            {
                if (string.IsNullOrEmpty(SearchText))
                    return true;

                var item = (KeyValuePair<string, string>)obj;
                return item.Value.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
            };
            LanguageListBox.SelectedIndex = 0;

            var factory = VsServiceProvider
                .GetService<SVsWindowSearchHostFactory, IVsWindowSearchHostFactory>();
            var host = factory.CreateWindowSearchHost(searchControlHost);

            host.SetupSearch(new ListBoxSearch(LanguageListBox, value => SearchText = value));
            host.Activate(); // set focus
        }

        private void OnSearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(LanguageListBox.ItemsSource).Refresh();
            if (LanguageListBox.Items.Count == 1 || LanguageListBox.SelectedItem == null)
                LanguageListBox.SelectedIndex = 0;
        }

        private void OnLanguageBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems != null && (e.AddedItems == null || e.AddedItems.Count == 0)) {
                if (LanguageListBox.Items.Count != 0)
                    LanguageListBox.SelectedIndex = 0;
            }
        }
    }
}
