/****************************************************************************
**
** Copyright (C) 2019 The Qt Company Ltd.
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

        public bool AllowCustomValues { get { return true; } }

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
