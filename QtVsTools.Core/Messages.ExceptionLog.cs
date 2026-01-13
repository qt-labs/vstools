// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.IO;

using static System.Environment;

namespace QtVsTools.Core
{
    using Common;
    using Options;

    /// <summary> Exception logging helpers for the Messages class. </summary>
    public static partial class Messages
    {
        private const string ExceptionLogDirName = "QtVsTools";
        private const string ExceptionLogFileName = "QtVsTools.Exceptions.log";
        private const string ExceptionLogDelimiter = "===== QtVsTools Exception =====";

        private const int ExceptionLogMaxSize = 1024 * 1024;
        private const int ExceptionLogTruncationSize = 768 * 1024;

        private static readonly Lazy<Utils.LogFile> ExceptionLogFile = new(CreateExceptionLogFile);

        [ThreadStatic]
        private static bool _logFileWriteInProgress;

        /// <summary> Log exception details to file and optionally to the output pane. </summary>
        public static void Log(this Exception exception, bool clear = false, bool activate = false)
        {
            LogExceptionToFile(exception);

            if (!QtOptionsPage.ShowExceptionsInOutputPane)
                return;

            var text = ExceptionToString(exception);
            MsgQueue.Enqueue(new Msg
            {
                Clear = clear,
                Text = text,
                Activate = activate
            });
            FlushMessages();
        }

        /// <summary> Format exception details for output or file logging. </summary>
        private static string ExceptionToString(Exception exception)
        {
            return $"An exception ({exception.GetType().Name}) occurred.{NewLine}"
                + $"Message:{NewLine}   {exception.Message}{NewLine}"
                + $"Stack Trace:{NewLine}   {exception.StackTrace.Trim()}{NewLine}{NewLine}";
        }

        /// <summary> Create the rotating exception log file under the temp folder. </summary>
        private static Utils.LogFile CreateExceptionLogFile()
        {
            var logDir = Path.Combine(Path.GetTempPath(), ExceptionLogDirName);
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, ExceptionLogFileName);
            return new Utils.LogFile(logPath, ExceptionLogMaxSize, ExceptionLogTruncationSize,
                ExceptionLogDelimiter);
        }

        /// <summary>
        /// Wrap exception details with timestamp and delimiter for rotation alignment.
        /// </summary>
        private static string ExceptionToLogEntry(Exception exception)
        {
            var timestamp = DateTimeOffset.Now.ToString("o");
            return $"{ExceptionLogDelimiter}{NewLine}"
                + $"[{timestamp}]{NewLine}"
                + ExceptionToString(exception);
        }

        /// <summary> Write the exception to the log file, avoiding recursive logging. </summary>
        private static void LogExceptionToFile(Exception exception)
        {
            if (_logFileWriteInProgress)
                return;
            _logFileWriteInProgress = true;
            try {
                ExceptionLogFile.Value.Write(ExceptionToLogEntry(exception));
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex);
            } finally {
                _logFileWriteInProgress = false;
            }
        }
    }
}
