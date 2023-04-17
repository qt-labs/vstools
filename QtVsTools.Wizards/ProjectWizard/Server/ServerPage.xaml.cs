/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Windows;
using System.Windows.Controls;

namespace QtVsTools.Wizards.ProjectWizard
{
    using Common;

    public partial class ServerPage : WizardPage
    {
        public ServerPage()
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
            Data.LowerCaseFileNames = LowerCaseFileNames.IsChecked.GetValueOrDefault();
            var filename = Data.LowerCaseFileNames ? ClassName.Text.ToLower() : ClassName.Text;

            ClassHeaderFile.Text = filename + @".h";
            ClassSourceFile.Text = filename + @".cpp";
            UiFile.Text = filename + @".ui";
        }
    }
}
