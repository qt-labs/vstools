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

namespace QtVsTools.Wizards.Common
{
    using ProjectWizard;

    public class WizardData
    {
        public WizardData()
        {
            Modules = new List<string>();
            DefaultModules = new List<string>();
        }

        public string ClassName { get; set; }
        public string BaseClass { get; set; }
        public string PluginClass { get; set; }
        public string ConstructorSignature { get; set; }

        public string ClassHeaderFile { get; set; }
        public string ClassSourceFile { get; set; }
        public string PluginHeaderFile { get; set; }
        public string PluginSourceFile { get; set; }

        public string UiFile { get; set; }
        public string QrcFile { get; set; }

        public List<string> Modules { get; set; }
        public List<string> DefaultModules { get; set; }

        public bool AddDefaultAppIcon { get; set; }
        public bool CreateStaticLibrary { get; set; }
        public bool UsePrecompiledHeader { get; set; }
        public bool InsertQObjectMacro { get; set; }
        public bool LowerCaseFileNames { get; set; }
        public UiClassInclusion UiClassInclusion { get; set; }

        public IEnumerable<IWizardConfiguration> Configs { get; set; }
    }
}
