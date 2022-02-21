/****************************************************************************
**
** Copyright (C) 2021 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Tools.
**
** $QT_BEGIN_LICENSE:GPL-EXCEPT$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see https://www.qt.io/terms-conditions. For further
** information use the contact form at https://www.qt.io/contact-us.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3 as published by the Free Software
** Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
** included in the packaging of this file. Please review the following
** information to ensure the GNU General Public License requirements will
** be met: https://www.gnu.org/licenses/gpl-3.0.html.
**
** $QT_END_LICENSE$
**
****************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;

namespace QtVsTools.Test.QtMsBuild.Tasks
{
    internal class TestTaskLoggingHelper : QtMSBuild.ITaskLoggingHelper
    {
        public bool HasLoggedErrors { get; set; } = false;

        public void LogMessage(
            string message,
            params object[] messageArgs)
        {
            Debug.WriteLine(message, messageArgs);
        }

        public void LogMessage(
            MessageImportance importance,
            string message,
            params object[] messageArgs)
        {
            Debug.WriteLine(message, messageArgs);
        }

        public void LogWarning(
            string message,
            params object[] messageArgs)
        {
            Debug.WriteLine("Warning: " + message, messageArgs);
        }

        public void LogError(
            string message,
            params object[] messageArgs)
        {
            Debug.WriteLine("ERROR: " + message, messageArgs);
        }

        public void LogCommandLine(string commandLine)
        {
            throw new NotImplementedException();
        }

        public void LogCommandLine(
            MessageImportance importance,
            string commandLine)
        {
            throw new NotImplementedException();
        }

        public void LogCriticalMessage(
            string subcategory,
            string code,
            string helpKeyword,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string message,
            params object[] messageArgs)
        {
            throw new NotImplementedException();
        }

        public void LogError(
            string subcategory,
            string errorCode,
            string helpKeyword,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string message,
            params object[] messageArgs)
        {
            throw new NotImplementedException();
        }

        public void LogError(
            string subcategory,
            string errorCode,
            string helpKeyword,
            string helpLink,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string message,
            params object[] messageArgs)
        {
            throw new NotImplementedException();
        }

        public void LogErrorFromException(Exception exception)
        {
            throw new NotImplementedException();
        }

        public void LogErrorFromException(
            Exception exception,
            bool showStackTrace)
        {
            throw new NotImplementedException();
        }

        public void LogErrorFromException(
            Exception exception,
            bool showStackTrace,
            bool showDetail,
            string file)
        {
            throw new NotImplementedException();
        }

        public void LogErrorFromResources(
            string messageResourceName,
            params object[] messageArgs)
        {
            throw new NotImplementedException();
        }

        public void LogErrorFromResources(
            string subcategoryResourceName,
            string errorCode,
            string helpKeyword,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string messageResourceName,
            params object[] messageArgs)
        {
            throw new NotImplementedException();
        }

        public void LogErrorWithCodeFromResources(
            string messageResourceName,
            params object[] messageArgs)
        {
            throw new NotImplementedException();
        }

        public void LogErrorWithCodeFromResources(
            string subcategoryResourceName,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string messageResourceName,
            params object[] messageArgs)
        {
            throw new NotImplementedException();
        }

        public void LogExternalProjectFinished(
            string message,
            string helpKeyword,
            string projectFile,
            bool succeeded)
        {
            throw new NotImplementedException();
        }

        public void LogExternalProjectStarted(
            string message,
            string helpKeyword,
            string projectFile,
            string targetNames)
        {
            throw new NotImplementedException();
        }

        public void LogMessage(
            string subcategory,
            string code,
            string helpKeyword,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            MessageImportance importance,
            string message,
            params object[] messageArgs)
        {
            throw new NotImplementedException();
        }

        public void LogMessageFromResources(
            string messageResourceName,
            params object[] messageArgs)
        {
            throw new NotImplementedException();
        }

        public void LogMessageFromResources(
            MessageImportance importance,
            string messageResourceName,
            params object[] messageArgs)
        {
            throw new NotImplementedException();
        }

        public bool LogMessageFromText(
            string lineOfText,
            MessageImportance messageImportance)
        {
            throw new NotImplementedException();
        }

        public bool LogMessagesFromFile(string fileName)
        {
            throw new NotImplementedException();
        }

        public bool LogMessagesFromFile(
            string fileName,
            MessageImportance messageImportance)
        {
            throw new NotImplementedException();
        }

        public bool LogMessagesFromStream(
            TextReader stream,
            MessageImportance messageImportance)
        {
            throw new NotImplementedException();
        }

        public bool LogsMessagesOfImportance(MessageImportance importance)
        {
            throw new NotImplementedException();
        }

        public void LogTelemetry(
            string eventName,
            IDictionary<string,
            string> properties)
        {
            throw new NotImplementedException();
        }

        public void LogWarning(
            string subcategory,
            string warningCode,
            string helpKeyword,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string message,
            params object[] messageArgs)
        {
            throw new NotImplementedException();
        }

        public void LogWarning(
            string subcategory,
            string warningCode,
            string helpKeyword,
            string helpLink,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string message,
            params object[] messageArgs)
        {
            throw new NotImplementedException();
        }

        public void LogWarningFromException(Exception exception)
        {
            throw new NotImplementedException();
        }

        public void LogWarningFromException(
            Exception exception,
            bool showStackTrace)
        {
            throw new NotImplementedException();
        }

        public void LogWarningFromResources(
            string messageResourceName,
            params object[] messageArgs)
        {
            throw new NotImplementedException();
        }

        public void LogWarningFromResources(
            string subcategoryResourceName,
            string warningCode,
            string helpKeyword,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string messageResourceName,
            params object[] messageArgs)
        {
            throw new NotImplementedException();
        }

        public void LogWarningWithCodeFromResources(
            string messageResourceName,
            params object[] messageArgs)
        {
            throw new NotImplementedException();
        }

        public void LogWarningWithCodeFromResources(
            string subcategoryResourceName,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string messageResourceName,
            params object[] messageArgs)
        {
            throw new NotImplementedException();
        }
    }
}
