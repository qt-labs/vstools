/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace QtVsTools.Editors
{
    [Guid(GuidString)]
    public class QtLinguist : Editor
    {
        public const string GuidString = "4A1333DC-5C94-4F14-A7BF-DC3D96092234";
        public const string Title = "Qt Linguist";

        private Guid? guid;
        public override Guid Guid => guid ??= new Guid(GuidString);

        public override string ExecutableName => "linguist.exe";

        public override Func<string, bool> WindowFilter =>
            caption => caption.EndsWith(Title);

        protected override string GetTitle(Process editorProcess)
        {
            return Title;
        }

        protected override bool Detached => QtVsToolsPackage.Instance.Options.LinguistDetached;
    }
}
