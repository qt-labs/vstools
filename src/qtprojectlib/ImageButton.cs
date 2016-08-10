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

using System.Drawing;
using System.Windows.Forms;

namespace QtProjectLib
{
    public class ImageButton : System.Windows.Forms.Button
    {
        private Image img;
        private Image dimg;
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

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);

            int xoffset = (Size.Width - img.Width) / 2;
            int yoffset = (Size.Height - img.Height) / 2;
            int imgWidth = img.Width;
            int imgHeight = img.Height;

            // make it smaller if necessary
            if (xoffset < 0)
                imgWidth = Size.Width;
            if (yoffset < 0)
                imgHeight = Size.Height;

            if ((dimg != null) && (!Enabled))
                pevent.Graphics.DrawImage(dimg, xoffset, yoffset,
                    imgWidth, imgHeight);
            else if (img != null)
                pevent.Graphics.DrawImage(img, xoffset, yoffset,
                    imgWidth, imgHeight);
        }
    }
}
