/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace QtVsTools.Wizards.ProjectWizard
{
    using Core;
    using QtVsTools.Common;
    using Wizards.Common;
    using static Utils;

    public partial class ConfigPage : WizardPage
    {
        interface ICloneable<T> where T : ICloneable<T>
        {
            T Clone();
        }

        class Module : ICloneable<Module>
        {
            public string Name { get; set; }
            public string Id { get; set; }
            public bool IsSelected { get; set; }
            public bool IsReadOnly { get; set; }
            public bool IsEnabled => !IsReadOnly;

            public Module Clone()
            {
                return new Module
                {
                    Name = Name,
                    Id = Id,
                    IsSelected = IsSelected,
                    IsReadOnly = IsReadOnly
                };
            }
        }

        class Config : ICloneable<Config>, IWizardConfiguration
        {
            public string Name { get; set; }
            public VersionInformation QtVersion { get; set; }
            public string QtVersionName { get; set; }
            public string QtVersionPath { get; set; }
            public string Target { get; set; }
            public string Platform { get; set; }
            public bool IsDebug { get; set; }

            public Dictionary<string, Module> Modules { get; set; }

            public IEnumerable<Module> AllModules
                => Modules.Values.OrderBy(module => module.Name);
            public IEnumerable<Module> SelectedModules
                => Modules.Values.Where((Module m) => m.IsSelected);

            IEnumerable<string> IWizardConfiguration.Modules
                => SelectedModules.SelectMany((Module m) => m.Id.Split(' '));

            public Config Clone()
            {
                return new Config
                {
                    Name = Name,
                    QtVersion = QtVersion,
                    QtVersionName = QtVersionName,
                    Target = Target,
                    Platform = Platform,
                    IsDebug = IsDebug,
                    Modules = AllModules
                        .Select((Module m) => m.Clone())
                        .ToDictionary((Module m) => m.Name)
                };
            }
        }

        class CloneableList<T> : List<T> where T : ICloneable<T>
        {
            public CloneableList() : base()
            { }

            public CloneableList(IEnumerable<T> collection) : base(collection)
            { }

            public CloneableList<T> Clone()
            {
                return new CloneableList<T>(this.Select(x => x.Clone()));
            }
        }

        const string QT_VERSION_DEFAULT = "<Default>";
        const string QT_VERSION_BROWSE = "<Browse...>";

        IEnumerable<string> qtVersionList = new[] { QT_VERSION_DEFAULT, QT_VERSION_BROWSE }
            .Union(QtVersionManager.The().GetVersions());

        readonly QtVersionManager qtVersionManager = QtVersionManager.The();
        readonly VersionInformation defaultQtVersionInfo;

        CloneableList<Config> defaultConfigs;
        List<Config> currentConfigs;
        bool initialNextButtonIsEnabled;
        bool initialFinishButtonIsEnabled;

        public bool ProjectModelEnabled { get; set; } = false;

        public ConfigPage()
        {
            InitializeComponent();

            string defaultQtVersionName = qtVersionManager.GetDefaultVersion();
            defaultQtVersionInfo = qtVersionManager.GetVersionInfo(defaultQtVersionName);

            ErrorIcon.Source = Imaging.CreateBitmapSourceFromHIcon(
                SystemIcons.Exclamation.Handle, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            DataContext = this;
            Loaded += OnLoaded;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            qtVersionList = new[] { QT_VERSION_DEFAULT, QT_VERSION_BROWSE }
                .Union(QtVersionManager.The().GetVersions());

            if (defaultQtVersionInfo == null) {
                Validate();
                return;
            }

            var qtModules = QtModules.Instance.GetAvailableModules(defaultQtVersionInfo.qtMajor)
                .Where(mi => mi.Selectable)
                .Select(mi => new Module()
                {
                    Name = mi.Name,
                    Id = mi.proVarQT,
                    IsSelected = Data.DefaultModules.Contains(mi.LibraryPrefix),
                    IsReadOnly = Data.DefaultModules.Contains(mi.LibraryPrefix),
                }).ToList();

            defaultConfigs = new CloneableList<Config> {
                new Config {
                    Name = "Debug",
                    IsDebug = true,
                    QtVersion = defaultQtVersionInfo,
                    QtVersionName = defaultQtVersionInfo.name,
                    Target = defaultQtVersionInfo.isWinRT()
                        ? ProjectTargets.WindowsStore.Cast<string>()
                        : ProjectTargets.Windows.Cast<string>(),
                    Platform
                        = defaultQtVersionInfo.platform() == Platform.x86
                            ? ProjectPlatforms.Win32.Cast<string>()
                        : defaultQtVersionInfo.platform() == Platform.x64
                            ? ProjectPlatforms.X64.Cast<string>()
                        : defaultQtVersionInfo.platform() == Platform.arm64
                            ? ProjectPlatforms.ARM64.Cast<string>()
                        : string.Empty,
                    Modules = qtModules.ToDictionary(m => m.Name)
                },
                new Config {
                    Name = "Release",
                    IsDebug = false,
                    QtVersion = defaultQtVersionInfo,
                    QtVersionName = defaultQtVersionInfo.name,
                    Target = defaultQtVersionInfo.isWinRT()
                        ? ProjectTargets.WindowsStore.Cast<string>()
                        : ProjectTargets.Windows.Cast<string>(),
                    Platform
                        = defaultQtVersionInfo.platform() == Platform.x86
                            ? ProjectPlatforms.Win32.Cast<string>()
                        : defaultQtVersionInfo.platform() == Platform.x64
                            ? ProjectPlatforms.X64.Cast<string>()
                        : defaultQtVersionInfo.platform() == Platform.arm64
                            ? ProjectPlatforms.ARM64.Cast<string>()
                        : string.Empty,
                    Modules = qtModules.ToDictionary(m => m.Name)
                }
            };
            currentConfigs = defaultConfigs.Clone();
            ConfigTable.ItemsSource = currentConfigs;

            initialNextButtonIsEnabled = NextButton.IsEnabled;
            initialFinishButtonIsEnabled = FinishButton.IsEnabled;

            Validate();
        }

        /// <summary>
        /// Callback to validate selected configurations.
        /// Must return an error message in case of failed validation.
        /// Otherwise, return empty string or null.
        /// </summary>
        public Func<IEnumerable<IWizardConfiguration>, string> ValidateConfigs { get; set; }

        void Validate()
        {
            if (currentConfigs == null) {
                ErrorMsg.Content = "Register at least one Qt version using \"Qt VS Tools\"" +
                    " -> \"Qt Options\".";
                ErrorPanel.Visibility = Visibility.Visible;
                NextButton.IsEnabled = false;
                FinishButton.IsEnabled = false;
            } else if (currentConfigs // "$(Configuration)|$(Platform)" must be unique
                .GroupBy((Config c) => $"{c.Name}|{c.Platform}")
                .Where((IGrouping<string, Config> g) => g.Count() > 1)
                .Any()) {
                ErrorMsg.Content = "(Configuration, Platform) must be unique";
                ErrorPanel.Visibility = Visibility.Visible;
                NextButton.IsEnabled = false;
                FinishButton.IsEnabled = false;
            } else if (ValidateConfigs != null
                && ValidateConfigs(currentConfigs) is string errorMsg
                && !string.IsNullOrEmpty(errorMsg)) {
                ErrorMsg.Content = errorMsg;
                ErrorPanel.Visibility = Visibility.Visible;
                NextButton.IsEnabled = false;
                FinishButton.IsEnabled = false;
            } else {
                ErrorMsg.Content = string.Empty;
                ErrorPanel.Visibility = Visibility.Hidden;
                NextButton.IsEnabled = initialNextButtonIsEnabled;
                FinishButton.IsEnabled = initialFinishButtonIsEnabled;
            }
        }

        void RemoveConfig_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button buttonRemove
                && GetBinding(buttonRemove) is Config config) {
                currentConfigs.Remove(config);
                if (!currentConfigs.Any()) {
                    currentConfigs = defaultConfigs.Clone();
                    ConfigTable.ItemsSource = currentConfigs;
                }
                ConfigTable.Items.Refresh();
                Validate();
            }
        }

        void DuplicateConfig_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button buttonDuplicate
                && GetBinding(buttonDuplicate) is Config config) {
                currentConfigs.Add(config.Clone());
                ConfigTable.Items.Refresh();
                Validate();
            }
        }

        void Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox txt && GetBinding(txt) is Config cfg)
                cfg.Name = txt.Text;
            Validate();
        }

        void QtVersion_ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBoxQtVersion
                && GetBinding(comboBoxQtVersion) is Config config) {
                comboBoxQtVersion.IsEnabled = false;
                comboBoxQtVersion.ItemsSource = qtVersionList;
                comboBoxQtVersion.Text = config.QtVersionName;
                comboBoxQtVersion.IsEnabled = true;
            }
        }

        void QtVersion_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is ComboBox comboBoxQtVersion
                && comboBoxQtVersion.IsEnabled
                && GetBinding(comboBoxQtVersion) is Config config
                && config.QtVersionName != comboBoxQtVersion.Text) {
                var oldQtVersion = config.QtVersion;
                if (comboBoxQtVersion.Text == QT_VERSION_DEFAULT) {
                    config.QtVersion = defaultQtVersionInfo;
                    config.QtVersionName = defaultQtVersionInfo.name;
                    config.QtVersionPath = defaultQtVersionInfo.qtDir;
                    comboBoxQtVersion.Text = defaultQtVersionInfo.name;
                } else if (comboBoxQtVersion.Text == QT_VERSION_BROWSE) {
                    var openFileDialog = new OpenFileDialog
                    {
                        Filter = "qmake|qmake.exe;qmake.bat"
                    };
                    if (openFileDialog.ShowDialog() == true) {
                        IEnumerable<string> binPath = Path.GetDirectoryName(openFileDialog.FileName)
                            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        string lastDirName = binPath.LastOrDefault();
                        if ("bin".Equals(lastDirName, IgnoreCase))
                            binPath = binPath.Take(binPath.Count() - 1);

                        var qtVersion = string.Join(
                            Path.DirectorySeparatorChar.ToString(), binPath);
                        var versionInfo = VersionInformation.Get(qtVersion);
                        if (versionInfo != null) {
                            versionInfo.name = qtVersion;
                            config.QtVersion = versionInfo;
                            config.QtVersionName = versionInfo.name;
                            config.QtVersionPath = config.QtVersion.qtDir;
                        }
                    }
                    comboBoxQtVersion.Text = config.QtVersionName;
                } else if (qtVersionManager.GetVersions().Contains(comboBoxQtVersion.Text)) {
                    config.QtVersion = qtVersionManager.GetVersionInfo(comboBoxQtVersion.Text);
                    config.QtVersionName = comboBoxQtVersion.Text;
                    config.QtVersionPath = qtVersionManager.GetInstallPath(comboBoxQtVersion.Text);
                } else {
                    config.QtVersion = null;
                    config.QtVersionName = config.QtVersionPath = comboBoxQtVersion.Text;
                }

                if (oldQtVersion != config.QtVersion) {
                    if (config.QtVersion != null) {
                        config.Target = config.QtVersion.isWinRT()
                            ? ProjectTargets.WindowsStore.Cast<string>()
                            : ProjectTargets.Windows.Cast<string>();
                        config.Platform
                            = config.QtVersion.platform() == Platform.x86
                                ? ProjectPlatforms.Win32.Cast<string>()
                            : config.QtVersion.platform() == Platform.x64
                                ? ProjectPlatforms.X64.Cast<string>()
                            : config.QtVersion.platform() == Platform.arm64
                                ? ProjectPlatforms.ARM64.Cast<string>()
                            : string.Empty;
                        config.Modules =
                            QtModules.Instance.GetAvailableModules(config.QtVersion.qtMajor)
                                .Where((QtModule mi) => mi.Selectable)
                                .Select((QtModule mi) => new Module()
                                {
                                    Name = mi.Name,
                                    Id = mi.proVarQT,
                                    IsSelected = Data.DefaultModules.Contains(mi.LibraryPrefix),
                                    IsReadOnly = Data.DefaultModules.Contains(mi.LibraryPrefix),
                                }).ToDictionary((Module m) => m.Name);
                    } else if (config.QtVersionPath.StartsWith("SSH:")) {
                        config.Target = ProjectTargets.LinuxSSH.Cast<string>();
                    } else if (config.QtVersionPath.StartsWith("WSL:")) {
                        config.Target = ProjectTargets.LinuxWSL.Cast<string>();
                    }
                    ConfigTable.Items.Refresh();
                }
                Validate();
            }
        }

        void Target_ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBoxTarget
                && GetBinding(comboBoxTarget) is Config config) {
                comboBoxTarget.IsEnabled = false;
                comboBoxTarget.ItemsSource = EnumExt.GetValues<string>(typeof(ProjectTargets));
                comboBoxTarget.Text = config.Target;
                comboBoxTarget.IsEnabled = true;
            }
        }

        void Target_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is ComboBox comboBoxTarget
                && comboBoxTarget.IsEnabled
                && GetBinding(comboBoxTarget) is Config config
                && config.Target != comboBoxTarget.Text) {
                config.Target = comboBoxTarget.Text;
                ConfigTable.Items.Refresh();
                Validate();
            }
        }

        void Platform_ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBoxPlatform
                && GetBinding(comboBoxPlatform) is Config config) {
                comboBoxPlatform.IsEnabled = false;
                comboBoxPlatform.ItemsSource = EnumExt.GetValues<string>(typeof(ProjectPlatforms));
                comboBoxPlatform.Text = config.Platform;
                comboBoxPlatform.IsEnabled = true;
            }
        }

        void Platform_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is ComboBox comboBoxPlatform
                && comboBoxPlatform.IsEnabled
                && GetBinding(comboBoxPlatform) is Config config
                && config.Platform != comboBoxPlatform.Text) {
                config.Platform = comboBoxPlatform.Text;
                ConfigTable.Items.Refresh();
                Validate();
            }
        }

        void Debug_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && GetBinding(checkBox) is Config config) {
                config.IsDebug = checkBox.IsChecked ?? false;
                if (config.IsDebug && config.Name.EndsWith("Release")) {
                    config.Name =
                        $"{config.Name.Substring(0, config.Name.Length - "Release".Length)}Debug";
                    ConfigTable.Items.Refresh();
                } else if (!config.IsDebug && config.Name.EndsWith("Debug")) {
                    config.Name =
                        $"{config.Name.Substring(0, config.Name.Length - "Debug".Length)}Release";
                    ConfigTable.Items.Refresh();
                }
                Validate();
            }
        }

        void Module_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBoxModule
                && (checkBoxModule.TemplatedParent as ContentPresenter)?.Content is Module
                && GetBinding(checkBoxModule) is Config config
                && FindAncestor(checkBoxModule, "Modules") is ComboBox comboBoxModules
                && FindDescendant(comboBoxModules, "SelectedModules") is ListView selectedModules) {
                selectedModules.ItemsSource = config.SelectedModules;
                Validate();
            }
        }

        protected override void OnNextButtonClick(object sender, RoutedEventArgs e)
        {
            Data.ProjectModel = (WizardData.ProjectModels)ProjectModel.SelectedIndex;
            Data.Configs = currentConfigs.Cast<IWizardConfiguration>();
            base.OnNextButtonClick(sender, e);
        }

        protected override void OnFinishButtonClick(object sender, RoutedEventArgs e)
        {
            Data.ProjectModel = (WizardData.ProjectModels)ProjectModel.SelectedIndex;
            Data.Configs = currentConfigs.Cast<IWizardConfiguration>();
            base.OnFinishButtonClick(sender, e);
        }

        static object GetBinding(FrameworkElement control)
        {
            if (control?.BindingGroup == null)
                return null;
            return control.BindingGroup.Items.Count == 0 ? null : control.BindingGroup.Items[0];
        }

        static FrameworkElement FindAncestor(FrameworkElement control, string name)
        {
            while (control != null && control.Name != name) {
                object parent = control.Parent
                    ?? control.TemplatedParent
                    ?? VisualTreeHelper.GetParent(control);
                control = parent as FrameworkElement;
            }
            return control;
        }

        static FrameworkElement FindDescendant(FrameworkElement control, string name)
        {
            var stack = new Stack<FrameworkElement>(new[] { control });
            while (stack.Any()) {
                control = stack.Pop();
                if (control?.Name == name && control is FrameworkElement result)
                    return result;
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(control); ++i) {
                    if (VisualTreeHelper.GetChild(control, i) is FrameworkElement child)
                        stack.Push(child);
                }
            }
            return null;
        }
    }
}
