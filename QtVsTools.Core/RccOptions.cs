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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using System;

namespace QtVsTools.Core
{
    /// <summary>
    /// Summary description for RccOptions.
    /// </summary>
    public class RccOptions
    {
        private readonly EnvDTE.Project project;
        private readonly string id;
        private readonly string name;
        private readonly string qrcFileName;

        public RccOptions(EnvDTE.Project pro, VCFile qrcFile)
        {
            project = pro;
            id = qrcFile.RelativePath;
            qrcFileName = qrcFile.FullPath;
            name = id;
            if (id.StartsWith(".\\", StringComparison.Ordinal))
                name = name.Substring(2);
            if (HelperFunctions.IsQrcFile(name))
                name = name.Substring(0, name.Length - 4);
        }

        public bool CompressFiles
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (project.Globals.get_VariablePersists("RccCompressFiles" + id)
                    && (string)project.Globals["RccCompressFiles" + id] == "true")
                    return true;
                return false;
            }
            set
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (value)
                    project.Globals["RccCompressFiles" + id] = "true";
                else
                    project.Globals["RccCompressFiles" + id] = "false";
                if (!project.Globals.get_VariablePersists("RccCompressFiles" + id))
                    project.Globals.set_VariablePersists("RccCompressFiles" + id, true);
            }
        }

        public int CompressLevel
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (project.Globals.get_VariablePersists("RccCompressLevel" + id))
                    return Convert.ToInt32((string)project.Globals["RccCompressLevel" + id], 10);
                return 0;
            }
            set
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                project.Globals["RccCompressLevel" + id] = value.ToString();
                if (!project.Globals.get_VariablePersists("RccCompressLevel" + id))
                    project.Globals.set_VariablePersists("RccCompressLevel" + id, true);
            }
        }

        public int CompressThreshold
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (project.Globals.get_VariablePersists("RccCompressThreshold" + id))
                    return Convert.ToInt32((string)project.Globals["RccCompressThreshold" + id], 10);
                return 0;
            }
            set
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                project.Globals["RccCompressThreshold" + id] = value.ToString();
                if (!project.Globals.get_VariablePersists("RccCompressThreshold" + id))
                    project.Globals.set_VariablePersists("RccCompressThreshold" + id, true);
            }
        }
    }
}
