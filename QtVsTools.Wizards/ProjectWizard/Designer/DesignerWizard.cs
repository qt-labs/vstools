/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using EnvDTE;
using QtVsTools.Common;
using QtVsTools.Wizards.Common;

namespace QtVsTools.Wizards.ProjectWizard
{
    using static EnumExt;

    public class DesignerWizard : ProjectTemplateWizard
    {
        protected override Options TemplateType =>
            Options.PluginProject | Options.DynamicLibrary | Options.GUISystem;

        enum NewClass
        {
            [String("classname")] ClassName,
            [String("baseclass")] BaseClass,
            [String("sourcefilename")] SourceFileName,
            [String("headerfilename")] HeaderFileName,
            [String("include")] Include,
        }

        enum NewDesignerPlugin
        {
            [String("plugin_class")] ClassName,
            [String("objname")] ObjectName,
            [String("pluginsourcefilename")] SourceFileName,
            [String("pluginheaderfilename")] HeaderFileName,
            [String("plugin_json")] JsonFileName,
        }

        WizardData _WizardData;
        protected override WizardData WizardData => _WizardData
            ?? (_WizardData = new WizardData
            {
                DefaultModules = new List<string> {
                    "QtCore", "QtGui", "QtWidgets", "QtXml"
                }
            });

        string[] _ExtraModules;
        protected override IEnumerable<string> ExtraModules => _ExtraModules
            ?? (_ExtraModules = new[] { "designer" });

        string[] _ExtraDefines;
        protected override IEnumerable<string> ExtraDefines => _ExtraDefines
            ?? (_ExtraDefines = new[] { "QT_PLUGIN" });

        WizardWindow _WizardWindow;
        protected override WizardWindow WizardWindow => _WizardWindow
            ?? (_WizardWindow = new WizardWindow(title: "Qt Custom Designer Widget")
            {
                new WizardIntroPage {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Custom Designer Widget",
                    Message = @"This wizard generates a custom designer widget which can be "
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
                    Header = @"Welcome to the Qt Custom Designer Widget",
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
                    Header = @"Welcome to the Qt Custom Designer Widget",
                    Message = @"This wizard generates a custom designer widget which can be "
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
                    return string.Format(
                        "Custom Designer Widget project not available for the '{0}' target.",
                        config.Target);
                }
            }
            return string.Empty;
        }

        protected override void BeforeWizardRun()
        {
            var className = Parameter[NewProject.SafeName];
            className = Regex.Replace(className, @"[^a-zA-Z0-9_]", string.Empty);
            className = Regex.Replace(className, @"^[\d-]*\s*", string.Empty);
            var result = new Util.ClassNameValidationRule().Validate(className, null);
            if (result != ValidationResult.ValidResult)
                className = @"MyDesignerWidget";

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
                include.AppendLine(string.Format("#include \"{0}\"", PrecompiledHeader.Include));
            include.AppendLine(string.Format("#include \"{0}\"", WizardData.ClassHeaderFile));
            Parameter[NewClass.Include] = FormatParam(include);

            Parameter[NewDesignerPlugin.ClassName] = WizardData.PluginClass;
            Parameter[NewDesignerPlugin.HeaderFileName] = WizardData.PluginHeaderFile;
            Parameter[NewDesignerPlugin.SourceFileName] = WizardData.PluginSourceFile;
            Parameter[NewDesignerPlugin.JsonFileName] = WizardData.PluginClass.ToLower() + ".json";
            Parameter[NewDesignerPlugin.ObjectName] = string.Format("{0}{1}",
                WizardData.ClassName[0],
                WizardData.ClassName.Substring(1));
        }

        protected override void OnProjectGenerated(Project project)
        {
            project.Globals["IsDesignerPlugin"] = true.ToString();
            if (!project.Globals.get_VariablePersists("IsDesignerPlugin"))
                project.Globals.set_VariablePersists("IsDesignerPlugin", true);
        }
    }
}
