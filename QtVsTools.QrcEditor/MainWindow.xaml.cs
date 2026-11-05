// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace QtVsTools.QrcEditor
{
    using Qrc;

    public partial class MainWindow
    {
        private const string AppName = "Qt Resource Editor";

        private QrcDocument document;
        private bool updatingProperties;
        private TextBox editingTextBox;
        private string lastFileDirectory;

        // Edits of a text box are merged into one undo step until the text box loses the focus
        // or the selection changes; see QrcDocument.SetAlias() and friends.
        private int mergeId;

        public MainWindow()
        {
            InitializeComponent();
            UpdateTitle();
        }

        #region Embedding

        private const double OffScreen = -32000;

        private bool startedOffScreen;
        private string pendingError;

        /// <summary>
        /// Shows the window outside the visible screen area, without activating it and
        /// without a taskbar button, until the extension embeds it.
        /// </summary>
        public void ShowOffScreen()
        {
            startedOffScreen = true;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = OffScreen;
            Top = OffScreen;
            ShowActivated = false;
            ShowInTaskbar = false;
            SourceInitialized += (_, _) =>
                HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(WndProc);
            Show();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam,
            ref bool handled)
        {
            const int WM_WINDOWPOSCHANGED = 0x0047;

            // When the window becomes a visible top-level window again, i.e. the extension
            // detached it from Visual Studio, give it back its taskbar button. Changing
            // ShowInTaskbar re-parents the window, so it must never happen while embedded.
            if (msg == WM_WINDOWPOSCHANGED && !ShowInTaskbar && IsDetached(hwnd)) {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!ShowInTaskbar && IsDetached(hwnd))
                        ShowInTaskbar = true;
                }));
            }
            // An error from before the window was embedded or shown can be displayed now.
            if (msg == WM_WINDOWPOSCHANGED && pendingError != null && !IsOutOfSight(hwnd)) {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var error = pendingError;
                    pendingError = null;
                    if (error != null)
                        ShowError(error);
                }));
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// True while a window started off screen is neither embedded nor shown on screen.
        /// </summary>
        private bool IsOutOfSight(IntPtr hwnd)
        {
            const uint GA_PARENT = 1;
            return startedOffScreen && GetAncestor(hwnd, GA_PARENT) == GetDesktopWindow()
                && !IsDetached(hwnd);
        }

        /// <summary>
        /// True if the window is a visible top-level window on screen. The extension embeds
        /// the window without setting WS_CHILD, so GetParent() would return null even while
        /// embedded; GetAncestor(GA_PARENT) returns the actual parent window.
        /// </summary>
        private static bool IsDetached(IntPtr hwnd)
        {
            const uint GA_PARENT = 1;
            return GetAncestor(hwnd, GA_PARENT) == GetDesktopWindow()
                && IsWindowVisible(hwnd) && GetWindowRect(hwnd, out var rect)
                && rect.Left > OffScreen / 2;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        #endregion

        private object SelectedItem => Tree.SelectedItem;
        private QrcPrefix SelectedPrefix => SelectedItem as QrcPrefix
            ?? (SelectedItem as QrcFile)?.Prefix;

        #region Document

        public void OpenFile(string filePath)
        {
            if (!ConfirmDiscardChanges())
                return;

            QrcDocument loaded;
            try {
                loaded = QrcDocument.Load(filePath);
            } catch (QrcException exception) {
                ShowStatus($"Unable to open {filePath}.");
                ShowError($"Unable to open {filePath}:\n\n{exception.Message}");
                return;
            }

            SetDocument(loaded);
            ShowStatus($"{loaded.FilePath} opened");

            if (loaded.Prefixes.FirstOrDefault() is { } first)
                SelectItem(first);
            Tree.Focus();
        }

        private void SetDocument(QrcDocument newDocument)
        {
            if (document != null)
                document.UndoStack.Changed -= UndoStack_Changed;

            document = newDocument;
            lastFileDirectory = null;
            DataContext = document;

            if (document != null)
                document.UndoStack.Changed += UndoStack_Changed;

            UpdateTitle();
            UpdateProperties();
            CommandManager.InvalidateRequerySuggested();
        }

        private void UndoStack_Changed(object sender, EventArgs e)
        {
            UpdateTitle();
            UpdateProperties();
            CommandManager.InvalidateRequerySuggested();
        }

        private void UpdateTitle()
        {
            // The extension finds the window of the editor by this title prefix.
            Title = document == null ? AppName
                : $"{AppName} - {Path.GetFileName(document.FilePath)}{(document.IsDirty ? " *" : "")}";
            AddButton.IsEnabled = document != null;
        }

        private bool Save()
        {
            try {
                document.Save();
                ShowStatus($"{document.FilePath} written");
                return true;
            } catch (QrcException exception) {
                ShowStatus($"Unable to write {document.FilePath}.");
                ShowError($"Unable to write {document.FilePath}:\n\n{exception.Message}");
                return false;
            }
        }

        private bool SaveAs()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save Resource File",
                Filter = "Resource files (*.qrc)|*.qrc",
                InitialDirectory = document.Directory,
                FileName = Path.GetFileName(document.FilePath)
            };
            if (dialog.ShowDialog(this) != true)
                return false;

            try {
                document.SaveAs(dialog.FileName);
            } catch (QrcException exception) {
                ShowError($"Unable to write {dialog.FileName}:\n\n{exception.Message}");
                return false;
            }
            ShowStatus($"{document.FilePath} written");
            UpdateTitle();
            UpdateProperties();
            return true;
        }

        /// <summary>
        /// Asks to save unsaved changes. Returns false if the user cancels.
        /// </summary>
        private bool ConfirmDiscardChanges()
        {
            if (document is not { IsDirty: true })
                return true;

            var result = MessageBox.Show(this,
                "The .qrc file has been modified.\n\nDo you want to save the changes?",
                AppName, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning,
                MessageBoxResult.Yes);
            return result switch
            {
                MessageBoxResult.Yes => Save(),
                MessageBoxResult.No => true,
                _ => false
            };
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = !ConfirmDiscardChanges();
        }

        #endregion

        #region Commands

        private void HasDocument_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = document != null;
        }

        private void HasSelection_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = document != null && SelectedItem != null;
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose Resource File",
                Filter = "Resource files (*.qrc)|*.qrc"
            };
            if (document != null)
                dialog.InitialDirectory = document.Directory;
            if (dialog.ShowDialog(this) == true)
                OpenFile(dialog.FileName);
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e) => Save();

        private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e) => SaveAs();

        private void Exit_Executed(object sender, ExecutedRoutedEventArgs e) => Close();

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // A text box handles Ctrl+Z/Ctrl+Y itself and stops the key there, even with its
            // own undo disabled. Route these keys to the document's undo stack first.
            foreach (var command in new[] { EditorCommands.Undo, EditorCommands.Redo }) {
                if (!command.InputGestures.OfType<KeyGesture>().Any(x => x.Matches(this, e)))
                    continue;
                if (command.CanExecute(null, this))
                    command.Execute(null, this);
                e.Handled = true;
                return;
            }
        }

        private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = document?.UndoStack.CanUndo == true;
        }

        private void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mergeId++;
            document.UndoStack.Undo();
        }

        private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = document?.UndoStack.CanRedo == true;
        }

        private void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mergeId++;
            document.UndoStack.Redo();
        }

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            // The toolbar is small enough to never overflow; hide the overflow button and the
            // edge the default template draws for it.
            if (sender is not ToolBar toolBar)
                return;
            if (toolBar.Template.FindName("OverflowGrid", toolBar) is FrameworkElement overflow)
                overflow.Visibility = Visibility.Collapsed;
            if (toolBar.Template.FindName("MainPanelBorder", toolBar) is FrameworkElement border)
                border.Margin = new Thickness(0);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (AddButton.ContextMenu is not {} menu)
                return;
            menu.PlacementTarget = AddButton;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void AddPrefix_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var prefix = document.AddPrefix();
            SelectItem(prefix);
            PrefixText.Focus();
            PrefixText.SelectAll();
        }

        private void AddFiles_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open File",
                Filter = "All files (*.*)|*.*",
                Multiselect = true,
                InitialDirectory = lastFileDirectory ?? document.Directory
            };
            if (dialog.ShowDialog(this) != true)
                return;

            lastFileDirectory = Path.GetDirectoryName(dialog.FileNames.Last());
            AddFiles(SelectedPrefix, dialog.FileNames);
        }

        private void AddFiles(QrcPrefix prefix, IEnumerable<string> fileNames)
        {
            var files = ResolveLocationIssues(fileNames.ToList());
            if (files.Count == 0)
                return;

            var added = document.AddFiles(prefix, files);
            if (added.Count > 0)
                SelectItem(added.Last());
            else
                ShowStatus("The files are already part of the prefix.");
        }

        private void Remove_Executed(object sender, ExecutedRoutedEventArgs e) => RemoveSelected();

        private void RemoveSelected()
        {
            // Select the item at the same place afterward: the next sibling, otherwise the
            // previous one, otherwise the parent prefix.
            object next = null;
            switch (SelectedItem) {
            case QrcFile file:
                var prefix = file.Prefix;
                var fileIndex = prefix.Files.IndexOf(file);
                document.RemoveFile(file);
                next = prefix.Files.Count > 0
                    ? prefix.Files[Math.Min(fileIndex, prefix.Files.Count - 1)] : prefix;
                break;
            case QrcPrefix selectedPrefix:
                var prefixIndex = document.Prefixes.IndexOf(selectedPrefix);
                document.RemovePrefix(selectedPrefix);
                if (document.Prefixes.Count > 0)
                    next = document.Prefixes[Math.Min(prefixIndex, document.Prefixes.Count - 1)];
                break;
            }
            if (next != null)
                SelectItem(next);
        }

        private void About_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show(this, $"{AppName}\n\nCopyright (C) 2026 The Qt Company Ltd.",
                $"About {AppName}", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Tree

        private void Tree_SelectedItemChanged(object sender,
            RoutedPropertyChangedEventArgs<object> e)
        {
            mergeId++;
            UpdateProperties();
        }

        private void Tree_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete || SelectedItem == null)
                return;
            RemoveSelected();
            e.Handled = true;
        }

        private void Tree_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = document != null && document.Prefixes.Count > 0
                && e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Tree_Drop(object sender, DragEventArgs e)
        {
            if (document == null || e.Data.GetData(DataFormats.FileDrop) is not string[] paths)
                return;

            // Add to the prefix under the mouse, else to the selected or the first prefix.
            var target = (e.OriginalSource as FrameworkElement)?.DataContext;
            var prefix = target as QrcPrefix ?? (target as QrcFile)?.Prefix
                ?? SelectedPrefix ?? document.Prefixes.FirstOrDefault();
            if (prefix == null)
                return;

            var files = paths.Where(File.Exists).ToList();
            if (files.Count < paths.Length)
                ShowStatus("Folders cannot be added; only files were added.");
            if (files.Count > 0) {
                // Adding can open dialogs. The drop source, e.g. Explorer, waits until the
                // drop returns, so add the files afterwards.
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Activate();
                    AddFiles(prefix, files);
                }));
            }
            e.Handled = true;
        }

        private void SelectItem(object item)
        {
            Tree.UpdateLayout();
            TreeViewItem container;
            if (item is QrcFile file) {
                if (Tree.ItemContainerGenerator.ContainerFromItem(file.Prefix)
                    is not TreeViewItem prefixItem) {
                    return;
                }
                prefixItem.IsExpanded = true;
                prefixItem.UpdateLayout();
                container = prefixItem.ItemContainerGenerator.ContainerFromItem(file)
                    as TreeViewItem;
            } else {
                container = Tree.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
            }
            if (container == null)
                return;
            container.IsSelected = true;
            container.BringIntoView();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Shows the properties of the selected item in the text boxes.
        /// </summary>
        private void UpdateProperties()
        {
            updatingProperties = true;
            try {
                var file = SelectedItem as QrcFile;
                var prefix = SelectedPrefix;
                var isPrefix = SelectedItem is QrcPrefix;

                AliasText.IsEnabled = file != null;
                SetText(AliasText, file?.Alias);

                PrefixText.IsEnabled = isPrefix;
                SetText(PrefixText, prefix?.Name);
                PrefixText.ClearValue(BorderBrushProperty);
                PrefixText.ToolTip = null;

                LanguageText.IsEnabled = isPrefix;
                SetText(LanguageText, prefix?.Language);
                LanguageText.ClearValue(BorderBrushProperty);
                LanguageText.ToolTip = null;

                PathText.IsEnabled = file != null;
                SetText(PathText, file?.ResourcePath);
            } finally {
                updatingProperties = false;
            }
        }

        private void SetText(TextBox textBox, string text)
        {
            // Do not overwrite the text box the user is typing in: the model may normalize the
            // value (e.g. "i" becomes the prefix "/i"), and replacing the text would move the
            // caret. The normalized value is shown when the text box loses the focus.
            if (textBox == editingTextBox)
                return;
            text ??= string.Empty;
            if (textBox.Text != text)
                textBox.Text = text;
        }

        private void EditFrom(TextBox textBox, Action apply)
        {
            if (updatingProperties)
                return;
            editingTextBox = textBox;
            try {
                apply();
            } finally {
                editingTextBox = null;
            }
        }

        private void AliasText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SelectedItem is QrcFile file)
                EditFrom(AliasText, () => document.SetAlias(file, AliasText.Text, mergeId));
        }

        private void PrefixText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SelectedItem is not QrcPrefix prefix)
                return;

            EditFrom(PrefixText, () =>
            {
                if (document.RenamePrefix(prefix, PrefixText.Text, mergeId)) {
                    PrefixText.ClearValue(BorderBrushProperty);
                    PrefixText.ToolTip = null;
                } else {
                    PrefixText.BorderBrush = (Brush)FindResource("Theme.Error");
                    PrefixText.ToolTip = "A prefix with this name and language already exists.";
                }
            });
        }

        private void LanguageText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SelectedItem is not QrcPrefix prefix)
                return;

            EditFrom(LanguageText, () =>
            {
                if (document.SetLanguage(prefix, LanguageText.Text, mergeId)) {
                    LanguageText.ClearValue(BorderBrushProperty);
                    LanguageText.ToolTip = null;
                } else {
                    LanguageText.BorderBrush = (Brush)FindResource("Theme.Error");
                    LanguageText.ToolTip = "A prefix with this name and language already exists.";
                }
            });
        }

        private void Text_LostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            mergeId++;
            // Show the normalized prefix, or the current value if the input was rejected.
            if (ReferenceEquals(sender, PrefixText) || ReferenceEquals(sender, LanguageText))
                UpdateProperties();
        }

        #endregion

        #region Files outside of the resource directory

        /// <summary>
        /// Files that are not located in the directory of the .qrc file or below cannot be
        /// accessed reliably at runtime. For each such file, ask whether to copy it into the
        /// directory, to skip it or to abort. Returns the files to add.
        /// </summary>
        private List<string> ResolveLocationIssues(List<string> files)
        {
            var result = new List<string>();
            var abort = false;
            foreach (var file in files) {
                if (!document.IsOutsideDirectory(file)) {
                    result.Add(file);
                    continue;
                }
                if (abort)
                    continue;

                var dialog = new LocationIssueDialog(file, canSkip: files.Count > 1)
                {
                    Owner = this
                };
                dialog.ShowDialog();
                switch (dialog.Choice) {
                case LocationIssueChoice.Copy:
                    if (CopyIntoDirectory(file) is { } copy)
                        result.Add(copy);
                    break;
                case LocationIssueChoice.Abort:
                    abort = true;
                    break;
                }
            }
            return result;
        }

        private string CopyIntoDirectory(string file)
        {
            var resources = Path.Combine(document.Directory, "Resources");
            var suggestion = Path.Combine(
                Directory.Exists(resources) ? resources : document.Directory,
                Path.GetFileName(file));

            while (true) {
                var dialog = new SaveFileDialog
                {
                    Title = "Choose Copy Location",
                    Filter = "All files (*.*)|*.*",
                    InitialDirectory = Path.GetDirectoryName(suggestion),
                    FileName = Path.GetFileName(suggestion)
                };
                if (dialog.ShowDialog(this) != true)
                    return null;

                if (document.IsOutsideDirectory(dialog.FileName)) {
                    ShowError("The copy must be located in the directory of the resource file "
                        + "or in one of its subdirectories.");
                    suggestion = dialog.FileName;
                    continue;
                }

                try {
                    Directory.CreateDirectory(Path.GetDirectoryName(dialog.FileName));
                    File.Copy(file, dialog.FileName, overwrite: true);
                    return dialog.FileName;
                } catch (Exception exception) when (exception is IOException
                    or UnauthorizedAccessException) {
                    ShowError($"Could not copy the file to {dialog.FileName}:\n\n"
                        + exception.Message);
                    return null;
                }
            }
        }

        #endregion

        private void ShowStatus(string message)
        {
            StatusText.Text = message;
        }

        internal void ShowUnexpectedError(Exception exception)
        {
            ShowStatus("An unexpected error occurred.");
            ShowError($"An unexpected error occurred:\n\n{exception.Message}");
        }

        private void ShowError(string message)
        {
            // A message box owned by a window off screen would be out of sight as well and
            // block the editor; show it once the window is embedded or on screen.
            if (IsOutOfSight(new WindowInteropHelper(this).Handle)) {
                pendingError = pendingError == null ? message : pendingError + "\n\n" + message;
                return;
            }
            MessageBox.Show(this, message, AppName, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
