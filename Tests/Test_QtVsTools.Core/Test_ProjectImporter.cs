// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.Core
{
    using QtVsTools.Core;

    [TestClass]
    public class Test_ProjectImporter
    {
        private static Type templateTypeEnum;
        private static List<string> templateTypeNames;
        private static MethodInfo getTemplateTypeMethod;

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            templateTypeEnum = typeof(ProjectImporter).GetNestedType("TemplateType",
                BindingFlags.NonPublic);
            Assert.IsNotNull(templateTypeEnum, "Failed to get the TemplateType enum.");

            templateTypeNames = new List<string>(Enum.GetNames(templateTypeEnum));
            Assert.IsTrue(templateTypeNames.Any(), "Failed to get the TemplateType names.");

            getTemplateTypeMethod = typeof(ProjectImporter).GetMethod("GetTemplateType",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(getTemplateTypeMethod, "Failed to get the GetTemplateType method.");
        }

        [TestMethod]
        [DataRow("Unknown", DisplayName = "Test Unknown TemplateType")]
        [DataRow("App", DisplayName = "Test App TemplateType")]
        [DataRow("Lib", DisplayName = "Test Lib TemplateType")]
        [DataRow("SubDirs", DisplayName = "Test SubDirs TemplateType")]
        [DataRow("Aux", DisplayName = "Test Aux TemplateType")]
        [DataRow("VcApp", DisplayName = "Test VcApp TemplateType")]
        [DataRow("VcLib", DisplayName = "Test VcLib TemplateType")]
        public void TestTemplateTypeEnumeration(string expectedEnumName)
        {
            Assert.IsTrue(templateTypeNames.Contains(expectedEnumName),
                $"Enum value {expectedEnumName} is not covered.");
        }

        [DataTestMethod]
        [DataRow(new[] { "TEMPLATE = app" }, "App", DisplayName = "Standard App Template")]
        [DataRow(new[] { "TEMPLATE = lib" }, "Lib", DisplayName = "Standard Lib Template")]
        [DataRow(new[] { "TEMPLATE = subdirs" }, "SubDirs",
            DisplayName = "Standard SubDirs Template")]
        [DataRow(new[] { "  TEMPLATE   =    lib   " }, "Lib",
            DisplayName = "Template with extra spaces")]
        [DataRow(new[] { "TEMPLATE = \\", "    subdirs" }, "SubDirs",
            DisplayName = "Multiline Template")]
        [DataRow(new[] { "TEMPLATE = unknown_value" }, "Unknown",
            DisplayName = "Invalid Template")]
        [DataRow(new[] { "QT += core gui" }, "Unknown", DisplayName = "No Template Declaration")]
        [DataRow(new string[] {}, "Unknown", DisplayName = "Empty Content")]
        [DataRow(new[] { "TEMPLATE = \\", "    \\", "    \\", "    subdirs" }, "SubDirs",
            DisplayName = "Multi-multiline Template")]
        [DataRow(new[] { "\tTEMPLATE = \\", "\t\\", "    \\", "\tsubdirs" }, "SubDirs",
            DisplayName = "Multi-multiline Template with tabs")]
        public void TestTemplateType(IEnumerable<string> testCase, string expectedTemplateType)
        {
            var expectedEnumValue = Enum.Parse(templateTypeEnum, expectedTemplateType);
            var result = getTemplateTypeMethod.Invoke(null, new object[] { testCase });
            Assert.AreEqual(expectedEnumValue, result, $"Expected: {expectedEnumValue}, Got: {result}");
        }
    }

}
