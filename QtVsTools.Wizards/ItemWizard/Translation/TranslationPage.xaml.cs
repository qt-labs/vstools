/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

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
