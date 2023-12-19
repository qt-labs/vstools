/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Workspace;
using Microsoft.VisualStudio.Workspace.Debug;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QtVsTools.Core.CMake
{
    using Common;

    public partial class CMakeProject : Concurrent<CMakeProject>
    {
        private async Task AddLaunchSettingsAsync(string target)
        {
            var hasQmlReference = HasQmlReference();
            var newTarget = hasQmlReference ? "QML Application" : "Qt Application";
            if (await CopyDefaultLaunchSettingsAsync(target, newTarget) is not { } launchVsJsonPath)
                return;

            JObject launchVs = null;
            try {
                launchVs = JObject.Parse(await Utils.ReadAllTextAsync(launchVsJsonPath));
            } catch (Exception e) {
                e.Log();
                return;
            }
            var launchConfigs = launchVs["configurations"]?.Where(
                    x => x["name"].Value<string>() is "QML Application" or "Qt Application"
                ).ToList();
            if (launchConfigs is null || !launchConfigs.Any())
                return;
            if (launchConfigs.FirstOrDefault() is not { } launchConfig)
                return;
            launchConfig["projectTarget"] = target;
            if (hasQmlReference && (launchConfig["args"] ??= new JArray()) is JArray args) {
                if (!args.Any(x => x.Value<string>().StartsWith("-qmljsdebugger=")))
                    args.Add($"-qmljsdebugger=file:{{{Guid.NewGuid()}}},block");
            }
            launchConfigs.Skip(1).ToList().ForEach(x => x.Remove());
            try {
                File.WriteAllText(launchVsJsonPath, launchVs.ToString(Formatting.Indented));
            } catch (Exception e) {
                e.Log();
            }
        }

        private async Task<string> CopyDefaultLaunchSettingsAsync(string target, string newName)
        {
            if (Config is null || Debug is null)
                return null;

            var path = target;
            if (!Debug.GetLaunchDebugTargetProviders(path, out var provider)) {
                path = target.Split('(', ')').Skip(1).FirstOrDefault();
                if (!Debug.GetLaunchDebugTargetProviders(path, out provider))
                    return null;
            }

            if (Config.AllProjectFileConfigurations is not { } configs)
                return null;
            if (configs.LastOrDefault(x => x.Target == target) is not { } config)
                return null;

            var newLaunchSettings = await Config.CreateCompositeLaunchSettingsAsync(config,
                new DebugLaunchActionContext(path, provider,
                    new ProjectTargetFileContext(config.FilePath, config.Target)),
                new PropertySettings { { "name", newName } });

            var res = await Config.CustomizeLaunchSettingsAsync(null, null,
                new ProjectConfiguration(config.FilePath, config.Target, newLaunchSettings),
                allowDuplicate: true, updateContent: true);
            return res?.Item1;
        }

        private async Task SelectLaunchSettingsAsync()
        {
            var launchConfig = Config.AllProjectFileConfigurations.FirstOrDefault(
                x => x.LaunchSettings["name"] is "QML Application" or "Qt Application"
            );
            if (launchConfig is {}) {
                await Config.SetCurrentProject(launchConfig,
                    launchConfig.LaunchSettings["name"] as string);
            }
        }
    }
}
