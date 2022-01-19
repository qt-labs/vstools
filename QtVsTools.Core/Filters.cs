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

namespace QtVsTools.Core
{
    public static class Filters
    {
        public static FakeFilter SourceFiles()
        {
            return new FakeFilter
            {
                UniqueIdentifier = "{4FC737F1-C7A5-4376-A066-2A32D752A2FF}",
                Name = SR.GetString("Resources_SourceFiles"),
                Filter = "cpp;c;cc;cxx;def;odl;idl;hpj;bat;asm;asmx"
            };
        }

        public static FakeFilter HeaderFiles()
        {
            return new FakeFilter
            {
                UniqueIdentifier = "{93995380-89BD-4b04-88EB-625FBE52EBFB}",
                Name = SR.GetString("Resources_HeaderFiles"),
                Filter = "h;hh;hpp;hxx;hm;inl;inc;xsd"
            };
        }

        public static FakeFilter FormFiles()
        {
            return new FakeFilter
            {
                UniqueIdentifier = "{99349809-55BA-4b9d-BF79-8FDBB0286EB3}",
                Name = SR.GetString("Resources_FormFiles"),
                Filter = "ui"
            };
        }

        public static FakeFilter ResourceFiles()
        {
            return new FakeFilter
            {
                UniqueIdentifier = "{D9D6E242-F8AF-46E4-B9FD-80ECBC20BA3E}",
                Name = SR.GetString("Resources_ResourceFiles"),
                ParseFiles = false,
                Filter = "qrc;*"
            };
        }

        public static FakeFilter GeneratedFiles()
        {
            return new FakeFilter
            {
                UniqueIdentifier = "{71ED8ED8-ACB9-4CE9-BBE1-E00B30144E11}",
                Name = SR.GetString("Resources_GeneratedFiles"),
                Filter = "moc;h;cpp",
            };
        }
    }
}
