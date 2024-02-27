/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace QtVsTools.Package.MsBuild
{
    using Core;

    [ExportDynamicEnumValuesProvider("QtVersionProvider")]
    [AppliesTo("IntegratedConsoleDebugging")]
    internal class QtVersionProvider : IDynamicEnumValuesProvider, IDynamicEnumValuesGenerator
    {
        [ImportingConstructor]
        protected QtVersionProvider(UnconfiguredProject project)
        {}

        public async Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair> opts)
        {
            return await Task.FromResult(this);
        }

        public bool AllowCustomValues => true;

        public async Task<ICollection<IEnumValue>> GetListedValuesAsync()
        {
            return await Task.FromResult(QtVersionManager.GetVersions()
                .Select(x => new PageEnumValue(new EnumValue
                {
                    Name = x,
                    DisplayName = x
                }))
                .Cast<IEnumValue>()
                .ToList());
        }

        public async Task<IEnumValue> TryCreateEnumValueAsync(string userSuppliedValue)
        {
            return await Task.FromResult(new PageEnumValue(new EnumValue
            {
                Name = userSuppliedValue,
                DisplayName = userSuppliedValue
            }));
        }
    }
}
