/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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

using Microsoft.VisualStudio.VCProjectEngine;
using System;

namespace QtProjectLib
{
    class DeploymentToolWrapper
    {
        object deploymentToolObj;
        Type deploymentToolType;

        public static DeploymentToolWrapper Create(VCConfiguration config)
        {
            DeploymentToolWrapper wrapper = null;
            try {
                wrapper = new DeploymentToolWrapper(config.DeploymentTool);
            } catch {
            }
            return (wrapper.deploymentToolObj == null) ? null : wrapper;
        }

        protected DeploymentToolWrapper(object tool)
        {
            deploymentToolObj = tool;
            deploymentToolType = deploymentToolObj.GetType();
        }

        public void Clear()
        {
            string filesToDeploy = "";
            deploymentToolType.InvokeMember(
                "AdditionalFiles",
                System.Reflection.BindingFlags.SetProperty,
                null,
                deploymentToolObj,
                new object[] { @filesToDeploy });
        }

        public void Add(string filename, string sourceDir, string destDir)
        {
            var filesToDeploy = GetAdditionalFiles();
            if (filesToDeploy.Length > 0)
                filesToDeploy += ";";
            filesToDeploy += filename + "|" + sourceDir + "|" + destDir + "|0";
            SetAdditionalFiles(filesToDeploy);
        }

        public void Remove(string filename, string sourceDir, string destDir)
        {
            var filesToDeploy = GetAdditionalFiles();
            filesToDeploy = filesToDeploy.Replace(filename + "|" + sourceDir + "|" + destDir + "|0", "");
            filesToDeploy = filesToDeploy.Replace(";;", ";");
            if (filesToDeploy.EndsWith(";"))
                filesToDeploy = filesToDeploy.Remove(filesToDeploy.Length - 1);
            SetAdditionalFiles(filesToDeploy);
        }

        public string GetAdditionalFiles()
        {
            object obj;
            try {
                obj = deploymentToolType.InvokeMember(
                    "AdditionalFiles",
                    System.Reflection.BindingFlags.GetProperty,
                    null,
                    deploymentToolObj,
                    null);
            } catch {
                obj = null;
            }

            if (obj != null)
                return (string) obj;

            return "";
        }

        public void SetAdditionalFiles(string value)
        {
            deploymentToolType.InvokeMember(
                "AdditionalFiles",
                System.Reflection.BindingFlags.SetProperty,
                null,
                deploymentToolObj,
                new object[] { @value });
        }
    }
}
