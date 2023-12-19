/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml.Linq;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Newtonsoft.Json.Linq;
using JsonFormatting = Newtonsoft.Json.Formatting;

namespace QtVsTools.Core.MsBuild
{
    using Common;
    using QtVsTools.Common;
    using static Common.Utils;
    using static MsBuildProjectReaderWriter;

    public class ConversionReport
    {
        public static ConversionReport Generate(ConversionData data)
        {
            if (GenerateReportXaml(data) is not { Length: > 0 } xaml)
                return null;
            return new ConversionReport { Xaml = xaml };
        }

        public bool Save(string path)
        {
            try {
                var xamlClean = Xaml.TrimEnd(' ', '\r', '\n') + "\r\n";
                var xamlUtf8 = Encoding.UTF8.GetBytes(xamlClean);
                using var xamlStream = File.Open(path, FileMode.Create);
                using var xamlFile = new BinaryWriter(xamlStream);
                xamlFile.Write(xamlUtf8);
                xamlFile.Write(Encoding.UTF8.GetBytes
                    ($"<!--{Convert.ToBase64String(Hash(xamlUtf8))}-->\r\n"));
                xamlFile.Flush();
                xamlFile.Close();
            } catch (Exception e) {
                e.Log();
                return false;
            }
            MsBuildProject.ShowProjectFormatUpdated(path);
            return true;
        }

        public static ConversionReport Load(string path)
        {
            var report = new ConversionReport();
            try {
                var xamlUtf8 = File.ReadAllBytes(path);
                var footerIdx = xamlUtf8.LastIndexOfArray(Encoding.UTF8.GetBytes("<!--"));
                if (footerIdx == -1)
                    return null;
                var cksum = Regex.Match(
                    Encoding.UTF8.GetString(xamlUtf8, footerIdx, xamlUtf8.Length - footerIdx),
                    @"<!--(?<hash>[A-Za-z0-9+/=]+)-->");
                if (!cksum.Success)
                    return null;
                var xamlHash = Convert.FromBase64String(cksum.Groups["hash"].Value);
                if (!xamlHash.SequenceEqual(Hash(xamlUtf8, 0, footerIdx)))
                    return null;
                report.Xaml = Encoding.UTF8.GetString(xamlUtf8, 0, footerIdx);
            } catch (Exception e) {
                e.Log();
                return null;
            }
            return report;
        }

        private ConversionReport()
        { }

        private LazyFactory Lazy { get; } = new();

        private string Xaml { get; set; }

        public FlowDocument Document => Lazy.Get(() => Document, () =>
        {
            try {
                if (XamlReader.Parse(Xaml) is FlowDocument doc)
                    return doc;
            } catch (Exception) {
                return null;
            }
            return null;
        });

        private const string DefaultFont = "Segoe UI";
        private const string DefaultMonospacedFont = "Consolas";
        private const double DefaultFontSize = 12.0;

        private static string GenerateReportXaml(ConversionData data)
        {
            XElement sectionFiles;
            XElement sectionCommits;
            var doc = new XDocument(
                new XElement("FlowDocument",
                    new XAttribute("FontFamily", DefaultFont),
                    new XAttribute("FontSize", DefaultFontSize),

                    XElement.Parse($@"
                        <FlowDocument.Tag>
                            <![CDATA[{EmbeddedMetadata(data)}]]>
                        </FlowDocument.Tag>"),

                    XElement.Parse($@"
                        <Section Margin=""0,24"" TextAlignment=""Center"">
                            <Paragraph FontSize=""24"" FontWeight=""Bold"" Margin=""12,0"">
                                <LineBreak />
                                <Span Foreground=""Gray"">Qt Visual Studio Tools</Span>
                            </Paragraph>
                            <Paragraph FontSize=""42"" Margin=""12,0"" FontWeight=""Bold"">
                                <Span TextDecorations=""Underline"">Project Format Conversion</Span>
                            </Paragraph>
                            <Paragraph Margin=""12,8"" FontSize=""18"">
                                <Span>Report generated on {data.DateTime:yyyy-MM-dd HH:mm:ss}</Span>
                            </Paragraph>
                        </Section>"),

                    sectionFiles = XElement.Parse(@"
                        <Section>
                            <Paragraph FontSize=""32"" FontWeight=""Bold"" Margin=""12,0"">
                                <Span>Files</Span>
                            </Paragraph>
                        </Section>"),

                    sectionCommits = XElement.Parse(@"
                        <Section>
                            <Paragraph FontSize=""32"" FontWeight=""Bold"" Margin=""12,0"">
                                <Span>Changes</Span>
                            </Paragraph>
                        </Section>"),

                    XElement.Parse(@"
                        <Section><Paragraph/></Section>")));

            foreach (var file in data.FilesChanged)
                GenerateFileReport(sectionFiles, file);

            foreach (var commit in data.Commits)
                GenerateCommitReport(sectionCommits, commit);

            return Regex.Replace(doc.ToString(), @"</Span>(\r?\n)+\s*<Span", @"</Span><Span")
                .Replace("<FlowDocument ", "<FlowDocument "
                    + "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" ");
        }

        private static void GenerateFileReport(XElement sectionFiles, FileChangeData file)
        {
            sectionFiles.Add(XElement.Parse($@"
                <Paragraph Margin=""24,12,0,0"">
                    <Span FontFamily=""{DefaultMonospacedFont}"" FontSize=""14""
                        Background=""WhiteSmoke"">
                        <![CDATA[{file.Path}]]>
                    </Span>
                    <LineBreak />
                    <Hyperlink NavigateUri=""file://{file.Path}?before"">[Before]</Hyperlink>
                    <Hyperlink NavigateUri=""file://{file.Path}?after"">[After]</Hyperlink>
                    <Hyperlink NavigateUri=""file://{file.Path}?diff&amp;before&amp;after"">
                        [Diff before/after]
                    </Hyperlink>
                    <Hyperlink NavigateUri=""file://{file.Path}?diff&amp;before&amp;current"">
                        [Diff before/current]
                    </Hyperlink>
                    <Hyperlink NavigateUri=""file://{file.Path}?diff&amp;after&amp;current"">
                        [Diff after/current]
                    </Hyperlink>
                    <LineBreak />
                </Paragraph>"));
        }

        private static void GenerateCommitReport(XElement sectionCommits, CommitData commit)
        {
            sectionCommits.Add(XElement.Parse($@"
                <Paragraph FontSize=""20"" FontWeight=""Bold"" Margin=""12"">
                    <Span><![CDATA[🡺 {commit.Message}]]></Span>
                </Paragraph>"));
            foreach (var file in commit.Changes) {
                XElement diffTable;
                sectionCommits.Add(
                    diffTable = XElement.Parse($@"
                        <Table CellSpacing=""0"" BorderBrush=""Gray"" BorderThickness=""0.5"">
                            <Table.Columns>
                                <TableColumn />
                                <TableColumn />
                            </Table.Columns>
                            <TableRowGroup>
                                <TableRow Background=""Orange"">
                                    <TableCell ColumnSpan=""2""
                                        BorderThickness=""0.5"" BorderBrush=""Gray"">
                                        <Paragraph Margin=""10,5"" FontWeight=""Bold"">
                                            <Span><![CDATA[{file.Path}]]></Span>
                                        </Paragraph>
                                    </TableCell>
                                </TableRow>
                            </TableRowGroup>
                        </Table>"));
                GenerateDiffReport(diffTable,
                    SideBySideDiffBuilder.Instance.BuildDiffModel(file.Before, file.After));
            }
        }

        private static void GenerateDiffReport(XElement table, SideBySideDiffModel diff)
        {
            var oldLines = diff.OldText.Lines;
            var newLines = diff.NewText.Lines;
            Debug.Assert(oldLines.Count == newLines.Count);

            var rows = table.Element("TableRowGroup");
            Debug.Assert(rows is not null);

            int lastLine = -1;
            for (int i = 0; i < Math.Min(oldLines.Count, newLines.Count); ++i) {
                var left = oldLines[i];
                var right = newLines[i];
                if ((left.Type == ChangeType.Imaginary || left.Type == ChangeType.Unchanged)
                    && right.Type == left.Type) {
                    continue;
                }

                if (lastLine == -1 || lastLine < i - 1) {
                    rows.Add(XElement.Parse(@"
                        <TableRow>
                            <TableCell ColumnSpan =""2"" Background=""WhiteSmoke""
                                BorderBrush=""Gray"" BorderThickness=""0.5"" />
                        </TableRow>"));
                }
                lastLine = i;

                XElement row;
                rows.Add(row = new XElement("TableRow"));

                GenerateDiffLineReport(row, left);
                GenerateDiffLineReport(row, right);
            }
        }

        private static void GenerateDiffLineReport(XElement row, DiffPiece line)
        {
            XElement cell;

            row.Add(new XElement("TableCell",
                new XAttribute("BorderThickness","0, 0, 0.5, 0"),
                new XAttribute("BorderBrush", "Gray"),
                cell = XElement.Parse($@"
                    <Paragraph FontFamily=""{DefaultMonospacedFont}"" Margin=""4, 0""/>")));

            if (line.Type == ChangeType.Imaginary)
                return;

            var linePieces = line.Type switch
            {
                ChangeType.Modified => line.SubPieces,
                _ => new() { line }
            };

            var span = new StringBuilder();
            for (int j = 0; j < linePieces.Count; ++j) {
                var piece = linePieces[j];
                if (piece.Type == ChangeType.Imaginary)
                    continue;
                span.Append(piece.Text);
                if (linePieces.ElementAtOrDefault(j + 1) is { } next
                    && next.Type == piece.Type) {
                    continue;
                }
                span.Replace(' ', '\x00a0');
                switch (piece.Type) {
                case ChangeType.Unchanged:
                    cell.Add(XElement.Parse($@"
                        <Span><![CDATA[{span}]]></Span>"));
                    break;
                case ChangeType.Modified:
                    cell.Add(XElement.Parse($@"
                        <Span Background=""LemonChiffon""><![CDATA[{span}]]></Span>"));
                    break;
                case ChangeType.Deleted:
                    cell.Add(XElement.Parse($@"
                        <Span Background=""LightCoral""><![CDATA[{span}]]></Span>"));
                    break;
                case ChangeType.Inserted:
                    cell.Add(XElement.Parse($@"
                        <Span Background=""LightGreen""><![CDATA[{span}]]></Span>"));
                    break;
                }
                span.Clear();
            }
        }

        private static string EmbeddedMetadata(ConversionData data)
        {
            var metadata = new JObject
            {
                ["timestamp"] = $"{data.DateTime:yyyy-MM-dd HH:mm:ss}",
                ["files"] = new JObject()
            };
            foreach (var file in data.FilesChanged) {
                metadata["files"][file.Path] = new JObject
                {
                    ["before"] = file.Before.ToZipBase64(),
                    ["after"] = file.After.ToZipBase64()
                };
            }
            return metadata.ToString(JsonFormatting.Indented);
        }
    }
}
