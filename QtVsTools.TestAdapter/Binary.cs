/**************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
**************************************************************************************************/

using System;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace QtVsTools.TestAdapter
{
    internal static class Binary
    {
        internal enum Type
        {
            Unknown,
            Console,
            Gui
        }

        internal static bool TryGetType(string filePath, Logger log, out Type type)
        {
            type = Type.Unknown;
            if (!File.Exists(filePath))
                return false;

            try {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var peReader = new PEReader(stream);
                type = peReader.PEHeaders.PEHeader?.Subsystem switch
                {
                    Subsystem.WindowsGui => Type.Gui,
                    Subsystem.WindowsCui => Type.Console,
                    _ => Type.Unknown
                };
            } catch (Exception exception) {
                log.SendMessage("Exception was thrown while checking the binary type."
                    + Environment.NewLine + exception, TestMessageLevel.Error);
            }

            return type != Type.Unknown;
        }
    }
}
