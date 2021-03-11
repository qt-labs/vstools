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
                    if (key.GetValue(QmlDebug.Enable.Cast<string>()) is int qmlDebugEnabled)
                        QmlDebuggerEnabled = (qmlDebugEnabled != 0);
                    if (key.GetValue(QmlDebug.Timeout.Cast<string>()) is int qmlDebugTimeout)
                        QmlDebuggerTimeout = (Timeout)qmlDebugTimeout;
                    if (key.GetValue(IntelliSense.OnBuild.Cast<string>()) is int iSenseOnBuild)
                        RefreshIntelliSenseOnBuild = (iSenseOnBuild != 0);
                    if (key.GetValue(IntelliSense.OnUiFile.Cast<string>()) is int iSenseOnUiFile)
                        RefreshIntelliSenseOnUiFile = (iSenseOnUiFile != 0);
                    if (key.GetValue(Help.Preference.Cast<string>()) is string preference)
                        HelpPreference = EnumExt.Cast(preference, QtHelp.SourcePreference.Online);
                    if (key.GetValue(Help.TryOnF1Pressed.Cast<string>()) is int tryOnF1)
                        TryQtHelpOnF1Pressed = (tryOnF1 != 0);
                    if (key.GetValue(Designer.Detached.Cast<string>()) is int designerDetached)
                        DesignerDetached = (designerDetached != 0);
                    if (key.GetValue(Linguist.Detached.Cast<string>()) is int linguistDetached)
                        LinguistDetached = (linguistDetached != 0);
                    if (key.GetValue(ResEditor.Detached.Cast<string>()) is int resEditorDetached)
                        ResourceEditorDetached = (resEditorDetached != 0);
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
                    key.SetValue(QmlDebug.Enable.Cast<string>(), QmlDebuggerEnabled ? 1 : 0);
                    key.SetValue(QmlDebug.Timeout.Cast<string>(), (int)QmlDebuggerTimeout);
                    key.SetValue(IntelliSense.OnBuild.Cast<string>(),
                        RefreshIntelliSenseOnBuild ? 1 : 0);
                    key.SetValue(IntelliSense.OnUiFile.Cast<string>(),
                        RefreshIntelliSenseOnUiFile ? 1 : 0);
                    key.SetValue(Help.Preference.Cast<string>(), HelpPreference.Cast<string>());
                    key.SetValue(Help.TryOnF1Pressed.Cast<string>(), TryQtHelpOnF1Pressed ? 1 : 0);
                    key.SetValue(Designer.Detached.Cast<string>(), DesignerDetached ? 1 : 0);
                    key.SetValue(Linguist.Detached.Cast<string>(), LinguistDetached ? 1 : 0);
                    key.SetValue(ResEditor.Detached.Cast<string>(), ResourceEditorDetached ? 1 : 0);
                }
            } catch (Exception exception) {
                Messages.Print(
                    exception.Message + "\r\n\r\nStacktrace:\r\n" + exception.StackTrace);
            }
        }
    }
}
