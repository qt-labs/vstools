// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Linq;
using Microsoft.VisualStudio.Workspace.Evaluator;

namespace QtVsTools.Core.CMake
{
    using QtVsTools.Common;
    using static QtVsTools.Common.EnumExt;

    public partial class CMakeProject : Concurrent<CMakeProject>
    {
        public enum Vars
        {
            [String("cmake")] Cache,
            [String("env")] Environment
        }

        public enum Cache
        {
            QtDir,
            QtFeature,
            [String("CMAKE_PREFIX_PATH")] PrefixPath,
            [String("CMAKE_GENERATOR")] Generator
        }

        public string this[string name] => this[Vars.Cache, name];

        public string this[Enum context, string name = ""] => context switch
        {
            Cache.QtDir when this[$"Qt{name}_DIR"] is { } qtDir => qtDir,
            Cache.QtDir when this[$"Qt6{name}_DIR"] is { } qt6Dir => qt6Dir,
            Cache.QtDir when this[$"Qt5{name}_DIR"] is { } qt5Dir => qt5Dir,
            Cache.QtFeature => this[$"QT_FEATURE_{name}"],
            Cache cacheVar => this[cacheVar.Cast<string>()],
            Vars vars => this[vars.Cast<string>(), name],
            _ => null
        };

        public string this[string nameSpace, string name]
        {
            get
            {
                var evaluators = PropertyEvaluator.GetPropertyEvaluators(nameSpace);
                var matchPropertyResults = PropertyEvaluator.SelectPropertyEvaluators(
                    new PropertyContext("CMakeList.txt", nameSpace, name, null), evaluators);
                return matchPropertyResults.FirstOrDefault()?.Property.Value;
            }
        }
    }
}
