// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.ComponentModel;

namespace QtVsTools.Core.Options
{
    using Common;

    using static Common.Converters;
    using static QtVsTools.Common.EnumExt;

    public partial class QtOptionsPage
    {

        public enum Notifications
        {
            [String("Notifications_AutoActivatePane")] AutoActivatePane,
            [String("Notifications_ShowExceptionsInOutputPane")] ShowExceptionsInOutputPane,
            [String("Notifications_Installed")] Installed,
            [String("Notifications_UpdateQtInstallation")] UpdateQtInstallation,
            [String("Notifications_UpdateProjectFormat")] UpdateProjectFormat,
            [String("Notifications_CMake_Incompatible")] CMakeIncompatible,
            [String("Notifications_CMake_Conversion")] CMakeConversion,
            [String("NotifySearchDevRelease")] NotifySearchDevRelease,
            [String("Notifications_SearchDevRelease")] NotifyQmlLanguageServersUpdateInstalled,
            [String("Notifications_DesignerDetachable")] NotifyDesignerDetachable,
            [String("Notifications_LinguistDetachable")] NotifyLinguistDetachable,
            [String("Notifications_ResourceEditorDetachable")] NotifyResourceEditorDetachable,
            [String("Notifications_ShowDevReleaseDownload")] NotifyShowDevReleaseDownload
        }

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
        [DisplayName("Show exception details in output pane")]
        [Description("Write exceptions to the Qt VS Tools output pane. When disabled, "
            + @"exceptions are written to %TEMP%\QtVsTools\QtVsTools.Exceptions.log.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool ShowExceptionsInOutputPaneOption
        {
            get => ShowExceptionsInOutputPane;
            set => ShowExceptionsInOutputPane = value;
        }

        [Settings(Notifications.ShowExceptionsInOutputPane, false)]
        public static bool ShowExceptionsInOutputPane
        {
            get => QtOptionsPageSettings.Instance.GetValue(() => ShowExceptionsInOutputPane);
            set => QtOptionsPageSettings.Instance.SetValue(() => ShowExceptionsInOutputPane, value);
        }

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

        [Category("Notifications")]
        [DisplayName("QML Language Server update")]
        [Description("Show notification when a new version of the local QML Language Server was"
            + " installed.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool NotifyQmlLanguageServerUpdateInstalledOption
        {
            get => NotifyQmlLanguageServerUpdateInstalled;
            set => NotifyQmlLanguageServerUpdateInstalled = value;
        }

        [Settings(Notifications.NotifyQmlLanguageServersUpdateInstalled, true)]
        public static bool NotifyQmlLanguageServerUpdateInstalled
        {
            get => QtOptionsPageSettings.Instance.GetValue(
                () => NotifyQmlLanguageServerUpdateInstalled);
            set => QtOptionsPageSettings.Instance.SetValue(
                () => NotifyQmlLanguageServerUpdateInstalled, value);
        }

        [Category("Notifications")]
        [DisplayName("Development releases")]
        [Description("Show a notification that allows users to enable or disable automatic "
            + "checks for newer development releases.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool NotifySearchDevReleaseOption
        {
            get => NotifySearchDevRelease;
            set => NotifySearchDevRelease = value;
        }

        [Settings(Notifications.NotifySearchDevRelease, true)]
        public static bool NotifySearchDevRelease
        {
            get => QtOptionsPageSettings.Instance.GetValue(() => NotifySearchDevRelease);
            set => QtOptionsPageSettings.Instance.SetValue(() => NotifySearchDevRelease, value);
        }

        [Category("Notifications")]
        [DisplayName("Designer window detachable")]
        [Description("Show notification when the Qt Widgets Designer is detachable.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool NotifyDesignerDetachableOption
        {
            get => NotifyDesignerDetachable;
            set => NotifyDesignerDetachable = value;
        }

        [Settings(Notifications.NotifyDesignerDetachable, true)]
        public static bool NotifyDesignerDetachable
        {
            get => QtOptionsPageSettings.Instance.GetValue(() => NotifyDesignerDetachable);
            set => QtOptionsPageSettings.Instance.SetValue(() => NotifyDesignerDetachable, value);
        }

        [Category("Notifications")]
        [DisplayName("Linguist window detachable")]
        [Description("Show notification when the Qt Linguist is detachable.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool NotifyLinguistDetachableOption
        {
            get => NotifyLinguistDetachable;
            set => NotifyLinguistDetachable = value;
        }

        [Settings(Notifications.NotifyLinguistDetachable, true)]
        public static bool NotifyLinguistDetachable
        {
            get => QtOptionsPageSettings.Instance.GetValue(() => NotifyLinguistDetachable);
            set => QtOptionsPageSettings.Instance.SetValue(() => NotifyLinguistDetachable, value);
        }

        [Category("Notifications")]
        [DisplayName("Qt Resource editor window detachable")]
        [Description("Show notification when the Qt Resource is detachable.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool NotifyResourceEditorDetachableOption
        {
            get => NotifyResourceEditorDetachable;
            set => NotifyResourceEditorDetachable = value;
        }

        [Settings(Notifications.NotifyResourceEditorDetachable, true)]
        public static bool NotifyResourceEditorDetachable
        {
            get => QtOptionsPageSettings.Instance.GetValue(() => NotifyResourceEditorDetachable);
            set => QtOptionsPageSettings.Instance.SetValue(() => NotifyResourceEditorDetachable, value);
        }

        [Category("Notifications")]
        [DisplayName("Development release download")]
        [Description("Show a notification to allow users to download the available development release.")]
        [TypeConverter(typeof(EnableDisableConverter))]
        public bool NotifyShowDevReleaseDownloadOption
        {
            get => NotifyShowDevReleaseDownload;
            set => NotifyShowDevReleaseDownload = value;
        }

        [Settings(Notifications.NotifyShowDevReleaseDownload, true)]
        public static bool NotifyShowDevReleaseDownload
        {
            get => QtOptionsPageSettings.Instance.GetValue(() => NotifyShowDevReleaseDownload);
            set => QtOptionsPageSettings.Instance.SetValue(() => NotifyShowDevReleaseDownload, value);
        }
    }
}
