// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

#if _DEBUG_DISCOVERER
using System.Diagnostics;
#endif

namespace QtVsTools.TestAdapter
{
    using QtVsTools.Core.Common;

    [FileExtension(Resources.FileExtension)]
    [DefaultExecutorUri(Resources.ExecutorUriString)]
    public class QtTestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext,
            IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            _ = sources ?? throw new ArgumentNullException(nameof(sources));
            _ = discoveryContext ?? throw new ArgumentNullException(nameof(discoveryContext));
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = discoverySink ?? throw new ArgumentNullException(nameof(discoverySink));

#if _DEBUG_DISCOVERER
            Debugger.Launch();
#endif

            using var log = new Logger(logger);
            if (!TryGetTests(sources, discoveryContext, log, out var discoveredCases))
                return;

            var tests = discoveredCases.GroupBy(testCase => testCase.Source, Utils.CaseIgnorer);
            foreach (var test in tests) {
                log.SendMessage($"Adding Qt auto-test: '{Path.GetFileName(test.Key)}'.");

                foreach (var testCase in test)
                    discoverySink.SendTestCase(testCase);

                log.ForceSendMessage($"Found {test.Count()} tests in the executable: '{test.Key}'.");
            }

        }

        internal static bool TryGetTests(IEnumerable<string> sources,
            IDiscoveryContext discoveryContext, Logger log, out List<TestCase> discoveredCases)
        {
            discoveredCases = new List<TestCase>();

            var provider = discoveryContext.RunSettings?.GetSettings(Resources.GlobalSettingsName);
            var settings = (provider as QtTestGlobalSettingsProvider)?.Settings;
            if (settings == null) {
                log.ForceSendMessage("Error reading the 'QtTestGlobal' section from the "
                    + ".runsettings file. This section is required. No further attempts "
                    + "will be made to examine executable files.");
                return false;
            }

            provider = discoveryContext.RunSettings?.GetSettings(Resources.SettingsName);
            var userSettings = (provider as QtTestSettingsProvider)?.Settings;
            if (userSettings == null) {
                log.ForceSendMessage("QtTest section not found in the .runsettings file. "
                    + "Continuing with default settings.");
            }

            QtTestSettings.MergeSettings(settings, userSettings);
            log.SetShowAdapterOutput(settings.ShowAdapterOutput);
            QtTestSettings.PrintSettings(settings, logger: log);
            if (!settings.EnabledAdapter) {
                log.ForceSendMessage("Qt test adapter is disabled. Test discovery will not be "
                    + "performed.");
                return false;
            }

            var filtered = sources.Where(source => source != null
                && source.EndsWith(Resources.FileExtension, Utils.IgnoreCase)).ToList();

            if (settings.SubsystemConsoleOnly) {
                filtered = filtered.Where(source => Binary.TryGetType(source, log, out var type)
                    && type != Binary.Type.Gui).ToList();
            }

            if (!filtered.Any()) {
                log.SendMessage("No Qt auto-tests discovered; source list is empty.");
                return false;
            }

            foreach (var filePath in filtered) {
                var any = TryGetSymbols(filePath, settings, log, out var dataTags);
                if (!any) {
                    log.SendMessage("No auto-test functions found.");
                    continue;
                }

                List<SourceInfo> sourceInfos = null;
                using var diaSession = new DiaSession(filePath);
                foreach (var dataTag in dataTags) {
                    log.SendMessage("Auto-test functions found. "
                        + $"Type: '{dataTag.Key}', Symbols: '{string.Join(", ", dataTag.Value)}'.");

                    if (settings.ParsePdbFiles) {
                        foreach (var symbol in dataTag.Value) {
                            SourceInfo info;
                            try {
                                var data = diaSession.GetNavigationData(dataTag.Key, symbol);
                                info = new SourceInfo
                                {
                                    SymbolName = $"{dataTag.Key}::{symbol}",
                                    LineNumber = data?.MinLineNumber ?? 0,
                                    SourceFile = data?.FileName
                                };
                            } catch (Exception exception) {
                                log.SendMessage("An exception occurred while using DiaSession."
                                    + $" GetNavigationData({dataTag.Key}, {symbol}). Retrying..."
                                    + Environment.NewLine + exception, TestMessageLevel.Error);

                                sourceInfos ??= PdbParser.Parse(filePath, log);
                                if (!TryGetSymbol(sourceInfos, symbol, out info)) {
                                    log.SendMessage("Failed to retrieve source info. Giving up...",
                                        TestMessageLevel.Error);
                                }
                            }

                            log.SendMessage("Source info from PDB: "
                                + $"Symbol name: '{info.SymbolName}', "
                                + $"Line number: '{info.LineNumber}', "
                                + $"File name: '{info.SourceFile ?? "<unknown>"}'.");

                            discoveredCases.Add(
                                new TestCase
                                {
                                    FullyQualifiedName = info.SymbolName,
                                    ExecutorUri = Resources.ExecutorUri,
                                    Source = filePath,
                                    LineNumber = info.LineNumber,
                                    DisplayName = $"{symbol}()",
                                    CodeFilePath = info.SourceFile
                                }
                            );
                        }
                    } else {
                        discoveredCases.AddRange(
                            dataTag.Value.Select(
                                symbol => new TestCase
                                {
                                    FullyQualifiedName = $"{dataTag.Key}::{symbol}",
                                    ExecutorUri = Resources.ExecutorUri,
                                    Source = filePath,
                                    DisplayName = $"{symbol}()"
                                }
                            )
                        );
                    }
                }
            }

            return discoveredCases.Any();
        }

        private static readonly Regex CppIdentifierRegex = new("^[a-zA-Z_][a-zA-Z0-9_]*$",
            RegexOptions.Compiled);
        private static readonly Regex UsageRegex = new(@"Usage:\s.*?\s\[options\] "
            + @"\[testfunction\[:testdata\]\]", RegexOptions.Compiled);

        private static bool TryGetSymbols(string filePath, QtTestSettings settings, Logger log,
            out Dictionary<string, HashSet<string>> dataTags)
        {
            dataTags = null;
            var exe = Path.GetFileName(filePath ?? "");
            if (string.IsNullOrEmpty(filePath))
                return false;

            log.SendMessage($"Attempting to populate Qt auto-tests from executable: '{exe}'.");

            var id = 0;
            try {
                var output = RunProcess("-help -platform offscreen", filePath, settings,
                    "Started process to verify QtTest binary", log, out id);
                if (!output.Any(line => UsageRegex.IsMatch(line))) {
                    log.SendMessage($"Executable: '{exe}' is not a QtTest application.");
                    return false;
                }

                output = RunProcess("-datatags -platform offscreen", filePath, settings,
                    "Started process to retrieve QtTest data tags", log, out id);
                dataTags = output
                    .Select(line => line.Split(' '))
                    .Where(parts => parts.Length >= 2)
                    .Where(parts => CppIdentifierRegex.IsMatch(parts[0]) &&
                        CppIdentifierRegex.IsMatch(parts[1]))
                    .GroupBy(parts => parts[0])
                    .ToDictionary(
                        group => group.Key,
                        group => new HashSet<string>(group.Select(parts => parts[1]))
                    );
            } catch (InvalidOperationException) {
                log.SendMessage($"Failed to start process: '{exe}'.", TestMessageLevel.Error);
            } catch (TimeoutException) {
                log.SendMessage($"Process '{exe}' (PID: {id}) did not exit within the expected "
                    + "time. Terminating...", TestMessageLevel.Error);
            } catch (Exception exception) {
                log.SendMessage("An exception occurred while discovering Qt auto-tests."
                    + Environment.NewLine + exception, TestMessageLevel.Error);
            }

            return dataTags?.Any() == true;
        }

        private static bool TryGetSymbol(List<SourceInfo> sourceInfos, string symbolName,
            out SourceInfo outInfo)
        {
            outInfo = new SourceInfo
            {
                SymbolName = symbolName
            };
            foreach (var sourceInfo in sourceInfos) {
                if (sourceInfo.SymbolName is not { } name || !name.Contains(symbolName))
                    continue;
                outInfo = sourceInfo;
                return true;
            }

            return false;
        }

        private static List<string> RunProcess(string args, string filePath,
            QtTestSettings settings, string message, Logger log, out int id)
        {
            var exe = Path.GetFileName(filePath);
            var startupInfo = ProcessMonitor.CreateStartInfo(filePath, args,
                redirectStandardOutput: true, Path.GetDirectoryName(filePath), settings, log);

            var monitor = new ProcessMonitor();
            monitor.StartProcess(startupInfo);
            log.SendMessage($"{message}: '{exe}', PID: '{id = monitor.ProcessId}'.");
            monitor.WaitForExit(settings.DiscoveryTimeout);
            log.SendMessage($"Process '{exe}', PID: '{id}', exited on time. Exit code: "
                + $"'{monitor.ExitCode}'.");

            return monitor.StandardOutput ?? new List<string>();
        }
    }
}
