#!/usr/bin/perl

use File::Copy;
use File::Path;
use Getopt::Long;
use Cwd 'abs_path';

my $destRootBase = "Y:";
my $destRootPath = $destRootBase . "\\addin7x";

my %args = ();
usage() if (!GetOptions("qt" => \$args{qt},
             "addin" => \$args{addin},
             "onlinehelp" => \$args{onlinehelp},
             "templates" => \$args{templates},
             "dest:s"=>\$args{destination},
             "help"=>\$args{help}
           ));

if ($args{help}) {
  usage();  
}

my $copyQt = $args{qt};
my $copyAddin = $args{addin};
my $copyHelp = $args{onlinehelp};
my $copyTemplates = $args{templates};

if ($copyQt eq "" && $copyAddin eq ""
    && $copyHelp eq "" && $copyTemplates eq "") {
  $copyQt = "1";
  $copyAddin = "1";
  $copyHelp = "1";
  $copyTemplates = "1";
}

if ($args{destination} ne "") {
  $destRootPath = $args{destination};
}

############################################################
# retrieve path and version information
############################################################

my $srcRootPath = abs_path($0);
$srcRootPath =~ s/collectInstallerFiles\.pl//;
$srcRootPath =~ s|^/(\w)/|$1\:/|;      # convert drive letter
$srcRootPath =~ s|/|\\|g;              # convert to backslashes
$srcRootPath =~ s|\\$||g;              # remove trailing \

my $qtDir = $ENV{'QTDIR'};
if ($copyQt eq "1" && $qtDir eq "") {
    die "QTDIR is not set!\n";
}
my $outPath = "";

die "Environment variable %VSINSTALLDIR% does not exist.\n" if (!$ENV{VSINSTALLDIR});

#############################################################################
# Retrieve the VS version by comparing %VSINSTALLDIR% with the common tools #
# directories. E.g. we have a VS 2005 shell, if and only if %VS80COMNTOOLS% #
# starts with the value of %VSINSTALLDIR%.                                  #
#############################################################################
my $vsVersion = '';
my $vsVersionLong = '';
if ($ENV{VS80COMNTOOLS} =~ /^\Q$ENV{VSINSTALLDIR}/) {
    $vsVersion = '8.0';
    $vsVersionLong = "2005";
} elsif ($ENV{VS90COMNTOOLS} =~ /^\Q$ENV{VSINSTALLDIR}/) {
    $vsVersion = '9.0';
    $vsVersionLong = "2008";
} elsif ($ENV{VS100COMNTOOLS} =~ /^\Q$ENV{VSINSTALLDIR}/) {
    $vsVersion = '10.0';
    $vsVersionLong = "2010";
}

if (!$vsVersion) {
    die "Cannot determine VS version. Please start this tool within a VS command shell.\n";
}

my $vsipVersion = "1.0.0";
my $vsipVersionMajor = 1;
my $vsipVersionMinor = 0;
my $vsipVersionPatch = 0;
opendir(DIR, $srcRootPath . "\\Qt4VSAddin") or die "Cannot open directory $srcDir: $!";
while (defined(my $file = readdir(DIR))) {
    if ($file =~ m/Changes-(([0-9]|\.)+)/) {
        my $v = $1;
        my @version = split('\\.', $v);
        if ( $version[0] > $vsipVersionMajor ||
            ($version[0] == $vsipVersionMajor && $version[1] > $vsipVersionMinor) ||
            ($version[0] == $vsipVersionMajor && $version[1] == $vsipVersionMinor && $version[2] > $vsipVersionPatch))
        {
            $vsipVersionMajor = $version[0];
            $vsipVersionMinor = $version[1];
            $vsipVersionPatch = $version[2];
            $vsipVersion = $v;
        }
    }
}
closedir(DIR);
print "Add-in Version (detected from Changes files): $vsipVersion\n";

my $h2regPath = $ENV{'ProgramFiles'} . "\\Helpware\\H2Reg";

if (! -d $destRootPath) {
  mkdir($destRootPath, 0777) or die "Cannot create VSIP directory: $destRootPath!\n";
}

############################################################
# copy info files
############################################################

$outPath = $destRootPath;

copyFile($srcRootPath . "\\ui.ico", $outPath);
copyFile($srcRootPath . "\\Qt4VSAddin\\Changes-" . $vsipVersion, $outPath);

############################################################
# copy Qt libs
############################################################

$outPath = $destRootPath . "\\bin";
mkdir $outPath;

if ($copyQt eq "1") {
  copyFile($qtDir . "\\bin\\QtCore4.dll", $outPath);
  copyFile($qtDir . "\\bin\\QtGui4.dll", $outPath);
  copyFile($qtDir . "\\bin\\QtXml4.dll", $outPath);
  copyFile($qtDir . "\\bin\\QtSvg4.dll", $outPath);
  $outPath = $destRootPath . "\\bin\\imageformats";
  mkdir $outPath;
  copyFile($qtDir . "\\plugins\\imageformats\\qgif4.dll", $outPath);
  copyFile($qtDir . "\\plugins\\imageformats\\qico4.dll", $outPath);
  copyFile($qtDir . "\\plugins\\imageformats\\qjpeg4.dll", $outPath);
  copyFile($qtDir . "\\plugins\\imageformats\\qmng4.dll", $outPath);
  copyFile($qtDir . "\\plugins\\imageformats\\qsvg4.dll", $outPath);
  copyFile($qtDir . "\\plugins\\imageformats\\qtiff4.dll", $outPath);
}

############################################################
# copy addin binaries
############################################################

if ($copyAddin eq "1") {
  $outPath = $destRootPath . "\\bin\\$vsVersion";
  mkpath($outPath);

  copyFile($srcRootPath . "\\Qt4VSAddin\\Release\\Qt4VSAddin.dll", $outPath);
  copyFile($srcRootPath . "\\Qt4VSAddin\\Release\\QtProjectLib.dll", $outPath);
  copyFile($srcRootPath . "\\Qt4VSAddin\\Release\\QtProjectEngineLib.dll", $outPath);
  copyFile($srcRootPath . "\\Qt4VSAddin\\Qt4VSAddin.AddIn", $outPath);
  copyFile($srcRootPath . "\\ComWrappers\\qmakewrapper\\qmakewrapper1Lib.dll", $outPath);

  ############################################################
  # Patch .AddIn file
  ############################################################
  open(FILE, "<" . $outPath . "\\Qt4VSAddin.AddIn");
  while (<FILE>) {
     $_ =~ s/<Version>.*<\/Version>/<Version>$vsVersion<\/Version>/;
     $_ =~ s/Qt Add-in Development Version/Qt Add-in $vsipVersion/;
     $file .= $_;
     }
  close FILE;
  
  open(FILE, ">" . $outPath . "\\Qt4VSAddin.AddIn");
  print FILE ("$file");
  close FILE;

  copySubDir($srcRootPath . "\\Qt4VSAddin\\Release", $outPath, "de");
  copySubDir($srcRootPath . "\\Qt4VSAddin\\Release", $outPath, "zh-cn");

  if ($vsVersionLong eq "2005") {
    copySubDir($srcRootPath . "\\Qt4VSAddin\\Release", $outPath, "en");
    copySubDir($srcRootPath . "\\Qt4VSAddin\\Release", $outPath, "es");
    copySubDir($srcRootPath . "\\Qt4VSAddin\\Release", $outPath, "fr");
    copySubDir($srcRootPath . "\\Qt4VSAddin\\Release", $outPath, "it");
    copySubDir($srcRootPath . "\\Qt4VSAddin\\Release", $outPath, "ja");
    copySubDir($srcRootPath . "\\Qt4VSAddin\\Release", $outPath, "ko");
    copySubDir($srcRootPath . "\\Qt4VSAddin\\Release", $outPath, "ru");
    copySubDir($srcRootPath . "\\Qt4VSAddin\\Release", $outPath, "zh-Hans");
    copySubDir($srcRootPath . "\\Qt4VSAddin\\Release", $outPath, "zh-Hant");
  }

  if ($vsVersionLong eq "2008") {
    $outPath = $destRootPath . "\\bin";
    mkpath($outPath);
    copyFile($srcRootPath . "\\Qt4VSAddin\\Release\\qtappwrapper.exe", $outPath);
    copyFile($srcRootPath . "\\Qt4VSAddin\\Release\\qrceditor.exe", $outPath);
    copyFile($srcRootPath . "\\ComWrappers\\qmakewrapper\\release\\qmakewrapper1.dll", $outPath);
  }

  $outPath = $destRootBase;
  copyFile($srcRootPath . "\\..\\LICENSE.LGPL", $outPath);

  # check for Visual C++ redistributable package
  $outPath = $destRootPath . "\\redist";
  mkdir $outPath;
  if (! -e "$outPath\\vcredist_x86.exe") {
    die "Cannot find $outPath\\vcredist_x86.exe\n";
  }

  # copy debug extensions
  $outPath = $destRootBase . "\\debugext";
  copyFile($srcRootPath . "\\..\\tools\\Qt4EEAddin\\autoexp.dat_entries.txt", $outPath);
  copyFile($srcRootPath . "\\..\\tools\\Qt4EEAddin\\autoexp.dat-autoexpand2005", $outPath);
  copyFile($srcRootPath . "\\..\\tools\\Qt4EEAddin\\autoexp.dat-autoexpand2008", $outPath);
  copyFile($srcRootPath . "\\..\\tools\\Qt4EEAddin\\autoexp.dat-visualizer2005", $outPath);
  copyFile($srcRootPath . "\\..\\tools\\Qt4EEAddin\\autoexp.dat-visualizer2008", $outPath);
}

############################################################
# copy plugins
############################################################

if ($copyQt eq "1") {
  $outPath = $destRootPath . "\\plugins";
  mkdir $outPath;
  
#  $outPath = $destRootPath . "\\plugins\\designer";
#  mkdir $outPath;  

#  copyFile($qtDir . "\\plugins\\designer\\customwidgetplugin.dll", $outPath);
#  copyFile($qtDir . "\\plugins\\designer\\worldtimeclockplugin.dll", $outPath);
#  copyFile($qtDir . "\\plugins\\designer\\qt3supportwidgets.dll", $outPath);
#  copyFile($qtDir . "\\plugins\\designer\\qaxwidget.dll", $outPath);
#  copyFile($qtDir . "\\plugins\\designer\\containerextension.dll", $outPath);
#  copyFile($qtDir . "\\plugins\\designer\\taskmenuextension.dll", $outPath);
#  copyFile($qtDir . "\\plugins\\designer\\qwebview.dll", $outPath);

#  copyFile($qtDir . "\\bin\\QtNetwork4.dll", $outPath);
#  copyFile($qtDir . "\\bin\\QtWebKit4.dll", $outPath);
#  copyFile($qtDir . "\\bin\\phonon4.dll", $outPath);
#  copyFile($qtDir . "\\bin\\QtSql4.dll", $outPath);
#  copyFile($qtDir . "\\bin\\Qt3Support4.dll", $outPath);
  
  $outPath = $destRootPath . "\\plugins\\imageformats";
  mkdir $outPath;
  copyFile($qtDir . "\\bin\\QtSvg4.dll", $outPath);
  copyFile($qtDir . "\\plugins\\imageformats\\qsvg4.dll", $outPath);
  copyFile($qtDir . "\\plugins\\imageformats\\qgif4.dll", $outPath);
  copyFile($qtDir . "\\plugins\\imageformats\\qjpeg4.dll", $outPath);
  copyFile($qtDir . "\\plugins\\imageformats\\qtiff4.dll", $outPath);
  copyFile($qtDir . "\\plugins\\imageformats\\qico4.dll", $outPath);
}

############################################################
# copy templates
############################################################

if ($copyTemplates eq "1") {
  $outPath = $destRootPath . "\\projects";
  mkdir $outPath;
  
  copyTemplate("projects", "Qt4ActiveQtServerProject");
  copyTemplate("projects", "Qt4ConsoleProject");
  copyTemplate("projects", "Qt4DesignerPluginProject");
  copyTemplate("projects", "Qt4GuiProject");
  copyTemplate("projects", "Qt4LibProject");
  copyTemplate("projects", "Qt4WinCELibProject");
  copyTemplate("projects", "Qt4WinCEProject");

  $outPath = $destRootPath . "\\items";
  mkdir $outPath;

  copyTemplate("items", "Qt4Class");
  copyTemplate("items", "Qt4GuiClass");

  mkdir $outPath . "\\qrcItems";
  copyDir($srcRootPath . "\\Items\\qrcItems\\*.ico", $outPath . "\\qrcItems");
  copyDir($srcRootPath . "\\Items\\qrcItems\\*.qrc", $outPath . "\\qrcItems");
  copyDir($srcRootPath . "\\Items\\qrcItems\\*.vsdir", $outPath . "\\qrcItems");

  mkdir $outPath . "\\uiItems";
  copyDir($srcRootPath . "\\Items\\uiItems\\*.ico", $outPath . "\\uiItems");
  copyDir($srcRootPath . "\\Items\\uiItems\\*.qrc", $outPath . "\\uiItems");
  copyDir($srcRootPath . "\\Items\\uiItems\\*.vsdir", $outPath . "\\uiItems");
  copyDir($srcRootPath . "\\Items\\uiItems\\*.ui", $outPath . "\\uiItems");

  $outPath = $destRootPath . "\\resources";
  mkdir $outPath;

  copyDir($srcRootPath . "\\ResourceItems\\*", $outPath);
}

############################################################
# copy h2reg
############################################################

if ($copyHelp eq "1") {
  $outPath = $destRootPath . "\\help";
  mkdir $outPath;

  if (-e $h2regPath) {
    copyFile($h2regPath . "\\h2reg.exe", $outPath);
    copyFile($h2regPath . "\\h2reg.ini", $outPath);
  } else {
    print "Could not copy h2reg.\n";
  }
}

############################################################
# copy addin help
############################################################

if ($copyAddin eq "1") {
  $outPath = $destRootPath . "\\help";
  mkdir $outPath;

  if (-e $srcRootPath . "\\..\\help\\test") {
    copyDir($srcRootPath . "\\..\\help\\test\\qt4vs_*", $outPath);
    copyDir($srcRootPath . "\\Doc\\html\\*.xml", $outPath);
  } else {
    print "Could not copy vsip documentation.\n";
  }
}

############################################################
# copy qt help
############################################################

if ($copyHelp eq "1") {
  $outPath = $destRootPath . "\\help";
  mkdir $outPath;

  if (-e $srcRootPath . "\\..\\help\\test") {
    copyDir($srcRootPath . "\\..\\help\\test\\qt_*", $outPath);
  } else {
    print "Could not copy Qt documentation.\n";
  }
}

############################################################
# helper functions
############################################################

sub copyTemplate {
  my ($relPath, $name) = @_;
  
  my $projectPath = $outPath . "//" . $name;
  mkdir $projectPath;
  
  mkdir $projectPath . "//1033";
  copyFile($srcRootPath . "\\" . $relPath . "\\" . $name . "\\1033\\styles.css", $projectPath . "\\1033");
  mkdir $projectPath . "\\HTML";
  mkdir $projectPath . "\\HTML\\1033";
  copyDir($srcRootPath . "\\" . $relPath . "\\" . $name . "\\HTML\\1033\\*.htm", $projectPath . "\\HTML\\1033");  
  mkdir $projectPath . "\\HTML\\1031";
  copyDir($srcRootPath . "\\" . $relPath . "\\" . $name . "\\HTML\\1031\\*.htm", $projectPath . "\\HTML\\1031");  
  mkdir $projectPath . "\\Images";
  copyDir($srcRootPath . "\\" . $relPath . "\\" . $name . "\\Images\\*", $projectPath . "\\Images");
  mkdir $projectPath . "\\Scripts";
  mkdir $projectPath . "\\Scripts\\1033";
  copyDir($srcRootPath . "\\" . $relPath . "\\" . $name . "\\Scripts\\1033\\*.js", $projectPath . "\\Scripts\\1033");  
  mkdir $projectPath . "\\Templates";
  mkdir $projectPath . "\\Templates\\1033";
  copyDir($srcRootPath . "\\" . $relPath . "\\" . $name . "\\Templates\\1033\\*", $projectPath . "\\Templates\\1033");  
  copyDir($srcRootPath . "\\" . $relPath . "\\" . $name . "\\*.ico", $projectPath);
  copyDir($srcRootPath . "\\" . $relPath . "\\" . $name . "\\*.vsdir", $projectPath);
}

sub copyDir {
  my ($src, $destDir) = @_;
  my($srcDir, $pattern) = $src =~ m/(.*\\)(.*)$/;
  $pattern =~ s/\./\\./g;
  $pattern =~ s/\*/.*/g;
  opendir(DIR, $srcDir) or die "Cannot open directory $srcDir: $!";
  while ( defined(my $file = readdir(DIR))) {
    if (!($file eq "." || $file eq "..")) {
      if ($file =~ m/$pattern/) {
        copyFile($srcDir . "\\" . $file, $destDir);
      }      
    }
  }
closedir(DIR);
}

sub copySubDir {
  my ($srcDir, $destDir, $subDir) = @_;
  mkdir $destDir . "\\" . $subDir;
  copyDir($srcDir . "\\" . $subDir . "\\*", $destDir . "\\" . $subDir);
}

sub copyFile {
  my ($srcFile, $destPath) = @_;
  $srcFile =~ s/\//\\/g;
  $srcFile =~ s/\\+/\\/g;
  $destPath =~ s/\//\\/g;
  $destPath =~ s/\\+/\\/g;
  my($destFile) = $srcFile =~ m/.*\\(.*)$/;
  $destFile = $destPath . "\\" . $destFile;
  if (-e $destFile) {
    unlink($destFile) or die "Cannot overwrite file $destFile!\n";
  }
  copy($srcFile, $destFile) or die "$srcFile cannot be copied to $destFile!\n";
  chmod(0755, $destFile);
}

sub usage {
  print "Usage of collectInstallerFiles:\n\n";
  print "--dest PATH   Copies the files to the\n";
  print "              specified PATH.\n";  
  print "--help        Prints this help message.\n\n";
  print "Component Options:\n\n";
  print "--qt          Copies the Qt libraries.\n";  
  print "--integration Copies the integration related\n";
  print "              files.\n";
  print "--onlinehelp  Copies the online help.\n";
  print "--templates   Copies the template files.\n";  
  print "If no component option is specified, all files\n";
  print "will be copied.\n\n";  
  exit;
}
