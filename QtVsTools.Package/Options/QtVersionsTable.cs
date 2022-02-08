/****************************************************************************
**
** Copyright (C) 2020 The Qt Company Ltd.
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using QtVsTools.Common;

namespace QtVsTools.Options
{
    using static EnumExt;

    public enum BuildHost
    {
        [String("Windows")] Windows,
        [String("Linux SSH")] LinuxSSH,
        [String("Linux WSL")] LinuxWSL,
    }

    public partial class QtVersionsTable : UserControl
    {
        public QtVersionsTable()
        {
            InitializeComponent();
        }

        public class Field
        {
            public string Value { get; set; }
            public Control Control { get; set; }
            public DataGridCell Cell { get; set; }
            public string ValidationError { get; set; }
            public bool IsValid => string.IsNullOrEmpty(ValidationError);
            public ToolTip ToolTip
                => IsValid ? null : new ToolTip() { Content = ValidationError };
            public int SelectionStart { get; set; }
        }

        public class Row
        {
            public enum FieldNames { IsDefault, VersionName, Host, Path, Compiler }

            private Dictionary<FieldNames, Field> _Fields;
            public Dictionary<FieldNames, Field> Fields => _Fields
                ?? (_Fields = GetValues<FieldNames>()
                    .Select(field => new KeyValuePair<FieldNames, Field>(field, null))
                    .ToDictionary(keyValue => keyValue.Key, keyValue => keyValue.Value));

            public Field FieldDefault => Fields[FieldNames.IsDefault]
                ?? (Fields[FieldNames.IsDefault] = new Field());
            public bool IsDefault
            {
                get => (FieldDefault.Value == true.ToString());
                set => FieldDefault.Value = value.ToString();
            }

            public Field FieldVersionName => Fields[FieldNames.VersionName]
                ?? (Fields[FieldNames.VersionName] = new Field());
            public string VersionName
            {
                get => FieldVersionName.Value;
                set => FieldVersionName.Value = value;
            }

            public Field FieldHost => Fields[FieldNames.Host]
                ?? (Fields[FieldNames.Host] = new Field());
            public BuildHost Host
            {
                get => FieldHost.Value.Cast(defaultValue: BuildHost.Windows);
                set => FieldHost.Value = value.Cast<string>();
            }

            public Field FieldPath => Fields[FieldNames.Path]
                ?? (Fields[FieldNames.Path] = new Field());
            public string Path
            {
                get => FieldPath.Value;
                set => FieldPath.Value = value;
            }

            public Field FieldCompiler => Fields[FieldNames.Compiler]
                ?? (Fields[FieldNames.Compiler] = new Field());
            public string Compiler
            {
                get => FieldCompiler.Value;
                set => FieldCompiler.Value = value;
            }

            public bool LastRow { get; set; }

            public bool DefaultEnabled => !IsDefault && !LastRow;
            public bool NameEnabled => !LastRow;
            public bool CompilerEnabled => (Host != BuildHost.Windows);
            public Visibility RowVisibility
                => LastRow ? Visibility.Hidden : Visibility.Visible;
            public Visibility ButtonAddVisibility
                => LastRow ? Visibility.Visible : Visibility.Hidden;
            public Visibility ButtonBrowseVisibility
                => (!LastRow && Host == BuildHost.Windows) ? Visibility.Visible : Visibility.Hidden;
            public Thickness PathMargin
                => new Thickness(((Host == BuildHost.Windows) ? 22 : 2), 0, 2, 0);
            public FontWeight FontWeight
                => IsDefault ? FontWeights.Bold : FontWeights.Normal;

            private static ImageSource _ExplorerIcon;
            public static ImageSource ExplorerIcon => _ExplorerIcon
                ?? (_ExplorerIcon = GetExplorerIcon());
        }

        public bool IsValid { get; private set; }

        Field FocusedField { get; set; }

        List<Row> _Rows;
        List<Row> Rows => _Rows ?? (_Rows = new List<Row>());
        public IEnumerable<Row> Versions => Rows.TakeWhile(item => !item.LastRow);

        public void UpdateVersions(IEnumerable<Row> versions)
        {
            Rows.Clear();
            Rows.AddRange(versions);
            Rows.Add(new Row { LastRow = true });
            DataGrid.ItemsSource = Rows;
            IsValid = true;
            FocusedField = null;
            Validate(true);
        }

        public IEnumerable<string> GetErrorMessages()
        {
            Validate(true);
            return Versions
                .SelectMany(v => v.Fields.Values.Select(f => f.ValidationError))
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct();
        }

        void Validate(bool mustRefresh)
        {
            /////////////////////////
            // Automatic cell values
            foreach (var version in Versions) {
                if (version.Host != BuildHost.Windows && version.Compiler == "msvc") {
                    version.Compiler = "g++";
                    version.FieldCompiler.SelectionStart = version.Compiler.Length;
                    mustRefresh = true;
                } else if (version.Host == BuildHost.Windows && version.Compiler != "msvc") {
                    version.Compiler = "msvc";
                    version.FieldCompiler.SelectionStart = version.Compiler.Length;
                    mustRefresh = true;
                }
            }

            ////////////////////////
            // Validate cell values
            string previousValidation;
            bool wasValid = IsValid;
            IsValid = true;
            foreach (var version in Versions) {

                //////////////////////
                // Default validation
                previousValidation = version.FieldDefault.ValidationError;
                version.FieldDefault.ValidationError = null;
                if (version.IsDefault && version.Host != BuildHost.Windows) {
                    version.FieldDefault.ValidationError = "Default version: host must be Windows";
                    IsValid = false;
                }
                if (previousValidation != version.FieldDefault.ValidationError)
                    mustRefresh = true;

                ///////////////////
                // Name validation
                previousValidation = version.FieldVersionName.ValidationError;
                version.FieldVersionName.ValidationError = null;
                if (string.IsNullOrEmpty(version.VersionName)) {
                    version.FieldVersionName.ValidationError = "Name cannot be empty";
                    IsValid = false;
                } else if (Versions
                    .Where(otherVersion => otherVersion != version
                        && otherVersion.VersionName == version.VersionName)
                    .Any()) {
                    version.FieldVersionName.ValidationError = "Duplicate version names";
                    IsValid = false;
                }
                if (previousValidation != version.FieldVersionName.ValidationError)
                    mustRefresh = true;

                ///////////////////
                // Host validation
                previousValidation = version.FieldHost.ValidationError;
                version.FieldHost.ValidationError = null;
                if (version.IsDefault && version.Host != BuildHost.Windows) {
                    version.FieldHost.ValidationError = "Default version: host must be Windows";
                    IsValid = false;
                }
                if (previousValidation != version.FieldHost.ValidationError)
                    mustRefresh = true;

                ///////////////////
                // Path validation
                previousValidation = version.FieldPath.ValidationError;
                version.FieldPath.ValidationError = null;
                if (string.IsNullOrEmpty(version.Path)) {
                    version.FieldPath.ValidationError = "Path cannot be empty";
                    IsValid = false;
                } else if (version.Host == BuildHost.Windows && !Directory.Exists(version.Path)) {
                    version.FieldPath.ValidationError = "Path does not exist";
                    IsValid = false;
                }
                if (previousValidation != version.FieldPath.ValidationError)
                    mustRefresh = true;

                ///////////////////////
                // Compiler validation
                previousValidation = version.FieldCompiler.ValidationError;
                version.FieldCompiler.ValidationError = null;
                if (string.IsNullOrEmpty(version.Compiler)) {
                    version.FieldCompiler.ValidationError = "Compiler cannot be empty";
                    IsValid = false;
                }
                if (previousValidation != version.FieldCompiler.ValidationError)
                    mustRefresh = true;
            }

            //////////////////////////////////////
            // Refresh versions table if required
            mustRefresh |= (wasValid != IsValid);
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

                Row.FieldNames field;
                if (string.IsNullOrEmpty(control.Name) || !control.Name.TryCast(out field))
                    return;

                var fieldBinding = version.Fields[field];
                fieldBinding.Control = control;
                fieldBinding.Cell = FindContainingCell(control);
                if (fieldBinding.Cell != null) {
                    fieldBinding.Cell.Background =
                        fieldBinding.IsValid ? Brushes.Transparent : InvalidCellBackground;
                }
                if (fieldBinding == FocusedField)
                    control.Focus();
                if (control is TextBox textBox && fieldBinding.SelectionStart >= 0)
                    textBox.Select(fieldBinding.SelectionStart, 0);
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
                Row.FieldNames field;
                if (string.IsNullOrEmpty(control.Name) || !control.Name.TryCast(out field))
                    return;
                var fieldBinding = version.Fields[field];
                if (fieldBinding.Control != control)
                    return;
                FocusedField = fieldBinding;
            }
        }

        void Control_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is Control control && GetBinding(control) is Row version) {
                Row.FieldNames field;
                if (string.IsNullOrEmpty(control.Name) || !control.Name.TryCast(out field))
                    return;
                var fieldBinding = version.Fields[field];
                if (fieldBinding != FocusedField || fieldBinding.Control != control)
                    return;
                FocusedField = null;
            }
        }

        void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && GetBinding(textBox) is Row version) {
                Row.FieldNames field;
                if (string.IsNullOrEmpty(textBox.Name) || !textBox.Name.TryCast(out field))
                    return;
                var fieldBinding = version.Fields[field];
                if (fieldBinding.Control != textBox)
                    return;
                fieldBinding.SelectionStart = textBox.SelectionStart;
            }
        }

        void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && GetBinding(textBox) is Row version) {
                Row.FieldNames field;
                if (string.IsNullOrEmpty(textBox.Name) || !textBox.Name.TryCast(out field))
                    return;
                var fieldBinding = version.Fields[field];
                if (fieldBinding == null
                    || fieldBinding.Control != textBox
                    || fieldBinding.Value == textBox.Text)
                    return;
                fieldBinding.SelectionStart = textBox.SelectionStart;
                fieldBinding.Value = textBox.Text;
                Validate(false);
            }
        }

        void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && GetBinding(comboBox) is Row version) {
                if (!comboBox.IsEnabled || comboBox.SelectedIndex < 0)
                    return;
                string comboBoxValue = comboBox.Items[comboBox.SelectedIndex] as string;
                string controlName = comboBox.Name;
                Row.FieldNames field;
                if (string.IsNullOrEmpty(controlName) || !controlName.TryCast(out field))
                    return;
                var fieldBinding = version.Fields[field];
                if (fieldBinding == null
                    || fieldBinding.Control != comboBox
                    || fieldBinding.Value == comboBoxValue)
                    return;
                fieldBinding.Value = comboBoxValue;
                Validate(false);
            }
        }

        void Default_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && GetBinding(checkBox) is Row version) {
                var defaultVersion = Rows.Where(row => row.IsDefault).FirstOrDefault();
                if (defaultVersion != null)
                    defaultVersion.IsDefault = false;
                version.IsDefault = true;
                Validate(true);
            }
        }

        void Add_Click(object sender, RoutedEventArgs e)
        {
            var version = new Row()
            {
                IsDefault = !Versions.Any(),
                Host = BuildHost.Windows,
                Path = "",
                Compiler = "msvc",
                LastRow = false
            };
            Rows.Insert(Rows.Count - 1, version);
            FocusedField = version.FieldVersionName;
            Validate(true);
        }

        void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && GetBinding(button) is Row version) {
                Rows.Remove(version);
                if (version.IsDefault && Versions.Any())
                    Versions.First().IsDefault = true;
                Validate(true);
            }
        }

        void Explorer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && GetBinding(button) is Row version) {
                var openFileDialog = new OpenFileDialog()
                {
                    AddExtension = false,
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Filter = "qmake Executable|qmake.exe",
                    Title = "Qt VS Tools - Select qmake.exe"
                };
                if (openFileDialog.ShowDialog() == true) {
                    var qmakePath = openFileDialog.FileName;
                    var qmakeDir = Path.GetDirectoryName(qmakePath);
                    if (Path.GetFileName(qmakeDir)
                        .Equals("bin", StringComparison.InvariantCultureIgnoreCase)) {
                        qmakeDir = Path.GetDirectoryName(qmakeDir);
                        version.Path = qmakeDir;
                    } else {
                        version.Path = qmakePath;
                    }
                    if (string.IsNullOrEmpty(version.VersionName)) {
                        version.VersionName = string.Format("{0}_{1}",
                            Path.GetFileName(Path.GetDirectoryName(qmakeDir)),
                            Path.GetFileName(qmakeDir))
                            .Replace(" ", "_");
                    }
                    Validate(true);
                }
            }
        }

        static ImageSource GetExplorerIcon()
        {
            var pathWindowsExplorer = string.Format(@"{0}\explorer.exe",
                Environment.GetFolderPath(Environment.SpecialFolder.Windows));

            NativeAPI.SHFILEINFO shellFileInfo = new NativeAPI.SHFILEINFO();
            NativeAPI.SHGetFileInfo(pathWindowsExplorer,
                0, ref shellFileInfo, Marshal.SizeOf(shellFileInfo),
                NativeAPI.SHGFI.Icon | NativeAPI.SHGFI.SmallIcon);
            if (shellFileInfo.hIcon == IntPtr.Zero)
                return null;

            var iconImageSource = Imaging.CreateBitmapSourceFromHIcon(
                shellFileInfo.hIcon, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            NativeAPI.DestroyIcon(shellFileInfo.hIcon);
            return iconImageSource;
        }

        static object GetBinding(FrameworkElement control)
        {
            if (control == null
            || control.BindingGroup == null
            || control.BindingGroup.Items == null
            || control.BindingGroup.Items.Count == 0) {
                return null;
            }
            return control.BindingGroup.Items[0];
        }

        static DataGridCell FindContainingCell(DependencyObject control)
        {
            while (control != null) {
                if (control is ContentPresenter contentPresenter
                && contentPresenter.Parent is DataGridCell cell) {
                    return cell;
                }
                control = VisualTreeHelper.GetParent(control);
            }
            return null;
        }
    }
}
