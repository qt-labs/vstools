/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Add-in.
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

namespace Digia.Qt5ProjectLib
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    public class ProjectMacros
    {
#if (VS2010 || VS2012 || VS2013)
        public const string Name = "%(Filename)";
        public const string FileName = "%(Identity)";
        public const string Path = "%(FullPath)";
#else
        public const string Name = "$(InputName)";
        public const string FileName = "$(InputFileName)";
        public const string Path = "$(InputPath)";
#endif
    }

    public class FakeFilter
    {
        private string uniqueIdentifier = "";
        private string name = "";
        private string filter = "";
        private bool parseFiles = true;
        private bool sccFiles = true;

        public string UniqueIdentifier
        {
            get { return uniqueIdentifier; }
            set { uniqueIdentifier = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string Filter
        {
            get { return filter; }
            set { filter = value; }
        }

        public bool ParseFiles
        {
            get { return parseFiles; }
            set { parseFiles = value; }
        }

        public bool SCCFiles
        {
            get { return sccFiles; }
            set { sccFiles = value; }
        }
    }

    public struct BuildConfig
    {
        public const uint Both    = 0x03;
        public const uint Release = 0x01;
        public const uint Debug   = 0x02;
    }

    public enum FilesToList 
    {
        FL_Resources    = 1,
        FL_CppFiles     = 2,
        FL_HFiles       = 3,
        FL_UiFiles      = 4,
        FL_Generated    = 5,
        FL_Translation  = 6,
        FL_WinResource  = 7
    }

    public struct TemplateType
    {
        // project type
        public const uint ProjectType       = 0x003; // 0011
        public const uint Application       = 0x000; // 0000
        public const uint DynamicLibrary    = 0x001; // 0001
        public const uint StaticLibrary     = 0x002; // 0010
        // subsystem
        public const uint GUISystem         = 0x004; // 0100
        public const uint ConsoleSystem     = 0x008; // 1000
        // qt3
        public const uint Qt3Project        = 0x010; //10000
        // plugin
        public const uint PluginProject     = 0x100;
        // Windows CE
        public const uint WinCEProject      = 0x200;
    }

    [Serializable]
    public class QtVSException : ApplicationException
    {
        public QtVSException(string message)
            : base(message)
        {
        }
    }

    public class MainWinWrapper : IWin32Window
    {
        private EnvDTE.DTE dteObject = null;

        public MainWinWrapper(EnvDTE.DTE dte)
        {
            dteObject = dte;
        }

        #region IWin32Window Members
        public System.IntPtr Handle
        {
            get
            {
                if (dteObject != null)
                    return new System.IntPtr(dteObject.MainWindow.HWnd);
                return new System.IntPtr(0);
            }
        }
        #endregion
    }

    public class ImageButton : System.Windows.Forms.Button
    {
        private Image img = null;
        private Image dimg = null;
        public ImageButton(Image image)
        {
            img = image;
            BackColor = System.Drawing.SystemColors.Control;
        }

        // support for disabled image
        public ImageButton(Image image, Image dimage)
        {
            img = image;
            dimg = dimage;
            BackColor = System.Drawing.SystemColors.Control;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int xoffset = (Size.Width - img.Width)/2;
            int yoffset = (Size.Height - img.Height)/2;
            int imgWidth = img.Width;
            int imgHeight = img.Height;

            // make it smaller if necessary
            if (xoffset < 0)
                imgWidth = Size.Width;
            if (yoffset < 0)
                imgHeight = Size.Height;

            if ((dimg != null) && (!this.Enabled))
                e.Graphics.DrawImage(dimg, xoffset, yoffset,
                    imgWidth, imgHeight);
            else if (img != null)
                e.Graphics.DrawImage(img, xoffset, yoffset,
                    imgWidth, imgHeight);
        }
    }    
}
