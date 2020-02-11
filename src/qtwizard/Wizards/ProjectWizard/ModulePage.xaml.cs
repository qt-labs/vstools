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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace QtVsTools.Wizards.ProjectWizard
{
    public partial class ModulePage : WizardPage
    {

        public class ModuleCheckBox : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private bool isChecked;
            private bool isEnabled;
            private QtModuleInfo module;

            public ModuleCheckBox()
            {
            }

            public ModuleCheckBox(QtModuleInfo module)
            {
                this.module = module;
                isChecked = isEnabled = false;
            }

            public QtModuleInfo Module
            {
                get { return module; }
                set
                {
                    module = value;
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("Module"));
                }
            }

            public bool IsChecked
            {
                get { return isChecked; }
                set
                {
                    isChecked = value;
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("IsChecked"));
                }
            }

            public bool IsEnabled
            {
                get { return isEnabled; }
                set
                {
                    isEnabled = value;
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("IsEnabled"));
                }
            }

            public string Content
            {
                get { return module.Name; }
            }

            public string Tag
            {
                get { return module.LibraryPrefix; }
            }

            public string ToolTip
            {
                get {
                    return string.Format(
                        "Select this if you want to include the {0} library",
                        module.Name);
                }
            }
        }

        public ObservableCollection<ModuleCheckBox> ModuleCheckBoxes { get; set; }

        public ModulePage()
        {
            InitializeComponent();

            ModuleCheckBoxes = new ObservableCollection<ModuleCheckBox>();
            var modules = QtModules.Instance.GetAvailableModuleInformation()
                .Where(x => x.Selectable)
                .OrderBy(x => x.Name);
            foreach (var module in modules)
                ModuleCheckBoxes.Add(new ModuleCheckBox(module));

            DataContext = this;
            Loaded += OnLoaded;
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

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            try {

                foreach (var checkBox in ModuleCheckBoxes) {
                    checkBox.IsChecked =
                        Data.DefaultModules.Contains(checkBox.Tag)
                        || Data.Modules.Contains(checkBox.Tag);

                    checkBox.IsEnabled =
                        !Data.DefaultModules.Contains(checkBox.Tag);
                }
            } catch {
                // Ignore if we can't find out if the library is installed.
                // We either disable and or un-check the button in the UI.
            }
        }

        private void CollectSelectedModules()
        {
            if (Data != null) {
                Data.Modules.Clear();
                foreach (var checkBox in ModuleCheckBoxes) {
                    if (checkBox.IsChecked)
                        Data.Modules.Add(checkBox.Module.LibraryPrefix);
                }
            }
        }
    }
}
