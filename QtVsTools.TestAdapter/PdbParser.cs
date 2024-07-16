/**************************************************************************************************
Copyright (C) 2024 The Qt Company Ltd.
SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
**************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Dia2Lib;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace QtVsTools.TestAdapter
{
    internal static class PdbParser
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        [DllImport(
            "msdia140.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
        private static extern object DllGetClassObject([In] in Guid rclSid, [In] in Guid rIid);

        [ComImport]
        [ComVisible(false)]
        [Guid("00000001-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDiaClassFactory
        {
            void CreateInstance([MarshalAs(UnmanagedType.Interface)] object aggregator,
                [In] in Guid refIid, [MarshalAs(UnmanagedType.Interface)] out object createdObject);
        }

        public static List<SourceInfo> Parse(string filePath, Logger log)
        {
            object classFactory = null;
            IDiaClassFactory factory = null;
            IDiaDataSource diaDataSource = null;
            IDiaSession diaSession = null;
            IDiaEnumSymbolsByAddr enumByAddress = null;

            List<SourceInfo> sourceInfos = new();
            filePath = Path.ChangeExtension(filePath, "pdb");
            try {
                var guid = new Guid("{e6756135-1e65-4d17-8576-610761398c3c}");
                classFactory = DllGetClassObject(guid, typeof(IDiaClassFactory).GetTypeInfo().GUID);
                if (classFactory is not IDiaClassFactory factoryInstance)
                    return sourceInfos;
                factory = factoryInstance;

                factory.CreateInstance(null, typeof(IDiaDataSource).GetTypeInfo().GUID,
                    out var createdObject);
                if (createdObject is not IDiaDataSource dataSourceInstance)
                    return sourceInfos;
                diaDataSource = dataSourceInstance;

                diaDataSource.loadDataFromPdb(filePath);
                diaDataSource.openSession(out diaSession);
                diaSession.getSymbolsByAddr(out enumByAddress);

                var symbol = enumByAddress.symbolByRVA(0);
                while (symbol != null) {
                    if (symbol.symTag == (uint)SymTagEnum.SymTagFunction) {
                        diaSession.findLinesByRVA(symbol.relativeVirtualAddress, 1, out var ppResult);
                        ppResult.Next(1, out var rgelt, out _);

                        sourceInfos.Add(new SourceInfo
                        {
                            SymbolName = symbol.name,
                            LineNumber = (int)(rgelt?.lineNumber ?? 0),
                            SourceFile = rgelt?.sourceFile?.fileName
                        });

                        if (rgelt != null)
                            Marshal.ReleaseComObject(rgelt);
                        Marshal.ReleaseComObject(ppResult);
                    }

                    enumByAddress.Next(1, out symbol, out _);
                }
            } catch (Exception exception) {
                log.SendMessage($"Exception was thrown while parsing PDB file: '{filePath}'."
                    + Environment.NewLine + exception, TestMessageLevel.Error);
            } finally {
                if (enumByAddress != null)
                    Marshal.ReleaseComObject(enumByAddress);
                if (diaSession != null)
                    Marshal.ReleaseComObject(diaSession);
                if (diaDataSource != null)
                    Marshal.ReleaseComObject(diaDataSource);
                if (factory != null)
                    Marshal.ReleaseComObject(factory);
                if (classFactory != null)
                    Marshal.ReleaseComObject(classFactory);
            }

            return sourceInfos;
        }
    }
}
