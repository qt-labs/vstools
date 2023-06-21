/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace QtVsTools.Core.Options
{
    using Common;
    using Core;
    using static Utils;
    using static Common.EnumExt;

    public enum BuildHost
    {
        [String("Windows")] Windows,
        [String("Linux SSH")] LinuxSSH,
        [String("Linux WSL")] LinuxWSL
    }

    public partial class QtVersionsTable : UserControl
    {
        LazyFactory Lazy { get; } = new();

        public QtVersionsTable()
        {
            InitializeComponent();
        }

        [Flags] public enum Column
        {
            IsDefault = 0x10,
            VersionName = 0x20,
            Host = 0x40,
            Path = 0x80,
            Compiler = 0x100
        }

        [Flags] public enum State
        {
            Unknown = 0x00,
            Existing = 0x01,
            Removed = 0x02,
            Modified = 0x04
        }

        public class Field
        {
            public string Value { get; set; }
            public Control Control { get; set; }
            public DataGridCell Cell { get; set; }
            private string error;
            public string ValidationError {
                set {
                    UpdateUi = value != error;
                    error = value;
                }
                get => error;
            }
            public bool IsValid => string.IsNullOrEmpty(ValidationError);
            public ToolTip ToolTip
                => IsValid ? null : new ToolTip { Content = ValidationError };
            public int SelectionStart { get; set; }
            public bool UpdateUi { get; private set; }
        }

        public class Row
        {
            LazyFactory Lazy { get; } = new();

            public Dictionary<Column, Field> Fields => Lazy.Get(() =>
                Fields, () => GetValues<Column>()
                    .Select(field => new KeyValuePair<Column, Field>(field, null))
                    .ToDictionary(keyValue => keyValue.Key, keyValue => keyValue.Value));

            public Field FieldDefault => Fields[Column.IsDefault]
                ?? (Fields[Column.IsDefault] = new Field());
            public bool IsDefault
            {
                get => FieldDefault.Value == true.ToString();
                set => FieldDefault.Value = value.ToString();
            }

            public Field FieldVersionName => Fields[Column.VersionName]
                ?? (Fields[Column.VersionName] = new Field());
            public string VersionName
            {
                get => FieldVersionName.Value;
                set => FieldVersionName.Value = value;
            }
            public string InitialVersionName { get; set; }

            public Field FieldHost => Fields[Column.Host]
                ?? (Fields[Column.Host] = new Field());
            public BuildHost Host
            {
                get => FieldHost.Value.Cast(defaultValue: BuildHost.Windows);
                set => FieldHost.Value = value.Cast<string>();
            }

            public Field FieldPath => Fields[Column.Path]
                ?? (Fields[Column.Path] = new Field());
            public string Path
            {
                get => FieldPath.Value;
                set => FieldPath.Value = value;
            }

            public Field FieldCompiler => Fields[Column.Compiler]
                ?? (Fields[Column.Compiler] = new Field());
            public string Compiler
            {
                get => FieldCompiler.Value;
                set => FieldCompiler.Value = value;
            }

            public bool LastRow { get; set; }

            public bool DefaultEnabled => !IsDefault && !LastRow;
            public bool NameEnabled => !LastRow;
            public bool CompilerEnabled => Host != BuildHost.Windows;
            public Visibility RowContentVisibility
                => LastRow ? Visibility.Hidden : Visibility.Visible;
            public Visibility ButtonAddVisibility
                => LastRow ? Visibility.Visible : Visibility.Hidden;
            public Visibility ButtonBrowseVisibility
                => !LastRow && Host == BuildHost.Windows ? Visibility.Visible : Visibility.Hidden;
            public Thickness PathMargin => new(Host == BuildHost.Windows ? 24 : 2, 4, 4, 4);
            public FontWeight FontWeight
                => IsDefault ? FontWeights.Bold : FontWeights.Normal;

            public State State { get; set; } = State.Unknown;
            public bool RowVisible => State != State.Removed;
        }

        Field FocusedField { get; set; }

        List<Row> Rows => Lazy.Get(() => Rows, () => new List<Row>());
        public IEnumerable<Row> Versions => Rows.TakeWhile(item => !item.LastRow);

        public void UpdateVersions(IEnumerable<Row> versions)
        {
            Rows.Clear();
            Rows.AddRange(versions);
            Rows.Add(new Row { LastRow = true });
            DataGrid.ItemsSource = Rows;
            FocusedField = null;
            Validate(true);
            Rows.ForEach(item => item.State = State.Existing);
        }

        public IEnumerable<string> GetErrorMessages()
        {
            Validate(true);
            return Versions
                .Where(v => v.State != State.Removed)
                .SelectMany(v => v.Fields.Values.Select(f => f.ValidationError))
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct();
        }

        void Validate(bool mustRefresh)
        {
            ////////////////////////
            // Validate cell values
            foreach (var version in Versions) {
                if (!version.State.HasFlag(State.Modified))
                    continue;

                //////////////////////
                // Default validation
                if (version.State.HasFlag((State)Column.IsDefault)) {
                    version.FieldDefault.ValidationError = null;
                    if (version is { IsDefault: true, Host: not BuildHost.Windows })
                        version.FieldDefault.ValidationError = "Default version: Host must be Windows";
                    mustRefresh |= version.FieldDefault.UpdateUi;
                }

                ///////////////////
                // Name validation
                if (version.State.HasFlag((State)Column.VersionName)) {
                    version.FieldVersionName.ValidationError = null;
                    if (string.IsNullOrEmpty(version.VersionName)) {
                        version.FieldVersionName.ValidationError = "Name cannot be empty";
                    } else if (Versions.Any(otherVersion => otherVersion != version
                        && otherVersion.VersionName == version.VersionName)) {
                        version.FieldVersionName.ValidationError = "Duplicate version names";
                    }
                    mustRefresh |= version.FieldVersionName.UpdateUi;
                }

                ///////////////////
                // Host validation
                if (version.State.HasFlag((State)Column.Host)) {
                    version.FieldHost.ValidationError = null;
                    if (version is { IsDefault: true, Host: not BuildHost.Windows })
                        version.FieldHost.ValidationError = "Default version: Host must be Windows";
                    mustRefresh |= version.FieldHost.UpdateUi;
                }

                ///////////////////
                // Path validation
                if (version.State.HasFlag((State)Column.Path)) {
                    version.FieldPath.ValidationError = null;
                    if (string.IsNullOrEmpty(version.Path)) {
                        version.FieldPath.ValidationError = "Path cannot be empty";
                    } else if (version.Host == BuildHost.Windows) {
                        string path = NormalizePath(version.Path);
                        if (path == null) {
                            version.FieldPath.ValidationError = "Invalid path format";
                        } else {
                            if (!QMake.Exists(path))
                                version.FieldPath.ValidationError = "Cannot find qmake.exe";
                        }
                    } else if (version.Host != BuildHost.Windows) {
                        if (version.Path.Contains(':'))
                            version.FieldPath.ValidationError = "Invalid character in path";
                    }
                    mustRefresh |= version.FieldPath.UpdateUi;
                }

                ///////////////////////
                // Compiler validation
                if (version.State.HasFlag((State)Column.Compiler)) {
                    version.FieldCompiler.ValidationError = null;
                    if (string.IsNullOrEmpty(version.Compiler)) {
                        version.FieldCompiler.ValidationError = "Compiler cannot be empty";
                    } else if (version.Host != BuildHost.Windows) {
                        if (version.Compiler.Contains(':'))
                            version.FieldCompiler.ValidationError = "Invalid character in name";
                    }
                    mustRefresh |= version.FieldCompiler.UpdateUi;
                }
            }

            //////////////////////////////////////
            // Refresh versions table if required
            if (mustRefresh) {
                // Reset bindings
                foreach (var version in Versions) {
                    foreach (var field in version.Fields.Values) {
                        field.Control = null;
                        field.Cell = null;
                    }
                }
                // Refresh UI
                DataGrid.Items.Refresh();
            }
        }

        static readonly Brush InvalidCellBackground = new DrawingBrush
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0.0, 0.0, 10.0, 10.0),
            ViewportUnits = BrushMappingMode.Absolute,
            Viewbox = new Rect(0.0, 0.0, 10.0, 10.0),
            ViewboxUnits = BrushMappingMode.Absolute,
            Drawing = new DrawingGroup
            {
                Children = new DrawingCollection
                {
                    new GeometryDrawing
                    {
                        Brush = Brushes.Red,
                        Geometry = new RectangleGeometry(new Rect(5, 0, 5, 10))
                    }
                }
            },
            Transform = new RotateTransform
            {
                Angle = -135.0,
                CenterX = 0.5,
                CenterY = 0.5
            },
            Opacity = 0.25
        };

        void Control_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Control control && GetBinding(control) is Row version) {
                if (version.LastRow)
                    return;
                if (string.IsNullOrEmpty(control.Name) || !control.Name.TryCast(out Column column))
                    return;

                var field = version.Fields[column];
                field.Control = control;
                field.Cell = FindContainingCell(control);
                if (field.Cell != null) {
                    field.Cell.Background =
                        field.IsValid ? Brushes.Transparent : InvalidCellBackground;
                }
                if (field == FocusedField)
                    control.Focus();
                if (control is TextBox textBox && field.SelectionStart >= 0)
                    textBox.Select(field.SelectionStart, 0);
            }
        }

        void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && GetBinding(comboBox) is Row version) {
                comboBox.IsEnabled = false;
                var hosts = GetValues<string>(typeof(BuildHost));
                comboBox.ItemsSource = hosts;
                comboBox.Text = version.Host.Cast<string>();
                comboBox.SelectedIndex = (int)version.Host;
                comboBox.IsEnabled = true;
            }
            Control_Loaded(sender, e);
        }

        void Control_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is Control control && GetBinding(control) is Row version) {
                if (string.IsNullOrEmpty(control.Name) || !control.Name.TryCast(out Column column))
                    return;

                var field = version.Fields[column];
                if (field.Control != control)
                    return;
                FocusedField = field;
            }
        }

        void Control_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is Control control && GetBinding(control) is Row version) {
                if (string.IsNullOrEmpty(control.Name) || !control.Name.TryCast(out Column column))
                    return;

                var field = version.Fields[column];
                if (field != FocusedField || field.Control != control)
                    return;
                FocusedField = null;
            }
        }

        void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && GetBinding(textBox) is Row version) {
                if (string.IsNullOrEmpty(textBox.Name) || !textBox.Name.TryCast(out Column column))
                    return;

                var field = version.Fields[column];
                if (field.Control != textBox)
                    return;
                field.SelectionStart = textBox.SelectionStart;
            }
        }

        void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && GetBinding(textBox) is Row version) {
                if (string.IsNullOrEmpty(textBox.Name) || !textBox.Name.TryCast(out Column column))
                    return;

                var field = version.Fields[column];
                if (field == null
                    || field.Control != textBox
                    || field.Value == textBox.Text)
                    return;

                field.SelectionStart = textBox.SelectionStart;
                field.Value = textBox.Text;
                version.State |= State.Modified | (State)column;

                Validate(false);
            }
        }

        void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && GetBinding(comboBox) is Row version) {
                if (!comboBox.IsEnabled || comboBox.SelectedIndex < 0)
                    return;
                if (string.IsNullOrEmpty(comboBox.Name) || !comboBox.Name.TryCast(out Column column))
                    return;

                string comboBoxValue = comboBox.Items[comboBox.SelectedIndex] as string;
                var field = version.Fields[column];
                if (field == null
                    || field.Control != comboBox
                    || field.Value == comboBoxValue)
                    return;

                field.Value = comboBoxValue;
                version.State |= State.Modified | (State)Column.Host;

                bool mustRefresh = false;
                if (version.Host != BuildHost.Windows && version.Compiler == "msvc") {
                    version.Compiler = "g++";
                    version.FieldCompiler.SelectionStart = version.Compiler.Length;
                    version.State |= (State)Column.Compiler;
                    mustRefresh = true;
                } else if (version is { Host: BuildHost.Windows, Compiler: not "msvc" }) {
                    version.Compiler = "msvc";
                    version.FieldCompiler.SelectionStart = version.Compiler.Length;
                    version.State |= (State)Column.Compiler;
                    mustRefresh = true;
                }

                Validate(mustRefresh);
            }
        }

        static void SetDefaultState(ref Row version, bool value)
        {
            version.IsDefault = value;
            version.State |= State.Modified | (State)Column.IsDefault;
        }

        void Default_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && GetBinding(checkBox) is Row version) {
                var defaultVersion =
                    Rows.FirstOrDefault(row => row is { IsDefault: true, RowVisible: true });
                if (defaultVersion != null)
                    SetDefaultState(ref defaultVersion, false);
                SetDefaultState(ref version, true);
                Validate(true);
            }
        }

        void Add_Click(object sender, RoutedEventArgs e)
        {
            var version = new Row
            {
                IsDefault = Versions.All(x => x.State == State.Removed),
                Host = BuildHost.Windows,
                Path = "",
                Compiler = "msvc",
                LastRow = false,
                State = State.Modified | (State)Column.VersionName | (State)Column.Host
                                       | (State)Column.Path | (State)Column.Compiler
            };
            if (version.IsDefault)
                version.State |= (State)Column.IsDefault;
            Rows.Insert(Rows.Count - 1, version);
            FocusedField = version.FieldVersionName;
            Validate(true);
        }

        void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && GetBinding(button) is Row version) {
                version.State = State.Removed;
                if (version.IsDefault) {
                    var first = Versions.FirstOrDefault(x => x.State != State.Removed);
                    if (first != null)
                        SetDefaultState(ref first, true);
                }
                Validate(true);
            }
        }

        static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            try {
                return Path.GetFullPath(new Uri(path).LocalPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .ToUpperInvariant();
            } catch (Exception) {
                return null;
            }
        }

        void Explorer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && GetBinding(button) is Row version) {
                var openFileDialog = new OpenFileDialog
                {
                    AddExtension = false,
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Filter = "qmake|qmake.exe;qmake.bat",
                    Title = "Qt VS Tools - Select qmake.exe"
                };
                if (openFileDialog.ShowDialog() == true) {
                    var qmakePath = openFileDialog.FileName;
                    var qmakeDir = Path.GetDirectoryName(qmakePath);
                    var previousPath = NormalizePath(version.Path);
                    if (Path.GetFileName(qmakeDir ?? "").Equals("bin", IgnoreCase)) {
                        qmakeDir = Path.GetDirectoryName(qmakeDir);
                        version.Path = qmakeDir;
                    } else {
                        version.Path = qmakePath;
                    }

                    if (previousPath != NormalizePath(version.Path))
                        version.State |= State.Modified | (State)Column.Path;

                    if (string.IsNullOrEmpty(version.VersionName)) {
                        version.VersionName = $"{Path.GetFileName(Path.GetDirectoryName(qmakeDir))}"
                          + $"_{Path.GetFileName(qmakeDir)}".Replace(" ", "_");
                        version.State |= State.Modified | (State)Column.VersionName;
                    }

                    Validate(true);
                }
            }
        }

        static object GetBinding(FrameworkElement control)
        {
            if (control?.BindingGroup == null)
                return null;
            return control.BindingGroup.Items.Count == 0 ? null : control.BindingGroup.Items[0];
        }

        static DataGridCell FindContainingCell(DependencyObject control)
        {
            while (control != null) {
                if (control is ContentPresenter {Parent: DataGridCell cell})
                    return cell;
                control = VisualTreeHelper.GetParent(control);
            }
            return null;
        }
    }
}
