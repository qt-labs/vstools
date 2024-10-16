/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Wizards.ProjectWizard
{
    using Common;
    using Core.MsBuild;
    using QtVsTools.Common;
    using Util;

    using static QtVsTools.Common.EnumExt;

    public class DesignerWizard : ProjectTemplateWizard
    {
        protected override Options TemplateType =>
            Options.PluginProject | Options.DynamicLibrary | Options.GUISystem;

        enum NewDesignerPlugin
        {
            [String("plugin_class")] ClassName,
            [String("objname")] ObjectName,
            [String("pluginsourcefilename")] SourceFileName,
            [String("pluginheaderfilename")] HeaderFileName,
            [String("plugin_json")] JsonFileName
        }

        protected override WizardData WizardData => Lazy.Get(() =>
            WizardData, () => new WizardData
            {
                DefaultModules = new List<string> {
                    "QtCore", "QtGui", "QtWidgets", "QtDesigner"
                }
            });

        protected override WizardWindow WizardWindow => Lazy.Get(() =>
            WizardWindow, () => new WizardWindow(title: "Qt Designer Custom Widget Wizard")
            {
                new WizardIntroPage {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Designer Custom Widget Wizard",
                    Message = @"This wizard generates a designer custom widget which can be "
                        + @"used in Qt Designer or Visual Studio."
                        + System.Environment.NewLine + System.Environment.NewLine
                        + "To continue, click Next.",
                    PreviousButtonEnabled = false,
                    NextButtonEnabled = true,
                    FinishButtonEnabled = false,
                    CancelButtonEnabled = true
                },
                new ConfigPage {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Designer Custom Widget Wizard",
                    Message =
                            @"Setup the configurations you want to include in your project. "
                            + @"The recommended settings for this project are selected by default.",
                    PreviousButtonEnabled = true,
                    NextButtonEnabled = true,
                    FinishButtonEnabled = false,
                    CancelButtonEnabled = true,
                    ValidateConfigs = ValidateConfigsForDesignerWidget
                },
                new DesignerPage {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Designer Custom Widget Wizard",
                    Message = @"This wizard generates a designer custom widget which can be "
                        + @"used in Qt Designer or Visual Studio.",
                    PreviousButtonEnabled = true,
                    NextButtonEnabled = false,
                    FinishButtonEnabled = true,
                    CancelButtonEnabled = true
                }
            }
        );

        string ValidateConfigsForDesignerWidget(IEnumerable<IWizardConfiguration> configs)
        {
            foreach (var config in configs) {
                if (config.Target.EqualTo(ProjectTargets.WindowsStore)) {
                    return "Custom Designer Widget project not available for the "
                        + $"'{config.Target}' target.";
                }
            }
            return string.Empty;
        }

        protected override void BeforeWizardRun()
        {
            var className = ClassNameValidationRule.SafeName(Parameter[NewProject.SafeName],
                "MyDesignerWidget");

            WizardData.ClassName = className;
            WizardData.BaseClass = @"QWidget";
            WizardData.ClassHeaderFile = className + @".h";
            WizardData.ClassSourceFile = className + @".cpp";

            WizardData.PluginClass = className + @"Plugin";
            WizardData.PluginHeaderFile = WizardData.PluginClass + @".h";
            WizardData.PluginSourceFile = WizardData.PluginClass + @".cpp";
        }

        protected override void BeforeTemplateExpansion()
        {
            Parameter[NewClass.ClassName] = WizardData.ClassName;
            Parameter[NewClass.BaseClass] = WizardData.BaseClass;
            Parameter[NewClass.HeaderFileName] = WizardData.ClassHeaderFile;
            Parameter[NewClass.SourceFileName] = WizardData.ClassSourceFile;

            var include = new StringBuilder();
            if (UsePrecompiledHeaders)
                include.AppendLine($"#include \"{PrecompiledHeader.Include}\"");
            include.AppendLine($"#include \"{WizardData.ClassHeaderFile}\"");
            Parameter[NewClass.Include] = FormatParam(include);

            Parameter[NewDesignerPlugin.ClassName] = WizardData.PluginClass;
            Parameter[NewDesignerPlugin.HeaderFileName] = WizardData.PluginHeaderFile;
            Parameter[NewDesignerPlugin.SourceFileName] = WizardData.PluginSourceFile;
            Parameter[NewDesignerPlugin.JsonFileName] = WizardData.PluginClass.ToLower() + ".json";
            Parameter[NewDesignerPlugin.ObjectName] =
                $"{WizardData.ClassName[0]}{WizardData.ClassName.Substring(1)}";
        }

        protected override void OnProjectGenerated(EnvDTE.Project dteProject)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            if (MsBuildProject.GetOrAdd(dteProject.Object as VCProject) is {} project)
                project.MarkAsQtPlugin();
        }
    }
}
