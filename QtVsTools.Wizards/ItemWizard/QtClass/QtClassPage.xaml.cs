/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Windows;
using System.Windows.Controls;

namespace QtVsTools.Wizards.ItemWizard
{
    using Common;

    public partial class QtClassPage : WizardPage
    {
        public QtClassPage()
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

            var index = filename.LastIndexOf(@":", System.StringComparison.Ordinal);
            if (index >= 0)
                filename = filename.Substring(index + 1);

            ClassHeaderFile.Text = filename + @".h";
            ClassSourceFile.Text = filename + @".cpp";
        }
    }
}
