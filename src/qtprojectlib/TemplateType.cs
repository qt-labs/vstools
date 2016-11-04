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

namespace QtProjectLib
{
    public struct TemplateType
    {
        // project type
        public const uint ProjectType = 0x003; // 0011
        public const uint Application = 0x000; // 0000
        public const uint DynamicLibrary = 0x001; // 0001
        public const uint StaticLibrary = 0x002; // 0010
        // subsystem
        public const uint GUISystem = 0x004; // 0100
        public const uint ConsoleSystem = 0x008; // 1000
        // // qt3
        // public const uint Qt3Project = 0x010; //10000
        // plugin
        public const uint PluginProject = 0x100;
    }
}
