/**************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
**************************************************************************************************/

using System.ComponentModel.Composition;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace QtVsTools.TestAdapter
{
    [Export(typeof(ISettingsProvider))]
    [SettingsName(Resources.SettingsName)]
    internal class QtTestSettingsProvider : ISettingsProvider
    {
        internal QtTestSettings Settings { get; private set; }

        public void Load(XmlReader reader) =>
            Settings = QtTestSettings.Load(reader, Resources.SettingsName);
    }
}
