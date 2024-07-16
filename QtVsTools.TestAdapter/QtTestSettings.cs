/**************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
**************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

#if _DEBUG
using System.Diagnostics;
#endif

namespace QtVsTools.TestAdapter
{
    using QtVsTools.Core.Common;

    internal class QtTestSettings
    {
        internal string QtInstall { get; private set; }
        internal bool ShowAdapterOutput { get; private set; }

        internal int TestTimeout { get; private set; } = -1;
        internal int DiscoveryTimeout { get; private set; } = 2000;

        internal bool ParsePdbFiles { get; private set; } = true;
        internal bool SubsystemConsoleOnly { get; private set; } = true;

        internal class OutputType
        {
            internal List<string> FilenameFormats { get; } = new();
        }
        internal OutputType Output { get; } = new();

        internal class VerbosityType
        {
            internal string Level { get; set; }
            internal bool LogSignals { get; set; }
        }
        internal VerbosityType Verbosity { get; } = new();

        internal class CommandsType
        {
            internal int EventDelay { get; set; } = -1;
            internal int KeyDelay { get; set; } = -1;
            internal int MouseDelay { get; set; } = -1;
            internal int MaxWarnings { get; set; } = 2000;
            internal bool NoCrashHandler { get; set; }
        }
        internal CommandsType Commands { get; } = new();

        internal static QtTestSettings Load(XmlReader xmlReader, string nodeName)
        {
#if _DEBUG
            Debugger.Launch();
#endif
            var schemaSet = new XmlSchemaSet();
            using var schemaStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("QtVsTools.TestAdapter.QtTestSettings.xsd");
            if (schemaStream == null)
                return new QtTestSettings();

            schemaSet.Add(null, XmlReader.Create(schemaStream));
            var settings = new XmlReaderSettings
            {
                Schemas = schemaSet,
                ValidationType = ValidationType.Schema,
                ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings
            };

            settings.ValidationEventHandler += (_, e) => throw e.Exception;

            using var reader = XmlReader.Create(xmlReader, settings);
            try {
                return PopulateSettings(reader, nodeName);
            } catch (InvalidOperationException e) when
                (e.InnerException is XmlSchemaValidationException) {
                throw new XmlSchemaValidationException("The file contains an invalid definition "
                    + $"under the '{Resources.GlobalSettingsName}' property. "
                    + $"Details: {e.InnerException.Message}"
                );
            }
        }

        internal static void MergeSettings(QtTestSettings global, QtTestSettings user)
        {
            if (user != null)
                MergeProperties(global, user);
        }

        private static void MergeProperties(object global, object user)
        {
            var userProperties = GetProperties(user);
            var globalProperties = GetProperties(global);

            foreach (var userProp in userProperties) {
                var globalProp = Array.Find(globalProperties, p => p.Name == userProp.Name);
                if (globalProp == null)
                    continue;

                var userValue = userProp.GetValue(user);
                var globalValue = globalProp.GetValue(global);
                if (userValue == null || userValue.Equals(globalValue))
                    continue;

                switch (userProp.PropertyType) {
                case { } type when type == typeof(List<string>):
                    var globalList = (List<string>)globalValue;
                    var userList = (List<string>)userValue;
                    foreach (var item in userList.Where(item => !globalList.Contains(item)))
                        globalList.Add(item);
                    break;
                case { } type
                    when type == typeof(OutputType)
                    || type == typeof(VerbosityType)
                    || type == typeof(CommandsType):
                    MergeProperties(globalProp.GetValue(global), userValue);
                    break;
                default:
                    globalProp.SetValue(global, userValue);
                    break;
                }
            }
        }

        internal static void PrintSettings(QtTestSettings settings, Logger logger)
        {
            logger.SendMessage("QtTestSettings: [");
            PrintProperties(settings, 1, logger);
            logger.SendMessage("]");
        }

        private static void PrintProperties(object obj, int indentLevel, Logger logger)
        {
            if (obj == null)
                return;

            var properties = GetProperties(obj);
            var indent = new string(' ', indentLevel * 2);

            foreach (var property in properties) {
                var value = property.GetValue(obj);
                switch (value) {
                case List<string> list:
                    logger.SendMessage($"{indent}{property.Name}: [");
                    foreach (var item in list)
                        logger.SendMessage($"{indent}  {item}");
                    logger.SendMessage($"{indent}]");
                    break;
                case OutputType:
                case VerbosityType:
                case CommandsType:
                    logger.SendMessage($"{indent}{property.Name}: [");
                    PrintProperties(value, indentLevel + 1, logger);
                    logger.SendMessage($"{indent}]");
                    break;
                default:
                    logger.SendMessage($"{indent}{property.Name}: {value}");
                    break;
                }
            }
        }

        private static PropertyInfo[] GetProperties(object from)
        {
            return from.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private static QtTestSettings PopulateSettings(XmlReader reader, string nodeName)
        {
            QtTestSettings settings = new();

            if (reader.IsEmptyElement)
                return settings;

            reader.Read();
            if (reader.NodeType != XmlNodeType.Element || !reader.Name.Equals(nodeName))
                return settings;

            reader.Read();
            while (reader.NodeType == XmlNodeType.Element) {
                switch (reader.Name.ToUpperInvariant()) {
                case "QTINSTALL":
                    settings.QtInstall = reader.ReadInnerXml();
                    break;
                case "SHOWADAPTEROUTPUT":
                    if (bool.TryParse(reader.ReadInnerXml(), out var adapterOutput))
                        settings.ShowAdapterOutput = adapterOutput;
                    break;
                case "TESTTIMEOUT":
                    if (int.TryParse(reader.ReadInnerXml(), out var testTimeout))
                        settings.TestTimeout = testTimeout;
                    break;
                case "DISCOVERYTIMEOUT":
                    if (int.TryParse(reader.ReadInnerXml(), out var discoveryTimeout))
                        settings.DiscoveryTimeout = discoveryTimeout;
                    break;
                case "PARSEPDBFILES":
                    if (bool.TryParse(reader.ReadInnerXml(), out var parsePdb))
                        settings.ParsePdbFiles = parsePdb;
                    break;
                case "SUBSYSTEMCONSOLEONLY":
                    if (bool.TryParse(reader.ReadInnerXml(), out var consoleOnly))
                        settings.SubsystemConsoleOnly = consoleOnly;
                    break;
                case "OUTPUT":
                    SetOutputOptions(reader.ReadSubtree(), settings);
                    reader.SkipToNextElement();
                    break;
                case "VERBOSITY":
                    SetVerbosityOptions(reader.ReadSubtree(), settings);
                    reader.SkipToNextElement();
                    break;
                case "COMMANDS":
                    SetCommandOptions(reader.ReadSubtree(), settings);
                    reader.SkipToNextElement();
                    break;
                default:
                    reader.SkipToNextElement();
                    break;
                }
            }

            return settings;
        }

        private static void SetOutputOptions(XmlReader reader, QtTestSettings settings)
        {
            reader.Read();
            if (reader.IsEmptyElement)
                return;

            reader.Read();
            while (reader.NodeType == XmlNodeType.Element) {
                switch (reader.Name.ToUpperInvariant()) {
                case "FILENAMEFORMAT":
                    var value = reader.ReadInnerXml();
                    if (IsValidFilenameFormat(value, out var formattedValue))
                        settings.Output.FilenameFormats.Add(formattedValue);
                    break;
                default:
                    reader.SkipToNextElement();
                    break;
                }
            }
        }

        private static bool IsValidFilenameFormat(string value, out string formattedValue)
        {
            formattedValue = null;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var parts = value.Split(',');
            if (parts.Length != 2)
                return false;

            var filename = parts[0].Trim();
            if (string.IsNullOrEmpty(filename) || filename == "-")
                return false;

            var format = parts[1].Trim();
            if (!Resources.SupportedOutputFormats.Contains(format))
                return false;

            filename = Utils.SafeQuote(filename);
            if (filename == null)
                return false;

            formattedValue = $"{filename},{format}";
            return true;
        }

        private static void SetVerbosityOptions(XmlReader reader, QtTestSettings settings)
        {
            reader.Read();
            if (reader.IsEmptyElement)
                return;

            reader.Read();
            while (reader.NodeType == XmlNodeType.Element) {
                switch (reader.Name.ToUpperInvariant()) {
                case "LEVEL":
                    settings.Verbosity.Level = reader.ReadInnerXml();
                    break;
                case "LOGSIGNALS":
                    if (bool.TryParse(reader.ReadInnerXml(), out var logSignals))
                        settings.Verbosity.LogSignals = logSignals;
                    break;
                default:
                    reader.SkipToNextElement();
                    break;
                }
            }
        }

        private static void SetCommandOptions(XmlReader reader, QtTestSettings settings)
        {
            reader.Read();
            if (reader.IsEmptyElement)
                return;

            reader.Read();
            var commands = settings.Commands;
            while (reader.NodeType == XmlNodeType.Element) {
                switch (reader.Name.ToUpperInvariant()) {
                case "EVENTDELAY":
                    if (int.TryParse(reader.ReadElementContentAsString(), out var eventDelay))
                        commands.EventDelay = eventDelay;
                    break;
                case "KEYDELAY":
                    if (int.TryParse(reader.ReadElementContentAsString(), out var keyDelay))
                        commands.KeyDelay = keyDelay;
                    break;
                case "MOUSEDELAY":
                    if (int.TryParse(reader.ReadElementContentAsString(), out var mouseDelay))
                        commands.MouseDelay = mouseDelay;
                    break;
                case "MAXWARNINGS":
                    if (int.TryParse(reader.ReadElementContentAsString(), out var maxWarnings))
                        commands.MaxWarnings = maxWarnings;
                    break;
                case "NOCRASHHANDLER":
                    if (bool.TryParse(reader.ReadElementContentAsString(), out var noCrashHandler))
                        commands.NoCrashHandler = noCrashHandler;
                    break;
                default:
                    reader.SkipToNextElement();
                    break;
                }
            }
        }
    }
}
