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

using EnvDTE;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;

namespace QtProjectWizard
{
    public enum ClassKind
    {
        Gui,
        Core
    }

    public class Class
    {
        public string ClassName
        {
            get; set;
        }

        public string DefaultName
        {
            get; set;
        }

        public string Type
        {
            get; set;
        }

        public string Description
        {
            get; set;
        }

        public string ImageSource
        {
            get; set;
        }

        public ClassKind Kind
        {
            get; set;
        }
    }

    public class SortComboBoxItem : ComboBoxItem
    {
        public ListSortDirection? SortDirection
        {
            get; set;
        }
    }

    public partial class AddClassPage : WizardPage
    {
        public AddClassPage()
        {
            InitializeComponent();
            DataContext = this;

            Classes = new List<Class> {
                new Class {
                    ClassName = @"Qt Class",
                    DefaultName = @"QtClass",
                    Type = @"Visual C++",
                    Description = @"Creates a C++ header and source file for a new class that you "
                        + @"can add to a Qt Project.",
                    ImageSource = @"Resources/Qt-logo-small.png",
                    Kind = ClassKind.Core
                },
                new Class {
                    ClassName = @"Qt GUI Class",
                    DefaultName = @"QtGuiClass",
                    Type = @"Visual C++",
                    Description = @"Creates a new empty Qt Designer form along with a matching "
                        + @"C++ header and source file for implementation purposes. You can add "
                        + @"the form and class to an existing Qt Project.",
                    ImageSource = @"Resources/Qt-logo-small.png",
                    Kind = ClassKind.Gui
                }
            };
            ClassView.SelectedIndex = 0;
            ClassView.ItemTemplate = ClassView.FindResource("MediumTemplate") as DataTemplate;
            VisualCppView.Focus();
        }

        public Class Class
        {
            get; private set;
        }

        public string Location
        {
            get; set;
        }

        public List<Class> Classes
        {
            get; private set;
        }

        private void OnExpanded(object sender, RoutedEventArgs e)
        {
            if (ClassView == null)
                return;
            ClassView.ItemsSource = Classes;
            ClassView.ItemTemplate = ClassView.FindResource(MediumIcons.IsChecked
                .GetValueOrDefault() ? "MediumTemplate" : "SmallTemplate") as DataTemplate;
            ClassView.ItemContainerStyle = FindResource("ListViewItemEnabledStyle") as Style;
            ClassView.SelectedIndex = 0;

            AddButton.IsEnabled = true;
            BrowseButton.IsEnabled = true;
            LocationComboBox.IsEnabled = true;
            DefaultNameTextBox.IsEnabled = true;
        }

        private void OnCollapsed(object sender, RoutedEventArgs e)
        {
            if (ClassView == null)
                return;

            ClassView.ItemsSource = new List<Class> {
                new Class()
            };
            ClassView.SelectedIndex = 0;
            ClassView.ItemTemplate = ClassView.FindResource("EmptyTemplate") as DataTemplate;
            ClassView.ItemContainerStyle = FindResource("ListViewItemDisabledStyle") as Style;
            ClassView.SelectedIndex = -1;

            AddButton.IsEnabled = false;
            BrowseButton.IsEnabled = false;
            LocationComboBox.IsEnabled = false;
            DefaultNameTextBox.IsEnabled = false;
        }

        private void OnComboBoxLoaded(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null)
                return;

            comboBox.ItemsSource = new List<SortComboBoxItem> {
                new SortComboBoxItem {
                    Content = "Default",
                    SortDirection = null
                },
                new SortComboBoxItem {
                    Content = "Name Ascending",
                    SortDirection = ListSortDirection.Ascending
                },
                new SortComboBoxItem {
                    Content = "Name Descending",
                    SortDirection = ListSortDirection.Descending
                }
            };
            comboBox.SelectedIndex = 0;
        }

        private void OnBrowseButtonClick(object sender, RoutedEventArgs e)
        {
            var block = System.IntPtr.Zero;
            try {
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                var serviceProvider = new ServiceProvider(dte as IServiceProvider);
                var iVsUIShell = serviceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;

                System.IntPtr owner;
                iVsUIShell.GetDialogOwnerHwnd(out owner);

                var browseInfo = new VSBROWSEINFOW[1];
                browseInfo[0].lStructSize = (uint) Marshal.SizeOf(typeof(VSBROWSEINFOW));
                browseInfo[0].pwzInitialDir = Location;
                browseInfo[0].pwzDlgTitle = @"Location";
                browseInfo[0].hwndOwner = owner;
                browseInfo[0].nMaxDirName = 260;
                block = Marshal.AllocCoTaskMem(520);
                browseInfo[0].pwzDirName = block;

                var result = iVsUIShell.GetDirectoryViaBrowseDlg(browseInfo);
                if (result == Microsoft.VisualStudio.VSConstants.S_OK) {
                    Location = Marshal.PtrToStringAuto(browseInfo[0].pwzDirName);
                    LocationComboBox.Text = Marshal.PtrToStringAuto(browseInfo[0].pwzDirName);
                }
            } finally {
                if (block != System.IntPtr.Zero)
                    Marshal.FreeCoTaskMem(block);
            }
        }

        private void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null)
                return;

            var item = comboBox.SelectedItem as SortComboBoxItem;
            if (item == null || ClassView == null)
                return;

            var view = CollectionViewSource.GetDefaultView(ClassView.ItemsSource) as CollectionView;
            view.SortDescriptions.Clear();
            if (item.SortDirection == null)
                return;
            view.SortDescriptions.Add(new SortDescription(@"ClassName", item.SortDirection
                .GetValueOrDefault()));
        }

        private void OnSmallIconsChecked(object sender, RoutedEventArgs e)
        {
            MediumIcons.IsChecked = false;
            if (ClassView != null)
                ClassView.ItemTemplate = ClassView.FindResource("SmallTemplate") as DataTemplate;
        }

        private void OnMediumIconsChecked(object sender, RoutedEventArgs e)
        {
            SmallIcons.IsChecked = false;
            if (ClassView != null)
                ClassView.ItemTemplate = ClassView.FindResource("MediumTemplate") as DataTemplate;
        }

        private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView == null)
                return;

            var item = listView.SelectedItem as Class;
            if (item == null)
                return;

            Class = item;
            Type.Text = item.Type;
            Description.Text = item.Description;
            DefaultNameTextBox.Text = item.DefaultName;
        }

        private void OnDefaultNameTextChanged(object sender, TextChangedEventArgs e)
        {
            var item = ClassView.SelectedItem as Class;
            if (item == null)
                return;
            item.DefaultName = DefaultNameTextBox.Text;
        }

        private void OnListViewItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            var item = (sender as ListViewItem).DataContext as Class;
            if (item == null)
                return;
            item.DefaultName = DefaultNameTextBox.Text;

            OnReturnEx(new ReturnEventArgs<WizardResult>(WizardResult.Finished));
        }
    }
}
