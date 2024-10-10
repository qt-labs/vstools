/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml.Linq;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Shell32;

namespace QtVsTools.Core.Options
{
    using Common;
    using Core;
    using QtVsTools.Common;
    using VisualStudio;

    using static HelperFunctions;
    using static QtVsTools.Common.EnumExt;

    public enum BuildHost
    {
        [String("Windows")] Windows,
        [String("Linux SSH")] LinuxSSH,
        [String("Linux WSL")] LinuxWSL
    }

    [Flags]
    public enum State
    {
        Unknown = 0x00,
        Existing = 0x01,
        DefaultModified = 0x02,
        NameModified = 0x04,
        PathModified = 0x08,
        HostModified = 0x10,
        CompilerModified = 0x20
    }

    public class UiAdorner : Adorner
    {
        private readonly Pen pen;
        private readonly Control control;

        public UiAdorner(Control adornedElement)
            : base(adornedElement)
        {
            pen = new Pen(Brushes.Red, 1);
            pen.Freeze();
            IsHitTestVisible = false;
            control = adornedElement;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var rect = new Rect(control.RenderSize);
            drawingContext.DrawRectangle(null, pen, rect);
        }
    }

    public class BuildHostConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is BuildHost buildHost ? buildHost.Cast<string>() : "";
        }

        public object ConvertBack(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string buildHost ? buildHost.Cast(BuildHost.Windows) : value;
        }
    }

    public class ErrorTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var errorMessage = value as string;
            return string.IsNullOrEmpty(errorMessage)
                ? null : new ToolTip { Content = errorMessage };
        }

        public object ConvertBack(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class QtVersionsTable
    {
        private UiAdorner nameAdorner;
        private UiAdorner pathAdorner;
        private UiAdorner compilerAdorner;

        private const string QtMaintenanceToolLnk = "Qt\\Qt Maintenance Tool.lnk";
        private const string QtVersionsXmlCreator = @"QtProject\qtcreator\qtversion.xml";
        private const string QtVersionsXmlInstaller =
            @"Tools\QtCreator\share\qtcreator\" + QtVersionsXmlCreator;
        private const string MaintenanceToolDat = "MaintenanceTool.dat";

        public ObservableCollection<QtVersion> QtVersions
        {
            set
            {
                if (ReferenceEquals(DataGrid.ItemsSource, value))
                    return;
                DataGrid.ItemsSource = value;
            }
            get => DataGrid.ItemsSource as ObservableCollection<QtVersion>;
        }

        public List<QtVersion> RemovedQtVersions { get; } = new();

        public QtVersionsTable()
        {
            InitializeComponent();
        }

        public IEnumerable<string> GetErrorMessages()
        {
            return QtVersions
                .Where(qtVersion => qtVersion.HasError)
                .Select(qtVersion => qtVersion.ErrorMessage)
                .Distinct();
        }

        private void OnQtVersionTable_Loaded(object sender, RoutedEventArgs e)
        {
            ClearAdornerLayer();
            VersionHost.SelectedIndex = 0;

            if (DataGrid.Items.Count > 0) {
                DataGrid.SelectedItem = DataGrid.Items[0];
                DataGrid.ScrollIntoView(DataGrid.SelectedItem);
                SetControlsEnabled(true, false);
            } else {
                SetControlsEnabled(false, true);
            }

            var enableAutodetect = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
            }.Any(folder => File.Exists(Path.Combine(folder, QtMaintenanceToolLnk)));
            enableAutodetect |= File.Exists(Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), QtVersionsXmlCreator));

            ButtonAutodetect.IsEnabled = enableAutodetect;
        }

        #region Table modifier

        private void OnDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGrid.SelectedItem is not QtVersion qtVersion)
                return;
            VersionHost.SelectedItem = qtVersion.Host.Cast<string>();
        }

        private void OnAddNewVersion_Click(object sender, RoutedEventArgs e)
        {
            var newVersion = new QtVersion
            {
                IsDefault = DataGrid.Items.Count <= 0,
                Name = "",
                Path = "",
                Host = BuildHost.Windows,
                Compiler = "msvc",
                State = State.NameModified | State.PathModified | State.HostModified
                    | State.CompilerModified
            };

            SortDescription? activeSortDescription = null;
            if (CollectionViewSource.GetDefaultView(DataGrid.ItemsSource) is var view) {
                if (view.SortDescriptions.Count > 0)
                    activeSortDescription = view.SortDescriptions[0];
                view.SortDescriptions.Clear();
            }

            if (DataGrid.SelectedItem is QtVersion selectedVersion) {
                var selectedIndex = QtVersions.IndexOf(selectedVersion);
                if (selectedIndex >= 0 && selectedIndex < QtVersions.Count)
                    QtVersions.Insert(selectedIndex + 1, newVersion);
            } else {
                QtVersions.Add(newVersion);
            }

            DataGrid.Items.Refresh();

            if (activeSortDescription.HasValue) {
                view.SortDescriptions.Add(activeSortDescription.Value);
                view.Refresh();
            }

            DataGrid.SelectedItem = newVersion;
            DataGrid.ScrollIntoView(DataGrid.SelectedItem);

            if (QtVersions.Count == 1)
                SetControlsEnabled(true, false);

            VersionName.Focus();
            OnVersionName_TextChanged(VersionName, null);
            OnVersionPath_TextChanged(VersionPath, null);
        }

        private void OnRemoveVersion_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid.SelectedItem is not QtVersion qtVersion)
                return;

            var selectedIndex = DataGrid.SelectedIndex;
            QtVersions.Remove(qtVersion);
            RemovedQtVersions.Add(qtVersion);

            if (!QtVersions.Any())
                SetControlsEnabled(false, true);

            UpdateSelection(selectedIndex);
        }

        private void OnSetAsDefault_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid.SelectedItem is not QtVersion qtVersion)
                return;

            foreach (var version in QtVersions) {
                version.IsDefault = version == qtVersion;
                switch (version.InitialIsDefault) {
                case true when version.IsDefault:
                case false when !version.IsDefault:
                    version.State &= ~State.DefaultModified;
                    break;
                default:
                    version.State |= State.DefaultModified;
                    break;
                }
            }
        }

        private void OnImportQtInstallation_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (VsServiceProvider.GetService<SVsUIShell, IVsUIShell>() is not {} iVsUiShell)
                return;

            var selectedPath = "";
            var pDirName = IntPtr.Zero;
            try {
                var browseInfo = new VSBROWSEINFOW[]
                {
                    new()
                    {
                        lStructSize = (uint)Marshal.SizeOf(typeof(VSBROWSEINFOW)),
                        hwndOwner = WindowHelper.GetDialogOwnerHandle(),
                        nMaxDirName = 260,
                        pwzDirName = pDirName = Marshal.AllocCoTaskMem(520),
                        pwzDlgTitle = "Please select a Qt installer location"
                    }
                };

                var result = iVsUiShell.GetDirectoryViaBrowseDlg(browseInfo);
                if (result == VSConstants.OLE_E_PROMPTSAVECANCELLED)
                    return;
                ErrorHandler.ThrowOnFailure(result);
                selectedPath = Marshal.PtrToStringAuto(browseInfo[0].pwzDirName);
                if (string.IsNullOrEmpty(selectedPath))
                    return;
            } catch (Exception ex) {
                ex.Log();
            } finally {
                if (pDirName != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pDirName);
            }

            var waitDialog = WaitDialog.Start("Qt VS Tools", "Searching for Qt Installations",
                delay: 2, isCancelable: true);

            try {
                var qmakePaths = Enumerable.Empty<string>();

                var versionsXml = SearchFileInDirectoriesUpwards(selectedPath, QtVersionsXmlInstaller);
                if (string.IsNullOrEmpty(versionsXml)) {
                    var datFile = SearchFileInDirectoriesUpwards(selectedPath, MaintenanceToolDat);
                    if (string.IsNullOrEmpty(datFile)) {
                        Messages.DisplayErrorMessage("The selected directory is not a Qt installer"
                            + " location.");
                    } else {
                        qmakePaths = SearchAllQMake(Path.GetDirectoryName(datFile), waitDialog);
                    }
                } else {
                    qmakePaths = ParseQtVersionsXml(File.ReadAllText(versionsXml, Encoding.UTF8));
                }

                AddQtVersionsFromPath(qmakePaths);
            } catch (Exception ex) {
                ex.Log();
            }
            waitDialog.Stop();
        }

        private void OnAutodetectQtInstallations_Click(object sender, RoutedEventArgs e)
        {
            var versions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void TryAddVersions(string path, string xmlVersions)
            {
                if (File.Exists(path = Path.Combine(path, xmlVersions)))
                    versions.AddRange(ParseQtVersionsXml(File.ReadAllText(path, Encoding.UTF8)));
            }

            TryAddVersions(GetShortcutTargetPath(Path.Combine(Environment.GetFolderPath(Environment
                .SpecialFolder.CommonPrograms), QtMaintenanceToolLnk)), QtVersionsXmlInstaller);
            TryAddVersions(GetShortcutTargetPath(Path.Combine(Environment.GetFolderPath(Environment
                .SpecialFolder.Programs), QtMaintenanceToolLnk)), QtVersionsXmlInstaller);
            TryAddVersions(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                QtVersionsXmlCreator);

            AddQtVersionsFromPath(versions);
        }

        private void OnCleanupQtInstallations_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = DataGrid.SelectedIndex;

            var updateDefault = false;
            for (var i = QtVersions.Count - 1; i >= 0; --i) {
                var tmp = QtVersions[i];
                if (tmp.Host != BuildHost.Windows)
                    continue;

                var path = NormalizePath(tmp.Path);
                if (QtPaths.Exists(path) || QMake.Exists(path))
                    continue;

                updateDefault |= tmp.IsDefault;

                QtVersions.RemoveAt(i);
                RemovedQtVersions.Add(tmp);
            }

            if (updateDefault
                && QtVersions.FirstOrDefault(v => v.Host == BuildHost.Windows) is {} version) {
                version.IsDefault = true;
                version.State |= State.DefaultModified;
            }

            if (!QtVersions.Any())
                SetControlsEnabled(false, true);

            UpdateSelection(selectedIndex);
        }

        #endregion

        #region Version details

        private void OnVersionName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox { Text: { } text } box)
                return;

            if (DataGrid.SelectedItem is not QtVersion qtVersion)
                return;

            if (qtVersion.State.HasFlag(State.Existing)) {
                if (string.Equals(qtVersion.InitialName, text, StringComparison.Ordinal))
                    qtVersion.State &= ~State.NameModified;
                else
                    qtVersion.State |= State.NameModified;
            }

            Validate(qtVersion);
            HandleError(ValidateVersionName(qtVersion), box, ref nameAdorner);
        }

        private void OnVersionPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox { Text: { } text } box)
                return;

            if (DataGrid.SelectedItem is not QtVersion qtVersion)
                return;

            if (qtVersion.State.HasFlag(State.Existing)) {
                var normalized = FromNativeSeparators(NormalizePath(text) ?? "");
                if (string.Equals(qtVersion.InitialPath, normalized, Utils.IgnoreCase))
                    qtVersion.State &= ~State.PathModified;
                else
                    qtVersion.State |= State.PathModified;
            }

            Validate(qtVersion);
            HandleError(ValidateVersionPath(qtVersion), box, ref pathAdorner);
        }

        private void OnUpdateVersionPath_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                AddExtension = false,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "Qt Tools (qtpaths, qmake)|qtpaths;qtpaths.exe;qmake;qmake.exe;qmake.bat",
                Title = "Qt VS Tools - Select qtpaths or qmake",
                InitialDirectory = NormalizePath(VersionPath.Text) ?? ""
            };
            if (openFileDialog.ShowDialog() != true)
                return;

            var qmakeBinDir = Path.GetDirectoryName(openFileDialog.FileName);
            VersionPath.Text = Path.GetDirectoryName(qmakeBinDir) ?? "";
            if (!string.IsNullOrEmpty(VersionName.Text))
                return;

            var compilerDir = Path.GetDirectoryName(qmakeBinDir);
            var qtVersionDir = Path.GetDirectoryName(compilerDir);
            VersionName.Text = $"{Path.GetFileName(qtVersionDir)}"
                + $"_{Path.GetFileName(compilerDir)}".Replace(" ", "_");
        }

        private void OnVersionHost_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGrid.SelectedItem is not QtVersion qtVersion)
                return;

            qtVersion.Host = VersionHost.SelectedItem.ToString().Cast(BuildHost.Windows);
            qtVersion.State = qtVersion.InitialHost == qtVersion.Host ?
                qtVersion.State & ~State.HostModified :
                qtVersion.State | State.HostModified;

            if (qtVersion.Host != BuildHost.Windows) {
                if (VersionCompiler.Text == "msvc")
                    VersionCompiler.Text = "g++";
            } else {
                VersionCompiler.Text = "msvc";
            }
            VersionCompiler.IsEnabled = qtVersion.Host != BuildHost.Windows;

            Validate(qtVersion);
            HandleError(ValidateVersionHost(qtVersion), VersionPath, ref compilerAdorner);
        }

        private void OnVersionCompiler_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox { Text: { } text } box)
                return;

            if (DataGrid.SelectedItem is not QtVersion qtVersion)
                return;

            if (qtVersion.State.HasFlag(State.Existing)) {
                if (string.Equals(qtVersion.InitialCompiler, text, StringComparison.Ordinal))
                    qtVersion.State &= ~State.CompilerModified;
                else
                    qtVersion.State |= State.CompilerModified;
            }

            Validate(qtVersion);
            HandleError(ValidateVersionCompiler(qtVersion), box, ref compilerAdorner);
        }

        #endregion

        #region Helper functions

        private static string GetShortcutTargetPath(string shortcutPath)
        {
            if (!File.Exists(shortcutPath))
                return "";
            try {
                var shell = new Shell();
                var folder = shell.NameSpace(Path.GetDirectoryName(shortcutPath));
                var item = folder.ParseName(Path.GetFileName(shortcutPath));
                if (item is {GetLink: ShellLinkObject link})
                    return Path.GetDirectoryName(link.Path);
            } catch (Exception ex) {
                ex.Log();
            }
            return "";
        }


        private static IEnumerable<string> ParseQtVersionsXml(string xmlData)
        {
            var xmlDoc = XDocument.Parse(xmlData);
            var dataNodes = xmlDoc.Descendants("data");
            return dataNodes.Select(dataNode => dataNode.Descendants("value")
                .FirstOrDefault(e => (string)e.Attribute("key") == "QMakePath")?.Value)
                .Where(value => value != null);
        }


        // Search for the given file name file starting from the given directory and
        // recursively moving up the directory tree until the root directory is reached.
        // Returns the full path to the file if found; otherwise, returns null.
        private static string SearchFileInDirectoriesUpwards(string directoryPath, string name)
        {
            while (!string.IsNullOrEmpty(directoryPath) && Directory.Exists(directoryPath)) {
                var xmlFilePath = Path.Combine(directoryPath, name);
                if (File.Exists(xmlFilePath))
                    return xmlFilePath;
                directoryPath = Path.GetDirectoryName(directoryPath);
            }
            return null;
        }

        private static IEnumerable<string> SearchAllQMake(string directoryPath, WaitDialog waitDialog)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!Directory.Exists(directoryPath))
                yield break;

            var binDirectories = Directory.GetDirectories(
                directoryPath, "bin", SearchOption.AllDirectories);
            foreach (var binDirectory in binDirectories) {
                foreach (var file in Directory.EnumerateFiles(binDirectory)) {
                    if (waitDialog.Canceled)
                        yield break;
                    var fileName = Path.GetFileName(file);
                    if (Regex.IsMatch(fileName, @"^(qtpaths|qmake)(\.exe|\.bat)?$", RegexOptions.IgnoreCase))
                        yield return file;
                }
            }
        }

        private void AddQtVersionsFromPath(IEnumerable<string> allQMakePath)
        {
            var versions = new List<QtVersion>();

            // Create a list of new versions
            foreach (var qmakePath in allQMakePath) {
                if (!File.Exists(qmakePath))
                    continue;
                var qmakeBinDir = Path.GetDirectoryName(qmakePath);
                var compilerDir = Path.GetDirectoryName(qmakeBinDir);
                var qtVersionDir = Path.GetDirectoryName(compilerDir);
                var versionName = $"{Path.GetFileName(qtVersionDir)}"
                  + $"_{Path.GetFileName(compilerDir)}".Replace(" ", "_");

                if (VersionInformation.GetOrAddByPath(compilerDir) is not {} versionInfo) {
                    Messages.Print($"Skip Qt version: {versionName}, "
                      + $"path: '{compilerDir}', failed to load version information.");
                    continue;
                }

                var generator = versionInfo.GetQMakeConfEntry("MAKEFILE_GENERATOR");
                if (generator is not ("MSVC.NET" or "MSBUILD")) {
                    Messages.Print($"Skip incompatible Qt version: {versionName}, "
                      + $"path: '{compilerDir}', makefile generator: {generator}.");
                    continue;
                }

                versions.Add(
                    new QtVersion
                    {
                        IsDefault = false,
                        Name = versionName,
                        Path = compilerDir,
                        Host = BuildHost.Windows,
                        Compiler = "msvc",
                        State = State.NameModified | State.PathModified | State.HostModified
                            | State.CompilerModified
                    });
            }

            if (!versions.Any())
                return;

            if (versions.FirstOrDefault() is {} version) {
                version.IsDefault = DataGrid.Items.Count <= 0;
                version.State |= version.IsDefault ? State.DefaultModified : State.Unknown;
            }

            foreach (var qtVersion in versions)
                Validate(qtVersion);

            QtVersions = new ObservableCollection<QtVersion>(
                QtVersions
                    .Union(versions)
                    .GroupBy(qt => qt.Name)
                    .Select(group => group.First()));

            if (QtVersions.Any())
                SetControlsEnabled(true, false);
        }

        private void Validate(QtVersion version)
        {
            if (version == null)
                return;

            var validationFunctions = new List<Func<QtVersion, string>>
            {
                ValidateIsDefault,
                ValidateVersionName,
                ValidateVersionPath,
                ValidateVersionHost,
                ValidateVersionCompiler
            };

            var errorMessage = string.Join(Environment.NewLine, validationFunctions
                .Select(validationFunction => validationFunction(version))
                .Where(message => !string.IsNullOrEmpty(message)));

            version.ErrorMessage = errorMessage;
        }

        private static string ValidateIsDefault(QtVersion version)
        {
            if (!version.State.HasFlag(State.DefaultModified))
                return "";
            return version is { IsDefault: true, Host: not BuildHost.Windows }
                ? "Default version: Host must be Windows" : "";
        }

        private string ValidateVersionName(QtVersion version)
        {
            if (!version.State.HasFlag(State.NameModified))
                return "";
            if (string.IsNullOrEmpty(version.Name))
                return "Name cannot be empty";
            if (version.Name.ToUpperInvariant() is "$(QTDIR)" or "$(DEFAULTQTVERSION)")
                return $"Name cannot be '{version.Name}'";
            return QtVersions.Any(otherVersion => otherVersion != version
                    && otherVersion.Name == version.Name)
                ? "Version name must be unique" : "";
        }

        private static string ValidateVersionPath(QtVersion version)
        {
            if (!version.State.HasFlag(State.PathModified))
                return "";
            if (string.IsNullOrEmpty(version.Path))
                return "Location cannot be empty";
            if (version.Host != BuildHost.Windows)
                return version.Path.Contains(':') ? "Invalid character in path" : "";
            var path = NormalizePath(version.Path);
            if (string.IsNullOrEmpty(path))
                return "Invalid path format";
            return QtPaths.Exists(path) || QMake.Exists(path) ? "" : "Cannot find qtpaths or qmake";
        }

        private static string ValidateVersionHost(QtVersion version)
        {
            if (!version.State.HasFlag(State.HostModified))
                return "";
            if (version.Host != BuildHost.Windows)
                return version.Path.Contains(':') ? "Invalid character in path" : "";
            return version is { IsDefault: true, Host: not BuildHost.Windows }
                ? "Default version: Host must be Windows" : "";
        }

        private static string ValidateVersionCompiler(QtVersion version)
        {
            if (!version.State.HasFlag(State.CompilerModified))
                return "";
            if (string.IsNullOrEmpty(version.Compiler))
                return "Compiler cannot be empty";
            if (version.Host == BuildHost.Windows)
                return "";
            return version.Compiler.Contains(':') ? "Invalid character in name" : "";
        }

        private void UpdateSelection(int index)
        {
            if (index < QtVersions.Count) {
                // current row or row behind
                DataGrid.SelectedIndex = index;
            } else if (index > 0) {
                // row before
                DataGrid.SelectedIndex = index - 1;
            } else {
                // no more rows available
                ClearAdornerLayer();
                DataGrid.SelectedItem = null;
            }
        }

        private void SetControlsEnabled(bool enabled, bool includeCompiler)
        {
            VersionName.IsEnabled = enabled;
            VersionPath.IsEnabled = enabled;
            VersionHost.IsEnabled = enabled;
            if (includeCompiler)
                VersionCompiler.IsEnabled = enabled;
        }

        private void ClearAdornerLayer()
        {
            HandleError("", VersionName, ref nameAdorner);
            HandleError("", VersionPath, ref pathAdorner);
            HandleError("", VersionCompiler, ref compilerAdorner);
        }

        private static void HandleError(string errorMessage, Control control, ref UiAdorner adorner)
        {
            control.ToolTip = string.IsNullOrEmpty(errorMessage) ? null : errorMessage;
            if (string.IsNullOrEmpty(errorMessage)) {
                if (adorner == null)
                    return;
                AdornerLayer.GetAdornerLayer(control)?.Remove(adorner);
                adorner = null;
            } else {
                if (adorner != null)
                    return;
                adorner = new UiAdorner(control);
                AdornerLayer.GetAdornerLayer(control)?.Add(adorner);
            }
        }

        #endregion
    }
}
