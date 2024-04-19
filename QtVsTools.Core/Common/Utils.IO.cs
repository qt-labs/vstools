/**************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
**************************************************************************************************/

using System;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Build.Tasks;

namespace QtVsTools.Core.Common
{
    public static partial class Utils
    {
        /// <summary>
        /// Recursively copies the contents of the given directory and all its children to the
        /// specified target path. If the target path does not exist, it will be created.
        /// </summary>
        /// <param name="directory">The directory to copy.</param>
        /// <param name="targetPath">The path where the contents will be copied to.</param>
        public static void CopyDirectory(string directory, string targetPath)
        {
            var sourceDir = new DirectoryInfo(directory);
            if (!sourceDir.Exists)
                return;

            try {
                if (!Directory.Exists(targetPath))
                    Directory.CreateDirectory(targetPath);

                var files = sourceDir.GetFiles();
                foreach (var file in files) {
                    try {
                        file.CopyTo(Path.Combine(targetPath, file.Name), true);
                    } catch (Exception exception) {
                        exception.Log();
                    }
                }
            } catch (Exception exception) {
                exception.Log();
            }

            var subDirs = sourceDir.GetDirectories();
            foreach (var subDir in subDirs)
                CopyDirectory(subDir.FullName, Path.Combine(targetPath, subDir.Name));
        }

        /// <summary>
        /// Asynchronously reads the contents of a text file and returns the content as a string.
        /// </summary>
        /// <param name="filePath">The path to the file to read.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the
        /// content of the file as a string.</returns>
        public static async Task<string> ReadAllTextAsync(string filePath)
        {
            using var reader = File.OpenText(filePath);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// Asynchronously writes the specified string to a file, creating the file if it does
        /// not exist, and overwriting the content if it does.
        /// </summary>
        /// <param name="filePath">The path to the file to write.</param>
        /// <param name="text">The string to write to the file.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task WriteAllTextAsync(string filePath, string text)
        {
            using var writer = File.CreateText(filePath);
            await writer.WriteAsync(text);
        }

        /// <summary>
        /// Deletes a file specified by its path. Does not throw any exceptions.
        /// </summary>
        /// <param name="file">The path to the file to delete.</param>
        public static void DeleteFile(string file)
        {
            try {
                DeleteFile(new FileInfo(file));
            } catch (Exception exception) {
                exception.Log();
            }
        }

        /// <summary>
        /// Deletes a file if it exists. Does not throw any exceptions.
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/> object representing the file to delete.</param>
        public static void DeleteFile(FileInfo file)
        {
            if (!file?.Exists ?? true)
                return;

            try {
                file.Delete();
            } catch (Exception exception) {
                exception.Log();
            }
        }

        /// <summary>
        /// Specifies the options for directory deletion.
        /// </summary>
        public enum Option
        {
            /// <summary>
            /// Only the specified directory itself is deleted, without deleting its contents.
            /// </summary>
            Shallow,

            /// <summary>
            /// The specified directory and all its contents, including subdirectories, are deleted.
            /// </summary>
            Recursive
        }

        /// <summary>
        /// Recursively deletes the given directory and all its contents. Does not throw any
        /// exceptions.
        /// </summary>
        /// <param name="path">The path of the directory to delete.</param>
        /// <param name="option">Specifies the options for directory deletion.</param>
        public static void DeleteDirectory(string path, Option option = Option.Shallow)
        {
            try {
                DeleteDirectory(new DirectoryInfo(path), option);
            } catch (Exception exception) {
                exception.Log();
            }
        }

        /// <summary>
        /// Recursively deletes the given directory and all its contents. Does not throw any
        /// exceptions.
        /// </summary>
        /// <param name="directory">The directory to delete.</param>
        /// <param name="option">Specifies the options for directory deletion.</param>
        public static void DeleteDirectory(DirectoryInfo directory, Option option = Option.Shallow)
        {
            if (!directory?.Exists ?? true)
                return;

            if (option == Option.Recursive) {
                foreach (var file in directory.GetFiles())
                    DeleteFile(file);

                try {
                    foreach (var subDir in directory.GetDirectories())
                        DeleteDirectory(subDir, option);
                } catch (Exception exception) {
                    exception.Log();
                }
            }

            try {
                directory.Delete();
            } catch (Exception exception) {
                exception.Log();
            }
        }
    }
}
