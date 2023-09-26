/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using EnvDTE;

namespace QtVsTools.Core.Options
{
    using Core;
    using VisualStudio;
    using static Utils;
    using static Common.EnumExt;

    public static class Options
    {
        public static QtOptionsPage Get()
        {
            if (VsServiceProvider.Instance is not AsyncPackage package)
                return null;
            return package.GetDialogPage(typeof(QtOptionsPage)) as QtOptionsPage;
        }
    }

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
            [String("Notifications_Installed")] Installed,
            [String("Notifications_UpdateProjectFormat")] UpdateProjectFormat,
            [String("Notifications_CMake_Incompatible")] CMakeIncompatible,
            [String("Notifications_CMake_Conversion")] CMakeConversion
        }

        public enum Natvis
        {
            [String("LinkNatvis")] Link
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

        private class EnableDisableConverter : BooleanConverter
        {
            public override object ConvertFrom(
                ITypeDescriptorContext context,
                CultureInfo culture,
                object value)
            {
                return string.Equals(value as string, "Enable", IgnoreCase);
            }

            public override object ConvertTo(
                ITypeDescriptorContext context,
                CultureInfo culture,
                object value,
                Type destinationType)
            {
                if (value is bool b && destinationType == typeof(string))
                    return b ? "Enable" : "Disable";
                return base.ConvertTo(context, culture, value, destinationType);
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
        public bool QmlDebuggerEnabled { get; set; }

        [Category("QML Debugging")]
        [DisplayName("Runtime connection timeout (msecs)")]
        [TypeConverter(typeof(TimeoutConverter))]
        public Timeout QmlDebuggerTimeout { get; set; }

        [Category("Help")]
        [DisplayName("Keyboard shortcut")]
        [Description("To change keyboard mapping, go to: Tools > Options > Keyboard")]
        [ReadOnly(true)]
        private string QtHelpKeyBinding { get; set; }

        public enum SourcePreference { Online, Offline }

        [Category("Help")]
        [DisplayName("Preferred source")]
        public SourcePreference HelpPreference { get; set; }

        [Category("Help")]
        [DisplayName("Try Qt documentation when F1 is pressed")]
        public bool TryQtHelpOnF1Pressed { get; set; }

        [Category("Qt Designer")]
        [DisplayName("Run in detached window")]
        public bool DesignerDetached { get; set; }

        [Category("Qt Linguist")]
        [DisplayName("Run in detached window")]
        public bool LinguistDetached { get; set; }

        [Category("Qt Resource Editor")]
        [DisplayName("Run in detached window")]
        public bool ResourceEditorDetached { get; set; }

        [Category("IntelliSense")]
        [DisplayName("Auto project tracking")]
        [Description(
            "Enable this option to automatically keep track of project changes and trigger a"
            + " background build of Qt targets if required to keep IntelliSense updated.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool ProjectTracking { get; set; }

        [Category("IntelliSense")]
        [DisplayName("Run Qt tools in background build")]
        [Description(
            "Enable this option to allow all Qt tools (e.g. moc, uic) to be invoked during a"
            + " background update of IntelliSense information. If disabled, only qmake will be"
            + " invoked during background builds, to update a minimal set of Qt build properties.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool BuildRunQtTools { get; set; }

        [Category("IntelliSense")]
        [DisplayName("Show debug information")]
        [Description("Enable this option to display debug information about IntelliSense updates.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool BuildDebugInformation { get; set; }

        [Category("IntelliSense")]
        [DisplayName("Verbosity of background build log")]
        [Description("Configure verbosity level of background build log.")]
        public LoggerVerbosity BuildLoggerVerbosity { get; set; }

        [Category("Notifications")]
        [DisplayName("New version installed")]
        [Description("Show notification when a new version was recently installed.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool NotifyInstalled { get; set; }

        [Category("Notifications")]
        [DisplayName("Update project format")]
        [Description("Show notification when a project uses some legacy code path of the Qt "
            + "Visual Studio Tools.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool UpdateProjectFormat
        {
            get; set;
        }

        [Category("Notifications")]
        [DisplayName("CMake incompatible project")]
        [Description("Qt reference detected on a project using CMakeSettings.json.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool NotifyCMakeIncompatible { get; set; }

        [Category("Notifications")]
        [DisplayName("CMake conversion confirmation")]
        [Description("Qt reference detected: ask to confirm conversion to Qt/CMake.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool NotifyCMakeConversion { get; set; }

        [Category("Natvis")]
        [DisplayName("Embed .natvis file into PDB")]
        [Description("Embeds the debugger visualizations (.natvis file) into the PDB file"
            + "generated by LINK. While setting this option, the embedded Natvis file will"
            + "take precedence over user-specific Natvis files(for example the files"
            + "located in %USERPROFILE%\\Documents\\Visual Studio 2022\\Visualizers).")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool LinkNatvis { get; set; }

        public override void ResetSettings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            QtMsBuildPath = "";
            QmlDebuggerEnabled = true;
            QmlDebuggerTimeout = (Timeout)60000;
            HelpPreference = SourcePreference.Online;
            TryQtHelpOnF1Pressed = true;
            DesignerDetached = LinguistDetached = ResourceEditorDetached = false;

            BuildRunQtTools = ProjectTracking = true;
            BuildDebugInformation = false;
            BuildLoggerVerbosity = LoggerVerbosity.Quiet;
            NotifyInstalled = true;
            UpdateProjectFormat = true;
            NotifyCMakeIncompatible = true;
            NotifyCMakeConversion = true;
            LinkNatvis = true;

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
        }

        public override void LoadSettingsFromStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ResetSettings();
            try {
                QtMsBuildPath = Environment.GetEnvironmentVariable("QTMSBUILD");

                using var key = Registry.CurrentUser
                    .OpenSubKey(@"SOFTWARE\" + Resources.registryPackagePath, writable: false);
                if (key == null)
                    return;
                Load(() => QmlDebuggerEnabled, key, QmlDebug.Enable);
                Load(() => QmlDebuggerTimeout, key, QmlDebug.Timeout);
                Load(() => HelpPreference, key, Help.Preference);
                Load(() => TryQtHelpOnF1Pressed, key, Help.TryOnF1Pressed);
                Load(() => DesignerDetached, key, Designer.Detached);
                Load(() => LinguistDetached, key, Linguist.Detached);
                Load(() => ResourceEditorDetached, key, ResEditor.Detached);
                Load(() => ProjectTracking, key, BkgBuild.ProjectTracking);
                Load(() => BuildRunQtTools, key, BkgBuild.RunQtTools);
                Load(() => BuildDebugInformation, key, BkgBuild.DebugInfo);
                Load(() => BuildLoggerVerbosity, key, BkgBuild.LoggerVerbosity);
                Load(() => NotifyInstalled, key, Notifications.Installed);
                Load(() => NotifyCMakeIncompatible, key, Notifications.CMakeIncompatible);
                Load(() => NotifyCMakeConversion, key, Notifications.CMakeConversion);
                Load(() => UpdateProjectFormat, key, Notifications.UpdateProjectFormat);
                Load(() => LinkNatvis, key, Natvis.Link);
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

                using var key = Registry.CurrentUser
                    .CreateSubKey(@"SOFTWARE\" + Resources.registryPackagePath);
                if (key == null)
                    return;
                Save(QmlDebuggerEnabled, key, QmlDebug.Enable);
                Save(QmlDebuggerTimeout, key, QmlDebug.Timeout);
                Save(HelpPreference, key, Help.Preference);
                Save(TryQtHelpOnF1Pressed, key, Help.TryOnF1Pressed);
                Save(DesignerDetached, key, Designer.Detached);
                Save(LinguistDetached, key, Linguist.Detached);
                Save(ResourceEditorDetached, key, ResEditor.Detached);
                Save(ProjectTracking, key, BkgBuild.ProjectTracking);
                Save(BuildRunQtTools, key, BkgBuild.RunQtTools);
                Save(BuildDebugInformation, key, BkgBuild.DebugInfo);
                Save(BuildLoggerVerbosity, key, BkgBuild.LoggerVerbosity);
                Save(NotifyInstalled, key, Notifications.Installed);
                Save(NotifyCMakeIncompatible, key, Notifications.CMakeIncompatible);
                Save(NotifyCMakeConversion, key, Notifications.CMakeConversion);
                Save(UpdateProjectFormat, key, Notifications.UpdateProjectFormat);
                Save(LinkNatvis, key, Natvis.Link);
            } catch (Exception exception) {
                exception.Log();
            }
        }

        private static void Save<T>(T property, RegistryKey key, Enum name)
        {
            object value = property;
            if (Equals<T, bool>())
                value = (bool)(object)property ? 1 : 0;
            else if (Equals<T, Timeout>())
                value = Convert.ToInt32(property);
            else if (typeof(T).IsEnum)
                value = Enum.GetName(typeof(T), property);
            if (value != null)
                key.SetValue(name.Cast<string>(), value);
        }

        private void Load<T>(Expression<Func<T>> propertyByRef, RegistryKey key, Enum name)
        {
            var propertyExpr = (MemberExpression)propertyByRef.Body;
            var property = (PropertyInfo)propertyExpr.Member;
            var regValue = key.GetValue(name.Cast<string>());
            if (Equals<T, bool>() && regValue is int numValue)
                property.SetValue(this, numValue == 1);
            else if (Equals<T, Timeout>() && regValue is int timeout)
                property.SetValue(this, (Timeout)timeout);
            else if (typeof(T).IsEnum && regValue is string enumValue)
                property.SetValue(this, Enum.Parse(typeof(T), enumValue));
            else if (regValue is T value)
                property.SetValue(this, value);
        }

        private static bool Equals<T1, T2>()
        {
            return typeof(T1) == typeof(T2);
        }
    }
}
