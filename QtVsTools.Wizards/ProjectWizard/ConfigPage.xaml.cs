/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace QtVsTools.Wizards.ProjectWizard
{
    using Common;
    using Core;
    using QtVsTools.Common;

    using static Common.WizardData;
    using static Core.Common.Utils;

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
            public ConfigPage ConfigPage { get; set; }
            public string Name { get; set; }
            public VersionInformation QtVersion { get; set; }
            public string QtVersionName { get; set; }
            public string QtVersionPath { get; set; }
            public string Target { get; set; }
            public string Platform { get; set; }
            public bool IsDebug { get; set; }
            public bool IsEnabled { get; set; }

            public Dictionary<string, Module> Modules { get; set; }

            public IEnumerable<Module> AllModules
                => Modules.Values.OrderBy(module => module.Name);
            public IEnumerable<Module> SelectedModules
                => Modules.Values.Where(m => m.IsSelected);

            IEnumerable<string> IWizardConfiguration.Modules
            {
                get
                {
                    if (ConfigPage.ProjectModel == ProjectModels.CMake) {
                        return ConfigPage.DefaultModules
                            .Where(module => module.IsSelected)
                            .Select(module => module.Id)
                            .ToList();
                    }
                    return SelectedModules.SelectMany(m => m.Id.Split(' '));
                }
            }

            public Config Clone()
            {
                return new Config
                {
                    ConfigPage = ConfigPage,
                    Name = Name,
                    QtVersion = QtVersion,
                    QtVersionName = QtVersionName,
                    Target = Target,
                    Platform = Platform,
                    IsDebug = IsDebug,
                    Modules = AllModules
                        .Select(m => m.Clone())
                        .ToDictionary(m => m.Name)
                };
            }
        }

        class CloneableList<T> : List<T> where T : ICloneable<T>
        {
            public CloneableList()
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

        private IEnumerable<string> qtVersionList;

        private readonly (string Name, VersionInformation VersionInfo) defaultVersion;

        CloneableList<Config> defaultConfigs;
        List<Config> currentConfigs;
        bool initialNextButtonIsEnabled;
        bool initialFinishButtonIsEnabled;

        public bool ProjectModelEnabled { get; set; } = true;
        public ProjectModels ProjectModel => (ProjectModels)ProjectModelSelection.SelectedIndex;

        public ConfigPage()
        {
            InitializeComponent();

            var name = QtVersionManager.GetDefaultVersion();
            defaultVersion = (name, VersionInformation.GetOrAddByName(name));

            DataContext = this;
            Loaded += OnLoaded;
        }

        private List<Module> DefaultModules { get; set; } = new();

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            qtVersionList = new[] { QT_VERSION_DEFAULT, QT_VERSION_BROWSE }
                .Union(QtVersionManager.GetVersions());

            if (defaultVersion.VersionInfo != null)
                SetupDefaultConfigsAndConfigTable(defaultVersion);
            else {
                if (qtVersionList.Count() > 2) {
                    var name = qtVersionList.Last();
                    SetupDefaultConfigsAndConfigTable((name, VersionInformation.GetOrAddByName(name)));
                }
            }
            initialNextButtonIsEnabled = NextButton.IsEnabled;
            initialFinishButtonIsEnabled = FinishButton.IsEnabled;

            Validate();
        }

        private void SetupDefaultConfigsAndConfigTable((string Name, VersionInformation VersionInfo) version)
        {
            if (version.VersionInfo is not {} versionInfo)
                return;

            DefaultModules = QtModules.Instance.GetAvailableModules(versionInfo.Major)
                .Where(mi => mi.Selectable)
                .Select(mi => new Module
                {
                    Name = mi.Name,
                    Id = mi.proVarQT,
                    IsSelected = Data.DefaultModules.Contains(mi.LibraryPrefix),
                    IsReadOnly = Data.DefaultModules.Contains(mi.LibraryPrefix)
                }).ToList();

            defaultConfigs = new CloneableList<Config> {
                new() {
                    ConfigPage = this,
                    Name = "Debug",
                    IsDebug = true,
                    QtVersion = versionInfo,
                    QtVersionName = version.Name,
                    Target = versionInfo.IsWinRt
                        ? ProjectTargets.WindowsStore.Cast<string>()
                        : ProjectTargets.Windows.Cast<string>(),
                    Platform
                        = versionInfo.Platform == Platform.x86
                            ? ProjectPlatforms.Win32.Cast<string>()
                            : versionInfo.Platform == Platform.x64
                                ? ProjectPlatforms.X64.Cast<string>()
                                : versionInfo.Platform == Platform.arm64
                                    ? ProjectPlatforms.ARM64.Cast<string>()
                                    : string.Empty,
                    Modules = DefaultModules.ToDictionary(m => m.Name)
                },
                new() {
                    ConfigPage = this,
                    Name = "Release",
                    IsDebug = false,
                    QtVersion = versionInfo,
                    QtVersionName = version.Name,
                    Target = versionInfo.IsWinRt
                        ? ProjectTargets.WindowsStore.Cast<string>()
                        : ProjectTargets.Windows.Cast<string>(),
                    Platform
                        = versionInfo.Platform == Platform.x86
                            ? ProjectPlatforms.Win32.Cast<string>()
                            : versionInfo.Platform == Platform.x64
                                ? ProjectPlatforms.X64.Cast<string>()
                                : versionInfo.Platform == Platform.arm64
                                    ? ProjectPlatforms.ARM64.Cast<string>()
                                    : string.Empty,
                    Modules = DefaultModules.ToDictionary(m => m.Name)
                }
            };
            currentConfigs = defaultConfigs.Clone();
            ConfigTable.ItemsSource = currentConfigs;
        }

        /// <summary>
        /// Callback to validate selected configurations.
        /// Must return an error message in case of failed validation.
        /// Otherwise, return empty string or null.
        /// </summary>
        public Func<IEnumerable<IWizardConfiguration>, string> ValidateConfigs { get; set; }

        public bool BrowseQtVersion { get; set; }

        private void Validate()
        {
            var errorMessage = "";
            var errorPanelVisibility = Visibility.Visible;
            var browseQtVersion = false;
            var nextButtonIsEnabled = false;
            var finishButtonIsEnabled = false;

            if (currentConfigs == null) {
                errorMessage = "No registered Qt version found. Click here to browse for a Qt version.";
                browseQtVersion = true;
            } else if (currentConfigs // "$(Configuration)|$(Platform)" must be unique
                .GroupBy(c => $"{c.Name}|{c.Platform}")
                .Any(g => g.Count() > 1)) {
                errorMessage = "(Configuration, Platform) must be unique";
            } else if (ValidateConfigs?.Invoke(currentConfigs) is { Length: > 0 } errorMsg) {
                errorMessage = errorMsg;
            } else {
                errorPanelVisibility = Visibility.Hidden;
                nextButtonIsEnabled = initialNextButtonIsEnabled;
                finishButtonIsEnabled = initialFinishButtonIsEnabled;
            }

            ErrorMsg.Content = errorMessage;
            ErrorPanel.Visibility = errorPanelVisibility;
            BrowseQtVersion = browseQtVersion;
            NextButton.IsEnabled = nextButtonIsEnabled;
            FinishButton.IsEnabled = finishButtonIsEnabled;
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

        private static string BrowseForAndGetQtVersion()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "qmake|qmake.exe;qmake.bat"
            };
            if (openFileDialog.ShowDialog() != true)
                return null;

            IEnumerable<string> binPath = Path.GetDirectoryName(openFileDialog.FileName)
                ?.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            binPath ??= new List<string>();
            var lastDirName = binPath.LastOrDefault();
            if ("bin".Equals(lastDirName, IgnoreCase))
                binPath = binPath.Take(binPath.Count() - 1);
            return string.Join(Path.DirectorySeparatorChar.ToString(), binPath);
        }

        void QtVersion_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not ComboBox { IsEnabled: true } comboBoxQtVersion
                || GetBinding(comboBoxQtVersion) is not Config config
                || config.QtVersionName == comboBoxQtVersion.Text)
                return;

            var oldQtVersion = config.QtVersion;
            switch (comboBoxQtVersion.Text) {
            case QT_VERSION_DEFAULT:
                config.QtVersion = defaultVersion.VersionInfo;
                config.QtVersionName = defaultVersion.Name;
                config.QtVersionPath = defaultVersion.VersionInfo.QtDir;
                comboBoxQtVersion.Text = defaultVersion.Name;
                break;
            case QT_VERSION_BROWSE:
                if (BrowseForAndGetQtVersion() is {} qtVersion) {
                    if (VersionInformation.GetOrAddByPath(qtVersion) is {} versionInfo) {
                        config.QtVersion = versionInfo;
                        config.QtVersionName = qtVersion;
                        config.QtVersionPath = config.QtVersion.QtDir;
                    }
                }
                comboBoxQtVersion.Text = config.QtVersionName;
                break;
            default:
                if (QtVersionManager.GetVersions().Contains(comboBoxQtVersion.Text)) {
                    var path = QtVersionManager.GetInstallPath(comboBoxQtVersion.Text);
                    config.QtVersion = VersionInformation.GetOrAddByPath(path);
                    config.QtVersionName = comboBoxQtVersion.Text;
                    config.QtVersionPath = path;
                } else {
                    config.QtVersion = null;
                    config.QtVersionName = config.QtVersionPath = comboBoxQtVersion.Text;
                }
                break;
            }

            if (oldQtVersion != config.QtVersion) {
                if (config.QtVersion != null) {
                    config.Target = config.QtVersion.IsWinRt
                        ? ProjectTargets.WindowsStore.Cast<string>()
                        : ProjectTargets.Windows.Cast<string>();
                    config.Platform
                        = config.QtVersion.Platform == Platform.x86
                            ? ProjectPlatforms.Win32.Cast<string>()
                            : config.QtVersion.Platform == Platform.x64
                                ? ProjectPlatforms.X64.Cast<string>()
                                : config.QtVersion.Platform == Platform.arm64
                                    ? ProjectPlatforms.ARM64.Cast<string>()
                                    : string.Empty;
                    config.Modules =
                        QtModules.Instance.GetAvailableModules(config.QtVersion.Major)
                            .Where(mi => mi.Selectable)
                            .Select(mi => new Module
                            {
                                Name = mi.Name,
                                Id = mi.proVarQT,
                                IsSelected = Data.DefaultModules.Contains(mi.LibraryPrefix),
                                IsReadOnly = Data.DefaultModules.Contains(mi.LibraryPrefix)
                            }).ToDictionary(m => m.Name);
                } else if (config.QtVersionPath.StartsWith("SSH:")) {
                    config.Target = ProjectTargets.LinuxSSH.Cast<string>();
                } else if (config.QtVersionPath.StartsWith("WSL:")) {
                    config.Target = ProjectTargets.LinuxWSL.Cast<string>();
                }
                ConfigTable.Items.Refresh();
            }
            Validate();
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
            if (sender is ComboBox {IsEnabled: true} comboBoxTarget
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
            if (sender is ComboBox {IsEnabled: true} comboBoxPlatform
                && GetBinding(comboBoxPlatform) is Config config
                && config.Platform != comboBoxPlatform.Text) {
                config.Platform = comboBoxPlatform.Text;
                ConfigTable.Items.Refresh();
                Validate();
            }
        }

        void Debug_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox || GetBinding(checkBox) is not Config config)
                return;

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
            Data.ProjectModel = ProjectModel;
            Data.Configs = currentConfigs;
            base.OnNextButtonClick(sender, e);
        }

        protected override void OnFinishButtonClick(object sender, RoutedEventArgs e)
        {
            Data.ProjectModel = ProjectModel;
            Data.Configs = currentConfigs;
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
                if (control?.Name == name && control is {} result)
                    return result;
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(control); ++i) {
                    if (VisualTreeHelper.GetChild(control, i) is FrameworkElement child)
                        stack.Push(child);
                }
            }
            return null;
        }

        private void QtMSBuild_Selected(object sender, RoutedEventArgs e)
        {
            if (ConfigTable != null)
                ConfigTable.Columns.Last().Visibility = Visibility.Visible;
        }

        private void QtCMake_Selected(object sender, RoutedEventArgs e)
        {
            if (ConfigTable != null)
                ConfigTable.Columns.Last().Visibility = Visibility.Hidden;
        }

        private void ErrorMsg_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var qmakePath = BrowseForAndGetQtVersion();
            if (VersionInformation.GetOrAddByPath(qmakePath) is not {} versionInfo)
                return;

            var qtVersionDir = Path.GetDirectoryName(qmakePath);
            var versionName = $"{Path.GetFileName(qtVersionDir)}_{Path.GetFileName(qmakePath)}";

            try {
                QtVersionManager.SaveVersion(versionName, qmakePath);
                QtVersionManager.SaveDefaultVersion(versionName);
            } catch (Exception exception) {
                Messages.Print("Could not save Qt version.");
                exception.Log();
            }

            qtVersionList = new[] { QT_VERSION_BROWSE }.Union(QtVersionManager.GetVersions());

            SetupDefaultConfigsAndConfigTable((versionName, versionInfo));

            Validate();
        }
    }
}
