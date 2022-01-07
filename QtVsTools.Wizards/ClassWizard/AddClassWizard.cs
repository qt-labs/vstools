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

using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using QtVsTools.VisualStudio;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace QtVsTools.Wizards.ClassWizard
{
    using QtVsTools.Wizards.Common;
    using Util;

    public class AddClassWizard : IClassWizard
    {
        public WizardResult Run(EnvDTE.DTE dte, string name, string location)
        {
            IVsUIShell iVsUIShell;
            try {
                ServiceProvider serviceProvider = new ServiceProvider(dte as IServiceProvider);
                iVsUIShell = VsServiceProvider.GetService<SVsUIShell, IVsUIShell>();
            } catch {
                return WizardResult.Exception;
            }

            iVsUIShell.EnableModeless(0);

            try {
                iVsUIShell.GetDialogOwnerHwnd(out System.IntPtr hwnd);

                var addClassPage = new AddClassPage { Location = location };
                var wizard = new WizardWindow(new List<WizardPage> { addClassPage })
                {
                    Width = 955,
                    Height = 660,
                    MinWidth = 800,
                    MinHeight = 450,

                    Title = @"Add Class - " + name,
                    MaxWidth = double.PositiveInfinity,
                    MaxHeight = double.PositiveInfinity,
                    ResizeMode = System.Windows.ResizeMode.CanResize
                };
                WindowHelper.ShowModal(wizard, hwnd);
                if (!wizard.DialogResult.GetValueOrDefault(false))
                    return WizardResult.Canceled;

                IClassWizard classWizard = null;
                switch (addClassPage.Class.Kind) {
                case ClassKind.Gui:
                    classWizard = new GuiClassWizard();
                    break;
                case ClassKind.Core:
                    classWizard = new CoreClassWizard();
                    break;
                default:
                    throw new System.Exception("Unexpected class kind.");
                }

                var className = addClassPage.Class.DefaultName;
                className = Regex.Replace(className, @"[^a-zA-Z0-9_]", string.Empty);
                className = Regex.Replace(className, @"^[\d-]*\s*", string.Empty);
                var result = new ClassNameValidationRule().Validate(className, null);
                if (result != ValidationResult.ValidResult)
                    className = string.Empty;

                classWizard.Run(dte, className, addClassPage.Location);

            } catch {
                return WizardResult.Exception;
            } finally {
                iVsUIShell.EnableModeless(1);
            }

            return WizardResult.Finished;
        }
    }
}
