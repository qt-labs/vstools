/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Windows;
using System.Windows.Controls;

namespace QtVsTools.Wizards.ProjectWizard
{
    using Common;

    public partial class LibraryClassPage : WizardPage
    {
        public LibraryClassPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void OnClassNameChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFileNames();
        }

        private void OnLowerCaseFileNamesClick(object sender, RoutedEventArgs e)
        {
            UpdateFileNames();
        }

        private void UpdateFileNames()
        {
            var filename = ClassName.Text;
            if (LowerCaseFileNames.IsChecked.GetValueOrDefault())
                filename = filename.ToLower();

            ClassHeaderFile.Text = filename + @".h";
            ClassSourceFile.Text = filename + @".cpp";
        }
    }
}
