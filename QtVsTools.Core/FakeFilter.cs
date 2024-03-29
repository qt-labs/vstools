/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

namespace QtVsTools.Core
{
    public class FakeFilter
    {
        public string Name { get; private set; }
        public string Filter { get; private set; }
        public string UniqueIdentifier { get; private set; }
        public bool ParseFiles { get; private set; } = true;

        public static FakeFilter SourceFiles()
        {
            return new FakeFilter
            {
                UniqueIdentifier = "{4FC737F1-C7A5-4376-A066-2A32D752A2FF}",
                Name = "Source Files",
                Filter = "cpp;c;cc;cxx;def;odl;idl;hpj;bat;asm;asmx"
            };
        }

        public static FakeFilter HeaderFiles()
        {
            return new FakeFilter
            {
                UniqueIdentifier = "{93995380-89BD-4b04-88EB-625FBE52EBFB}",
                Name = "Header Files",
                Filter = "h;hh;hpp;hxx;hm;inl;inc;xsd"
            };
        }

        public static FakeFilter FormFiles()
        {
            return new FakeFilter
            {
                UniqueIdentifier = "{99349809-55BA-4b9d-BF79-8FDBB0286EB3}",
                Name = "Form Files",
                Filter = "ui"
            };
        }

        public static FakeFilter ResourceFiles()
        {
            return new FakeFilter
            {
                UniqueIdentifier = "{D9D6E242-F8AF-46E4-B9FD-80ECBC20BA3E}",
                Name = "Resource Files",
                ParseFiles = false,
                Filter = "qrc;*"
            };
        }

        public static FakeFilter GeneratedFiles()
        {
            return new FakeFilter
            {
                UniqueIdentifier = "{71ED8ED8-ACB9-4CE9-BBE1-E00B30144E11}",
                Name = "Generated Files",
                Filter = "moc;h;cpp"
            };
        }

        public static FakeFilter TranslationFiles()
        {
            return new FakeFilter
            {
                UniqueIdentifier = "{639EADAA-A684-42e4-A9AD-28FC9BCB8F7C}",
                Name = "Translation Files",
                Filter = "ts;qm"
            };
        }
    }
}
