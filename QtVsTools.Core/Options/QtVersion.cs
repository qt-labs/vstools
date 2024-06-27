/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QtVsTools.Core.Options
{
    using QtVsTools.Common;

    public class QtVersion : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool isDefault;
        public bool IsDefault
        {
            get => isDefault;
            set
            {
                if (isDefault == value)
                    return;
                isDefault = value;
                OnPropertyChanged();
            }
        }
        public bool InitialIsDefault { get; set; }

        private string name;
        public string Name
        {
            get => name;
            set
            {
                if (name == value)
                    return;
                name = value;
                OnPropertyChanged();
            }
        }

        public string InitialName { get; set; } = "";

        private string path;
        public string Path
        {
            get => path;
            set
            {
                if (path == value)
                    return;
                path = value;
                OnPropertyChanged();
            }
        }
        public string InitialPath { get; set; } = "";

        private BuildHost host;
        public BuildHost Host
        {
            get => host;
            set
            {
                if (host == value)
                    return;
                host = value;
                OnPropertyChanged();
            }
        }
        public BuildHost InitialHost { get; set; } = BuildHost.Windows;

        private string compiler;
        public string Compiler
        {
            get => compiler;
            set
            {
                if (compiler == value)
                    return;
                compiler = value;
                OnPropertyChanged();
            }
        }
        public string InitialCompiler { get; set; } = "msvc";

        private string errorMessage;
        public string ErrorMessage
        {
            get => errorMessage;
            set
            {
                if (errorMessage == value)
                    return;
                errorMessage = value;
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged();
            }
        }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public State State { get; set; } = State.Existing;

        public static IEnumerable<string> BuildHostStrings()
        {
            return EnumExt.GetValues<string>(typeof(BuildHost));
        }

        private void OnPropertyChanged([CallerMemberName] string member = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(member));
        }
    }
}
