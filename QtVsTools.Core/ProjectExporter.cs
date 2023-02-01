/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using EnvDTE;

namespace QtVsTools.Core
{
    public class ProjectExporter
    {
        public static List<string> ConvertFilesToFullPath(List<string> files, string path)
        {
            var ret = new List<string>(files.Count);
            foreach (var file in files) {
                FileInfo fi;
                if (file.IndexOf(':') != 1)
                    fi = new FileInfo(path + Path.DirectorySeparatorChar + file);
                else
                    fi = new FileInfo(file);

                ret.Add(fi.FullName);
            }
            return ret;
        }

        private static VCFilter BestMatch(string path, Hashtable pathFilterTable)
        {
            var bestMatch = ".";
            var inPath = path;
            if (inPath.StartsWith(".\\", StringComparison.Ordinal))
                inPath = inPath.Substring(2);
            foreach (string p in pathFilterTable.Keys) {
                var best = 0;
                for (var i = 0; i < inPath.Length; ++i) {
                    if (i < p.Length && inPath[i] == p[i])
                        ++best;
                    else
                        break;
                }
                if (best > bestMatch.Length && p.Length == best)
                    bestMatch = p;
            }
            return pathFilterTable[bestMatch] as VCFilter;
        }

        private static void CollectFilters(VCFilter filter, string path, ref Hashtable filterPathTable,
            ref Hashtable pathFilterTable)
        {
            var newPath = ".";
            if (path != null)
                newPath = path + Path.DirectorySeparatorChar + filter.Name;
            newPath = newPath.ToLower().Trim();
            newPath = Regex.Replace(newPath, @"\\+\.?\\+", "\\");
            newPath = Regex.Replace(newPath, @"\\\.?$", "");
            if (newPath.StartsWith(".\\", StringComparison.Ordinal))
                newPath = newPath.Substring(2);
            filterPathTable.Add(filter, newPath);
            pathFilterTable.Add(newPath, filter);
            foreach (VCFilter f in (IVCCollection)filter.Filters)
                CollectFilters(f, newPath, ref filterPathTable, ref pathFilterTable);
        }

        public static void SyncIncludeFiles(VCProject vcproj, List<string> priFiles,
            List<string> projFiles, DTE dte, bool flat, FakeFilter fakeFilter)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var cmpPriFiles = new List<string>(priFiles.Count);
            foreach (var s in priFiles)
                cmpPriFiles.Add(HelperFunctions.NormalizeFilePath(s).ToLower());
            cmpPriFiles.Sort();

            var cmpProjFiles = new List<string>(projFiles.Count);
            foreach (var s in projFiles)
                cmpProjFiles.Add(HelperFunctions.NormalizeFilePath(s).ToLower());

            var qtPro = QtProject.Create(vcproj);
            var filterPathTable = new Hashtable(17);
            var pathFilterTable = new Hashtable(17);
            if (!flat && fakeFilter != null) {
                var rootFilter = qtPro.FindFilterFromGuid(fakeFilter.UniqueIdentifier);
                if (rootFilter == null)
                    qtPro.AddFilterToProject(Filters.SourceFiles());

                CollectFilters(rootFilter, null, ref filterPathTable, ref pathFilterTable);
            }

            // first check for new files
            foreach (var file in cmpPriFiles) {
                if (cmpProjFiles.IndexOf(file) > -1)
                    continue;

                if (flat) {
                    vcproj.AddFile(file); // the file is not in the project
                } else {
                    var path = HelperFunctions.GetRelativePath(vcproj.ProjectDirectory, file);
                    if (path.StartsWith(".\\", StringComparison.Ordinal))
                        path = path.Substring(2);

                    var i = path.LastIndexOf(Path.DirectorySeparatorChar);
                    if (i > -1)
                        path = path.Substring(0, i);
                    else
                        path = ".";

                    if (pathFilterTable.Contains(path)) {
                        var f = pathFilterTable[path] as VCFilter;
                        f.AddFile(file);
                        continue;
                    }

                    var filter = BestMatch(path, pathFilterTable);

                    var filterDir = filterPathTable[filter] as string;
                    var name = path;
                    if (!name.StartsWith("..", StringComparison.Ordinal) && name.StartsWith(filterDir, StringComparison.Ordinal))
                        name = name.Substring(filterDir.Length + 1);

                    var newFilter = filter.AddFilter(name) as VCFilter;
                    newFilter.AddFile(file);

                    filterPathTable.Add(newFilter, path);
                    pathFilterTable.Add(path, newFilter);
                }
            }

            // then check for deleted files
            foreach (var file in cmpProjFiles) {
                if (cmpPriFiles.IndexOf(file) == -1) {
                    // the file is not in the pri file
                    // (only removes it from the project, does not del. the file)
                    var info = new FileInfo(file);
                    HelperFunctions.RemoveFileInProject(vcproj, file);
                    Messages.Print("--- (Importing .pri file) file: " + info.Name +
                        " does not exist in .pri file, move to " + vcproj.ProjectDirectory + "Deleted");
                }
            }
        }
    }
}
