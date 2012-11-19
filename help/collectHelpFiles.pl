#!perl

use File::Path;
use File::Copy;
use Getopt::Long;

&GetOptions('qtdir:s', \$qtDir, 'outDir:s', \$outDir, 'version:s', \$ver, 'compile!', \$compile,
			'nowritefiles!', \$noWriteFiles);

my $version = "3_3";

if ($ver ne "") {
	$version = $ver;
}

my $writeTitleInIndices = 0;

my $title = "Qt Help Collection";
my $fileVersion = "1.0.0.0";
my $namespace = "DigiaPlc.Qt.1033";
my $uniqueID = "QtHelp" . $version;

my $docSet = "qtrefdoc" . $version;
my $docSetTitle = "Qt Reference Documentation";
my $filterName = "Qt Reference Documentation";
my $linkGroup = "qtrefdoclg" . $version;
my $linkGroupTitle = "Qt Help";
my $linkGroupPriority = "1300";
my $dynamicHelpDefaultLink = "ms-help://MS.VSCC.2003/" . $namespace . "/" . $uniqueID . "/html/index.html";

my $pluginStyle = "Hierarchical";
my $pluginTitle = $title;

my $outPath = ".\\help\\";

if ($qtDir eq "") {
	print "No qtDir specified! Exit now.\n";
	exit;
}
my $sourcePath_tmp = $qtDir . "\\";

if ($outDir ne "") {
	$outPath = $outDir . "\\";
}

my $outSourcePath = $outPath . "html\\";
my $outCollectionPath = $outPath . "collectionFiles\\";
my $sourcePath = $sourcePath_tmp . "\\doc\\html\\";
my $gifPath = $sourcePath_tmp . "\\gif\\";


###################
# define file names

my $projectName = "qt-" . $version . ".HWProj";

my $outHxSFile = "qt-" . $version . ".HxS";

my $outHxCFile = "qt-" . $version . ".HxC";
my $outHxFFile = "qt-" . $version . ".HxF";
my $outHxVFile = "qt-" . $version . ".HxV";
my $outHxTFile = "qt-" . $version . ".HxT";
my $outHxKKFile = "qt-" . $version . "K.HxK";
my $outHxKFFile = "qt-" . $version . "F.HxK";

my $outHxCColFile = "qt-" . $version . "C.HxC";
my $outHxTColFile = "qt-" . $version . "C.HxT";
my $outHxKKColFile = "qt-" . $version . "KC.HxK";
my $outHxKFColFile = "qt-" . $version . "FC.HxK";
my $outHxAColFile = "qt-" . $version . "C.HxA";

my $groupDefFile = "qt-" . $version . ".xml";
my $h2regFile = "qt-" . $version . ".ini";

my $logFile = "compile.log";


##############
# create paths

if (opendir OUTPATH, $outPath) {
    closedir OUTPATH;
} else {
    mkpath($outPath, 1, 0711);
}

if (opendir OUTPATH, $outSourcePath) {
    closedir OUTPATH;
} else {
    mkpath($outSourcePath, 1, 0711);
}

if (opendir OUTPATH, $outCollectionPath) {
    closedir OUTPATH;
} else {
    mkpath($outCollectionPath, 1, 0711);
}


##########################
# collect all source files

if ($noWriteFiles == 0) {
opendir SOURCEDIR, $sourcePath or die "can not open directory $sourcePath";
my @files = grep /(\.png$|\.html$)/, readdir SOURCEDIR;
closedir SOURCEDIR;

opendir SOURCEDIR, $gifPath or die "can not open directory $gifPath";
my @files_tmp = grep /(\.png$)/, readdir SOURCEDIR;
closedir SOURCEDIR;

foreach (@files) {
	my $file = $_;
	my $sfile = $sourcePath . $file;
	my $dfile = $outSourcePath . $file;
	print "writing file $dfile\n";
	unless ($file =~ /\.html$/) {
		copy($sfile, $dfile);
		next;
	}
	unless (open IN, "< $sfile") {
		print "warning: can not open file $sfile - skipping it!\n";
		next;
	}
	unless (open OUT, "> $dfile") {
		next;
	}

	while (readline IN) {
		my $line = $_;
		if ($line =~ /^<html>\s*$/) {
			print OUT "<html xmlns:MSHelp=\"http://msdn.microsoft.com/mshelp\">\n";
			next;
		}
		if ($line =~ /^<\/head>\s*/) {
			print OUT "<xml>\n";
			print OUT "\t<MSHelp:Attr Name=\"DocSet\" Value=\"$docSet\"/>\n";
			print OUT "\t<MSHelp:Attr Name=\"Locale\" Value=\"kbEnglish\"/>\n";
			print OUT "\t<MSHelp:Attr Name=\"TopicType\" Value=\"kbSyntax\"/>\n";
			print OUT "\t<MSHelp:Attr Name=\"DevLang\" Value=\"C++\"/>\n";
			print OUT "\t<MSHelp:Attr Name=\"LinkGroup\" Value=\"$linkGroup\"/>\n";
			print OUT "</xml>\n";
			print OUT "</head>\n";
			next;
		}	
		print OUT $line;
	}
	close OUT;
	close IN;
}

foreach (@files_tmp) {
	my $file = $_;
	my $sfile = $gifPath . $file;
	my $dfile = $outSourcePath . $file;
	print "writing file $dfile\n";
	copy($sfile, $dfile);
}
}
################################
# start writing the project file

my $fullName = $outPath . $projectName;
open PRO, "> $fullName" or die "can not open file $fullName";
print PRO "<?xml version=\"1.0\"?>\n";
print PRO "<HelpWorkshopProject>\n";
print PRO "\t<ProjectFile Name=\"$outHxCFile\"/>\n";
print PRO "\t<IncludeFile Name=\"$outHxFFile\"/>\n";
print PRO "\t<Files>\n";
print PRO "\t\t<File Url=\"$outHxTFile\"/>\n";
print PRO "\t\t<File Url=\"$outHxKKFile\"/>\n";
print PRO "\t\t<File Url=\"$outHxKFFile\"/>\n";
print PRO "\t\t<File Url=\"$outHxVFile\"/>\n";
print PRO "\t\t<File Url=\"$groupDefFile\"/>\n";


################
# write HxC file

$fullName = $outPath . $outHxCFile;
open HxC, "> $fullName" or die "can not open file $fullName";
print HxC "<?xml version=\"1.0\"?>\n";
print HxC "<!DOCTYPE HelpCollection SYSTEM \"ms-help://hx/resources/HelpCollection.DTD\">\n";
print HxC "<HelpCollection DTDVersion=\"1.0\" LangId=\"1033\" Title=\"$title\" FileVersion=\"$fileVersion\" Copyright=\"Digia Plc\">\n";
print HxC "\t<CompilerOptions CreateFullTextIndex=\"Yes\" CompileResult=\"Hxs\">\n";
print HxC "\t\t<IncludeFile File=\"$outHxFFile\"/>\n";
print HxC "\t</CompilerOptions>\n";
print HxC "\t<VTopicDef File=\"$outHxVFile\"/>\n";
print HxC "\t<TOCDef File=\"$outHxTFile\"/>\n";
print HxC "\t<KeywordIndexDef File=\"$outHxKKFile\"/>\n";
print HxC "\t<KeywordIndexDef File=\"$outHxKFFile\"/>\n";
print HxC "\t<ItemMoniker Name=\"!DefaultNamedUrlIndex\" ProgId=\"HxDs.HxIndex\" InitData=\"K\"/>\n";
print HxC "\t<ItemMoniker Name=\"!DefaultToc\" ProgId=\"HxDs.HxHierarchy\" InitData=\"\"/>\n";
print HxC "\t<ItemMoniker Name=\"!DefaultFullTextSearch\" ProgId=\"HxDs.HxFullTextSearch\" InitData=\"\"/>\n";
print HxC "\t<ItemMoniker Name=\"!DefaultAssociativeIndex\" ProgId=\"HxDs.HxIndex\" InitData=\"A\"/>\n";
print HxC "\t<ItemMoniker Name=\"!DefaultKeywordIndex\" ProgId=\"HxDs.HxIndex\" InitData=\"K\"/>\n";
print HxC "\t<ItemMoniker Name=\"!SampleInfo\" ProgId=\"HxDs.HxSampleCollection\" InitData=\"\"/>\n";
print HxC "\t<ItemMoniker Name=\"!DefaultContextWindowIndex\" ProgId=\"HxDs.HxIndex\" InitData=\"F\"/>\n";
print HxC "\t<ToolData Name=\"MSVHWLogLevel\" Value=\"3\"/>\n";
print HxC "\t<ToolData Name=\"MSVHWNamespace\" Value=\"$namespace\"/>\n";
print HxC "\t<ToolData Name=\"MSVHWUniqueID\" Value=\"$uniqueID\"/>\n";
print HxC "</HelpCollection>";
close HxC;

################
# write HxF file

$fullName = $outPath . $outHxFFile;
open HxF, "> $fullName" or die "can not open file $fullName";

opendir SOURCEDIR, $sourcePath or die "can not open directory $sourcePath";
@files = grep /(\.png$|\.html$)/, readdir SOURCEDIR;
closedir SOURCEDIR;

opendir SOURCEDIR, $gifPath or die "can not open directory $gifPath";
@files_tmp = grep /(\.png$)/, readdir SOURCEDIR;
closedir SOURCEDIR;

push @files, @files_tmp;

print HxF "<?xml version=\"1.0\"?>\n";
print HxF "<!DOCTYPE HelpFileList SYSTEM \"ms-help://hx/resources/HelpFileList.DTD\">\n";
print HxF "<HelpFileList DTDVersion=\"1.0\">\n";
foreach (@files) {
	print HxF "\t<File Url=\"html\\$_\"/>\n";
	print PRO "\t\t<File Url=\"html\\$_\"/>\n";	
}
print HxF "\t<File Url=\"$groupDefFile\"/>\n";    
print HxF "</HelpFileList>";
close HxF;


####################################
# write the rest of the project file

print PRO "\t</Files>\n";
print PRO "\t<Dirs>\n";
print PRO "\t<Dir Url=\"html\"/>\n";
print PRO "\t\t</Dirs>\n";
print PRO "\t<Options MSTOCMRUDIR=\"$outSourcePath\" MSTOCMRUFT=\"0\"/>\n";
print PRO "</HelpWorkshopProject>";
close PRO;


###############
# write indices

$fullName = $outPath . $outHxKKFile;
open HxKK, "> $fullName" or die "can not open file $fullName";
$fullName = $outPath . $outHxKFFile;
open HxKF, "> $fullName" or die "can not open file $fullName";

my $titleIndex = $sourcePath . "titleindex";
open TITLEINDEX, "< $titleIndex" or die "can not open file $titleIndex";

print HxKK "<?xml version=\"1.0\"?>\n";
print HxKK "<!DOCTYPE HelpIndex SYSTEM \"ms-help://hx/resources/HelpIndex.DTD\">\n";
print HxKK "<HelpIndex DTDVersion=\"1.0\" Name=\"K\">\n";
print HxKF "<?xml version=\"1.0\"?>\n";
print HxKF "<!DOCTYPE HelpIndex SYSTEM \"ms-help://hx/resources/HelpIndex.DTD\">\n";
print HxKF "<HelpIndex DTDVersion=\"1.0\" Name=\"F\">\n";

if ($writeTitleInIndices == 1) {
while (readline TITLEINDEX) {
	my $line = $_;
	chomp($line);
	my($term, $url) = split /\s\|\s/, $line;
	$term =~ s/"/&quot;/g;
	if ($term =~ /^\s*$/) {
		next;
	}
	print HxKK "\t<Keyword Term=\"$term\">\n";
	print HxKK "\t\t<Jump Url=\"html\\$url\"/>\n";
	print HxKK "\t</Keyword>\n";
	print HxKF "\t<Keyword Term=\"$term\">\n";
	print HxKF "\t\t<Jump Url=\"html\\$url\"/>\n";
	print HxKF "\t</Keyword>\n";
}
}

print HxKK "</HelpIndex>";
print HxKF "</HelpIndex>";
close HxKK;
close HxKF;


#########################
# write table of contants

$fullName = $outPath . $outHxTFile;
open HxT, "> $fullName" or die "can not open file $fullName";

my $qtdcf = $sourcePath . "qt.dcf";
open QTDCF, "< $qtdcf" or die "can not open file $qtdcf";

print HxT "<?xml version=\"1.0\"?>\n";
print HxT "<!DOCTYPE HelpTOC SYSTEM \"ms-help://hx/resources/HelpTOC.DTD\">\n";
print HxT "<HelpTOC DTDVersion=\"1.0\">\n";

# write Qt Reference Documentation TOC
print HxT "\t<HelpTOCNode Title=\"Qt Reference Documentation\" Url=\"html\\index.html\">\n";
my $i = 0;
while (readline QTDCF) {
	my $line = $_;
	if ($line =~ /^<section\sref="(\S+)"\stitle="(.+)"/) {
		my $url = $1;
		my $t = $2;
		$t =~ s/"/&quot;/g;
		if ($i > 0) {
			print HxT "\t\t</HelpTOCNode>\n";
		}
		print HxT "\t\t<HelpTOCNode Title=\"$t\" Url=\"html\\$url\">\n";
		$i = $i + 1;
	}
	if ($line =~ /^\s{4}<section\sref="(\S+)"\stitle="(.+)"/) {
		my $url = $1;
		my $t = $2;
		$t =~ s/"/\&quot;/g;		
		print HxT "\t\t\t<HelpTOCNode Title=\"$t\" Url=\"html\\$url\"/>\n";		
	}	
}
if ($i > 0) {
	print HxT "\t\t</HelpTOCNode>\n";
}
print HxT "\t</HelpTOCNode>\n";
print HxT "\t<ToolData Name=\"MSTOCEXPST\" Value=\"Expanded\"/>\n";    
print HxT "</HelpTOC>";

close QTDCF;
close HxT;


######################
# write virtual topics

$fullName = $outPath . $outHxVFile;
open HxV, "> $fullName" or die "can not open file $fullName";

my $qtdcf = $sourcePath . "qt.dcf";
open QTDCF, "< $qtdcf" or die "can not open file $qtdcf";

print HxV "<?xml version=\"1.0\"?>\n";
print HxV "<!DOCTYPE VTopicSet SYSTEM \"ms-help://hx/resources/HelpVTopic.DTD\">\n";
print HxV "<VTopicSet DTDVersion=\"1.0\">\n";

my $classScope;
while (readline QTDCF) {
	my $line = $_;
	if ($line =~ /^\s{4}<keyword\sref="(\S+)">(.+)<\/keyword>/) {
		my $url = $1;
		my $term = $2;
		unless ($url =~ /#/) {
			$classScope = $term;
		}
		$term =~ s/"/&quot;/g;			
		my $rlTitle = $classScope . "::" . $term;
		print HxV "\t<VTopic Url=\"html/$url\" RLTitle=\"$rlTitle\">\n";
		print HxV "\t\t<Attr Name=\"Locale\" Value=\"kbEnglish\"/>\n";
		print HxV "\t\t<Attr Name=\"TopicType\" Value=\"kbSyntax\"/>\n";
		print HxV "\t\t<Attr Name=\"DevLang\" Value=\"C++\"/>\n";
		print HxV "\t\t<Attr Name=\"DocSet\" Value=\"$docSet\"/>\n";
		print HxV "\t\t<Attr Name=\"LinkGroup\" Value=\"$linkGroup\"/>\n";
		print HxV "\t\t<Keyword Index=\"K\" Term=\"$term\"/>\n";
		print HxV "\t\t<Keyword Index=\"F\" Term=\"$term\"/>\n";				
		print HxV "\t</VTopic>\n";		
	}	
}
print HxV "</VTopicSet>\n";
close QTDCF;
close HxV;


##################
# write link group

$fullName = $outPath . $groupDefFile;
open QTXML, "> $fullName" or die "can not open file $fullName";

print QTXML "<?xml version=\"1.0\"?>\n";
print QTXML "<DynamicHelp xmlns=\"http://microsoft.com/vstudio/tbd/vsdh.xsd\">\n";
print QTXML "\t<LINKGROUP ID=\"$linkGroup\" Title=\"$linkGroupTitle\" Priority=\"$linkGroupPriority\">\n";
print QTXML "\t\t<GLYPH Collapsed=\"1\" Expanded=\"2\"/>\n";
print QTXML "\t</LINKGROUP>\n";
print QTXML "\t<Context>\n";
print QTXML "\t\t<Keywords>\n";
print QTXML "\t\t\t<KItem Name=\"VS.TextEditor\"/>\n";
print QTXML "\t\t</Keywords>\n";
print QTXML "\t\t<Attributes>\n";
print QTXML "\t\t\t<AItem Name=\"DocSet\" Value=\"$docSet\"/>\n";
print QTXML "\t\t\t<AItem Name=\"DevLang\" Value=\"C++\"/>\n";
print QTXML "\t\t</Attributes>\n";
print QTXML "\t\t<Links>\n";
print QTXML "\t\t</Links>\n";
print QTXML "\t</Context>\n";
print QTXML "</DynamicHelp>";
close QTXML;


##############################
# write collection level files

# write collection
$fullName = $outCollectionPath . $outHxCColFile;
open HxCCOL, "> $fullName" or die "can not open file $fullName";
print HxCCOL "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n"; 
print HxCCOL "<!DOCTYPE HelpCollection>\n";
print HxCCOL "<HelpCollection DTDVersion=\"1.0\" FileVersion=\"$fileVersion\" LangId=\"1033\" Title=\"$title\" Copyright=\"Digia Plc\">\n";
print HxCCOL "\t<AttributeDef File=\"$outHxAColFile\"/>\n";
print HxCCOL "\t<TOCDef File=\"$outHxTColFile\"/>\n";   
print HxCCOL "\t<KeywordIndexDef File=\"$outHxKKColFile\"/>\n";
print HxCCOL "\t<KeywordIndexDef File=\"$outHxKFColFile\"/>\n";   
print HxCCOL "\t<ItemMoniker Name=\"!DefaultToc\" ProgId=\"HxDs.HxHierarchy\" InitData=\"\"/>\n";
print HxCCOL "\t<ItemMoniker Name=\"!DefaultFullTextSearch\" ProgId=\"HxDs.HxFullTextSearch\" InitData=\"\"/>\n";
print HxCCOL "\t<ItemMoniker Name=\"!DefaultKeywordIndex\" ProgId=\"HxDs.HxIndex\" InitData=\"K\"/>\n";
print HxCCOL "\t<ItemMoniker Name=\"!DefaultContextWindowIndex\" ProgId=\"HxDs.HxIndex\" InitData=\"F\"/>\n";
print HxCCOL "</HelpCollection>";
close HxCCOL;

# write toc
$fullName = $outCollectionPath . $outHxTColFile;
open HxTCOL, "> $fullName" or die "can not open file $fullName";
print HxTCOL "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n"; 
print HxTCOL "<!DOCTYPE HelpTOC>\n";
print HxTCOL "<HelpTOC DTDVersion=\"1.0\" PluginStyle=\"$pluginStyle\" PluginTitle=\"$pluginTitle\">\n";
print HxTCOL "\t<HelpTOCNode NodeType=\"TOC\" Url=\"$uniqueID\"/>\n";
print HxTCOL "</HelpTOC>";
close HxTCOL;

# write indices
$fullName = $outCollectionPath . $outHxKKColFile;
open HxKKCOL, "> $fullName" or die "can not open file $fullName";
print HxKKCOL "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n"; 
print HxKKCOL "<!DOCTYPE HelpIndex>\n";
print HxKKCOL "<HelpIndex DTDVersion=\"1.0\" Name=\"K\" Visible=\"No\" LangId=\"1033\">\n";
print HxKKCOL "</HelpIndex>";
close HxKKCOL;

$fullName = $outCollectionPath . $outHxKFColFile;
open HxKFCOL, "> $fullName" or die "can not open file $fullName";
print HxKFCOL "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n"; 
print HxKFCOL "<!DOCTYPE HelpIndex>\n";
print HxKFCOL "<HelpIndex DTDVersion=\"1.0\" Name=\"F\" Visible=\"No\" LangId=\"1033\">\n";
print HxKFCOL "</HelpIndex>";
close HxKFCOL;

# write attributes
$fullName = $outCollectionPath . $outHxAColFile;
open HxACOL, "> $fullName" or die "can not open file $fullName";
print HxACOL "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n"; 
print HxACOL "<!DOCTYPE HelpAttributes>\n";
print HxACOL "<HelpAttributes DTDVersion=\"1.0\">\n";
print HxACOL "\t<AttName Id=\"1\" Name=\"DocSet\" Display=\"Yes\" UIString=\"DocSet\" AttType=\"Enum\">\n";
print HxACOL "\t\t<AttVal Id=\"1_1\" Name=\"$docSet\" Display=\"Yes\" UIString=\"$docSetTitle\" />\n";
print HxACOL "\t</AttName>\n";
print HxACOL "\t<AttName Id=\"2\" Name=\"LinkGroup\" Display=\"No\" UIString=\"LinkGroup\" AttType=\"Enum\">\n";
print HxACOL "\t\t<AttVal Id=\"2_1\" Name=\"$linkGroup\" Display=\"No\" UIString=\"$linkGroupTitle\" />\n";
print HxACOL "\t</AttName>\n";
print HxACOL "\t<AttName Id=\"3\" Name=\"DevLang\" Display=\"Yes\" UIString=\"DevLang\" AttType=\"Enum\">\n";
print HxACOL "\t\t<AttVal Id=\"3_1\" Name=\"C++\" Display=\"Yes\" UIString=\"C++\"/>\n";
print HxACOL "\t</AttName>\n";
print HxACOL "\t<AttName Id=\"4\" Name=\"Locale\" Display=\"No\" UIString=\"Locale\" AttType=\"Enum\">\n";
print HxACOL "\t\t<AttVal Id=\"4_1\" Name=\"kbEnglish\" Display=\"No\" UIString=\"English\"/>\n";
print HxACOL "\t</AttName>\n";
print HxACOL "\t<AttName Id=\"5\" Name=\"TopicType\" Display=\"No\" UIString=\"TopicType\" AttType=\"Enum\">\n";
print HxACOL "\t\t<AttVal Id=\"5_1\" Name=\"kbSyntax\" Display=\"No\" UIString=\"Syntax\"/>\n";
print HxACOL "\t\t<AttVal Id=\"5_2\" Name=\"kbArticle\" Display=\"No\" UIString=\"Article\"/>\n";
print HxACOL "\t\t<AttVal Id=\"5_3\" Name=\"kbHowTo\" Display=\"No\" UIString=\"How To\"/>\n";
print HxACOL "\t</AttName>\n";	
print HxACOL "</HelpAttributes>";
close HxACOL;


##################
# write h2reg file

$fullName = $outCollectionPath . $h2regFile;
open H2REG, "> $fullName" or die "can not open file $fullName";

print H2REG ";------- Register -r switch\n\n";
print H2REG "[Reg_Namespace]\n";
print H2REG ";<nsName>|<nsColfile>|<nsDesc>\n";
print H2REG "$namespace|$outHxCColFile|Qt Reference Documentaton - qt.nokia.com\n";

print H2REG "[Reg_Title]\n";
print H2REG ";<nsName>|<TitleID>|<LangId>|<HxS_HelpFile>\n";
print H2REG "$namespace|$uniqueID|1033|$outHxSFile\n";

print H2REG "[Reg_Plugin]\n";
print H2REG ";<nsName_Parent>|<HxT_Parent>|<nsName_Child>|<HxT_Child>|<HxA_Child>\n";
print H2REG "MS.VSCC+|_DEFAULT|$namespace|_DEFAULT|$outHxAColFile\n";

print H2REG "[Reg_Filter]\n";
print H2REG ";<nsName>|<FilterName>|<FilterQueryStr>\n";
print H2REG "$namespace|$filterName|(\"docset\"=\"$docSet\")\n\n";

print H2REG ";------- UnRegister -u switch\n";

print H2REG "[UnReg_Namespace]\n";
print H2REG ";<nsName>\n";
print H2REG "$namespace\n";

print H2REG "[UnReg_Title]\n";
print H2REG ";<nsName>|<TitleID>|<LangId>\n";
print H2REG "$namespace|$uniqueID|1033\n";

print H2REG "[UnReg_Plugin]\n";
print H2REG ";<nsName_Parent>|<HxT_Parent>|<nsName_Child>|<HxT_Child>|<HxA_Child>\n";
print H2REG "MS.VSCC+|_DEFAULT|$namespace|_DEFAULT|$outHxAColFile\n";

print H2REG "[UnReg_Filter]\n";
print H2REG ";<nsName>|<FilterName>\n";
print H2REG "$namespace|$filterName";


############
# run hxcomp

if ($compile == 1) {
	$fullName = $outPath . $outHxCFile;
	my $fullOutHxSFile = $outPath . $outHxSFile;
	my $fullLogFile = $outPath . $logFile;
	@cmds = ("hxcomp", "-p", "$fullName", "-o", "$fullOutHxSFile", "-l", "$fullLogFile");
	system (@cmds) == 0
		or die "system @cmds failed: $?";
}

print "Done.\n";
