// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Windows.Input;

namespace QtVsTools.QrcEditor
{
    /// <summary>
    /// Commands of the editor that have no equivalent in <see cref="ApplicationCommands"/>.
    /// </summary>
    /// <remarks>
    /// Undo and redo are separate from <see cref="ApplicationCommands.Undo"/>, which the text
    /// boxes would otherwise handle with their own, character based undo history.
    /// </remarks>
    public static class EditorCommands
    {
        public static RoutedUICommand Undo { get; } = new("_Undo", nameof(Undo),
            typeof(EditorCommands), new InputGestureCollection {
                new KeyGesture(Key.Z, ModifierKeys.Control)
            });

        public static RoutedUICommand Redo { get; } = new("_Redo", nameof(Redo),
            typeof(EditorCommands), new InputGestureCollection {
                new KeyGesture(Key.Y, ModifierKeys.Control),
                new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift)
            });

        public static RoutedUICommand AddFiles { get; } = new("Add _Files...", nameof(AddFiles),
            typeof(EditorCommands));

        public static RoutedUICommand AddPrefix { get; } = new("Add _Prefix", nameof(AddPrefix),
            typeof(EditorCommands));

        // The Delete key removes items only while the tree has the focus; see MainWindow.
        public static RoutedUICommand Remove { get; } = new("_Remove", nameof(Remove),
            typeof(EditorCommands));

        public static RoutedUICommand Exit { get; } = new("E_xit", nameof(Exit),
            typeof(EditorCommands));

        public static RoutedUICommand About { get; } = new("_About", nameof(About),
            typeof(EditorCommands));
    }
}
