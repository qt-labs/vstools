/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.Build.Framework.XamlTypes;

namespace QtVsTools.QtMsBuild
{
    [ExportDynamicEnumValuesProvider("QtVersionProvider")]
    [AppliesTo("IntegratedConsoleDebugging")]
    internal class QtVersionProvider :
        IDynamicEnumValuesProvider,
        IDynamicEnumValuesGenerator
    {

        [ImportingConstructor]
        protected QtVersionProvider(UnconfiguredProject project)
        { }

        public async Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair> opts)
        {
            return await Task.FromResult(this);
        }

        public bool AllowCustomValues => true;

        public async Task<ICollection<IEnumValue>> GetListedValuesAsync()
        {
            using (var qtVersions = Registry.CurrentUser.OpenSubKey(@"Software\Digia\Versions")) {

                return await Task.FromResult(
                    qtVersions.GetSubKeyNames()
                        .Select(x => new PageEnumValue(new EnumValue()
                        {
                            Name = x,
                            DisplayName = x
                        }))
                        .Cast<IEnumValue>()
                        .ToList());
            }
        }

        public async Task<IEnumValue> TryCreateEnumValueAsync(string userSuppliedValue)
        {
            return await Task.FromResult(new PageEnumValue(new EnumValue()
            {
                Name = userSuppliedValue,
                DisplayName = userSuppliedValue
            }));
        }
    }
}
