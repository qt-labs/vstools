/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Windows;
using System.Windows.Controls;

namespace QtVsTools.Wizards.Common
{
    public partial class GuiPage : WizardPage
    {
        public bool IsClassWizardPage { get; set; } = false;
        public Visibility ClassPageVisible => IsClassWizardPage ? Visibility.Hidden : Visibility.Visible;
        public Visibility QObjectMacro => IsClassWizardPage ? Visibility.Visible : Visibility.Collapsed;

        public GuiPage()
        {
            InitializeComponent();
            DataContext = this;
            UpdateFileNames();
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

            // support namespaces in class name
            var index = filename.LastIndexOf(@":", System.StringComparison.Ordinal);
            if (index >= 0)
                filename = filename.Substring(index + 1);

            ClassHeaderFile.Text = filename + @".h";
            ClassSourceFile.Text = filename + @".cpp";
            UiFile.Text = filename + @".ui";
            QrcFile.Text = filename + @".qrc";
        }
    }
}
