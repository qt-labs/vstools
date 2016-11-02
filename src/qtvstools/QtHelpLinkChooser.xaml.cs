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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace QtVsTools
{
    public class VsToolsDialogWindow : Microsoft.VisualStudio.PlatformUI.DialogWindow
    {
        // Workaround to avoid referencing Microsoft.VisualStudio.Shell.14.0 in XAML.
    }

    partial class QtHelpLinkChooser : VsToolsDialogWindow
    {
        public QtHelpLinkChooser()
        {
            InitializeComponent();

            DataContext = this;
            Loaded += OnLoaded;
        }

        public string Link { get; private set; }
        public string Keyword { private get; set; }
        public Dictionary<string, string> Links { private get; set; }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(linkListBox.ItemsSource);
            view.Filter = obj =>
            {
                if (string.IsNullOrEmpty(searchBox.Text))
                    return true;

                var item = (KeyValuePair<string, string>) obj;
                return item.Key.IndexOf(searchBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;
            };
            linkListBox.SelectedIndex = 0;
        }

        private void OnSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(linkListBox.ItemsSource).Refresh();
            if (linkListBox.Items.Count == 1 || linkListBox.SelectedItem == null)
                linkListBox.SelectedIndex = 0;
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
