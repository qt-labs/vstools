/**************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
**************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.Shell;

namespace QtVsTools.Wizards.Util
{
    using Core;

    public static class TextAndWhitespace
    {

        /// <summary>
        /// Adjusts the whitespaces and tabs in the given file according to VS settings.
        /// </summary>
        /// <param name="dte"></param>
        /// <param name="file"></param>
        public static void Adjust(EnvDTE.DTE dte, string file)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!File.Exists(file))
                return;

            // only replace whitespaces in known types
            if (!HelperFunctions.IsSourceFile(file) && !HelperFunctions.IsHeaderFile(file)
                && !HelperFunctions.IsUicFile(file)) {
                return;
            }

            try {
                var prop = dte.Properties["TextEditor", "C/C++"];
                var tabSize = Convert.ToInt64(prop.Item("TabSize").Value);
                var insertTabs = Convert.ToBoolean(prop.Item("InsertTabs").Value);

                var oldValue = insertTabs ? "    " : "\t";
                var newValue = insertTabs ? "\t" : GetWhitespaces(tabSize);

                var list = new List<string>();
                var reader = new StreamReader(file);
                while (reader.ReadLine() is { } line) {
                    if (line.StartsWith(oldValue, StringComparison.Ordinal))
                        line = line.Replace(oldValue, newValue);
                    list.Add(line);
                }
                reader.Close();

                var writer = new StreamWriter(file);
                foreach (var l in list)
                    writer.WriteLine(l);
                writer.Close();
            } catch (Exception e) {
                Messages.Print("Cannot adjust whitespace or tabs in file (write)."
                    + Environment.NewLine + $"({e})");
            }
        }

        private static string GetWhitespaces(long size)
        {
            var whitespaces = string.Empty;
            for (long i = 0; i < size; ++i)
                whitespaces += " ";
            return whitespaces;
        }
    }
}
