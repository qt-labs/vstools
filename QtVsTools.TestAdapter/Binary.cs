// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using static System.Environment;

namespace QtVsTools.TestAdapter
{
    internal static class Binary
    {
        internal enum Type
        {
            Unknown,
            Console,
            Gui
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_DOS_HEADER
        {
            public ushort e_magic;
            public ushort e_cblp;
            public ushort e_cp;
            public ushort e_crlc;
            public ushort e_cparhdr;
            public ushort e_minalloc;
            public ushort e_maxalloc;
            public ushort e_ss;
            public ushort e_sp;
            public ushort e_csum;
            public ushort e_ip;
            public ushort e_cs;
            public ushort e_lfarlc;
            public ushort e_ovno;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public ushort[] e_res1;
            public ushort e_oemid;
            public ushort e_oeminfo;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public ushort[] e_res2;
            public int e_lfanew; // Offset to the NT header
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_FILE_HEADER
        {
            public ushort Machine;
            public ushort NumberOfSections;
            public uint TimeDateStamp;
            public uint PointerToSymbolTable;
            public uint NumberOfSymbols;
            public ushort SizeOfOptionalHeader;
            public ushort Characteristics;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_OPTIONAL_HEADER
        {
            public ushort Magic;
            public byte MajorLinkerVersion;
            public byte MinorLinkerVersion;
            public uint SizeOfCode;
            public uint SizeOfInitializedData;
            public uint SizeOfUninitializedData;
            public uint AddressOfEntryPoint;
            public uint BaseOfCode;
            public ulong ImageBase;
            public uint SectionAlignment;
            public uint FileAlignment;
            public ushort MajorOperatingSystemVersion;
            public ushort MinorOperatingSystemVersion;
            public ushort MajorImageVersion;
            public ushort MinorImageVersion;
            public ushort MajorSubsystemVersion;
            public ushort MinorSubsystemVersion;
            public uint Win32VersionValue;
            public uint SizeOfImage;
            public uint SizeOfHeaders;
            public uint CheckSum;
            public ushort Subsystem;
            public ushort DllCharacteristics;
            public ulong SizeOfStackReserve;
            public ulong SizeOfStackCommit;
            public ulong SizeOfHeapReserve;
            public ulong SizeOfHeapCommit;
            public uint LoaderFlags;
            public uint NumberOfRvaAndSizes;
            // IMAGE_DATA_DIRECTORY is variable-length; exclude
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_NT_HEADERS
        {
            public uint Signature;
            public IMAGE_FILE_HEADER FileHeader;
            public IMAGE_OPTIONAL_HEADER OptionalHeader;
        }

        private const ushort IMAGE_DOS_SIGNATURE = 0x5A4D; // "MZ"
        private const uint IMAGE_NT_SIGNATURE = 0x00004550; // "PE\0\0"
        private const int IMAGE_SUBSYSTEM_WINDOWS_GUI = 2;
        private const int IMAGE_SUBSYSTEM_WINDOWS_CUI = 3;

        internal static bool TryGetType(string filePath, Logger log, out Type type)
        {
            type = Type.Unknown;

            if (!File.Exists(filePath)) {
                log.SendMessage($"Binary type check failed - File not found: '{filePath}'.",
                    TestMessageLevel.Error);
                return false;
            }

            try {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var reader = new BinaryReader(stream);

                if (stream.Length < Marshal.SizeOf<IMAGE_DOS_HEADER>()) {
                    log.SendMessage("Binary type check failed - File is too small to contain a "
                        + $"valid DOS header: '{filePath}'.", TestMessageLevel.Error);
                    return false;
                }

                var dosHeader = ReadStruct<IMAGE_DOS_HEADER>(reader);
                if (dosHeader.e_magic != IMAGE_DOS_SIGNATURE) {
                    log.SendMessage("Binary type check failed - Invalid DOS header signature in "
                        + $"file: '{filePath}'. Expected: {IMAGE_DOS_SIGNATURE}, "
                        + $"Actual: {dosHeader.e_magic}.", TestMessageLevel.Error);
                    return false;
                }

                if (dosHeader.e_lfanew < 0
                    || dosHeader.e_lfanew > stream.Length - Marshal.SizeOf<IMAGE_NT_HEADERS>()) {
                    log.SendMessage("Binary type check failed - Invalid or corrupted NT header "
                        + $"offset in file: '{filePath}'. Offset: {dosHeader.e_lfanew}.",
                        TestMessageLevel.Error);
                    return false;
                }

                stream.Seek(dosHeader.e_lfanew, SeekOrigin.Begin);

                var ntHeaders = ReadStruct<IMAGE_NT_HEADERS>(reader);
                if (ntHeaders.Signature != IMAGE_NT_SIGNATURE) {
                    log.SendMessage("Binary type check failed - Invalid NT header signature in "
                        + $"file: '{filePath}'. Expected: {IMAGE_NT_SIGNATURE}, "
                        + $"Actual: {ntHeaders.Signature}.", TestMessageLevel.Error);
                    return false;
                }

                type = ntHeaders.OptionalHeader.Subsystem switch
                {
                    IMAGE_SUBSYSTEM_WINDOWS_GUI => Type.Gui,
                    IMAGE_SUBSYSTEM_WINDOWS_CUI => Type.Console,
                    _ => Type.Unknown
                };
            } catch (IOException ioException) {
                log.SendMessage($"IOException occurred while checking the binary type.{NewLine}"
                    + $"{ioException}", TestMessageLevel.Error);
            } catch (UnauthorizedAccessException unauthorizedAccessException) {
                log.SendMessage("UnauthorizedAccessException occurred while checking the binary "
                    + $"type.{NewLine}{unauthorizedAccessException}", TestMessageLevel.Error);
            } catch (Exception exception) {
                log.SendMessage($"An exception occurred while checking the binary type.{NewLine}"
                    + $"{exception}", TestMessageLevel.Error);
            }

            return type != Type.Unknown;
        }

        private static T ReadStruct<T>(BinaryReader reader) where T : struct
        {
            var size = Marshal.SizeOf<T>();
            var bytes = reader.ReadBytes(size);

            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            } finally {
                handle.Free();
            }
        }
    }
}
