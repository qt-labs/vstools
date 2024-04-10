/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.QtMsBuild.Build
{
    [TestClass]
    public class AssemblyResolver
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
        }

        private static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var binaryDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var requestedAssembly = new AssemblyName(args.Name);
            var deployedAssembly = $@"{binaryDir}\{requestedAssembly.Name}.dll";
            if (!File.Exists(deployedAssembly))
                return null;
            return Assembly.LoadFrom(deployedAssembly);
        }
    }
}
