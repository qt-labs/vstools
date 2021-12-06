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
using System.IO;
using Microsoft.Build.Framework;

namespace QtVsTools.QtMSBuild
{
    public interface ITaskLoggingHelper
    {
        bool HasLoggedErrors { get; }

        void LogCommandLine(
            string commandLine);

        void LogCommandLine(
            MessageImportance importance,
            string commandLine);

        void LogCriticalMessage(
            string subcategory,
            string code,
            string helpKeyword,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string message,
            params object[] messageArgs);

        void LogError(
            string message,
            params object[] messageArgs);

        void LogError(
            string subcategory,
            string errorCode,
            string helpKeyword,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string message,
            params object[] messageArgs);

        void LogError(
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
            params object[] messageArgs);

        void LogErrorFromException(
            Exception exception);

        void LogErrorFromException(
            Exception exception,
            bool showStackTrace);

        void LogErrorFromException(
            Exception exception,
            bool showStackTrace,
            bool showDetail,
            string file);

        void LogErrorFromResources(
            string messageResourceName,
            params object[] messageArgs);

        void LogErrorFromResources(
            string subcategoryResourceName,
            string errorCode,
            string helpKeyword,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string messageResourceName,
            params object[] messageArgs);

        void LogErrorWithCodeFromResources(
            string messageResourceName,
            params object[] messageArgs);

        void LogErrorWithCodeFromResources(
            string subcategoryResourceName,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string messageResourceName,
            params object[] messageArgs);

        void LogExternalProjectFinished(
            string message,
            string helpKeyword,
            string projectFile,
            bool succeeded);

        void LogExternalProjectStarted(
            string message,
            string helpKeyword,
            string projectFile,
            string targetNames);

        void LogMessage(
            string message,
            params object[] messageArgs);

        void LogMessage(
            MessageImportance importance,
            string message,
            params object[] messageArgs);

        void LogMessage(
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
            params object[] messageArgs);

        void LogMessageFromResources(
            string messageResourceName,
            params object[] messageArgs);

        void LogMessageFromResources(
            MessageImportance importance,
            string messageResourceName,
            params object[] messageArgs);

        bool LogMessageFromText(
            string lineOfText,
            MessageImportance messageImportance);

        bool LogMessagesFromFile(
            string fileName);

        bool LogMessagesFromFile(
            string fileName,
            MessageImportance messageImportance);

        bool LogMessagesFromStream(
            TextReader stream,
            MessageImportance messageImportance);

        bool LogsMessagesOfImportance(
            MessageImportance importance);

        void LogTelemetry(
            string eventName,
            IDictionary<string,
            string> properties);

        void LogWarning(
            string message,
            params object[] messageArgs);

        void LogWarning(
            string subcategory,
            string warningCode,
            string helpKeyword,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string message,
            params object[] messageArgs);

        void LogWarning(
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
            params object[] messageArgs);

        void LogWarningFromException(
            Exception exception);

        void LogWarningFromException(
            Exception exception,
            bool showStackTrace);

        void LogWarningFromResources(
            string messageResourceName,
            params object[] messageArgs);

        void LogWarningFromResources(
            string subcategoryResourceName,
            string warningCode,
            string helpKeyword,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string messageResourceName,
            params object[] messageArgs);

        void LogWarningWithCodeFromResources(
            string messageResourceName,
            params object[] messageArgs);

        void LogWarningWithCodeFromResources(
            string subcategoryResourceName,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string messageResourceName,
            params object[] messageArgs);
    }
}
