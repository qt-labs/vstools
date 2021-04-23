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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using QtVsTools.Core;
using QtVsTools.Common;
using QtVsTools.VisualStudio;
using System.Reflection;
using System.Linq.Expressions;

namespace QtVsTools.Options
{
    using static EnumExt;

    public class QtOptionsPage : DialogPage, IQtVsToolsOptions
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

        public enum IntelliSense
        {
            [String("IntelliSense_OnBuild")] OnBuild,
            [String("IntelliSense_OnUiFile")] OnUiFile
        }

        public enum Help
        {
            [String("Help_Preference")] Preference,
            [String("Help_TryOnF1Pressed")] TryOnF1Pressed
        }

        public enum Designer
        {
            [String("Designer_Detached")] Detached,
        }

        public enum Linguist
        {
            [String("Linguist_Detached")] Detached,
        }

        public enum ResEditor
        {
            [String("ResourceEditor_Detached")] Detached,
        }

        public enum BkgBuild
        {
            [String("BkgBuild_OnProjectCreated")] OnProjectCreated,
            [String("BkgBuild_OnProjectOpened")] OnProjectOpened,
            [String("BkgBuild_OnProjectChanged")] OnProjectChanged,
            [String("BkgBuild_OnBuildComplete")] OnBuildComplete,
            [String("BkgBuild_OnUiFileAdded")] OnUiFileAdded,
            [String("BkgBuild_OnUiFileSaved")] OnUiFileSaved,
            [String("BkgBuild_DebugInfo")] DebugInfo
        }

        public enum Timeout : uint { Disabled = 0 }

        class TimeoutConverter : EnumConverter
        {
            public TimeoutConverter(Type t) : base(t)
            { }

            public override bool GetStandardValuesSupported(ITypeDescriptorContext c)
                => true;

            public override bool GetStandardValuesExclusive(ITypeDescriptorContext c)
                => false;

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext c)
                => new StandardValuesCollection(new[] { Timeout.Disabled });

            public override object ConvertFrom(
                ITypeDescriptorContext context,
                CultureInfo culture,
                object value)
            {
                uint n = 0;
                try {
                    n = Convert.ToUInt32(value);
                } catch { }
                return (Timeout)n;
            }

            public override object ConvertTo(
                ITypeDescriptorContext context,
                CultureInfo culture,
                object value,
                Type destinationType)
            {
                if (destinationType == typeof(string))
                    return value.ToString();
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        class EnableDisableConverter : BooleanConverter
        {
            public override object ConvertFrom(
                ITypeDescriptorContext context,
                CultureInfo culture,
                object value)
            {
                return string
                    .Equals(value as string, "Enable", StringComparison.InvariantCultureIgnoreCase);
            }

            public override object ConvertTo(
                ITypeDescriptorContext context,
                CultureInfo culture,
                object value,
                Type destinationType)
            {
                if (value.GetType() == typeof(bool) && destinationType == typeof(string))
                    return ((bool)value) ? "Enable" : "Disable";
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        [Category("Qt/MSBuild")]
        [DisplayName("Path to Qt/MSBuild files")]
        [Description("Corresponds to the QTMSBUILD environment variable")]
        public string QtMsBuildPath { get; set; }

        [Category("QML Debugging")]
        [DisplayName("Process debug events")]
        [Description("Set to false to turn off processing of all debug events by the QML debug engine, effectively excluding it from the debugging environment. Disabling the QML debug engine will skip debugging of QML code for all projects.")]
        public bool QmlDebuggerEnabled { get; set; }

        [Category("QML Debugging")]
        [DisplayName("Runtime connection timeout (msecs)")]
        [TypeConverter(typeof(TimeoutConverter))]
        public Timeout QmlDebuggerTimeout { get; set; }
        int IQtVsToolsOptions.QmlDebuggerTimeout => (int)QmlDebuggerTimeout;

        [Category("IntelliSense")]
        [DisplayName("Refresh after build")]
        public bool RefreshIntelliSenseOnBuild { get; set; }

        [Category("IntelliSense")]
        [DisplayName("Refresh after changes to UI file")]
        public bool RefreshIntelliSenseOnUiFile { get; set; }

        [Category("Help")]
        [DisplayName("Keyboard shortcut")]
        [Description("To change keyboard mapping, go to: Tools > Options > Keyboard")]
        [ReadOnly(true)]
        public string QtHelpKeyBinding { get; set; }

        [Category("Help")]
        [DisplayName("Preferred source")]
        public QtHelp.SourcePreference HelpPreference { get; set; }
        bool IQtVsToolsOptions.HelpPreferenceOnline
            => (HelpPreference == QtHelp.SourcePreference.Online);

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

        [Category("Background Build")]
        [DisplayName("On project created")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool BuildOnProjectCreated { get; set; }

        [Category("Background Build")]
        [DisplayName("On project opened")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool BuildOnProjectOpened { get; set; }

        [Category("Background Build")]
        [DisplayName("On project changed")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool BuildOnProjectChanged { get; set; }

        [Category("Background Build")]
        [DisplayName("On project build complete")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool BuildOnProjectBuildComplete { get; set; }

        [Category("Background Build")]
        [DisplayName("On .ui file added")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool BuildOnUiFileAdded { get; set; }

        [Category("Background Build")]
        [DisplayName("On .ui file changed")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool BuildOnUiFileChanged { get; set; }

        [Category("Background Build")]
        [DisplayName("Show debug information")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool BuildDebugInformation { get; set; }

        public override void ResetSettings()
        {
            QtMsBuildPath = "";
            QmlDebuggerEnabled = true;
            QmlDebuggerTimeout = (Timeout)60000;
            RefreshIntelliSenseOnBuild = true;
            RefreshIntelliSenseOnUiFile = true;
            HelpPreference = QtHelp.SourcePreference.Online;
            TryQtHelpOnF1Pressed = true;
            DesignerDetached = LinguistDetached = ResourceEditorDetached = false;

            BuildOnProjectCreated = BuildOnProjectOpened = BuildOnProjectChanged
                = BuildOnProjectBuildComplete
                = BuildOnUiFileAdded = BuildOnUiFileChanged
                = true;
            BuildDebugInformation = false;

            ////////
            // Get Qt Help keyboard shortcut
            //
            var dte = VsServiceProvider.GetService<SDTE, DTE>();
            var f1QtHelpBindings = dte.Commands.Item("QtVSTools.F1QtHelp")?.Bindings as Array;
            var binding = f1QtHelpBindings.Cast<string>()
                .Select(x => x.Split(new[] { "::" }, StringSplitOptions.None))
                .Select(x => new { Scope = x.FirstOrDefault(), Shortcut = x.LastOrDefault() })
                .FirstOrDefault();
            if (binding != null)
                QtHelpKeyBinding = string.Format("[{0}] {1}", binding.Scope, binding.Shortcut);
            else
                QtHelpKeyBinding = "";
        }

        public override void LoadSettingsFromStorage()
        {
            ResetSettings();
            try {
                QtMsBuildPath = Environment.GetEnvironmentVariable("QTMSBUILD");

                using (var key = Registry.CurrentUser
                    .OpenSubKey(@"SOFTWARE\" + Resources.registryPackagePath, writable: false)) {
                    if (key == null)
                        return;
                    Load(() => QmlDebuggerEnabled, key, QmlDebug.Enable);
                    Load(() => QmlDebuggerTimeout, key, QmlDebug.Timeout);
                    Load(() => RefreshIntelliSenseOnBuild, key, IntelliSense.OnBuild);
                    Load(() => RefreshIntelliSenseOnUiFile, key, IntelliSense.OnUiFile);
                    Load(() => HelpPreference, key, Help.Preference);
                    Load(() => TryQtHelpOnF1Pressed, key, Help.TryOnF1Pressed);
                    Load(() => DesignerDetached, key, Designer.Detached);
                    Load(() => LinguistDetached, key, Linguist.Detached);
                    Load(() => ResourceEditorDetached, key, ResEditor.Detached);
                    Load(() => BuildOnProjectCreated, key, BkgBuild.OnProjectCreated);
                    Load(() => BuildOnProjectOpened, key, BkgBuild.OnProjectOpened);
                    Load(() => BuildOnProjectChanged, key, BkgBuild.OnProjectChanged);
                    Load(() => BuildOnProjectBuildComplete, key, BkgBuild.OnBuildComplete);
                    Load(() => BuildOnUiFileAdded, key, BkgBuild.OnUiFileAdded);
                    Load(() => BuildOnUiFileChanged, key, BkgBuild.OnUiFileSaved);
                    Load(() => BuildDebugInformation, key, BkgBuild.DebugInfo);
                }
            } catch (Exception exception) {
                Messages.Print(
                    exception.Message + "\r\n\r\nStacktrace:\r\n" + exception.StackTrace);
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

                using (var key = Registry.CurrentUser
                    .CreateSubKey(@"SOFTWARE\" + Resources.registryPackagePath)) {
                    if (key == null)
                        return;
                    Save(QmlDebuggerEnabled, key, QmlDebug.Enable);
                    Save(QmlDebuggerTimeout, key, QmlDebug.Timeout);
                    Save(RefreshIntelliSenseOnBuild, key, IntelliSense.OnBuild);
                    Save(RefreshIntelliSenseOnUiFile, key, IntelliSense.OnUiFile);
                    Save(HelpPreference, key, Help.Preference);
                    Save(TryQtHelpOnF1Pressed, key, Help.Preference);
                    Save(DesignerDetached, key, Designer.Detached);
                    Save(LinguistDetached, key, Linguist.Detached);
                    Save(ResourceEditorDetached, key, ResEditor.Detached);
                    Save(BuildOnProjectCreated, key, BkgBuild.OnProjectCreated);
                    Save(BuildOnProjectOpened, key, BkgBuild.OnProjectOpened);
                    Save(BuildOnProjectChanged, key, BkgBuild.OnProjectChanged);
                    Save(BuildOnProjectBuildComplete, key, BkgBuild.OnBuildComplete);
                    Save(BuildOnUiFileAdded, key, BkgBuild.OnUiFileAdded);
                    Save(BuildOnUiFileChanged, key, BkgBuild.OnUiFileSaved);
                    Save(BuildDebugInformation, key, BkgBuild.DebugInfo);
                }
            } catch (Exception exception) {
                Messages.Print(
                    exception.Message + "\r\n\r\nStacktrace:\r\n" + exception.StackTrace);
            }
        }

        void Save<T>(T property, RegistryKey key, Enum name)
        {
            object value = property;
            if (Equals<T, bool>())
                value = ((bool)(object)property) ? 1 : 0;
            else if (Equals<T, Timeout>())
                value = Convert.ToInt32(property);
            else if (typeof(T).IsEnum)
                value = Enum.GetName(typeof(T), property);
            key.SetValue(name.Cast<string>(), value);
        }

        void Load<T>(Expression<Func<T>> propertyByRef, RegistryKey key, Enum name)
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

        bool Equals<T1, T2>()
        {
            return typeof(T1) == typeof(T2);
        }
    }
}
