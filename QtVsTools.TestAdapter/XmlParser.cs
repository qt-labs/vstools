/**************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
**************************************************************************************************/

using System.Linq;
using System.Xml.Linq;

namespace QtVsTools.TestAdapter
{
    internal static class XmlParser
    {
        internal static QtTestResult Parse(string xmlString)
        {
            var xmlDoc = XDocument.Parse(xmlString);

            var testCaseElement = xmlDoc.Element("TestCase");
            var environmentElement = testCaseElement?.Element("Environment");
            var testCaseResult = new QtTestResult
            {
                Name = (string)testCaseElement?.Attribute("name"),
                QtVersion = (string)environmentElement?.Element("QtVersion"),
                QtBuild = (string)environmentElement?.Element("QtBuild"),
                QTestVersion = (string)environmentElement?.Element("QTestVersion"),
                TotalDuration = (double)testCaseElement?.Element("Duration")?.Attribute("msecs"),
                TestFunctions = testCaseElement?.Elements("TestFunction").ToDictionary(
                    tf => (string)tf.Attribute("name"),
                    tf => new QtTestResult.TestFunction
                    {
                        Name = (string)tf.Attribute("name"),
                        Duration = (double)tf.Element("Duration")?.Attribute("msecs"),
                        IncidentType = (string)tf.Element("Incident")?.Attribute("type"),
                        IncidentFile = (string)tf.Element("Incident")?.Attribute("file"),
                        IncidentLine = (int)tf.Element("Incident")?.Attribute("line"),
                        IncidentDescription = (string)tf.Element("Incident")?.Element("Description")
                    }
                )
            };
            return testCaseResult;
        }
    }
}
