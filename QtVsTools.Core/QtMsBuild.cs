/****************************************************************************
**
** Copyright (C) 2017 The Qt Company Ltd.
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

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.IO;

using CommandLineParser = QtVsTools.Core.CommandLine.Parser;
using CommandLineOption = QtVsTools.Core.CommandLine.Option;

namespace QtVsTools.Core.QtMsBuild
{
    public interface IVSMacroExpander
    {
        string ExpandString(string stringToExpand);
    }

    public interface IPropertyStorageProvider
    {
        string GetProperty(
            object propertyStorage,
            string itemType,
            string propertyName);

        bool SetProperty(
            object propertyStorage,
            string itemType,
            string propertyName,
            string propertyValue);

        bool DeleteProperty(
            object propertyStorage,
            string itemType,
            string propertyName);

        string GetConfigName(
            object propertyStorage);

        string GetItemType(
            object propertyStorage);

        string GetItemName(
            object propertyStorage);

        object GetParentProject(
            object propertyStorage);

        object GetProjectConfiguration(
            object project,
            string configName);

        IEnumerable<object> GetItems(
            object project,
            string itemType,
            string configName = "");
    }

    public class QtMsBuildContainer
    {

        IPropertyStorageProvider provider;
        public QtMsBuildContainer(IPropertyStorageProvider provider)
        {
            this.provider = provider;
        }

        public string GetPropertyValueByName(
            object propertyStorage,
            string itemType,
            string propertyName)
        {
            return provider.GetProperty(propertyStorage, itemType, propertyName);
        }

        bool SetPropertyValueByName(
            object propertyStorage,
            string itemType,
            string propertyName,
            string propertyValue)
        {
            return provider.SetProperty(propertyStorage, itemType, propertyName, propertyValue);
        }

        bool DeletePropertyByName(
            object propertyStorage,
            string itemType,
            string propertyName)
        {
            return provider.DeleteProperty(propertyStorage, itemType, propertyName);
        }

        class ItemPropertyChange
        {
            //key
            public string ConfigName;
            public string ItemTypeName;
            public string ItemName;
            public string PropertyName;

            //value
            public string PropertyValue;
            public object PropertyStorage;

            public void CopyFrom(ItemPropertyChange change)
            {
                ConfigName = change.ConfigName;
                ItemTypeName = change.ItemTypeName;
                ItemName = change.ItemName;
                PropertyName = change.PropertyName;
                PropertyValue = change.PropertyValue;
                PropertyStorage = change.PropertyStorage;
            }

            public bool IsMocSource
            {
                get
                {
                    return ItemTypeName == QtMoc.ItemTypeName
                        && !HelperFunctions.IsHeaderFile(ItemName);
                }
            }

            public string GroupKey
            {
                get
                {
                    return string.Join(",", new string[] {
                        ConfigName, ItemTypeName, PropertyName, PropertyValue,
                        IsMocSource.ToString()
                    });
                }
            }

            public string Key
            {
                get
                {
                    return string.Join(",", new string[] {
                        ConfigName, ItemTypeName, PropertyName, ItemName
                    });
                }
            }
        }

        public IEnumerable<object> GetItems(string itemType, string configName = "")
        {
            return provider.GetItems(GetProject(), itemType, configName);
        }

        int GetItemCount(string itemType, bool? isMocSource = null, string configName = "")
        {
            var items = GetItems(itemType, configName);
            if (!isMocSource.HasValue) {
                return items
                .Count();
            } else if (!isMocSource.Value) {
                return items.Where(x =>
                provider.GetItemType(x) != QtMoc.ItemTypeName
                || HelperFunctions.IsHeaderFile(provider.GetItemName(x)))
                .Count();
            } else {
                return items.Where(x =>
                provider.GetItemType(x) == QtMoc.ItemTypeName
                && !HelperFunctions.IsHeaderFile(provider.GetItemName(x)))
                .Count();
            }
        }

        object GetProjectConfiguration(string configName)
        {
            return provider.GetProjectConfiguration(GetProject(), configName);
        }

        Dictionary<string, ItemPropertyChange> itemPropertyChanges
            = new Dictionary<string, ItemPropertyChange>();
        Dictionary<string, List<ItemPropertyChange>> itemPropertyChangesGrouped
            = new Dictionary<string, List<ItemPropertyChange>>();
        bool pendingChanges = false;

        void AddChange(ItemPropertyChange newChange)
        {
            ItemPropertyChange oldChange;
            if (itemPropertyChanges.TryGetValue(newChange.Key, out oldChange)) {
                if (oldChange.GroupKey == newChange.GroupKey) {
                    oldChange.CopyFrom(newChange);
                    return;
                } else {
                    RemoveChange(oldChange);
                }
            }

            List<ItemPropertyChange> changeGroup;
            if (!itemPropertyChangesGrouped.TryGetValue(newChange.GroupKey, out changeGroup)) {
                itemPropertyChangesGrouped.Add(
                    newChange.GroupKey,
                    changeGroup = new List<ItemPropertyChange>());
            }
            changeGroup.Add(newChange);
            itemPropertyChanges.Add(newChange.Key, newChange);
        }

        void RemoveChange(ItemPropertyChange change)
        {
            List<ItemPropertyChange> changeGroup;
            if (itemPropertyChangesGrouped.TryGetValue(change.GroupKey, out changeGroup)) {
                changeGroup.Remove(change);
                if (changeGroup.Count == 0)
                    itemPropertyChangesGrouped.Remove(change.GroupKey);
            }
            itemPropertyChanges.Remove(change.Key);
        }

        object GetProject()
        {
            var change = itemPropertyChanges.Values.FirstOrDefault();
            if (change == null)
                return null;
            return provider.GetParentProject(change.PropertyStorage);
        }

        public bool BeginSetItemProperties()
        {
            if (pendingChanges)
                return false;
            itemPropertyChanges.Clear();
            itemPropertyChangesGrouped.Clear();
            pendingChanges = true;
            return true;
        }

        public bool SetItemPropertyByName(
            object propertyStorage,
            string propertyName,
            string propertyValue)
        {
            if (propertyStorage == null)
                return false;

            string configName = provider.GetConfigName(propertyStorage);
            string itemType = provider.GetItemType(propertyStorage);
            string itemName = provider.GetItemName(propertyStorage);

            var newChange = new ItemPropertyChange
            {
                ConfigName = configName,
                ItemTypeName = itemType,
                ItemName = itemName,
                PropertyName = propertyName,
                PropertyValue = propertyValue,
                PropertyStorage = propertyStorage,
            };

            if (!pendingChanges) {
                if (!BeginSetItemProperties())
                    return false;
                AddChange(newChange);
                if (!EndSetItemProperties())
                    return false;
            } else {
                AddChange(newChange);
            }

            return true;
        }

        bool SetGroupItemProperties(List<ItemPropertyChange> changeGroup)
        {
            //grouped by configName, itemTypeName, propertyName, propertyValue, isMocSource
            var firstChange = changeGroup.FirstOrDefault();
            if (firstChange == null)
                return false;
            string configName = firstChange.ConfigName;
            string itemTypeName = firstChange.ItemTypeName;
            string propertyName = firstChange.PropertyName;
            string propertyValue = firstChange.PropertyValue;
            bool isMocSource = firstChange.IsMocSource;
            int itemCount = GetItemCount(itemTypeName, isMocSource, configName);
            int groupCount = changeGroup.Count;
            object projConfig = GetProjectConfiguration(configName);

            if (!isMocSource && groupCount == itemCount) {
                //all items are setting the same value for this property
                // -> set at project level
                if (!SetPropertyValueByName(
                    projConfig,
                    itemTypeName,
                    propertyName,
                    propertyValue)) {
                    return false;
                }

                // -> remove old property from each item
                foreach (var change in changeGroup) {
                    if (!DeletePropertyByName(
                        change.PropertyStorage,
                        change.ItemTypeName,
                        change.PropertyName)) {
                        return false;
                    }

                }
            } else {
                //different property values per item
                // -> set at each item
                foreach (var change in changeGroup) {
                    if (GetPropertyValueByName(
                        projConfig,
                        change.ItemTypeName,
                        change.PropertyName) != change.PropertyValue) {
                        if (!SetPropertyValueByName(
                            change.PropertyStorage,
                            change.ItemTypeName,
                            change.PropertyName,
                            change.PropertyValue)) {
                            return false;
                        }

                    }
                }
            }
            return true;
        }

        public bool EndSetItemProperties()
        {
            if (!pendingChanges)
                return false;

            var changeGroupsNormal = itemPropertyChangesGrouped.Values
                .Where(x => x.Any() && !x.First().IsMocSource);
            foreach (var changeGroup in changeGroupsNormal)
                SetGroupItemProperties(changeGroup);

            var changeGroupsMocSource = itemPropertyChangesGrouped.Values
                .Where(x => x.Any() && x.First().IsMocSource);
            foreach (var changeGroup in changeGroupsMocSource)
                SetGroupItemProperties(changeGroup);

            itemPropertyChanges.Clear();
            itemPropertyChangesGrouped.Clear();
            pendingChanges = false;
            return true;
        }

        string GetPropertyChangedValue(
            string configName,
            string itemTypeName,
            string itemName,
            string propertyName)
        {
            if (!pendingChanges)
                return null;

            var change = new ItemPropertyChange
            {
                ConfigName = configName,
                ItemTypeName = itemTypeName,
                ItemName = itemName,
                PropertyName = propertyName
            };
            if (!itemPropertyChanges.TryGetValue(change.Key, out change))
                return null;

            return change.PropertyValue;
        }

        public string GetPropertyChangedValue(
            QtMoc.Property property,
            string itemName,
            string configName)
        {
            return GetPropertyChangedValue(
                configName,
                QtMoc.ItemTypeName,
                itemName,
                property.ToString());
        }

        public string GetPropertyChangedValue(
            QtRcc.Property property,
            string itemName,
            string configName)
        {
            return GetPropertyChangedValue(
                configName,
                QtRcc.ItemTypeName,
                itemName,
                property.ToString());
        }

        public string GetPropertyChangedValue(
            QtRepc.Property property,
            string itemName,
            string configName)
        {
            return GetPropertyChangedValue(
                configName,
                QtRepc.ItemTypeName,
                itemName,
                property.ToString());
        }

        public string GetPropertyChangedValue(
            QtUic.Property property,
            string itemName,
            string configName)
        {
            return GetPropertyChangedValue(
                configName,
                QtUic.ItemTypeName,
                itemName,
                property.ToString());
        }

        public bool SetCommandLine(
            string itemType,
            object propertyStorage,
            string commandLine,
            IVSMacroExpander macros)
        {
            switch (itemType) {
            case QtMoc.ItemTypeName:
                return SetQtMocCommandLine(propertyStorage, commandLine, macros);
            case QtRcc.ItemTypeName:
                return SetQtRccCommandLine(propertyStorage, commandLine, macros);
            case QtRepc.ItemTypeName:
                return SetQtRepcCommandLine(propertyStorage, commandLine, macros);
            case QtUic.ItemTypeName:
                return SetQtUicCommandLine(propertyStorage, commandLine, macros);
            }
            return false;
        }

        #region QtMoc
        static QtMoc qtMocInstance;
        public static QtMoc QtMocInstance
        {
            get
            {
                if (qtMocInstance == null)
                    qtMocInstance = new QtMoc();
                return qtMocInstance;
            }
        }

        public string GetPropertyValue(object propertyStorage, QtMoc.Property property)
        {
            return GetPropertyValueByName(
                propertyStorage,
                QtMoc.ItemTypeName,
                property.ToString());
        }

        public bool SetItemProperty(
            object propertyStorage,
            QtMoc.Property property,
            string propertyValue)
        {
            return SetItemPropertyByName(propertyStorage, property.ToString(), propertyValue);
        }

        public bool SetQtMocCommandLine(
            object propertyStorage,
            string commandLine,
            IVSMacroExpander macros)
        {
            Dictionary<QtMoc.Property, string> properties;
            if (!QtMocInstance.ParseCommandLine(commandLine, macros, out properties))
                return false;
            foreach (var property in properties) {
                if (!SetItemProperty(propertyStorage, property.Key, property.Value))
                    return false;
            }
            return true;
        }

        public string GenerateQtMocCommandLine(object propertyStorage)
        {
            return QtMocInstance.GenerateCommandLine(this, propertyStorage);
        }
        #endregion

        #region QtRcc
        static QtRcc qtRccInstance;
        public static QtRcc QtRccInstance
        {
            get
            {
                if (qtRccInstance == null)
                    qtRccInstance = new QtRcc();
                return qtRccInstance;
            }
        }

        public string GetPropertyValue(object propertyStorage, QtRcc.Property property)
        {
            return GetPropertyValueByName(
                propertyStorage,
                QtRcc.ItemTypeName,
                property.ToString());
        }

        public bool SetItemProperty(
            object propertyStorage,
            QtRcc.Property property,
            string propertyValue)
        {
            return SetItemPropertyByName(propertyStorage, property.ToString(), propertyValue);
        }

        public bool SetQtRccCommandLine(
            object propertyStorage,
            string commandLine,
            IVSMacroExpander macros)
        {
            Dictionary<QtRcc.Property, string> properties;
            if (!QtRccInstance.ParseCommandLine(commandLine, macros, out properties))
                return false;
            foreach (var property in properties) {
                if (!SetItemProperty(propertyStorage, property.Key, property.Value))
                    return false;
            }
            return true;
        }

        public string GenerateQtRccCommandLine(object propertyStorage)
        {
            return QtRccInstance.GenerateCommandLine(this, propertyStorage);
        }
        #endregion

        #region QtRepc
        static QtRepc qtRepcInstance;
        public static QtRepc QtRepcInstance
        {
            get
            {
                if (qtRepcInstance == null)
                    qtRepcInstance = new QtRepc();
                return qtRepcInstance;
            }
        }

        public string GetPropertyValue(object propertyStorage, QtRepc.Property property)
        {
            return GetPropertyValueByName(
                propertyStorage,
                QtRepc.ItemTypeName,
                property.ToString());
        }

        public bool SetItemProperty(
            object propertyStorage,
            QtRepc.Property property,
            string propertyValue)
        {
            return SetItemPropertyByName(propertyStorage, property.ToString(), propertyValue);
        }

        public bool SetQtRepcCommandLine(
            object propertyStorage,
            string commandLine,
            IVSMacroExpander macros)
        {
            Dictionary<QtRepc.Property, string> properties;
            if (!QtRepcInstance.ParseCommandLine(commandLine, macros, out properties))
                return false;
            foreach (var property in properties) {
                if (!SetItemProperty(propertyStorage, property.Key, property.Value))
                    return false;
            }
            return true;
        }

        public string GenerateQtRepcCommandLine(object propertyStorage)
        {
            return QtRepcInstance.GenerateCommandLine(this, propertyStorage);
        }
        #endregion

        #region QtUic
        static QtUic qtUicInstance;
        public static QtUic QtUicInstance
        {
            get
            {
                if (qtUicInstance == null)
                    qtUicInstance = new QtUic();
                return qtUicInstance;
            }
        }

        public string GetPropertyValue(object propertyStorage, QtUic.Property property)
        {
            return GetPropertyValueByName(
                propertyStorage,
                QtUic.ItemTypeName,
                property.ToString());
        }

        public bool SetItemProperty(
            object propertyStorage,
            QtUic.Property property,
            string propertyValue)
        {
            return SetItemPropertyByName(propertyStorage, property.ToString(), propertyValue);
        }

        public bool SetQtUicCommandLine(
            object propertyStorage,
            string commandLine,
            IVSMacroExpander macros)
        {
            Dictionary<QtUic.Property, string> properties;
            if (!QtUicInstance.ParseCommandLine(commandLine, macros, out properties))
                return false;
            foreach (var property in properties) {
                if (!SetItemProperty(propertyStorage, property.Key, property.Value))
                    return false;
            }
            return true;
        }

        public string GenerateQtUicCommandLine(object propertyStorage)
        {
            return QtUicInstance.GenerateCommandLine(this, propertyStorage);
        }
        #endregion

    }

    public abstract class QtTool
    {
        protected CommandLineParser parser;
        protected CommandLineOption outputOption;
        protected CommandLineOption helpOption;
        protected CommandLineOption versionOption;

        protected QtTool(bool defaultInputOutput = true)
        {
            parser = new CommandLineParser();
            parser.SetSingleDashWordOptionMode(
                CommandLineParser.SingleDashWordOptionMode.ParseAsLongOptions);

            helpOption = parser.AddHelpOption();
            versionOption = parser.AddVersionOption();

            if (defaultInputOutput) {
                outputOption = new CommandLineOption("o");
                outputOption.ValueName = "file";
                outputOption.Flags = CommandLineOption.Flag.ShortOptionStyle;
                parser.AddOption(outputOption);
            }
        }

        protected virtual void ExtractInputOutput(
            string toolExecName,
            out string inputPath,
            out string outputPath)
        {
            inputPath = outputPath = "";

            string filePath = parser.PositionalArguments.Where(
                arg => !arg.EndsWith(toolExecName, StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(filePath))
                inputPath = filePath;

            if (outputOption != null && parser.IsSet(outputOption))
                outputPath = parser.Value(outputOption);
        }

        protected bool ParseCommandLine(
            string commandLine,
            IVSMacroExpander macros,
            string toolExecName,
            out string qtDir,
            out string inputPath,
            out string outputPath)
        {
            qtDir = inputPath = outputPath = "";
            if (!parser.Parse(commandLine, macros, toolExecName))
                return false;

            string execPath = parser.PositionalArguments.Where(
                arg => arg.EndsWith(toolExecName, StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(execPath)) {
                var execDir = Path.GetDirectoryName(execPath);
                if (!string.IsNullOrEmpty(execDir))
                    qtDir = HelperFunctions.CanonicalPath(Path.Combine(execDir, ".."));
            }

            ExtractInputOutput(toolExecName, out inputPath, out outputPath);
            return true;
        }

        protected void GenerateCommandLineOption(
            StringBuilder commandLine,
            CommandLineOption option,
            string values = "",
            bool useQuotes = false)
        {
            var name = option.Names.First();
            var escape = (name.Length == 1) ? "-" : "--";

            if (!string.IsNullOrEmpty(option.ValueName)) {
                foreach (var value in values.Split(new char[] { ';' })) {
                    if (useQuotes)
                        commandLine.AppendFormat(" {0}{1} \"{2}\"", escape, name, value);
                    else
                        commandLine.AppendFormat(" {0}{1} {2}", escape, name, value);
                }
            } else {
                commandLine.AppendFormat(" {0}{1}", escape, name);
            }
        }
    }

    public sealed class QtMoc : QtTool
    {
        public const string ItemTypeName = "QtMoc";
        public const string ToolExecName = "moc.exe";

        public enum Property
        {
            ExecutionDescription,
            QTDIR,
            InputFile,
            OutputFile,
            IncludePath,
            MacFramework,
            PreprocessOnly,
            Define,
            Undefine,
            Metadata,
            CompilerFlavor,
            NoInclude,
            PathPrefix,
            ForceInclude,
            PrependInclude,
            Include,
            NoNotesWarnings,
            NoNotes,
            NoWarnings,
            IgnoreConflicts,
            OptionsFile,
            CommandLineTemplate,
            AdditionalOptions,
            DynamicSource,
            ParallelProcess,
            AdditionalDependencies,
        }

        Dictionary<Property, CommandLineOption> options
            = new Dictionary<Property, CommandLineOption>();

        public QtMoc() : base()
        {
            parser.AddOption(options[Property.IncludePath] =
                new CommandLineOption("I")
                {
                    ValueName = "dir",
                    Flags = CommandLineOption.Flag.ShortOptionStyle
                });

            parser.AddOption(options[Property.MacFramework] =
                new CommandLineOption("F")
                {
                    ValueName = "framework",
                    Flags = CommandLineOption.Flag.ShortOptionStyle
                });

            parser.AddOption(options[Property.PreprocessOnly] =
                new CommandLineOption("E"));

            parser.AddOption(options[Property.Define] =
                new CommandLineOption("D")
                {
                    ValueName = "macro[=def]",
                    Flags = CommandLineOption.Flag.ShortOptionStyle
                });

            parser.AddOption(options[Property.Undefine] =
                new CommandLineOption("U")
                {
                    ValueName = ("macro"),
                    Flags = CommandLineOption.Flag.ShortOptionStyle
                });

            parser.AddOption(options[Property.Metadata] =
                new CommandLineOption("M")
                {
                    ValueName = "key=value",
                    Flags = CommandLineOption.Flag.ShortOptionStyle
                });

            parser.AddOption(options[Property.CompilerFlavor] =
                new CommandLineOption("compiler-flavor")
                {
                    ValueName = "flavor"
                });

            parser.AddOption(options[Property.NoInclude] =
                new CommandLineOption("i"));

            parser.AddOption(options[Property.PathPrefix] =
                new CommandLineOption("p")
                {
                    ValueName = "path",
                    Flags = CommandLineOption.Flag.ShortOptionStyle
                });

            parser.AddOption(options[Property.ForceInclude] =
                new CommandLineOption("f")
                {
                    ValueName = "file",
                    Flags = CommandLineOption.Flag.ShortOptionStyle
                });

            parser.AddOption(options[Property.PrependInclude] =
                new CommandLineOption("b")
                {
                    ValueName = "file"
                });

            parser.AddOption(options[Property.Include] =
                new CommandLineOption("include")
                {
                    ValueName = "file"
                });

            parser.AddOption(options[Property.NoNotesWarnings] =
                new CommandLineOption("n")
                {
                    ValueName = "which",
                    Flags = CommandLineOption.Flag.ShortOptionStyle
                });

            parser.AddOption(options[Property.NoNotes] =
                new CommandLineOption("no-notes"));

            parser.AddOption(options[Property.NoWarnings] =
                new CommandLineOption("no-warnings"));

            parser.AddOption(options[Property.IgnoreConflicts] =
                new CommandLineOption("ignore-option-clashes"));
        }

        public bool ParseCommandLine(
            string commandLine,
            IVSMacroExpander macros,
            out Dictionary<Property, string> properties)
        {
            properties = new Dictionary<Property, string>();

            string qtDir, inputPath, outputPath;
            if (!ParseCommandLine(
                commandLine,
                macros,
                ToolExecName,
                out qtDir,
                out inputPath,
                out outputPath)) {
                return false;
            }

            if (!string.IsNullOrEmpty(qtDir))
                properties[Property.QTDIR] = qtDir;

            if (!string.IsNullOrEmpty(inputPath))
                properties[Property.InputFile] = inputPath;

            if (!string.IsNullOrEmpty(outputPath))
                properties[Property.OutputFile] = outputPath;

            if (parser.IsSet(options[Property.IncludePath])) {
                properties[Property.IncludePath] =
                    string.Join(";", parser.Values(options[Property.IncludePath]));
            }

            if (parser.IsSet(options[Property.MacFramework])) {
                properties[Property.MacFramework] =
                    string.Join(";", parser.Values(options[Property.MacFramework]));
            }

            if (parser.IsSet(options[Property.PreprocessOnly]))
                properties[Property.PreprocessOnly] = "true";

            if (parser.IsSet(options[Property.Define])) {
                properties[Property.Define] =
                    string.Join(";", parser.Values(options[Property.Define]));
            }

            if (parser.IsSet(options[Property.Undefine])) {
                properties[Property.Undefine] =
                    string.Join(";", parser.Values(options[Property.Undefine]));
            }

            if (parser.IsSet(options[Property.Metadata])) {
                properties[Property.Metadata] =
                    string.Join(";", parser.Values(options[Property.Metadata]));
            }

            if (parser.IsSet(options[Property.CompilerFlavor])) {
                properties[Property.CompilerFlavor] =
                    string.Join(";", parser.Values(options[Property.CompilerFlavor]));
            }

            if (parser.IsSet(options[Property.NoInclude]))
                properties[Property.NoInclude] = "true";

            if (parser.IsSet(options[Property.PathPrefix])) {
                properties[Property.PathPrefix] =
                    string.Join(";", parser.Values(options[Property.PathPrefix]));
            }

            if (parser.IsSet(options[Property.ForceInclude])) {
                properties[Property.ForceInclude] =
                    string.Join(";", parser.Values(options[Property.ForceInclude]));
            }

            if (parser.IsSet(options[Property.PrependInclude])) {
                properties[Property.PrependInclude] =
                    string.Join(";", parser.Values(options[Property.PrependInclude]));
            }

            if (parser.IsSet(options[Property.Include])) {
                properties[Property.Include] =
                    string.Join(";", parser.Values(options[Property.Include]));
            }

            if (parser.IsSet(options[Property.NoNotesWarnings])) {
                properties[Property.NoNotesWarnings] =
                    string.Join(";", parser.Values(options[Property.NoNotesWarnings]));
            }

            if (parser.IsSet(options[Property.NoNotes]))
                properties[Property.NoNotes] = "true";

            if (parser.IsSet(options[Property.NoWarnings]))
                properties[Property.NoWarnings] = "true";

            if (parser.IsSet(options[Property.IgnoreConflicts]))
                properties[Property.IgnoreConflicts] = "true";

            return true;
        }

        public string GenerateCommandLine(QtMsBuildContainer container, object propertyStorage)
        {
            var cmd = new StringBuilder();
            cmd.AppendFormat(@"""{0}\bin\{1}"" ""{2}"" -o ""{3}""",
                container.GetPropertyValue(propertyStorage, Property.QTDIR),
                ToolExecName,
                container.GetPropertyValue(propertyStorage, Property.InputFile),
                container.GetPropertyValue(propertyStorage, Property.OutputFile));

            string value = container.GetPropertyValue(propertyStorage, Property.IncludePath);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.IncludePath], value, true);

            value = container.GetPropertyValue(propertyStorage, Property.MacFramework);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.MacFramework], value);

            if (container.GetPropertyValue(propertyStorage, Property.PreprocessOnly) == "true")
                GenerateCommandLineOption(cmd, options[Property.PreprocessOnly]);

            value = container.GetPropertyValue(propertyStorage, Property.Define);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.Define], value);

            value = container.GetPropertyValue(propertyStorage, Property.Undefine);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.Undefine], value);

            value = container.GetPropertyValue(propertyStorage, Property.Metadata);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.Metadata], value, true);

            value = container.GetPropertyValue(propertyStorage, Property.CompilerFlavor);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.CompilerFlavor], value);

            if (container.GetPropertyValue(propertyStorage, Property.NoInclude) == "true")
                GenerateCommandLineOption(cmd, options[Property.NoInclude]);

            value = container.GetPropertyValue(propertyStorage, Property.PathPrefix);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.PathPrefix], value, true);

            value = container.GetPropertyValue(propertyStorage, Property.ForceInclude);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.ForceInclude], value, true);

            value = container.GetPropertyValue(propertyStorage, Property.PrependInclude);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.PrependInclude], value, true);

            value = container.GetPropertyValue(propertyStorage, Property.Include);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.Include], value, true);

            value = container.GetPropertyValue(propertyStorage, Property.NoNotesWarnings);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.NoNotesWarnings], value, true);

            if (container.GetPropertyValue(propertyStorage, Property.NoNotes) == "true")
                GenerateCommandLineOption(cmd, options[Property.NoNotes]);

            if (container.GetPropertyValue(propertyStorage, Property.NoWarnings) == "true")
                GenerateCommandLineOption(cmd, options[Property.NoWarnings]);

            if (container.GetPropertyValue(propertyStorage, Property.IgnoreConflicts) == "true")
                GenerateCommandLineOption(cmd, options[Property.IgnoreConflicts]);

            return cmd.ToString();
        }

    }

    public sealed class QtRcc : QtTool
    {
        public const string ItemTypeName = "QtRcc";
        public const string ToolExecName = "rcc.exe";

        public enum Property
        {
            ExecutionDescription,
            QTDIR,
            InputFile,
            OutputFile,
            TempFile,
            InitFuncName,
            Root,
            Compression,
            NoCompression,
            CompressThreshold,
            BinaryOutput,
            PassNumber,
            NoNamespace,
            Verbose,
            List,
            Project,
            FormatVersion,
            CommandLineTemplate,
            AdditionalOptions,
            DynamicSource,
            ParallelProcess,
            AdditionalDependencies,
        }

        Dictionary<Property, CommandLineOption> options
            = new Dictionary<Property, CommandLineOption>();

        public QtRcc() : base()
        {
            parser.AddOption(options[Property.TempFile] =
                new CommandLineOption(new string[] { "t", "temp" }, "file"));

            parser.AddOption(options[Property.InitFuncName] =
                new CommandLineOption("name", "name"));

            parser.AddOption(options[Property.Root] =
                new CommandLineOption("root", "path"));

            parser.AddOption(options[Property.Compression] =
                new CommandLineOption("compress", "level"));

            parser.AddOption(options[Property.NoCompression] =
                new CommandLineOption("no-compress"));

            parser.AddOption(options[Property.CompressThreshold] =
                new CommandLineOption("threshold", "level"));

            parser.AddOption(options[Property.BinaryOutput] =
                new CommandLineOption("binary"));

            parser.AddOption(options[Property.PassNumber] =
                new CommandLineOption("pass", "number"));

            parser.AddOption(options[Property.NoNamespace] =
                new CommandLineOption("namespace"));

            parser.AddOption(options[Property.Verbose] =
                new CommandLineOption("verbose"));

            parser.AddOption(options[Property.List] =
                new CommandLineOption("list"));

            parser.AddOption(options[Property.Project] =
                new CommandLineOption("project"));

            parser.AddOption(options[Property.FormatVersion] =
                new CommandLineOption("format-version", "number"));
        }

        public bool ParseCommandLine(
            string commandLine,
            IVSMacroExpander macros,
            out Dictionary<Property, string> properties)
        {
            properties = new Dictionary<Property, string>();

            string qtDir, inputPath, outputPath;
            if (!ParseCommandLine(
                commandLine,
                macros,
                ToolExecName,
                out qtDir,
                out inputPath,
                out outputPath)) {
                return false;
            }

            if (!string.IsNullOrEmpty(qtDir))
                properties[Property.QTDIR] = qtDir;

            if (!string.IsNullOrEmpty(inputPath))
                properties[Property.InputFile] = inputPath;

            if (!string.IsNullOrEmpty(outputPath))
                properties[Property.OutputFile] = outputPath;

            if (parser.IsSet(options[Property.InitFuncName]))
                properties[Property.InitFuncName] = parser.Value(options[Property.InitFuncName]);

            if (parser.IsSet(options[Property.Root]))
                properties[Property.Root] = parser.Value(options[Property.Root]);

            if (parser.IsSet(options[Property.Compression])) {
                int level;
                if (!int.TryParse(parser.Value(options[Property.Compression]), out level))
                    return false;
                if (level < 1 || 9 < level)
                    return false;
                properties[Property.Compression] = string.Format("level{0}", level);
            } else {
                properties[Property.Compression] = "default";
            }

            if (parser.IsSet(options[Property.NoCompression]))
                properties[Property.NoCompression] = "true";

            if (parser.IsSet(options[Property.CompressThreshold])) {
                properties[Property.CompressThreshold] =
                    parser.Value(options[Property.CompressThreshold]);
            }

            if (parser.IsSet(options[Property.BinaryOutput]))
                properties[Property.BinaryOutput] = "true";

            if (parser.IsSet(options[Property.PassNumber]))
                properties[Property.PassNumber] = parser.Value(options[Property.PassNumber]);

            if (parser.IsSet(options[Property.NoNamespace]))
                properties[Property.NoNamespace] = "true";

            if (parser.IsSet(options[Property.Verbose]))
                properties[Property.Verbose] = "true";

            if (parser.IsSet(options[Property.List]))
                properties[Property.List] = "true";

            if (parser.IsSet(options[Property.Project]))
                properties[Property.Project] = "true";

            if (parser.IsSet(options[Property.FormatVersion]))
                properties[Property.FormatVersion] = parser.Value(options[Property.FormatVersion]);

            return true;
        }

        public string GenerateCommandLine(QtMsBuildContainer container, object propertyStorage)
        {
            var cmd = new StringBuilder();
            cmd.AppendFormat(@"""{0}\bin\{1}"" ""{2}"" -o ""{3}""",
                container.GetPropertyValue(propertyStorage, Property.QTDIR),
                ToolExecName,
                container.GetPropertyValue(propertyStorage, Property.InputFile),
                container.GetPropertyValue(propertyStorage, Property.OutputFile));

            string value = container.GetPropertyValue(propertyStorage, Property.InitFuncName);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.InitFuncName], value);

            value = container.GetPropertyValue(propertyStorage, Property.Root);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.Root], value, true);

            value = container.GetPropertyValue(propertyStorage, Property.Compression);
            if (value.StartsWith("level")) {
                GenerateCommandLineOption(cmd,
                    options[Property.Compression],
                    value.Substring(5), true);
            }

            if (container.GetPropertyValue(propertyStorage, Property.NoCompression) == "true")
                GenerateCommandLineOption(cmd, options[Property.NoCompression]);

            value = container.GetPropertyValue(propertyStorage, Property.CompressThreshold);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.CompressThreshold], value);

            if (container.GetPropertyValue(propertyStorage, Property.BinaryOutput) == "true")
                GenerateCommandLineOption(cmd, options[Property.BinaryOutput]);

            value = container.GetPropertyValue(propertyStorage, Property.PassNumber);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.PassNumber], value);

            if (container.GetPropertyValue(propertyStorage, Property.Verbose) == "true")
                GenerateCommandLineOption(cmd, options[Property.Verbose]);

            if (container.GetPropertyValue(propertyStorage, Property.List) == "true")
                GenerateCommandLineOption(cmd, options[Property.List]);

            if (container.GetPropertyValue(propertyStorage, Property.Project) == "true")
                GenerateCommandLineOption(cmd, options[Property.Project]);

            value = container.GetPropertyValue(propertyStorage, Property.FormatVersion);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.FormatVersion], value);

            return cmd.ToString();
        }
    }

    public sealed class QtRepc : QtTool
    {
        public const string ItemTypeName = "QtRepc";
        public const string ToolExecName = "repc.exe";

        public enum Property
        {
            ExecutionDescription,
            QTDIR,
            InputFileType,
            InputFile,
            OutputFileType,
            OutputFile,
            IncludePath,
            AlwaysClass,
            PrintDebug,
        }

        Dictionary<Property, CommandLineOption> options
            = new Dictionary<Property, CommandLineOption>();

        public QtRepc() : base(defaultInputOutput: false)
        {
            parser.AddOption(options[Property.InputFileType] =
                new CommandLineOption("i")
                {
                    ValueName = "<rep|src>",
                    Flags = CommandLineOption.Flag.ShortOptionStyle
                });

            parser.AddOption(options[Property.OutputFileType] =
                new CommandLineOption("o")
                {
                    ValueName = "<source|replica|merged|rep>",
                    Flags = CommandLineOption.Flag.ShortOptionStyle
                });

            parser.AddOption(options[Property.IncludePath] =
                new CommandLineOption("I")
                {
                    ValueName = "dir",
                    Flags = CommandLineOption.Flag.ShortOptionStyle
                });

            parser.AddOption(options[Property.AlwaysClass] =
                new CommandLineOption("c"));

            parser.AddOption(options[Property.PrintDebug] =
                new CommandLineOption("d"));
        }

        protected override void ExtractInputOutput(
            string toolExecName,
            out string inputPath,
            out string outputPath)
        {
            inputPath = outputPath = "";

            var args = new Queue<string>(parser.PositionalArguments
                .Where(arg => !arg.EndsWith(toolExecName,
                    StringComparison.InvariantCultureIgnoreCase)));

            if (args.Any())
                inputPath = args.Dequeue();

            if (args.Any())
                outputPath = args.Dequeue();
        }

        public bool ParseCommandLine(
            string commandLine,
            IVSMacroExpander macros,
            out Dictionary<Property, string> properties)
        {
            properties = new Dictionary<Property, string>();

            string qtDir, inputPath, outputPath;
            if (!ParseCommandLine(
                commandLine,
                macros,
                ToolExecName,
                out qtDir,
                out inputPath,
                out outputPath)) {
                return false;
            }

            if (!string.IsNullOrEmpty(qtDir))
                properties[Property.QTDIR] = qtDir;

            if (parser.IsSet(options[Property.InputFileType])) {
                properties[Property.InputFileType] =
                    string.Join(";", parser.Values(options[Property.InputFileType]));
            }

            if (!string.IsNullOrEmpty(inputPath))
                properties[Property.InputFile] = inputPath;

            if (parser.IsSet(options[Property.OutputFileType])) {
                properties[Property.OutputFileType] =
                    string.Join(";", parser.Values(options[Property.OutputFileType]));
            }

            if (!string.IsNullOrEmpty(outputPath))
                properties[Property.OutputFile] = outputPath;

            if (parser.IsSet(options[Property.IncludePath])) {
                properties[Property.IncludePath] =
                    string.Join(";", parser.Values(options[Property.IncludePath]));
            }

            if (parser.IsSet(options[Property.AlwaysClass]))
                properties[Property.AlwaysClass] = "true";

            if (parser.IsSet(options[Property.PrintDebug]))
                properties[Property.PrintDebug] = "true";

            return true;
        }

        public string GenerateCommandLine(QtMsBuildContainer container, object propertyStorage)
        {
            var cmd = new StringBuilder();
            cmd.AppendFormat(@"""{0}\bin\{1}""",
                container.GetPropertyValue(propertyStorage, Property.QTDIR), ToolExecName);

            var inputType = container.GetPropertyValue(propertyStorage, Property.InputFileType);
            if (!string.IsNullOrEmpty(inputType))
                GenerateCommandLineOption(cmd, options[Property.InputFileType], inputType);

            var outputType = container.GetPropertyValue(propertyStorage, Property.OutputFileType);
            if (!string.IsNullOrEmpty(outputType))
                GenerateCommandLineOption(cmd, options[Property.OutputFileType], outputType);

            string value = container.GetPropertyValue(propertyStorage, Property.IncludePath);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.IncludePath], value, true);

            if (container.GetPropertyValue(propertyStorage, Property.AlwaysClass) == "true")
                GenerateCommandLineOption(cmd, options[Property.AlwaysClass]);

            if (container.GetPropertyValue(propertyStorage, Property.PrintDebug) == "true")
                GenerateCommandLineOption(cmd, options[Property.PrintDebug]);

            value = container.GetPropertyValue(propertyStorage, Property.InputFile);
            if (!string.IsNullOrEmpty(value))
                cmd.AppendFormat(" \"{0}\"", value);

            value = container.GetPropertyValue(propertyStorage, Property.OutputFile);
            if (!string.IsNullOrEmpty(value))
                cmd.AppendFormat(" \"{0}\"", value);

            return cmd.ToString();
        }

    }

    public sealed class QtUic : QtTool
    {
        public const string ItemTypeName = "QtUic";
        public const string ToolExecName = "uic.exe";

        public enum Property
        {
            ExecutionDescription,
            QTDIR,
            InputFile,
            OutputFile,
            DisplayDependencies,
            NoProtection,
            NoImplicitIncludes,
            Postfix,
            Translate,
            Include,
            Generator,
            IdBased,
            CommandLineTemplate,
            AdditionalOptions,
            ParallelProcess,
            AdditionalDependencies,
        }

        Dictionary<Property, CommandLineOption> options
            = new Dictionary<Property, CommandLineOption>();

        public QtUic() : base()
        {
            parser.AddOption(options[Property.DisplayDependencies] =
                new CommandLineOption(new string[] { "d", "dependencies" }));

            parser.AddOption(options[Property.NoProtection] =
                new CommandLineOption(new string[] { "p", "no-protection" }));

            parser.AddOption(options[Property.NoImplicitIncludes] =
                new CommandLineOption(new string[] { "n", "no-implicit-includes" }));

            parser.AddOption(options[Property.Postfix] =
                new CommandLineOption("postfix", "postfix"));

            parser.AddOption(options[Property.Translate] =
                new CommandLineOption(new string[] { "tr", "translate" }, "function"));

            parser.AddOption(options[Property.Include] =
                new CommandLineOption("include", "include-file"));

            parser.AddOption(options[Property.Generator] =
                new CommandLineOption(new string[] { "g", "generator" }, "java|cpp"));

            parser.AddOption(options[Property.IdBased] =
                new CommandLineOption("idbased"));
        }

        public bool ParseCommandLine(
            string commandLine,
            IVSMacroExpander macros,
            out Dictionary<Property, string> properties)
        {
            properties = new Dictionary<Property, string>();

            string qtDir, inputPath, outputPath;
            if (!ParseCommandLine(
                commandLine,
                macros,
                ToolExecName,
                out qtDir,
                out inputPath,
                out outputPath)) {
                return false;
            }

            if (!string.IsNullOrEmpty(qtDir))
                properties[Property.QTDIR] = qtDir;

            if (!string.IsNullOrEmpty(inputPath))
                properties[Property.InputFile] = inputPath;

            if (!string.IsNullOrEmpty(outputPath))
                properties[Property.OutputFile] = outputPath;

            if (parser.IsSet(options[Property.DisplayDependencies]))
                properties[Property.DisplayDependencies] = "true";

            if (parser.IsSet(options[Property.NoProtection]))
                properties[Property.NoProtection] = "true";

            if (parser.IsSet(options[Property.NoImplicitIncludes]))
                properties[Property.NoImplicitIncludes] = "true";

            if (parser.IsSet(options[Property.Postfix]))
                properties[Property.Postfix] = parser.Value(options[Property.Postfix]);

            if (parser.IsSet(options[Property.Translate]))
                properties[Property.Translate] = parser.Value(options[Property.Translate]);

            if (parser.IsSet(options[Property.Include]))
                properties[Property.Include] = parser.Value(options[Property.Include]);

            if (parser.IsSet(options[Property.Generator]))
                properties[Property.Generator] = parser.Value(options[Property.Generator]);

            if (parser.IsSet(options[Property.IdBased]))
                properties[Property.IdBased] = "true";

            return true;
        }

        public string GenerateCommandLine(QtMsBuildContainer container, object propertyStorage)
        {
            var cmd = new StringBuilder();
            cmd.AppendFormat(@"""{0}\bin\{1}"" ""{2}"" -o ""{3}""",
                container.GetPropertyValue(propertyStorage, Property.QTDIR),
                ToolExecName,
                container.GetPropertyValue(propertyStorage, Property.InputFile),
                container.GetPropertyValue(propertyStorage, Property.OutputFile));

            if (container.GetPropertyValue(
                propertyStorage,
                Property.DisplayDependencies)
                == "true") {
                GenerateCommandLineOption(cmd, options[Property.DisplayDependencies]);
            }

            if (container.GetPropertyValue(propertyStorage, Property.NoProtection) == "true")
                GenerateCommandLineOption(cmd, options[Property.NoProtection]);

            if (container.GetPropertyValue(propertyStorage, Property.NoImplicitIncludes) == "true")
                GenerateCommandLineOption(cmd, options[Property.NoImplicitIncludes]);

            string value = container.GetPropertyValue(propertyStorage, Property.Postfix);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.Postfix], value);

            value = container.GetPropertyValue(propertyStorage, Property.Translate);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.Translate], value);

            value = container.GetPropertyValue(propertyStorage, Property.Include);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.Include], value);

            value = container.GetPropertyValue(propertyStorage, Property.Generator);
            if (!string.IsNullOrEmpty(value))
                GenerateCommandLineOption(cmd, options[Property.Generator], value);

            if (container.GetPropertyValue(propertyStorage, Property.IdBased) == "true")
                GenerateCommandLineOption(cmd, options[Property.IdBased]);

            return cmd.ToString();
        }
    }
}
