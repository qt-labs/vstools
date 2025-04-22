// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace QtVsTools.Editors
{
    using Core;
    using Core.Options;
    using QtVsTools.Core.Common;

    internal class QtResourceFileSniffer : IFileTypeSniffer
    {
        private static readonly Regex Regex = new(@"<\s*(?i:rcc)\s*>");

        public bool IsSupportedFile(string filePath)
        {
            try {
                return File.ReadLines(filePath).Take(3).Any(line => Regex.IsMatch(line.Trim()));
            } catch {
                return false;
            }
        }
    }

    [Guid(GuidString)]
    public class QtResourceEditor : Editor
    {
        public const string GuidString = "D0FFB6E6-5829-4DD9-835E-2965449AC6BF";
        public const string Title = "Qt Resource Editor";

        public QtResourceEditor()
            : base(new QtResourceFileSniffer())
        {}

        private Guid? guid;
        public override Guid Guid => guid ??= new Guid(GuidString);

        public override string ExecutableName => "QrcEditor.exe";

        protected override string GetToolsPath() => Utils.PackageInstallPath;

        public override Func<string, bool> WindowFilter =>
            caption => caption.StartsWith(Title);

        protected override string GetTitle(Process editorProcess) => Title;

        protected override bool Detached => QtOptionsPage.ResourceEditorDetached;

        protected override bool ShowDetachNotification => QtOptionsPage
            .NotifyResourceEditorDetachable;

        protected override void DisableDetachNotification()
        {
            try {
                QtOptionsPage.NotifyResourceEditorDetachable = false;
                QtOptionsPage.SaveSettingsToStorageStatic();
            } catch (Exception ex) {
                ex.Log();
            }
        }
    }
}
