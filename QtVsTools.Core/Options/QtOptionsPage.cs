/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.Core.Options
{
    using Common;
    using Core;
    using VisualStudio;

    using static Common.Converters;
    using static QtVsTools.Common.EnumExt;

    public class QtOptionsPage : DialogPage
    {
        public enum QtMsBuild
        {
            [String("QtMsBuild_Path")] Path
        }

        public enum QmlDebug
        {
            [String("QMLDebug_Enable")] Enable,
            [String("QMLDebug_Timeout")] Timeout
        }

        public enum Help
        {
            [String("Help_Preference")] Preference,
            [String("Help_TryOnF1Pressed")] TryOnF1Pressed
        }

        public enum Designer
        {
            [String("Designer_Detached")] Detached
        }

        public enum Linguist
        {
            [String("Linguist_Detached")] Detached
        }

        public enum ResEditor
        {
            [String("ResourceEditor_Detached")] Detached
        }

        public enum BkgBuild
        {
            [String("BkgBuild_ProjectTracking")] ProjectTracking,
            [String("BkgBuild_RunQtTools")] RunQtTools,
            [String("BkgBuild_DebugInfo")] DebugInfo,
            [String("BkgBuild_LoggerVerbosity")] LoggerVerbosity
        }

        public enum Notifications
        {
            [String("Notifications_AutoActivatePane")] AutoActivatePane,
            [String("Notifications_Installed")] Installed,
            [String("Notifications_UpdateQtInstallation")] UpdateQtInstallation,
            [String("Notifications_UpdateProjectFormat")] UpdateProjectFormat,
            [String("Notifications_CMake_Incompatible")] CMakeIncompatible,
            [String("Notifications_CMake_Conversion")] CMakeConversion
        }

        public enum Natvis
        {
            [String("LinkNatvis")] Link
        }

        public enum QmlLsp
        {
            [String("QmlLsp_Enable")] Enable,
            [String("QmlLsp_QtVersion")] QtVersion,
            [String("QmlLsp_Log")] Log,
            [String("QmlLsp_LogSize")] LogSize
        }

        public enum Style
        {
            [String("Style_ColorTheme")] ColorTheme,
            [String("Style_CustomStylesheetPath")] StylesheetPath
        }

        public enum DevelopmentReleases
        {
            [String("NotifySearchDevRelease")] NotifySearchDevRelease,
            [String("SearchDevRelease")] SearchDevRelease,
            [String("SearchDevReleaseTimeout")]  SearchDevReleaseTimeout
        }

        public enum Timeout : uint { Disabled = 0 }

        private class TimeoutConverter : EnumConverter
        {
            public TimeoutConverter(Type t) : base(t)
            { }

            public override bool GetStandardValuesSupported(ITypeDescriptorContext c)
                => true;

            public override bool GetStandardValuesExclusive(ITypeDescriptorContext c)
                => false;

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext c)
                => new(new[] { Timeout.Disabled });

            public override object ConvertFrom(
                ITypeDescriptorContext context,
                CultureInfo culture,
                object value)
            {
                uint n = 0;
                try {
                    n = Convert.ToUInt32(value);
                } catch (Exception e) {
                    e.Log();
                }
                return (Timeout)n;
            }

            public override object ConvertTo(
                ITypeDescriptorContext context,
                CultureInfo culture,
                object value,
                Type destinationType)
            {
                if (destinationType == typeof(string))
                    return value?.ToString();
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        private class QmlLspProviderConverter : QtVersionConverter
        {
            protected override bool IsCompatible(string qtVersion)
            {
                return VersionInformation.GetOrAddByName(qtVersion) is {LibExecs: {}  libExecs}
                    && File.Exists(Path.Combine(libExecs, "qmlls.exe"));
            }
        }

        [Category("Qt/MSBuild")]
        [DisplayName("Path to Qt/MSBuild files")]
        [Description("Corresponds to the QTMSBUILD environment variable")]
        public string QtMsBuildPath { get; set; }

        [Category("QML Debugging")]
        [DisplayName("Process debug events")]
        [Description("Set to false to turn off processing of all debug events by the QML debug "
            + "engine, effectively excluding it from the debugging environment. Disabling the "
            + "QML debug engine will skip debugging of QML code for all projects.")]
        public bool QmlDebuggerEnabledOption
        {
            get => QmlDebuggerEnabled;
            set => QtOptionsPageSettings.Instance.SetValue(() => QmlDebuggerEnabled, value);
        }

        [Settings(QmlDebug.Enable, true)]
        public static bool QmlDebuggerEnabled =>
            QtOptionsPageSettings.Instance.GetValue(() => QmlDebuggerEnabled);

        [Category("QML Debugging")]
        [DisplayName("Runtime connection timeout (msecs)")]
        [TypeConverter(typeof(TimeoutConverter))]
        public Timeout QmlDebuggerTimeoutOption
        {
            get => QmlDebuggerTimeout;
            set => QtOptionsPageSettings.Instance.SetValue(() => QmlDebuggerTimeout, value);
        }

        [Settings(QmlDebug.Timeout, (Timeout)60000)]
        public static Timeout QmlDebuggerTimeout =>
            QtOptionsPageSettings.Instance.GetValue(() => QmlDebuggerTimeout);

        [Category("Help")]
        [DisplayName("Keyboard shortcut")]
        [Description("To change keyboard mapping, go to: Tools > Options > Keyboard")]
        [ReadOnly(true)]
        private string QtHelpKeyBinding { get; set; }

        public enum SourcePreference { Online, Offline }

        [Category("Help")]
        [DisplayName("Preferred source")]
        public SourcePreference HelpPreferenceOption
        {
            get => HelpPreference;
            set => QtOptionsPageSettings.Instance.SetValue(() => HelpPreference, value);
        }

        [Settings(Help.Preference, SourcePreference.Online)]
        public static SourcePreference HelpPreference =>
            QtOptionsPageSettings.Instance.GetValue(() => HelpPreference);

        [Category("Help")]
        [DisplayName("Try Qt documentation when F1 is pressed")]
        public bool TryQtHelpOnF1PressedOption
        {
            get => TryQtHelpOnF1Pressed;
            set => QtOptionsPageSettings.Instance.SetValue(() => TryQtHelpOnF1Pressed, value);
        }

        [Settings(Help.TryOnF1Pressed, true)]
        public static bool TryQtHelpOnF1Pressed =>
            QtOptionsPageSettings.Instance.GetValue(() => TryQtHelpOnF1Pressed);

        [Category("Qt Designer")]
        [DisplayName("Run in detached window")]
        public bool DesignerDetachedOption
        {
            get => DesignerDetached;
            set => QtOptionsPageSettings.Instance.SetValue(() => DesignerDetached, value);
        }

        [Settings(Designer.Detached, false)]
        public static bool DesignerDetached =>
            QtOptionsPageSettings.Instance.GetValue(() => DesignerDetached);

        [Category("Qt Linguist")]
        [DisplayName("Run in detached window")]
        public bool LinguistDetachedOption
        {
            get => LinguistDetached;
            set => QtOptionsPageSettings.Instance.SetValue(() => LinguistDetached, value);
        }

        [Settings(Linguist.Detached, false)]
        public static bool LinguistDetached =>
            QtOptionsPageSettings.Instance.GetValue(() => LinguistDetached);

        [Category("Qt Resource Editor")]
        [DisplayName("Run in detached window")]
        public bool ResourceEditorDetachedOption
        {
            get => ResourceEditorDetached;
            set => QtOptionsPageSettings.Instance.SetValue(() => ResourceEditorDetached, value);
        }

        [Settings(ResEditor.Detached, false)]
        public static bool ResourceEditorDetached =>
            QtOptionsPageSettings.Instance.GetValue(() => ResourceEditorDetached);

        [Category("IntelliSense")]
        [DisplayName("Auto project tracking")]
        [Description(
            "Enable this option to automatically keep track of project changes and trigger a"
            + " background build of Qt targets if required to keep IntelliSense updated.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool ProjectTrackingOption
        {
            get => ProjectTracking;
            set => QtOptionsPageSettings.Instance.SetValue(() => ProjectTracking, value);
        }

        [Settings(BkgBuild.ProjectTracking, true)]
        public static bool ProjectTracking =>
            QtOptionsPageSettings.Instance.GetValue(() => ProjectTracking);

        [Category("IntelliSense")]
        [DisplayName("Run Qt tools in background build")]
        [Description(
            "Enable this option to allow all Qt tools (e.g. moc, uic) to be invoked during a"
            + " background update of IntelliSense information. If disabled, only qmake will be"
            + " invoked during background builds, to update a minimal set of Qt build properties.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool BuildRunQtToolsOption
        {
            get => BuildRunQtTools;
            set => QtOptionsPageSettings.Instance.SetValue(() => BuildRunQtTools, value);
        }

        [Settings(BkgBuild.RunQtTools, true)]
        public static bool BuildRunQtTools =>
            QtOptionsPageSettings.Instance.GetValue(() => BuildRunQtTools);

        [Category("IntelliSense")]
        [DisplayName("Show debug information")]
        [Description("Enable this option to display debug information about IntelliSense updates.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool BuildDebugInformationOption
        {
            get => BuildDebugInformation;
            set => QtOptionsPageSettings.Instance.SetValue(() => BuildDebugInformation, value);
        }

        [Settings(BkgBuild.DebugInfo, false)]
        public static bool BuildDebugInformation =>
            QtOptionsPageSettings.Instance.GetValue(() => BuildDebugInformation);

        [Category("IntelliSense")]
        [DisplayName("Verbosity of background build log")]
        [Description("Configure verbosity level of background build log.")]
        public LoggerVerbosity BuildLoggerVerbosityOption
        {
            get => BuildLoggerVerbosity;
            set => QtOptionsPageSettings.Instance.SetValue(() => BuildLoggerVerbosity, value);
        }

        [Settings(BkgBuild.LoggerVerbosity, LoggerVerbosity.Quiet)]
        public static LoggerVerbosity BuildLoggerVerbosity
            => QtOptionsPageSettings.Instance.GetValue(() => BuildLoggerVerbosity);

        [Category("Notifications")]
        [DisplayName("Auto activate console pane")]
        [Description("Automatically activate the Qt Visual Studio Tools pane of the console on "
            + "new messages.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool AutoActivatePaneOption
        {
            get => AutoActivatePane;
            set => QtOptionsPageSettings.Instance.SetValue(() => AutoActivatePane, value);
        }

        [Settings(Notifications.AutoActivatePane, true)]
        public static bool AutoActivatePane
            => QtOptionsPageSettings.Instance.GetValue(() => AutoActivatePane);

        [Category("Notifications")]
        [DisplayName("New version installed")]
        [Description("Show notification when a new version was recently installed.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool NotifyInstalledOption
        {
            get => NotifyInstalled;
            set => NotifyInstalled = value;
        }

        [Settings(Notifications.Installed, true)]
        public static bool NotifyInstalled
        {
            get => QtOptionsPageSettings.Instance.GetValue(() => NotifyInstalled);
            set => QtOptionsPageSettings.Instance.SetValue(() => NotifyInstalled, value);
        }

        [Category("Notifications")]
        [DisplayName("Update Qt installation")]
        [Description("Show notification when a project uses an invalid Qt Installation.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool UpdateQtInstallationOption
        {
            get => UpdateQtInstallation;
            set => UpdateProjectFormat = value;
        }

        [Settings(Notifications.UpdateQtInstallation, true)]
        public static bool UpdateQtInstallation
        {
            get => QtOptionsPageSettings.Instance.GetValue(() => UpdateProjectFormat);
            set => QtOptionsPageSettings.Instance.SetValue(() => UpdateProjectFormat, value);
        }

        [Category("Notifications")]
        [DisplayName("Update project format")]
        [Description("Show notification when a project uses some legacy code path of the Qt "
            + "Visual Studio Tools.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool UpdateProjectFormatOption
        {
            get => UpdateProjectFormat;
            set => UpdateProjectFormat = value;
        }

        [Settings(Notifications.UpdateProjectFormat, true)]
        public static bool UpdateProjectFormat
        {
            get => QtOptionsPageSettings.Instance.GetValue(() => UpdateProjectFormat);
            set => QtOptionsPageSettings.Instance.SetValue(() => UpdateProjectFormat, value);
        }

        [Category("Notifications")]
        [DisplayName("CMake incompatible project")]
        [Description("Qt reference detected on a project using CMakeSettings.json.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool NotifyCMakeIncompatibleOption
        {
            get => NotifyCMakeIncompatible;
            set => NotifyCMakeIncompatible = value;
        }

        [Settings(Notifications.CMakeIncompatible, true)]
        public static bool NotifyCMakeIncompatible
        {
            get => QtOptionsPageSettings.Instance.GetValue(() => NotifyCMakeIncompatible);
            set => QtOptionsPageSettings.Instance.SetValue(() => NotifyCMakeIncompatible, value);
        }

        [Category("Notifications")]
        [DisplayName("CMake conversion confirmation")]
        [Description("Qt reference detected: ask to confirm conversion to Qt/CMake.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool NotifyCMakeConversionOption
        {
            get => NotifyCMakeConversion;
            set => NotifyCMakeConversion = value;
        }

        [Settings(Notifications.CMakeConversion, true)]
        public static bool NotifyCMakeConversion
        {
            get => QtOptionsPageSettings.Instance.GetValue(() => NotifyCMakeConversion);
            set => QtOptionsPageSettings.Instance.SetValue(() => NotifyCMakeConversion, value);
        }

        [Category("Natvis")]
        [DisplayName("Embed .natvis file into PDB")]
        [Description("Embeds the debugger visualizations (.natvis file) into the PDB file "
            + "generated by LINK. While setting this option, the embedded Natvis file will "
            + "take precedence over user-specific Natvis files(for example the files "
            + @"located in %USERPROFILE%\Documents\Visual Studio 2022\Visualizers).")]
        [TypeConverter(typeof(EnableDisableConverter))]
        [Settings(Natvis.Link, true)]
        public bool LinkNatvis
        {
            get => QtOptionsPageSettings.Instance.GetValue(() => LinkNatvis);
            set => QtOptionsPageSettings.Instance.SetValue(() => LinkNatvis, value);
        }

        [Category("QML Language Server")]
        [DisplayName("Enable")]
        [Description("Connect to a QML Language Server for enhanced code editing experience. "
            + "Restarting Visual Studio might be required after enabling the QML Language Server.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool QmlLspEnableOption
        {
            get => QmlLspEnable;
            set => QtOptionsPageSettings.Instance.SetValue(() => QmlLspEnable, value);
        }

        [Settings(QmlLsp.Enable, false)]
        public static bool QmlLspEnable
             => QtOptionsPageSettings.Instance.GetValue(() => QmlLspEnable);

        [Category("QML Language Server")]
        [DisplayName("Qt Version")]
        [Description("Look for a QML Language Server in the specified Qt installation.")]
        [TypeConverter(typeof(QmlLspProviderConverter))]
        public string QmlLspVersionOption
        {
            get => QmlLspVersion;
            set => QtOptionsPageSettings.Instance.SetValue(() => QmlLspVersion, value);
        }

        [Settings(QmlLsp.QtVersion, "$(DefaultQtVersion)")]
        public static string QmlLspVersion
            => QtOptionsPageSettings.Instance.GetValue(() => QmlLspVersion);

        [Category("QML Language Server")]
        [DisplayName("Log")]
        [Description("Write exchanged LSP messages to log file in %TEMP%.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool QmlLspLogOption
        {
            get => QmlLspLog;
            set => QtOptionsPageSettings.Instance.SetValue(() => QmlLspLog, value);
        }

        [Settings(QmlLsp.Log, false)]
        public static bool QmlLspLog
            => QtOptionsPageSettings.Instance.GetValue(() => QmlLspLog);

        [Category("QML Language Server")]
        [DisplayName("Log Size")]
        [Description("Maximum size (in KB) of QML LSP log file.")]
        public int QmlLspLogSizeOption
        {
            get => QmlLspLogSize;
            set => QtOptionsPageSettings.Instance.SetValue(() => QmlLspLogSize, value);
        }

        [Settings(QmlLsp.LogSize, 2500)]
        public static int QmlLspLogSize
            => QtOptionsPageSettings.Instance.GetValue(() => QmlLspLogSize);

        public enum EditorColorTheme
        {
            Consistent,
            Dark,
            Light
        }

        [Category("Style")]
        [DisplayName("Color theme")]
        [Description("Color theme used in editors (Qt Designer, Qt Linguist, Qt Resource Editor). "
            + "By default consistent with the Visual Studio color theme.")]
        public EditorColorTheme ColorThemeOption
        {
            get => QtOptionsPageSettings.Instance.GetValue(() => ColorTheme);
            set => QtOptionsPageSettings.Instance.SetValue(() => ColorTheme, value);
        }

        [Settings(Style.ColorTheme, EditorColorTheme.Consistent)]
        public static EditorColorTheme ColorTheme =>
            QtOptionsPageSettings.Instance.GetValue(() => ColorTheme);

        [Category("Style")]
        [DisplayName("Path to stylesheet")]
        [Description("Path to stylesheet used in editors (Qt Designer, Qt Linguist, Qt Resource "
            + "Editor).")]
        public string StylesheetPathOption
        {
            get => StylesheetPath;
            set => QtOptionsPageSettings.Instance.SetValue(() => StylesheetPath, value);
        }

        [Settings(Style.StylesheetPath, "")]
        public static string StylesheetPath =>
            QtOptionsPageSettings.Instance.GetValue(() => StylesheetPath);

        [Category("Development releases")]
        [DisplayName("Notification")]
        [Description("Show a notification to allow the user to enable automatic searching for "
            + "development releases.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool NotifySearchDevReleaseOption
        {
            get => NotifySearchDevRelease;
            set => NotifySearchDevRelease = value;
        }

        [Settings(DevelopmentReleases.NotifySearchDevRelease, true)]
        public static bool NotifySearchDevRelease
        {
            get => QtOptionsPageSettings.Instance.GetValue(() => NotifySearchDevRelease);
            set => QtOptionsPageSettings.Instance.SetValue(() => NotifySearchDevRelease, value);
        }

        [Category("Development releases")]
        [DisplayName("Search automatically")]
        [Description("If enabled, automatically searches for development releases on "
            + "Visual Studio startup.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool SearchDevReleaseOption
        {
            get => SearchDevRelease;
            set => SearchDevRelease = value;
        }

        [Settings(DevelopmentReleases.SearchDevRelease, false)]
        public static bool SearchDevRelease
        {
            get => QtOptionsPageSettings.Instance.GetValue(() => SearchDevRelease);
            set => QtOptionsPageSettings.Instance.SetValue(() => SearchDevRelease, value);
        }

        [Category("Development releases")]
        [DisplayName("Search timeout")]
        [Description("Sets the time in seconds to wait before the search request for development "
            + "releases times out.")]
        public int SearchDevReleaseTimeoutOption
        {
            get => SearchDevReleaseTimeout;
            set => QtOptionsPageSettings.Instance.SetValue(() => SearchDevReleaseTimeout, value);
        }

        [Settings(DevelopmentReleases.SearchDevReleaseTimeout, 3)]
        public static int SearchDevReleaseTimeout =>
            QtOptionsPageSettings.Instance.GetValue(() => SearchDevReleaseTimeout);

        public override void LoadSettingsFromStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try {
                ////////
                // Get Qt Help keyboard shortcut
                //
                var dte = VsServiceProvider.GetService<SDTE, DTE>();
                var f1QtHelpBindings = dte.Commands.Item("QtVSTools.F1QtHelp")?.Bindings as Array;
                var binding = f1QtHelpBindings?.Cast<string>()
                    .Select(x => x.Split(new[] { "::" }, StringSplitOptions.None))
                    .Select(x => new { Scope = x.FirstOrDefault(), Shortcut = x.LastOrDefault() })
                    .FirstOrDefault();
                QtHelpKeyBinding = binding != null ? $"[{binding.Scope}] {binding.Shortcut}" : "";
                QtMsBuildPath = Environment.GetEnvironmentVariable("QTMSBUILD");
            } catch (Exception exception) {
                exception.Log();
            }
        }

        public override void SaveSettingsToStorage()
        {
            try {
                if (!string.IsNullOrEmpty(QtMsBuildPath)
                    && QtMsBuildPath != Environment.GetEnvironmentVariable("QTMSBUILD")) {
                    Environment.SetEnvironmentVariable(
                        "QTMSBUILD", QtMsBuildPath, EnvironmentVariableTarget.User);
                    Environment.SetEnvironmentVariable(
                        "QTMSBUILD", QtMsBuildPath, EnvironmentVariableTarget.Process);
                }

                if (QmlLspLogSizeOption < 100)
                    QmlLspLogSizeOption = 100;
                SaveSettingsToStorageStatic();
            } catch (Exception exception) {
                exception.Log();
            }
        }

        public static void SaveSettingsToStorageStatic()
        {
            QtOptionsPageSettings.Instance.SaveSettings();
        }
    }
}
