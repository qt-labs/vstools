/**************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
**************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;

using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace QtVsTools.Package.CMake
{
    using Core;
    using Core.CMake;
    using Microsoft.VisualStudio.Workspace.VSIntegration.UI;
    using QtVsTools.Core.Common;
    using VisualStudio;
    using Wizards.Util;

    internal class CMakeVsProject : IVsProject
    {
        private IFileSystemNode SelectedNode { get; set; }

        public CMakeVsProject(IFileSystemNode node)
        {
            SelectedNode = node;
        }

        public int GenerateUniqueItemName(uint itemIdLoc, string extension, string baseName,
            out string newItemName)
        {
            var files = Enumerable.Empty<string>();
            try {
                files = Directory.GetFiles(CMakeProject.ActiveProject.RootPath, baseName + "*",
                    SearchOption.AllDirectories);
            } catch (Exception exception) {
                exception.Log();
            }

            // extract base file names and select distinct values to calculate the unique name
            var number = files.Select(Path.GetFileNameWithoutExtension).Distinct().Count();
            newItemName = $"{baseName}{(number > 0 ? number : "")}{extension}";
            return VSConstants.S_OK;
        }

        public int AddItem(uint itemIdLoc, VSADDITEMOPERATION addItemOperation, string itemName,
            uint numberOfFilesToOpen, string[] filesToOpen, IntPtr dialogOwner, VSADDRESULT[] result)
        {
            if (result is not { Length: not 0 })
                return VSConstants.E_FAIL;

            try {
                var value = AddItemPrivate(addItemOperation, itemName, filesToOpen);
                result[0] = value == VSConstants.S_OK
                    ? VSADDRESULT.ADDRESULT_Success
                    : VSADDRESULT.ADDRESULT_Failure;
                return value;
            } catch (NotSupportedException exception) {
                exception.Log();
                Messages.DisplayErrorMessage("This item wizard does only support adding Qt items.");
                return VSConstants.E_FAIL;
            }
        }

        private int AddItemPrivate(VSADDITEMOPERATION addItemOperation, string safeItemName,
            string[] filesToOpen)
        {
            if (filesToOpen is not { Length: not 0 })
                throw new NotSupportedException("Missing file to open.");

            if (addItemOperation != VSADDITEMOPERATION.VSADDITEMOP_RUNWIZARD)
                throw new NotSupportedException("Item operations was not run wizard.");

            if (!Path.GetExtension(filesToOpen[0]).Equals(".vstemplate", Utils.IgnoreCase))
                throw new NotSupportedException("No .vstemplate file was provided.");

            var vsTemplate = new VsTemplate(filesToOpen[0]);
            if (!vsTemplate.IsValid)
                throw new NotSupportedException(".vstemplate file was not a Qt template.");

            if (VsServiceProvider.GetService<EnvDTE.DTE, EnvDTE.DTE>() is not {} dte)
                return VSConstants.E_FAIL;

            var targetPath = SelectedNode switch
            {
                IFolderNode => SelectedNode.FullPath,
                IFileNode => Path.GetDirectoryName(SelectedNode.FullPath),
                _ => null
            };

            if (string.IsNullOrEmpty(targetPath))
                return VSConstants.E_FAIL;

            try {
                var templatePath = Path.GetDirectoryName(filesToOpen[0]);
                if (string.IsNullOrEmpty(targetPath)|| string.IsNullOrEmpty(templatePath))
                    return VSConstants.E_FAIL;

                var projectItems = vsTemplate.ProjectItems.ToList();
                if (string.IsNullOrEmpty(vsTemplate.Assembly)) {
                    if (projectItems.Count > 1) // Might be the case if we could not extract the
                        return VSConstants.E_FAIL; // the assembly name from the .vstemplate file.

                    File.Copy( // Templates without wizard, just copy the file(s).
                        Path.Combine(templatePath, projectItems.ElementAt(0).TemplateFileName),
                        Path.Combine(targetPath, safeItemName));
                    return VSConstants.S_OK;
                }

                var assembly = Assembly.Load(vsTemplate.Assembly);
                if (assembly == null || assembly.GetType(vsTemplate.FullClassName) is not { } type)
                    return VSConstants.E_FAIL;
                if (Activator.CreateInstance(type) is not IWizard wizard)
                    return VSConstants.E_FAIL;

                var replacements = new Dictionary<string, string>
                    {
                        {
                            "$safeitemname$", safeItemName
                        }
                    };
                wizard.RunStarted(dte, replacements, WizardRunKind.AsNewItem, null);

                foreach (var projectItem in projectItems) {
                    if (!replacements.TryGetValue(projectItem.TargetFileName, out var target))
                        return VSConstants.E_FAIL;
                    target = Path.Combine(targetPath, target);

                    File.Copy(Path.Combine(templatePath, projectItem.TemplateFileName), target);

                    if (projectItem.ReplaceParameters) {
                        var fileContent = File.ReadAllText(target);
                        fileContent = replacements.Aggregate(fileContent, (current, pair)
                            => current.Replace(pair.Key, pair.Value));
                        File.WriteAllText(target, fileContent);
                    }

                    TextAndWhitespace.Adjust(dte, target);
                }
            } catch (Exception exception) {
                exception.Log();
                return VSConstants.E_FAIL;
            }
            return VSConstants.S_OK;
        }

        #region ### BOILERPLATE ###################################################################
        public int IsDocumentInProject(string documentMoniker, out int documentFound,
            VSDOCUMENTPRIORITY[] priorityLevel, out uint itemId)
        {
            documentFound = 0;
            itemId = 0u;
            return VSConstants.E_NOTIMPL;
        }

        public int GetMkDocument(uint itemId, out string documentMoniker)
        {
            documentMoniker = null;
            return VSConstants.E_NOTIMPL;
        }

        public int OpenItem(uint itemId, ref Guid guidLogicalView, IntPtr documentDataExisting,
            out IVsWindowFrame windowFrame)
        {
            windowFrame = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetItemContext(uint itemId, out IServiceProvider serviceProvider)
        {
            serviceProvider = null;
            return VSConstants.E_NOTIMPL;
        }
        #endregion ### BOILERPLATE ################################################################
    }
}
