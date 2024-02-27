/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

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

        private List<string> Modules { get; }
        public List<string> DefaultModules { get; set; }

        public bool AddDefaultAppIcon { get; set; }
        public bool CreateStaticLibrary { get; set; }
        public bool UsePrecompiledHeader { get; set; }
        public bool InsertQObjectMacro { get; set; }
        public bool LowerCaseFileNames { get; set; }
        public UiClassInclusion UiClassInclusion { get; set; }

        public enum ProjectModels
        {
            MsBuild = 0,
            CMake = 1
        }

        public ProjectModels ProjectModel { get; set;}

        public IEnumerable<IWizardConfiguration> Configs { get; set; }
    }
}
