// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Design;
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

    public partial class QtOptionsPage : DialogPage
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


        public enum Natvis
        {
            [String("LinkNatvis")] Link
        }

        public enum QmlLanguageServer
        {
            [String("QmlLsp_Enable")] Enable,
            [String("QmlLsp_Path")] Path,
            [String("QmlLsp_Log")] Log,
            [String("QmlLsp_LogSize")] LogSize
        }

        public enum Style
        {
            [String("Style_ColorTheme")] ColorTheme,
            [String("Style_CustomStylesheetPath")] StylesheetPath
        }

        public enum Telemetry
        {
            [String("Telemetry_Enable")] Enable
        }

        public enum DevelopmentReleases
        {
            [String("SearchDevRelease")] SearchDevRelease,
            [String("SearchDevReleaseTimeout")] SearchDevReleaseTimeout
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

        private class QmlLanguageServerPathEditor : FileNameEditor
        {
            protected override void InitializeDialog(OpenFileDialog openFileDialog)
            {
                openFileDialog.Title = "Select QML Language Server";
                openFileDialog.Filter = "QML Language Server (qmlls)|qmlls;qmlls.exe";
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
        [DisplayName("Runtime connection timeout (ms)")]
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
        public bool QmlLanguageServerEnableOption
        {
            get => QmlLanguageServerEnable;
            set => QtOptionsPageSettings.Instance.SetValue(() => QmlLanguageServerEnable, value);
        }

        [Settings(QmlLanguageServer.Enable, false)]
        public static bool QmlLanguageServerEnable
             => QtOptionsPageSettings.Instance.GetValue(() => QmlLanguageServerEnable);

        [Category("QML Language Server")]
        [DisplayName("QML Language Server path")]
        [Description("Select the QML Language Server to use. Leave the path empty to use default "
            + "provided one by the Qt VS Tools extension, which includes the latest features and "
            + "fixes.")]
        [Editor(typeof(QmlLanguageServerPathEditor), typeof(UITypeEditor))]
        public string QmlLanguageServerPathOption
        {
            get => QmlLanguageServerPath;
            set => QtOptionsPageSettings.Instance.SetValue(() => QmlLanguageServerPath, value);
        }

        [Settings(QmlLanguageServer.Path, "")]
        public static string QmlLanguageServerPath
            => QtOptionsPageSettings.Instance.GetValue(() => QmlLanguageServerPath);

        [Category("QML Language Server")]
        [DisplayName("Log")]
        [Description("Write exchanged LSP messages to log file in %TEMP%.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool QmlLanguageServerLogOption
        {
            get => QmlLanguageServerLog;
            set => QtOptionsPageSettings.Instance.SetValue(() => QmlLanguageServerLog, value);
        }

        [Settings(QmlLanguageServer.Log, false)]
        public static bool QmlLanguageServerLog
            => QtOptionsPageSettings.Instance.GetValue(() => QmlLanguageServerLog);

        [Category("QML Language Server")]
        [DisplayName("Log Size")]
        [Description("Maximum size (in KB) of QML LSP log file.")]
        public int QmlLanguageServerLogSizeOption
        {
            get => QmlLanguageServerLogSize;
            set => QtOptionsPageSettings.Instance.SetValue(() => QmlLanguageServerLogSize, value);
        }

        [Settings(QmlLanguageServer.LogSize, 2500)]
        public static int QmlLanguageServerLogSize
            => QtOptionsPageSettings.Instance.GetValue(() => QmlLanguageServerLogSize);

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

        [Category("Telemetry")]
        [DisplayName("Enable")]
        [Description("Enable telemetry.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool TelemetryEnableOption
        {
            get => TelemetryEnable;
            set => QtOptionsPageSettings.Instance.SetValue(() => TelemetryEnable, value);
        }

        [Settings(Telemetry.Enable, true)]
        public static bool TelemetryEnable
            => QtOptionsPageSettings.Instance.GetValue(() => TelemetryEnable);

        [Category("Development releases")]
        [DisplayName("Search automatically")]
        [Description("If enabled, runs once every 24 hours after Visual Studio has been idle for "
            + "60 seconds.")]
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
        [DisplayName("Search timeout in seconds")]
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

                if (QmlLanguageServerLogSizeOption < 100)
                    QmlLanguageServerLogSizeOption = 100;
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
