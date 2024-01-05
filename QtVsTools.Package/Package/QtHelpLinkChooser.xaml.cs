/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using QtVsTools.VisualStudio;

namespace QtVsTools
{
    using static Core.Common.Utils;

    partial class QtHelpLinkChooser : DialogWindow
    {
        public QtHelpLinkChooser()
        {
            InitializeComponent();

            DataContext = this;
            Loaded += OnLoaded;
        }

        public string Link { get; set; }
        public string SearchText { get; set; }

        public string Keyword { private get; set; }
        public Dictionary<string, string> Links { private get; set; }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var view = CollectionViewSource.GetDefaultView(linkListBox.ItemsSource);
            view.Filter = obj =>
            {
                if (string.IsNullOrEmpty(SearchText))
                    return true;

                var item = (KeyValuePair<string, string>)obj;
                return item.Key.IndexOf(SearchText, IgnoreCase) >= 0;
            };
            linkListBox.SelectedIndex = 0;

            var factory = VsServiceProvider
                .GetService<SVsWindowSearchHostFactory, IVsWindowSearchHostFactory>();
            var host = factory.CreateWindowSearchHost(searchControlHost);

            host.SetupSearch(new ListBoxSearch(linkListBox, value => SearchText = value));
            host.Activate(); // set focus
        }

        private void OnListBoxItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                OnShowButton_Click(sender, null);
        }

        private void OnShowButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnLinkListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems != null && (e.AddedItems == null || e.AddedItems.Count == 0)) {
                if (linkListBox.Items.Count != 0)
                    linkListBox.SelectedIndex = 0;
            }
        }
    }
}
