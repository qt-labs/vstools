/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

using static Microsoft.Build.Evaluation.ProjectCollection;

namespace QtVsTools.Test.QtMsBuild.Build
{
    using static SyntaxAnalysis.RegExpr;

    public partial class Logger : ILogger
    {
        private string FormatEvent(EventArgs e) => e switch
        {
            ProjectAddedToProjectCollectionEventArgs evt => FormatImport(evt),

            ProjectChangedEventArgs evt => FormatProjectChange(evt),

            ProjectXmlChangedEventArgs evt => FormatXmlChange(evt),

            TelemetryEventArgs evt => FormatBuildPerformance(evt),

            BuildStartedEventArgs evt => FormatBuildStart(evt),

            BuildFinishedEventArgs evt => FormatBuild(evt),

            ProjectEvaluationStartedEventArgs evt => FormatEvaluationStart(evt),

            ProjectEvaluationFinishedEventArgs evt => FormatEvaluation(evt),

            EnvironmentVariableReadEventArgs evt => FormatEnvironmentVariableRead(evt),

            BuildMessageEventArgs evt when evt.Message.StartsWith("Property reassignment:")
                => FormatReassignment(evt),

            ProjectStartedEventArgs evt => FormatProjectStart(evt),

            ProjectFinishedEventArgs evt => FormatProject(evt),

            TargetStartedEventArgs evt => FormatTargetStart(evt),

            TargetFinishedEventArgs evt => FormatTarget(evt),

            TaskStartedEventArgs evt => evt.TaskName != "Message" ? FormatTaskStart(evt) : "",

            TaskFinishedEventArgs evt => evt.TaskName != "Message" ? FormatTask(evt) : "",

            TaskCommandLineEventArgs evt => FormatTaskCommandLine(evt),

            TargetSkippedEventArgs evt => FormatSkipTarget(evt),

            BuildErrorEventArgs evt => FormatError(evt),

            BuildWarningEventArgs evt => FormatWarning(evt),

            BuildMessageEventArgs evt when evt.Message.StartsWith("Task")
                && evt.Message.Contains("skipped, due to false condition") => FormatSkipTask(evt),

            BuildMessageEventArgs evt => FormatMessage(evt),

            _ => string.Empty
        };

        private string FormatImport(ProjectAddedToProjectCollectionEventArgs evt) => $@"
==> Import: {evt.ProjectRootElement.FullPath}";

        private string FormatProjectChange(ProjectChangedEventArgs evt) => $@"
==> Project changed: {evt.Project.FullPath}";

        private string FormatXmlChange(ProjectXmlChangedEventArgs evt) => $@"
==> Project XML: {evt.ProjectXml.FullPath}
{evt.Reason}";

        private string FormatBuildStart(BuildStartedEventArgs evt) => $@"
[>] Build started...
    {(evt.BuildEnvironment.Any() ? "Environment:" : "")}{string.Join(@"
    ", evt.BuildEnvironment.Select(x => $"{x.Key} = {FormatValue(x.Value)}"))}";

        private string FormatBuild(BuildFinishedEventArgs evt) => $@"
==> Build: {(evt.Succeeded ? "OK" : "FAIL!!")}";

        private string FormatBuildPerformance(TelemetryEventArgs evt) => $@"
[%] Build performance metrics
    {string.Join(@"
    ", evt.Properties.Select(x => $"{x.Key} = {FormatValue(x.Value)}"))}";

        private string FormatEvaluationStart(ProjectEvaluationStartedEventArgs evt) => $@"
[>] {evt.Message}";

        private string FormatEvaluation(ProjectEvaluationFinishedEventArgs evt)
        {
            var properties = evt.Properties
                .Cast<object>()
                .SelectMany(x => x switch
                {
                    ProjectProperty prop => new[] { (prop.Name, prop.EvaluatedValue) },
                    ProjectPropertyInstance prop => new[] { (prop.Name, prop.EvaluatedValue) },
                    _ => Array.Empty<(string Name, string EvaluatedValue)>()
                })
                .OrderBy(x => x.Name)
                .ToList();

            var items = evt.Items
                .Cast<object>()
                .SelectMany(x => x switch
                {
                    ProjectItem item => new[] { (item.ItemType, item.EvaluatedInclude) },
                    ProjectItemInstance item => new[] { (item.ItemType, item.EvaluatedInclude) },
                    KeyValuePair<string, LinkedList<ProjectItem>> kvp => kvp.Value
                        .Select(y => (y.ItemType, y.EvaluatedInclude)),
                    _ => Array.Empty<(string ItemType, string EvaluatedInclude)>()
                })
                .OrderBy(x => x.ItemType)
                .ToList();

            return $@"
==> Evaluation: {(evt.Message)}
    Properties
        {string.Join(@"
        ", properties.Select(x => $"{x.Name} = {FormatValue(x.EvaluatedValue)}"))}
    Items
        {string.Join(@"
        ", items.Select(x => $"{x.ItemType}[ {FormatValue(x.EvaluatedInclude)} ]"))}";
        }

        private string FormatEnvironmentVariableRead(EnvironmentVariableReadEventArgs evt) => $@"
[ENV] {evt.EnvironmentVariableName} = {FormatValue(evt.Message)}";

        private class Reassignment
        {
            public string Name { get; set; } = "";
            public string Value { get; set; } = "";
            public string OldValue { get; set; } = "";
            public string Location { get; set; } = "";
        }

        private string FormatReassignment(BuildMessageEventArgs evt)
        {
            var tokenName = "$(" & new Token("NAME", Word) & ")";
            var tokenValue = "\"" & new Token("VALUE", (~Chars['"']).Repeat()) & "\"";
            var tokenOldValue = "\"" & new Token("OLDVALUE", (~Chars['"']).Repeat()) & "\"";
            var tokenLocation = new Token("LOCATION", AnyChar.Repeat());
            var tokenReassignment =
                new Token("REASSIGNMENT", StartOfFile & "Property reassignment: "
                & tokenName & "=" & tokenValue & " (previous value: " & tokenOldValue
                & ") at " & tokenLocation & EndOfFile)
                {
                    new Rule<Reassignment>
                    {
                        Capture(_ => new Reassignment()),
                        Update("NAME", (Reassignment x, string s) => x.Name = s),
                        Update("VALUE", (Reassignment x, string s) => x.Value = s),
                        Update("OLDVALUE", (Reassignment x, string s) => x.OldValue = s),
                        Update("LOCATION", (Reassignment x, string s) => x.Location = s)
                    }
                };

            Reassignment reassignment = null;
            try {
                var parser = tokenReassignment.Render();
                var result = parser.Parse(evt.Message);
                if (result["REASSIGNMENT"] is Reassignment r)
                    reassignment = r;
            } catch (RegExprException) {
            }
            if (reassignment == null) {
                return $@"
[SET] {FormatValue(evt.Message)}";
            }

            return $@"
[SET] {reassignment.Name} = ""{FormatValue(reassignment.Value)}""
    was: ""{FormatValue(reassignment.OldValue)}""
    at: {reassignment.Location}";
        }

        private string FormatProjectStart(ProjectStartedEventArgs evt) => $@"
[>] Project: {evt.ProjectFile}";

        private string FormatProject(ProjectFinishedEventArgs evt) => $@"
==> Project: {(evt.Succeeded ? "OK" : "FAIL!!")} {evt.ProjectFile}";

        private string FormatTargetStart(TargetStartedEventArgs evt) => $@"
[>] Target: {evt.TargetName}";

        private string FormatTarget(TargetFinishedEventArgs evt) => $@"
==> Target: {evt.TargetName} {(evt.Succeeded ? "OK" : "FAIL!!")}";

        private string FormatSkipTarget(TargetSkippedEventArgs evt) => $@"
(/) Target {evt.TargetName}: {evt.Message}";

        private string FormatTaskStart(TaskStartedEventArgs evt) => $@"
[>] Task: {evt.TaskName}";

        private string FormatTask(TaskFinishedEventArgs evt) => $@"
==> Task: {evt.TaskName} {(evt.Succeeded ? "OK" : "FAIL!!")}";

        private string FormatSkipTask(BuildMessageEventArgs evt) => $@"
(/) {evt.Message}";

        private string FormatTaskCommandLine(TaskCommandLineEventArgs evt) => $@"
[$> {evt.TaskName} command line:
    {evt.CommandLine}";

        private string FormatError(BuildErrorEventArgs evt) => $@"
[X] ERROR: {evt.Message}";

        private string FormatWarning(BuildWarningEventArgs evt) => $@"
[!] Warning: {evt.Message}";

        private string FormatMessage(BuildMessageEventArgs evt) => $@"
[i] {evt.Message}";

        private string FormatValue(string value)
        {
            value = value.Trim().Replace("\r", "").Replace("\n", "");
            var parts = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= 1)
                return value;
            return string.Join(";", parts.Select(x => x.Trim()));
        }
    }
}
