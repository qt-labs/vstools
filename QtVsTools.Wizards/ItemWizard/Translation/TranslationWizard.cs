/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using EnvDTE;

namespace QtVsTools.Wizards.ItemWizard
{
    using QtVsTools.Common;
    using Core;
    using Wizards.Common;
    using Wizards.ProjectWizard;
    using Wizards.Util;

    using static QtVsTools.Common.EnumExt;

    public sealed class TsWizardData : WizardData
    {
        public string TsFile { get; set; }
        public string CultureInfoName { get; set; }
        public List<KeyValuePair<string, string>> CultureInfos { get; set; }
    }

    public sealed class TranslationWizard : ProjectTemplateWizard
    {
        LazyFactory Lazy { get; } = new LazyFactory();

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
                },
            });

        protected override void BeforeWizardRun()
        {
            var tmp = WizardData as TsWizardData;
            tmp.TsFile = Parameter[NewTranslationItem.SafeItemName];
            tmp.CultureInfos = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .ToDictionary(
                    mc => mc.Name.Replace("-", "_"),
                    mc => mc.EnglishName,
                    StringComparer.OrdinalIgnoreCase
                ).OrderBy(item => item.Value).ToList();
        }

        protected override void BeforeTemplateExpansion()
        {
            var tmp = WizardData as TsWizardData;
            Parameter[NewTranslationItem.CultureInfoName] = tmp.CultureInfoName;
            Parameter[NewTranslationItem.TsFileName] = tmp.TsFile + "_" + tmp.CultureInfoName + ".ts";
        }

        protected override void Expand()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            VCRulePropertyStorageHelper.SetQtModules(Dte, WizardData.DefaultModules);
        }

        public override void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            QtProject.AdjustWhitespace(Dte, projectItem.Properties.Item("FullPath").Value.ToString());
        }
    }
}
