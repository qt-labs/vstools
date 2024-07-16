/**************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
**************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace QtVsTools.TestAdapter
{
    using QtVsTools.Core.Common;
    using TestGroup = IGrouping<string, TestCase>;

    [ExtensionUri(Resources.ExecutorUriString)]
    public class QtTestExecutor : ITestExecutor
    {
        private IRunContext runContext;
        private IFrameworkHandle frameworkHandle;

        private readonly CancellationTokenSource cancellationSource = new();

        public void Cancel()
        {
            cancellationSource.Cancel();
            cancellationSource.Dispose();
        }

        public void RunTests(IEnumerable<string> sources, IRunContext context,
            IFrameworkHandle handle)
        {
            using var log = new Logger(frameworkHandle, "execution");
            if (QtTestDiscoverer.TryGetTests(sources, runContext, log, out var testCases))
                RunTests(testCases, context, handle);
        }

        public void RunTests(IEnumerable<TestCase> testCases, IRunContext context,
            IFrameworkHandle framework)
        {
            _ = testCases ?? throw new ArgumentNullException(nameof(testCases));
            runContext = context ?? throw new ArgumentNullException(nameof(context));
            frameworkHandle = framework ?? throw new ArgumentNullException(nameof(framework));

            using var log = new Logger(framework, "execution");

            var provider = context.RunSettings?.GetSettings(Resources.GlobalSettingsName);
            var settings = (provider as QtTestGlobalSettingsProvider)?.Settings;
            if (settings == null) {
                log.SendMessage("Error reading the 'QtTestGlobal' section from the .runsettings "
                    + "file. This section is expected to be always present. No further attempt is "
                    + "made to run executable files.");
                return;
            }

            provider = context.RunSettings?.GetSettings(Resources.SettingsName);
            var userSettings = (provider as QtTestSettingsProvider)?.Settings;
            if (userSettings == null)
                log.SendMessage("Cannot find QtTest section in .runsettings file.");

            QtTestSettings.MergeSettings(settings, userSettings);
            log.SetShowAdapterOutput(settings.ShowAdapterOutput);
            QtTestSettings.PrintSettings(settings, logger: log);

            var tasks = new List<Task>();
            var groupedTests = testCases.GroupBy(testCase => testCase.Source, Utils.CaseIgnorer)
                .ToImmutableList();
            foreach (var group in groupedTests) {
                foreach (var testCase in group)
                    frameworkHandle.RecordStart(testCase);
                tasks.Add(RunTestAsync(group, settings, log));
            }

#pragma warning disable VSTHRD002
            Task.WaitAll(tasks.ToArray(), cancellationSource.Token);
#pragma warning restore VSTHRD002
        }

        private static string Arguments(QtTestSettings settings)
        {
            var arguments = new StringBuilder();
            if (!string.IsNullOrEmpty(settings.Verbosity.Level))
                arguments.Append($" {settings.Verbosity.Level}");
            if (settings.Verbosity.LogSignals)
                arguments.Append(" -vs");

            if (settings.Commands.EventDelay >= 0)
                arguments.Append($" -eventdelay {settings.Commands.EventDelay}");
            if (settings.Commands.KeyDelay >= 0)
                arguments.Append($" -keydelay {settings.Commands.KeyDelay}");
            if (settings.Commands.EventDelay >= 0)
                arguments.Append($" -mousedelay {settings.Commands.MouseDelay}");
            if (settings.Commands.MaxWarnings != 2000)
                arguments.Append($" -maxwarnings {settings.Commands.MaxWarnings}");
            if (settings.Commands.NoCrashHandler)
                arguments.Append(" -nocrashhandler");

            if (settings.Output.FilenameFormats.Any())
                arguments.Append($" -o {string.Join(" -o ", settings.Output.FilenameFormats)}");
            return arguments.ToString();
        }

        private async Task RunTestAsync(TestGroup group, QtTestSettings settings, Logger log)
        {
            var filePath = group.Key;
            var tmpFile = Path.GetTempFileName();
            var arguments = Utils.JoinWithTransform(" ",
                    value => Regex.Replace(value.ToString(), @"\(.+$", ""),
                    group.Select(testCase => testCase.DisplayName))
                + Arguments(settings) + $" -o {Utils.SafeQuote(tmpFile)},xml";

            log.SendMessage($"Running Qt auto-test '{Path.GetFileName(filePath)}' "
                + $"with arguments: '{arguments}'"
                + $"{(runContext.IsBeingDebugged ? " attached to the debugger." : ".")}");

            try {
                await Task.Run(async () =>
                {
                    cancellationSource.Token.ThrowIfCancellationRequested();
                    var startInfo = ProcessMonitor.CreateStartInfo(filePath, arguments, false,
                        Path.GetDirectoryName(filePath), settings, log);

                    ProcessMonitor monitor;
                    if (runContext.IsBeingDebugged) {
                        var pid = frameworkHandle.LaunchProcessWithDebuggerAttached(filePath,
                            Path.GetDirectoryName(filePath), arguments, startInfo.Environment);
                        monitor = new ProcessMonitor(Process.GetProcessById(pid), cancellationSource);
                    } else {
                        (monitor = new ProcessMonitor(cancellationSource)).StartProcess(startInfo);
                    }

                    log.SendMessage($"Started process: '{filePath}', PID: '{monitor.ProcessId}'.");
                    monitor.WaitForExit(runContext.IsBeingDebugged ? -1 : settings.TestTimeout);
                    log.SendMessage($"Process finished, Exit code: '{monitor.ExitCode}'.");

                    var result = XmlParser.Parse(await Utils.ReadAllTextAsync(tmpFile));

                    var attachments = new List<UriDataAttachment>();
                    if (settings.Output.FilenameFormats.Any()) {
                        attachments.AddRange(settings.Output.FilenameFormats
                            .Select(value => value.Split(',').First())
                            .Select(Utils.Unquote)
                            .Select(file => new UriDataAttachment(new Uri(file), Path.GetFileName(file))));
                    }

                    foreach (var testCase in group) {
                        var name = Regex.Replace(testCase.DisplayName, @"\(.+$", "");
                        var testResult = new TestResult(testCase)
                        {
                            Outcome = TestOutcome.NotFound
                        };

                        if (result.TestFunctions.TryGetValue(name, out var testFunction)) {
                            testResult.Outcome = QtTestResult.MapType(testFunction.IncidentType);
                            testResult.Duration = TimeSpan.FromMilliseconds(testFunction.Duration);
                            foreach (var attachment in attachments) {
                                testResult.Attachments.Add(
                                    new AttachmentSet(Resources.ExecutorUri, Resources.SettingsName)
                                    {
                                        Attachments =
                                        {
                                            attachment
                                        }
                                    }
                                );
                                log.SendMessage($"Adding attachment: '{testResult.Attachments.Last()}");
                            }

                            if (testResult.Outcome == TestOutcome.Failed) {
                                testResult.ErrorMessage = testFunction.IncidentDescription;
                                testResult.ErrorStackTrace = $"at {testCase.FullyQualifiedName} in"
                                    + $" {testFunction.IncidentFile}:line {testFunction.IncidentLine}";
                            }
                        }

                        frameworkHandle.RecordEnd(testCase, testResult.Outcome);
                        frameworkHandle.RecordResult(testResult);
                    }

                    Utils.DeleteFile(tmpFile);
                }, cancellationSource.Token);
            } catch (OperationCanceledException) {
                log.SendMessage("Cancellation requested while running Qt auto-test "
                    + $"'{Path.GetFileName(filePath)}' with arguments: '{arguments}'.",
                    TestMessageLevel.Error);
                SetTestCasesDoNotHaveAnOutcome(group);
            } catch (Exception exception) {
                log.SendMessage("Exception was thrown while running Qt auto-test "
                    + $"'{Path.GetFileName(filePath)}' with arguments: '{arguments}'."
                    + Environment.NewLine + exception, TestMessageLevel.Error);
                SetTestCasesDoNotHaveAnOutcome(group);
            }
        }

        private void SetTestCasesDoNotHaveAnOutcome(IEnumerable<TestCase> testCases)
        {
            foreach (var testCase in testCases) {
                var testResult = new TestResult(testCase)
                {
                    Outcome = TestOutcome.None
                };
                frameworkHandle.RecordEnd(testCase, testResult.Outcome);
                frameworkHandle.RecordResult(testResult);
            }
        }
    }
}
