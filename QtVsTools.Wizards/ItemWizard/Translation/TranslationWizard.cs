/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace QtVsTools.Wizards.ItemWizard
{
    using Common;
    using Microsoft.VisualStudio.VCProjectEngine;
    using ProjectWizard;
    using QtVsTools.Common;
    using QtVsTools.Core;
    using Util;

    using static QtVsTools.Common.EnumExt;

    public sealed class TsWizardData : WizardData
    {
        public string TsFile { get; set; }
        public string CultureInfoName { get; set; }
        public List<KeyValuePair<string, string>> CultureInfos { get; set; }
    }

    public sealed class TranslationWizard : ProjectTemplateWizard
    {
        LazyFactory Lazy { get; } = new();

        protected override Options TemplateType => Options.ConsoleSystem | Options.GUISystem;

        enum NewTranslationItem
        {
            [String("safeitemname")] SafeItemName,
            [String("tsfilename")] TsFileName,
            [String("cultureinfoname")] CultureInfoName
        }

        protected override WizardData WizardData => Lazy.Get(() =>
            WizardData, () => new TsWizardData
            {
                DefaultModules = new List<string> { "core"}
            });

        protected override WizardWindow WizardWindow => Lazy.Get(() =>
            WizardWindow, () => new WizardWindow(title: "Qt Translation File Wizard")
            {
                new TranslationPage
                {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Translation File Wizard",
                    Message = @"This wizard will add a new Qt empty translation file to your "
                        + @"project. The wizard creates a .ts for the selected language.",
                    PreviousButtonEnabled = false,
                    NextButtonEnabled = false,
                    FinishButtonEnabled = true,
                    CancelButtonEnabled = true
                }
            });

        protected override void BeforeWizardRun()
        {
            if (WizardData is TsWizardData tmp) {
                tmp.TsFile = Parameter[NewTranslationItem.SafeItemName];
                tmp.CultureInfos = CultureInfo.GetCultures(CultureTypes.AllCultures)
                    .ToDictionary(
                        mc => mc.Name.Replace("-", "_"),
                        mc => mc.EnglishName,
                        StringComparer.OrdinalIgnoreCase
                    ).OrderBy(item => item.Value).ToList();
            }
        }

        protected override void BeforeTemplateExpansion()
        {
            if (WizardData is TsWizardData ts) {
                Parameter[NewTranslationItem.CultureInfoName] = ts.CultureInfoName;
                Parameter[NewTranslationItem.TsFileName] = $"{ts.TsFile}_{ts.CultureInfoName}.ts";
            }
        }

        protected override void Expand()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            VCRulePropertyStorageHelper.SetQtModules(Dte, WizardData.DefaultModules);
        }

        public override void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectItem.Object is not VCFile vcFile)
                return;

            var fullPath = vcFile.FullPath;
            TextAndWhitespace.Adjust(Dte, fullPath);

            if (HelperFunctions.IsTranslationFile(fullPath))
                vcFile.MoveToFilter(FakeFilter.TranslationFiles());
        }
    }
}
