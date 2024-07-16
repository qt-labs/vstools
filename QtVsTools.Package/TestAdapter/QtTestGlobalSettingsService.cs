/**************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
**************************************************************************************************/

using System;
using System.ComponentModel.Composition;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

using TestWindow = Microsoft.VisualStudio.TestWindow.Extensibility;

namespace QtVsTools.Package.TestAdapter
{
    using QTA = QtVsTools.TestAdapter;

    [Export(typeof(TestWindow.IRunSettingsService))]
    [SettingsName(QTA.Resources.GlobalSettingsName)]
    internal class QtTestGlobalSettingsService : TestWindow.IRunSettingsService
    {
        public string Name => QTA.Resources.GlobalSettingsName;

        public IXPathNavigable AddRunSettings(IXPathNavigable inputRunSettingDocument,
            TestWindow.IRunSettingsConfigurationInfo configurationInfo, TestWindow.ILogger logger)
        {
            var settingsNavigator = inputRunSettingDocument.CreateNavigator();
            _ = settingsNavigator ?? throw new ArgumentNullException(nameof(settingsNavigator));

            if (!settingsNavigator.MoveToChild(Constants.RunSettingsName, "")) {
                logger.Log(TestWindow.MessageLevel.Warning, "No QtTestGlobal section in found "
                    + "in .runsettings file.");
                return settingsNavigator;
            }

            var settingsContainer = new QtTestGlobalSettingsContainer();
            settingsNavigator.AppendChild(settingsContainer.ToXml().CreateNavigator());

            settingsNavigator.MoveToRoot();
            return settingsNavigator;
        }
    }

    [XmlRoot(QTA.Resources.GlobalSettingsName)]
    public class QtTestGlobalSettingsContainer : TestRunSettings
    {
        public QtTestGlobalSettingsContainer()
            : base(QTA.Resources.GlobalSettingsName)
        {}

        public override XmlElement ToXml()
        {
            var document = new XmlDocument();
            var root = document.CreateElement(QTA.Resources.GlobalSettingsName);

            AppendChildElement(document, root, "QtInstall", QTA.QtTestPage.QtInstall);
            AppendChildElement(document, root, "ShowAdapterOutput", QTA.QtTestPage.ShowAdapterOutput);
            AppendChildElement(document, root, "TestTimeout", QTA.QtTestPage.TestTimeout);
            AppendChildElement(document, root, "DiscoveryTimeout", QTA.QtTestPage.DiscoveryTimeout);
            AppendChildElement(document, root, "ParsePdbFiles", QTA.QtTestPage.ParsePdbFiles);
            AppendChildElement(document, root, "SubsystemConsoleOnly", QTA.QtTestPage.SubsystemConsoleOnly);

            var fileName = QTA.QtTestPage.FileName;
            var fileFormat = QTA.QtTestPage.FileFormat;
            if (!string.IsNullOrWhiteSpace(fileName) && !string.IsNullOrEmpty(fileFormat)) {
                var node = document.CreateElement("Output");
                AppendChildElement(document, node, "FilenameFormat", $"{fileName},{fileFormat}");
                root.AppendChild(node);
            }

            var element = document.CreateElement("Verbosity");
            AppendChildElement(document, element, "Level", QTA.QtTestPage.Level);
            AppendChildElement(document, element, "LogSignals", QTA.QtTestPage.LogSignals);
            root.AppendChild(element);

            element = document.CreateElement("Commands");
            AppendChildElement(document, element, "EventDelay", QTA.QtTestPage.EventDelay);
            AppendChildElement(document, element, "KeyDelay", QTA.QtTestPage.KeyDelay);
            AppendChildElement(document, element, "MouseDelay", QTA.QtTestPage.MouseDelay);
            AppendChildElement(document, element, "MaxWarnings", QTA.QtTestPage.MaxWarnings);
            AppendChildElement(document, element, "NoCrashHandler", QTA.QtTestPage.NoCrashHandler);
            root.AppendChild(element);

            document.AppendChild(root);
            return document.DocumentElement
                ?? throw new InvalidOperationException("DocumentElement is null.");
        }

        private static void AppendChildElement(XmlDocument doc, XmlElement parent, string name,
            object value)
        {
            if (value == null)
                return;
            var element = doc.CreateElement(name);
            element.InnerText = value is bool ? value.ToString().ToLower() : value.ToString();
            parent.AppendChild(element);
        }
    }
}
