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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;

namespace QtVsTools.QtMsBuild
{
    public partial class QtModulesPopup : DialogWindow
    {
        public class Module
        {
            public string Name { get; set; }
            public int Id { get; set; }
            public bool IsReadOnly { get; set; }
            public bool IsEnabled => !IsReadOnly;
            public HashSet<string> QT { get; set; }
            public bool IsSelected { get; set; }
            public CheckBox CheckBox { get; set; }
        }

        public QtModulesPopup()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        public void SetModules(IEnumerable<Module> modules)
        {
            PopupListBox.ItemsSource = modules;
        }

        private Module GetCheckBoxModule(CheckBox checkBox)
        {
            return (checkBox?.TemplatedParent as ContentPresenter)?.Content as Module;
        }

        private void CheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox) {
                var module = GetCheckBoxModule(checkBox);
                if (module != null)
                    module.CheckBox = checkBox;
            }
        }

        private void Module_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox check) {
                var module = GetCheckBoxModule(check);
                if (module != null)
                    module.IsSelected = check.IsChecked == true;
            }
        }

        private void PopupListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space) {
                if (PopupListBox.SelectedItem is Module module && module.IsEnabled)
                    module.CheckBox.IsChecked = (module.CheckBox.IsChecked != true);
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
