/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

using static System.Environment;

namespace QtVsTools.Wizards.ProjectWizard
{
    using Common;
    using Core;
    using Core.MsBuild;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.VCProjectEngine;
    using QtVsTools.Common;
    using QtVsTools.Core.Common;
    using Util;

    public class TestWizard : ProjectTemplateWizard
    {
        private enum NewTest
        {
            [EnumExt.String("mocfile")] MocFile,
            [EnumExt.String("qtinstall")] QtInstall,
            [EnumExt.String("RunSettingsPropertyGroup")] RunSettingsPropertyGroup,
            [EnumExt.String("RunSettingsItemGroup")] RunSettingsItemGroup,
            [EnumExt.String("RunSettingsFilter")] RunSettingsFilter,
            [EnumExt.String("RunSettingsFilterItemGroup")] RunSettingsFilterItemGroup
        }

        protected override Options TemplateType => Options.Application | Options.ConsoleSystem;

        protected override WizardData WizardData => Lazy.Get(() =>
            WizardData, () => new WizardData
            {
                DefaultModules = new List<string> { "QtCore", "QtTest" }
            });

        protected override WizardWindow WizardWindow => Lazy.Get(() =>
            WizardWindow, () => new WizardWindow(title: "Qt Test Application Wizard")
            {
                new WizardIntroPage {
                    Data = WizardData,
                    Header = "Welcome to the Qt Test Application Wizard",
                    Message = "This wizard generates a Qt test application project."
                        + NewLine + NewLine + "To continue, click Next.",
                    PreviousButtonEnabled = false,
                    NextButtonEnabled = true,
                    FinishButtonEnabled = false,
                    CancelButtonEnabled = true
                },
                new ConfigPage {
                    Data = WizardData,
                    Header = "Welcome to the Qt Test Application Wizard",
                    Message = "Setup the configurations you want to include in your project. "
                        + "The recommended settings for this project are selected by default.",
                    PreviousButtonEnabled = true,
                    NextButtonEnabled = true,
                    FinishButtonEnabled = false,
                    CancelButtonEnabled = true,
                    PchSupportVisible = Visibility.Collapsed,
                    ValidateConfigs = ValidateConfigs
                },
                new TestPage {
                    Data = WizardData,
                    Header = "Welcome to the Qt Test Application Wizard",
                    Message = "This wizard generates a Qt test application project.",
                    PreviousButtonEnabled = true,
                    NextButtonEnabled = false,
                    FinishButtonEnabled = true,
                    CancelButtonEnabled = true
                }
            });

        private static string ValidateConfigs(IEnumerable<IWizardConfiguration> configs)
        {
            foreach (var config in configs) {
                if (config.Target.EqualTo(ProjectTargets.WindowsStore)) {
                    return $"Qt test application project not available for the '{config.Target}' "
                        + "target.";
                }
            }
            return string.Empty;
        }

        protected override void BeforeWizardRun()
        {
            var safeProjectName = ClassNameValidationRule.SafeName(Parameter[NewProject.SafeName],
                "QtTest");

            WizardData.ClassName = safeProjectName;
            WizardData.ClassSourceFile = safeProjectName + ".cpp";
        }

        protected override void BeforeTemplateExpansion()
        {
            Parameter[NewClass.ClassName] = WizardData.ClassName;
            Parameter[NewClass.SourceFileName] = WizardData.ClassSourceFile;
            Parameter[NewTest.MocFile] =
                Path.ChangeExtension(WizardData.ClassSourceFile, ".moc");

            foreach (var configuration in Configurations) {
                if (!configuration.IsDebug)
                    continue;
                Parameter[NewTest.QtInstall] = configuration.QtVersionName;
                break;
            }

            Parameter[NewTest.RunSettingsPropertyGroup] = "";
            Parameter[NewTest.RunSettingsItemGroup] = "";
            Parameter[NewTest.RunSettingsFilter] = "";
            Parameter[NewTest.RunSettingsFilterItemGroup] = "";
            if (!WizardData.AddRunSettingsToProject)
                return;

            const string runSettingsPropertyGroup =
@"  <PropertyGroup>
    <RunSettingsFilePath>$(MSBuildProjectDirectory)\$projectname$.runsettings</RunSettingsFilePath>
  </PropertyGroup>";
            Parameter[NewTest.RunSettingsPropertyGroup] =
                runSettingsPropertyGroup.Replace("$projectname$", Parameter[NewProject.Name]);

            const string runSettingsItemGroup =
@"  <ItemGroup>
    <None Include=""$projectname$.runsettings"" />
  </ItemGroup>";
            Parameter[NewTest.RunSettingsItemGroup] =
                runSettingsItemGroup.Replace("$projectname$", Parameter[NewProject.Name]);

            Parameter[NewTest.RunSettingsFilter] =
@"    <Filter Include=""Project Items"">
      <UniqueIdentifier>{cef0649c-da9a-4118-907c-6d516b118863}</UniqueIdentifier>
    </Filter>";

            const string runSettingsFilterItemGroup =
@"  <ItemGroup>
    <None Include=""$projectname$.runsettings"">
      <Filter>Project Items</Filter>
    </None>
  </ItemGroup>";
            Parameter[NewTest.RunSettingsFilterItemGroup] =
                runSettingsFilterItemGroup.Replace("$projectname$", Parameter[NewProject.Name]);
        }

        protected override void OnProjectGenerated(EnvDTE.Project dteProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try {
                if (MsBuildProject.GetOrAdd(dteProject.Object as VCProject) is {} project) {
                    QtMoc.SetMocItemType(
                        project.GetFilesFromProject(Parameter[NewClass.SourceFileName]).First()
                    );
                }
            } catch (Exception exception) {
                exception.Log();
            }

            if (!WizardData.AddRunSettingsToProject)
                return;
            var runSettingsFile = Path.Combine(Parameter[NewProject.DestinationDirectory],
                Parameter[NewProject.Name] + ".runsettings");
            if (File.Exists(runSettingsFile))
                return;
            try {
                var templateRunSettingsFile = Path.Combine(Utils.PackageInstallPath,
                    @"ProjectTemplates\VC\Qt\1033\test\.runsettings");
                var content = File.ReadAllText(templateRunSettingsFile);
                content = content.Replace("$qtinstall$", Parameter[NewTest.QtInstall]);
                File.WriteAllText(runSettingsFile, content);
            } catch (Exception exception) {
                exception.Log();
            }
        }
    }
}
