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

using QtProjectLib;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace QtProjectWizard
{
    public partial class ModulePage : WizardPage
    {
        public ModulePage()
        {
            InitializeComponent();
            this.DataContext = this;

            try {
                var projectEngine = new QtProjectEngine();
                foreach (var checkBox in Children<CheckBox>(ModuleGrid))
                    checkBox.IsEnabled = projectEngine.IsModuleInstalled(checkBox.Name);

                foreach (var module in Data.DefaultModules) {
                    var checkbox = ModuleGrid.FindName(module) as CheckBox;
                    if (checkbox == null)
                        continue;
                    checkbox.IsChecked = projectEngine.IsModuleInstalled(module);
                    checkbox.IsEnabled = false; // Required module, always disabled.
                }
            } catch {
                // Ignore if we can't find out if the library is installed.
                // We either disable and or un-check the button in the UI.
            }
        }

        protected override void OnNextButtonClick(object sender, RoutedEventArgs e)
        {
            CollectSelectedModules();
            base.OnNextButtonClick(sender, e);
        }

        protected override void OnFinishButtonClick(object sender, RoutedEventArgs e)
        {
            CollectSelectedModules();
            base.OnFinishButtonClick(sender, e);
        }

        private void CollectSelectedModules()
        {
            if (Data != null) {
                Data.Modules.Clear();
                foreach (var checkBox in Children<CheckBox>(ModuleGrid)) {
                    if (checkBox.IsChecked.GetValueOrDefault())
                        Data.Modules.Add(checkBox.Name);
                }
            }
        }

        private IEnumerable<T> Children<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj == null)
                yield break;

            foreach (var tmp in LogicalTreeHelper.GetChildren(obj)) {
                var child = tmp as DependencyObject;
                if (child != null) {
                    if (child is T)
                        yield return child as T;
                    foreach (T t in Children<T>(child))
                        yield return t;
                }
            }
            yield break;
        }
    }
}
