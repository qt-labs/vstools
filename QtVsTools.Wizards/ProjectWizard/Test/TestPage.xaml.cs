/**************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
**************************************************************************************************/

using System.Windows;
using System.Windows.Controls;

namespace QtVsTools.Wizards.ProjectWizard
{
    using Common;

    public partial class TestPage : WizardPage
    {
        public TestPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void OnClassNameChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFileName();
        }

        private void OnLowerCaseFileNamesClick(object sender, RoutedEventArgs e)
        {
            UpdateFileName();
        }

        private void UpdateFileName()
        {
            Data.LowerCaseFileNames = LowerCaseFileNames.IsChecked.GetValueOrDefault();
            ClassSourceFile.Text =
                (Data.LowerCaseFileNames ? ClassName.Text.ToLower() : ClassName.Text) + ".cpp";
        }
    }
}
