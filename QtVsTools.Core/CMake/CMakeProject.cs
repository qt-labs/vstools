/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    using static Common.EnumExt;
    using static SyntaxAnalysis.RegExpr;

    public partial class CMakeProject : Concurrent<CMakeProject>
    {
        public static CMakeProject ActiveProject { get; private set; }
        public string RootPath { get; }

        public static void Load(IWorkspace projectFolder)
        {
            if (projectFolder == null)
                return;
            var self = new CMakeProject(projectFolder);
            if (!File.Exists(self.RootListsPath))
                return;
            if (!StaticAtomic(() => ActiveProject == null, () => ActiveProject = self))
                return;
            if (!File.Exists(self.PresetsPath) && !File.Exists(self.UserPresetsPath)) {
                File.WriteAllText(self.PresetsPath, NullPresetsText);
                File.WriteAllText(self.UserPresetsPath, NullPresetsText);
                self.Status = QtStatus.NullPresets;
            }
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(self.LoadAsync);
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
            if (project.TryLoadPresets())
                return;
            await project.RefreshAsync();
        }

        private static LazyFactory StaticLazy { get; } = new();
        private LazyFactory Lazy { get; } = new();

        private IWorkspace Project { get; }
        private IIndexWorkspaceService3 Index { get; set; }
        private IFileWatcherService FileWatcher { get; set; }

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
            Index = await Project.GetServiceAsync<IIndexWorkspaceService3>();
            FileWatcher = await Project.GetServiceAsync<IFileWatcherService>();
            SubscribeEvents();
            await CheckQtStatusAsync();
        }

        private async Task UnloadAsync()
        {
            UnsubscribeEvents();
            await CloseMessagesAsync();
        }

        private string RootListsPath => Path.Combine(RootPath, "CMakeLists.txt");
        private string PresetsPath => Path.Combine(RootPath, "CMakePresets.json");
        private string UserPresetsPath => Path.Combine(RootPath, "CMakeUserPresets.json");
        private string SettingsPath => Path.Combine(RootPath, "CMakeSettings.json");

        private static HashSet<string> ProjectFileNames { get; } = new(Utils.CaseIgnorer)
        {
            "CMakeLists.txt",
            "CMakePresets.json",
            "CMakeUserPresets.json",
            "CMakeSettings.json"
        };

        private static JObject NullPresets { get; } = new() { ["version"] = 3 };
        private static string NullPresetsText { get; } = NullPresets.ToString(Formatting.Indented);

        private JObject Presets { get; set; }
        private JObject UserPresets { get; set; }

        IEnumerable<string> ListFiles()
        {
            string[] lists = Array.Empty<string>();
            try {
                lists = Directory.GetFiles(RootPath, "CMakeLists.txt", SearchOption.AllDirectories);
            } catch (Exception ex) {
                ex.Log();
            }
            return lists;
        }

        private Parser CMakeListsParser => Lazy.Get(() => CMakeListsParser, () =>
        {
            var cmakeListsParser
                = WordBoundary & "find_package" & Space & "(" & Space
                & (Word & Space & "NAMES" & Space).Optional()
                & new Token("qt_version", "Qt" & CharSet['5', '6']) & ~CharWord;
            return cmakeListsParser.Render(HorizSpace);
        });

        private bool HasQtReference(IEnumerable<string> listFiles)
        {
            foreach (var listFile in listFiles) {
                var listFilePath = Path.Combine(RootPath, listFile);
                if (!File.Exists(listFilePath))
                    continue;
                try {
                    if (!CMakeListsParser.Parse(File.ReadAllText(listFilePath)).Any())
                        continue;
                    if (IsCompatible())
                        return true;
                    _ = ThreadHelper.JoinableTaskFactory.RunAsync(ShowIncompatibleProjectAsync);
                    return false;
                } catch (ParseErrorException) {
                }
            }
            return false;
        }

        private bool IsCompatible()
        {
            return !File.Exists(SettingsPath);
        }

        private static bool IsProjectFile(string path)
        {
            return ProjectFileNames.Contains(Path.GetFileName(path));
        }

        private bool IsAutoConfigurable()
        {
            var configs = Presets?["configurePresets"]?.Cast<JObject>();
            return configs == null || configs.All(x => x["hidden"] is JValue y && y.Value<bool>());
        }
    }
}
