/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Workspace;
using Microsoft.VisualStudio.Workspace.Indexing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Core
{
    using CMake;
    public static partial class Instances
    {
        public static (CMakeProject ActiveProject, string RootPath)
            CMake => new(CMakeProject.ActiveProject, CMakeProject.ActiveProject.RootPath);
    }
}

namespace QtVsTools.Core.CMake
{
    using Common;
    using static SyntaxAnalysis.RegExpr;

    public partial class CMakeProject : Concurrent<CMakeProject>
    {
        public static CMakeProject ActiveProject { get; private set; }
        public string RootPath { get; private set; }

        public static CMakeProject Load(IWorkspace projectFolder)
        {
            if (projectFolder == null)
                return null;
            var self = new CMakeProject(projectFolder);
            if (!File.Exists(self.RootListsPath))
                return null;
            if (!StaticAtomic(() => ActiveProject == null, () => ActiveProject = self))
                return null;
            if (!File.Exists(self.PresetsPath) && !File.Exists(self.UserPresetsPath)) {
                File.WriteAllText(self.PresetsPath, NullPresetsText);
                File.WriteAllText(self.UserPresetsPath, NullPresetsText);
                self.Status = QtStatus.NullPresets;
            }
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(self.LoadAsync);
            return self;
        }

        public static void Unload()
        {
            var self = ActiveProject;
            if (!StaticAtomic(() => ActiveProject != null, () => ActiveProject = null))
                return;
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(self.UnloadAsync);
        }

        public static void Convert(string projectFolder)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () => await ConvertAsync(projectFolder));
        }

        public static async Task ConvertAsync(string projectFolder)
        {
            var project = new CMakeProject(projectFolder);
            if (project.TryLoadQtConfig())
                return;
            await project.RefreshAsync();
        }

        private CMakeProject(IWorkspace projectFolder)
        {
            Project = projectFolder;
            RootPath = Project.Location;
        }

        private CMakeProject(string projectFolder)
        {
            RootPath = projectFolder;
        }

        private async Task LoadAsync()
        {
            Index = Project.GetIndexWorkspaceService() as IIndexWorkspaceService3;
            FileWatcher = Project.GetFileWatcherService();
            SubscribeEvents();
            await CheckQtStatusAsync();
        }

        private async Task UnloadAsync()
        {
            UnsubscribeEvents();
            await CloseMessagesAsync();
        }

        private static LazyFactory StaticLazy { get; } = new LazyFactory();
        private LazyFactory Lazy { get; } = new LazyFactory();

        private IWorkspace Project { get; set; }
        private IIndexWorkspaceService3 Index { get; set; }
        private IFileWatcherService FileWatcher { get; set; }

        private string RootListsPath => Path.Combine(RootPath, "CMakeLists.txt");
        private string PresetsPath => Path.Combine(RootPath, "CMakePresets.json");
        private string UserPresetsPath => Path.Combine(RootPath, "CMakeUserPresets.json");
        private string SettingsPath => Path.Combine(RootPath, "CMakeSettings.json");

        private static HashSet<string> ProjectFileNames { get; } = new()
        {
            "CMakeLists.txt",
            "CMakePresets.json",
            "CMakeUserPresets.json",
            "CMakeSettings.json"
        };

        private static JObject NullPresets { get; } = new JObject { ["version"] = 3 };
        private static string NullPresetsText { get; } = NullPresets.ToString(Formatting.Indented);

        private JObject Presets { get; set; }
        private JObject UserPresets { get; set; }

        private Parser CMakeListsParser => Lazy.Get(() => CMakeListsParser, () =>
        {
            var cmakeListsParser
                = WordBoundary & "find_package" & Space & "(" & Space
                & (Word & Space & "NAMES" & Space).Optional()
                & new Token("qt_version", "Qt" & CharSet['5', '6']) & ~CharWord;
            return cmakeListsParser.Render(HorizSpace);
        });
    }
}
