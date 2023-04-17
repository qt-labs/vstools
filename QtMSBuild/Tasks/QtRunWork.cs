/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#region Task TaskName="QtRunWork"

#region Reference
#endregion

#region Using
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
#endregion

#region Comment
/////////////////////////////////////////////////////////////////////////////////////////////////
/// TASK QtRunWork
/////////////////////////////////////////////////////////////////////////////////////////////////
// Run work items in parallel processes.
// Parameters:
//      in ITaskItem[] QtWork:      work items
//      in int         QtMaxProcs:  maximum number of processes to run in parallel
//      in bool        QtDebug:     generate debug messages
//     out ITaskItem[] Result:      list of new items with the result of each work item
#endregion

namespace QtVsTools.QtMsBuild.Tasks
{
    public static class QtRunWork
    {
        public static QtMSBuild.ITaskLoggingHelper Log { get; set; }

        public static bool Execute(
        #region Parameters
            Microsoft.Build.Framework.ITaskItem[] QtWork,
            System.Int32 QtMaxProcs,
            System.Boolean QtDebug,
            out Microsoft.Build.Framework.ITaskItem[] Result
            )
        #endregion
        {
            #region Code
            Result = new ITaskItem[] { };
            bool ok = true;
            var Comparer = StringComparer.InvariantCultureIgnoreCase;
            var Comparison = StringComparison.InvariantCultureIgnoreCase;

            // Work item key = "%(WorkType)(%(Identity))"
            Func<string, string, string> KeyString = (x, y) => string.Format("{0}{{{1}}}", x, y);
            Func<ITaskItem, string> Key = item =>
                KeyString(item.GetMetadata("WorkType"), item.ItemSpec);
            var workItemKeys = new HashSet<string>(QtWork.Select(x => Key(x)), Comparer);

            // Work items, indexed by %(Identity)
            var workItemsByIdentity = QtWork
                .GroupBy(x => x.ItemSpec, x => Key(x), Comparer)
                .ToDictionary(x => x.Key, x => new List<string>(x), Comparer);

            // Work items, indexed by work item key
            var workItems = QtWork.Select(x => new
            {
                Self = x,
                Key = Key(x),
                ToolPath = x.GetMetadata("ToolPath"),
                Message = x.GetMetadata("Message"),
                DependsOn = new HashSet<string>(comparer: Comparer,
                    collection: x.GetMetadata("DependsOn")
                        .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(y => workItemsByIdentity.ContainsKey(y))
                        .SelectMany(y => workItemsByIdentity[y])
                    .Union(x.GetMetadata("DependsOnWork")
                        .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(y => KeyString(y, x.ItemSpec))
                        .Where(y => workItemKeys.Contains(y)))
                    .GroupBy(y => y, Comparer).Select(y => y.Key)
                    .Where(y => !y.Equals(Key(x), Comparison))),
                ProcessStartInfo = new ProcessStartInfo
                {
                    FileName = x.GetMetadata("ToolPath"),
                    Arguments = x.GetMetadata("Options"),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            })
            // In case of items with duplicate keys, use only the first one
            .GroupBy(x => x.Key, Comparer)
            .ToDictionary(x => x.Key, x => x.First(), Comparer);

            // Result
            var result = workItems.Values
                .ToDictionary(x => x.Key, x => new TaskItem(x.Self));

            // Dependency relation [item -> dependent items]
            var dependentsOf = workItems.Values
                .Where(x => x.DependsOn.Any())
                .SelectMany(x => x.DependsOn.Select(y => new { Dependent = x.Key, Dependency = y }))
                .GroupBy(x => x.Dependency, x => x.Dependent, Comparer)
                .ToDictionary(x => x.Key, x => new List<string>(x), Comparer);

            // Work items that are ready to start; initially queue all independent items
            var workQueue = new Queue<string>(workItems.Values
                .Where(x => !x.DependsOn.Any())
                .Select(x => x.Key));

            if (QtDebug) {
                Log.LogMessage(MessageImportance.High,
                    string.Format("## QtRunWork queueing\r\n##    {0}",
                    string.Join("\r\n##    ", workQueue)));
            }

            // Postponed items; save dependent items to queue later when ready
            var postponedItems = new HashSet<string>(workItems.Values
                .Where(x => x.DependsOn.Any())
                .Select(x => x.Key));

            if (QtDebug && postponedItems.Any()) {
                Log.LogMessage(MessageImportance.High,
                    string.Format("## QtRunWork postponed dependents\r\n##    {0}",
                    string.Join("\r\n##    ", postponedItems
                        .Select(x => string.Format("{0} <- {1}", x,
                                     string.Join(", ", workItems[x].DependsOn))))));
            }

            // Work items that are running; must synchronize with the exit of all processes
            var running = new Queue<KeyValuePair<string, Process>>();

            // Work items that have terminated
            var terminated = new HashSet<string>(Comparer);

            // While there are work items queued, start a process for each item
            while (ok && workQueue.Any()) {

                var workItem = workItems[workQueue.Dequeue()];
                Log.LogMessage(MessageImportance.High, workItem.Message);

                try {
                    var proc = Process.Start(workItem.ProcessStartInfo);
                    proc.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data)) {
                            Log.LogMessage(MessageImportance.High, string.Join(" ",
                                QtDebug ? "[" + ((Process)sender).Id + "]" : "", e.Data));
                        }
                    };
                    proc.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data)) {
                            Log.LogMessage(MessageImportance.High, string.Join(" ",
                                QtDebug ? "[" + ((Process)sender).Id + "]" : "", e.Data));
                        }
                    };
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                    running.Enqueue(new KeyValuePair<string, Process>(workItem.Key, proc));
                } catch (Exception e) {
                    Log.LogError(
                        string.Format("[QtRunWork] Error starting process {0}: {1}",
                        workItem.ToolPath, e.Message));
                    ok = false;
                }

                string qtDebugRunning = "";
                if (QtDebug) {
                    qtDebugRunning = string.Format("## QtRunWork waiting {0}",
                        string.Join(", ", running
                            .Select(x => string.Format("{0} [{1}]", x.Key, x.Value.Id))));
                }

                // Wait for process to terminate when there are processes running, and...
                while (ok && running.Any()
                    // ...work is queued but already reached the maximum number of processes, or...
                    && ((workQueue.Any() && running.Count >= QtMaxProcs)
                    // ...work queue is empty but there are dependents that haven't yet been queued
                    || (!workQueue.Any() && postponedItems.Any()))) {

                    var itemProc = running.Dequeue();
                    workItem = workItems[itemProc.Key];
                    var proc = itemProc.Value;

                    if (QtDebug && !string.IsNullOrEmpty(qtDebugRunning)) {
                        Log.LogMessage(MessageImportance.High, qtDebugRunning);
                        qtDebugRunning = "";
                    }

                    if (proc.WaitForExit(100)) {
                        if (QtDebug) {
                            Log.LogMessage(MessageImportance.High,
                                string.Format("## QtRunWork exit {0} [{1}] = {2} ({3:0.00} msecs)",
                                workItem.Key, proc.Id, proc.ExitCode,
                                (proc.ExitTime - proc.StartTime).TotalMilliseconds));
                        }

                        // Process terminated; check exit code and close
                        terminated.Add(workItem.Key);
                        result[workItem.Key].SetMetadata("ExitCode", proc.ExitCode.ToString());
                        ok &= proc.ExitCode == 0;
                        proc.Close();

                        // Add postponed dependent items to work queue
                        if (ok && dependentsOf.ContainsKey(workItem.Key)) {
                            // Dependents of workItem...
                            var readyDependents = dependentsOf[workItem.Key]
                                // ...that have not yet been queued...
                                .Where(x => postponedItems.Contains(x)
                                    // ...and whose dependending items have all terminated.
                                    && workItems[x].DependsOn.All(y => terminated.Contains(y)))
                                .ToList();
                            if (QtDebug && readyDependents.Any()) {
                                Log.LogMessage(MessageImportance.High,
                                string.Format("## QtRunWork queueing\r\n##    {0}",
                                string.Join("\r\n##    ", readyDependents)));
                            }

                            foreach (var dependent in readyDependents) {
                                postponedItems.Remove(dependent);
                                workQueue.Enqueue(dependent);
                            }
                        }
                    } else {
                        // Process is still running; feed it back into the running queue
                        running.Enqueue(itemProc);
                    }
                }
            }

            // If there are items still haven't been queued, that means a circular dependency exists
            if (ok && postponedItems.Any()) {
                ok = false;
                Log.LogError("[QtRunWork] Error: circular dependency");
                if (QtDebug) {
                    Log.LogMessage(MessageImportance.High,
                        string.Format("## QtRunWork circularity\r\n##    {0}",
                        string.Join("\r\n##    ", postponedItems
                            .Select(x => string.Format("{0} <- {1}", x,
                                         string.Join(", ", workItems[x].DependsOn))))));
                }
            }

            if (ok && QtDebug) {
                Log.LogMessage(MessageImportance.High,
                    "## QtRunWork all work queued");
                if (running.Any()) {
                    Log.LogMessage(MessageImportance.High,
                        string.Format("## QtRunWork waiting {0}",
                        string.Join(", ", running
                            .Select(x => string.Format("{0} [{1}]", x.Key, x.Value.Id)))));
                }
            }

            // Wait for all running processes to terminate
            while (running.Any()) {
                var itemProc = running.Dequeue();
                var workItem = workItems[itemProc.Key];
                var proc = itemProc.Value;
                if (proc.WaitForExit(100)) {
                    if (QtDebug) {
                        Log.LogMessage(MessageImportance.High,
                            string.Format("## QtRunWork exit {0} [{1}] = {2} ({3:0.00} msecs)",
                            workItem.Key, proc.Id, proc.ExitCode,
                            (proc.ExitTime - proc.StartTime).TotalMilliseconds));
                    }
                    // Process terminated; check exit code and close
                    result[workItem.Key].SetMetadata("ExitCode", proc.ExitCode.ToString());
                    ok &= proc.ExitCode == 0;
                    proc.Close();
                } else {
                    // Process is still running; feed it back into the running queue
                    running.Enqueue(itemProc);
                }
            }

            if (QtDebug) {
                Log.LogMessage(MessageImportance.High,
                    string.Format("## QtRunWork result {0}", ok ? "ok" : "FAILED!"));
            }

            Result = result.Values.ToArray();
            if (!ok)
                return false;
            #endregion

            return true;
        }
    }
}
#endregion
