/****************************************************************************
**
** Copyright (C) 2012 Digia Plc and/or its subsidiary(-ies).
** Contact: http://www.qt-project.org/legal
**
** This file is part of the Qt VS Add-in.
**
** $QT_BEGIN_LICENSE:LGPL$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Digia.  For licensing terms and
** conditions see http://qt.digia.com/licensing.  For further information
** use the contact form at http://qt.digia.com/contact-us.
**
** GNU Lesser General Public License Usage
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** In addition, as a special exception, Digia gives you certain additional
** rights.  These rights are described in the Digia Qt LGPL Exception
** version 1.1, included in the file LGPL_EXCEPTION.txt in this package.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3.0 as published by the Free Software
** Foundation and appearing in the file LICENSE.GPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU General Public License version 3.0 requirements will be
** met: http://www.gnu.org/copyleft/gpl.html.
**
**
** $QT_END_LICENSE$
**
****************************************************************************/

using System;
using System.ComponentModel;
using System.Windows.Forms.Design;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Digia.Qt5ProjectLib
{
    /// <summary>
    /// Summary description for RccOptions.
    /// </summary>
    public class RccOptions
    {
        private EnvDTE.Project project;
        private string id;
        private string name;
        private string qrcFileName;

        public RccOptions(EnvDTE.Project pro, VCFile qrcFile)
        {
            project = pro;
            id = qrcFile.RelativePath;
            qrcFileName = qrcFile.FullPath;
            name = id;
            if (id.StartsWith(".\\"))
                name = name.Substring(2);
            if (name.EndsWith(".qrc"))
                name = name.Substring(0, name.Length-4);
        }

        #region Properties
        public string Prefix
        {
            get
            {
                if (project.Globals.get_VariablePersists("RccPrefix"+id))
                    return (string)project.Globals["RccPrefix"+id];
                else
                    return "/" + project.Name;
            }
            set
            {
                project.Globals["RccPrefix" + id] = value;
                if (!project.Globals.get_VariablePersists("RccPrefix" + id))
                    project.Globals.set_VariablePersists("RccPrefix" + id, true);
            }
        }

        public bool CompressFiles
        {
            get
            {
                if (project.Globals.get_VariablePersists("RccCompressFiles"+id)
                    && (string)project.Globals["RccCompressFiles"+id] == "true")
                    return true;
                else
                    return false;
            }
            set
            {
                if (value)
                    project.Globals["RccCompressFiles"+id] = "true";
                else
                    project.Globals["RccCompressFiles"+id] = "false";
                if (!project.Globals.get_VariablePersists("RccCompressFiles" + id))
                    project.Globals.set_VariablePersists("RccCompressFiles" + id, true);
            }
        }

        public int CompressLevel
        {
            get
            {
                if (project.Globals.get_VariablePersists("RccCompressLevel"+id))
                    return Convert.ToInt32((string)project.Globals["RccCompressLevel"+id], 10);
                else
                    return 0;
            }
            set
            {
                project.Globals["RccCompressLevel"+id] = value.ToString();
                if (!project.Globals.get_VariablePersists("RccCompressLevel" + id))
                    project.Globals.set_VariablePersists("RccCompressLevel" + id, true);
            }
        }

        public int CompressThreshold
        {
            get
            {
                if (project.Globals.get_VariablePersists("RccCompressThreshold"+id))
                    return Convert.ToInt32((string)project.Globals["RccCompressThreshold"+id], 10);
                else
                    return 0;
            }
            set
            {
                project.Globals["RccCompressThreshold"+id] = value.ToString();
                if (!project.Globals.get_VariablePersists("RccCompressThreshold" + id))
                    project.Globals.set_VariablePersists("RccCompressThreshold" + id, true);
            }
        }

        public string OutputFileName
        {
            get
            {
                if (project.Globals.get_VariablePersists("RccOutput"+id)) 
                {
                    return (string)project.Globals["RccOutput"+id];
                }
                else 
                {
                    string s = name.Replace('\\', '/');
                    s = s.Substring(s.LastIndexOf('/')+1);
                    return "qrc_" + s + ".cpp";                
                }
            }
            set
            {
                project.Globals["RccOutput"+id] = value;
                if (!project.Globals.get_VariablePersists("RccOutput" + id))
                    project.Globals.set_VariablePersists("RccOutput" + id, true);
            }
        }

        public string QrcFileName
        {
            get { return qrcFileName; }
        }

        public string InitName
        {
            get
            {
                if (project.Globals.get_VariablePersists("RccInitName"+id)) 
                {
                    return (string)project.Globals["RccInitName"+id];
                }
                else
                {
                    string s = name.Replace('\\', '/');
                    s = s.Substring(s.LastIndexOf('/')+1);
                    return s;
                }
            }
            set
            {
                project.Globals["RccInitName"+id] = value;
                if (!project.Globals.get_VariablePersists("RccInitName" + id))
                    project.Globals.set_VariablePersists("RccInitName" + id, true);
            }
        }
        #endregion
    }
}
