// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Windows;

namespace QtVsTools.QrcEditor
{
    public enum LocationIssueChoice
    {
        Copy,
        Skip,
        Abort
    }

    /// <summary>
    /// Asks what to do with a file that is located outside the directory of the .qrc file.
    /// </summary>
    public partial class LocationIssueDialog
    {
        public LocationIssueDialog(string file, bool canSkip)
        {
            InitializeComponent();
            MessageText.Text = $"The file {file} is not in a subdirectory of the resource "
                + "file. You now have the option to copy this file to a valid location.";
            SkipButton.Visibility = canSkip ? Visibility.Visible : Visibility.Collapsed;
            // Closing the dialog skips the file, or aborts if skipping is not offered.
            Choice = canSkip ? LocationIssueChoice.Skip : LocationIssueChoice.Abort;
        }

        public LocationIssueChoice Choice { get; private set; }

        private void Choose(LocationIssueChoice choice)
        {
            Choice = choice;
            DialogResult = true;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
            => Choose(LocationIssueChoice.Copy);

        private void SkipButton_Click(object sender, RoutedEventArgs e)
            => Choose(LocationIssueChoice.Skip);

        private void AbortButton_Click(object sender, RoutedEventArgs e)
            => Choose(LocationIssueChoice.Abort);
    }
}
