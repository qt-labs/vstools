/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;

namespace QtVsTools.Core.MsBuild
{
    public partial class QtModulesPopup : DialogWindow
    {
        public class Module
        {
            public string Name { get; set; }
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

        private static Module GetCheckBoxModule(CheckBox checkBox)
        {
            return (checkBox?.TemplatedParent as ContentPresenter)?.Content as Module;
        }

        private void CheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox)
                return;
            if (GetCheckBoxModule(checkBox) is {} module)
                module.CheckBox = checkBox;
        }

        private void Module_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox check)
                return;
            if (GetCheckBoxModule(check) is {} module)
                module.IsSelected = check.IsChecked == true;
        }

        private void PopupListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is not Key.Enter and not Key.Space)
                return;
            if (PopupListBox.SelectedItem is Module {IsEnabled: true} module)
                module.CheckBox.IsChecked = module.CheckBox.IsChecked != true;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
