#!/usr/bin/perl

use File::Copy;
use File::Path;
use Cwd 'abs_path';

my $srcRootPath = abs_path($0);
$srcRootPath =~ s/createWizardFiles\.pl//;

my $pathSuffix = "\\Trolltech AS\\IntegrationTest\\";
my $targetRootPath = $ENV{'ProgramFiles(x86)'} . $pathSuffix;
if ($targetRootPath eq "") {
  $targetRootPath = $ENV{'ProgramFiles'} . $pathSuffix;
}

if (! -d $targetRootPath) {
  if (!mkpath($targetRootPath, 0777)) {
    print "Cannot create root directory.\n";
    exit;
  }
}

my $targetPath = $targetRootPath . "uiItems\\";
my $srcPath = $srcRootPath . "Items\\uiItems\\";
my $file = "";

############################################################
# ui items
############################################################
if (! -d $targetPath) {
  mkdir($targetPath, 0777);
}
  
opendir(DIR, $srcPath) or die "Can't opendir $srcPath: $!";
while ( defined($file = readdir(DIR))) {
  if (!($file eq "." || $file eq "..")) {
    copyFile($srcPath . $file, $targetPath . $file);
  }
}
closedir(DIR);

############################################################
# qrc items
############################################################
$targetPath = $targetRootPath . "qrcItems\\";
$srcPath = $srcRootPath . "Items\\qrcItems\\";

if (! -d $targetPath) {
  mkdir($targetPath, 0777);
}
  
opendir(DIR, $srcPath) or die "Can't opendir $srcPath: $!";
while ( defined($file = readdir(DIR))) {
  if (!($file eq "." || $file eq "..")) {
    copyFile($srcPath . $file, $targetPath . $file);
  }
}
closedir(DIR);

############################################################
# resources
############################################################
$targetPath = $targetRootPath . "resources\\";
$srcPath = $srcRootPath . "ResourceItems\\";

if (! -d $targetPath) {
  mkdir($targetPath, 0777);
}
  
opendir(DIR, $srcPath) or die "Can't opendir $srcPath: $!";
while ( defined($file = readdir(DIR))) {
  if (!($file eq "." || $file eq "..")) {
    copyFile($srcPath . $file, $targetPath . $file);
  }
}
closedir(DIR);

############################################################
# wizards
############################################################
$targetPath = $targetRootPath . "wizards\\";
if (! -d $targetPath) {
  mkdir($targetPath, 0777);
}

$targetPath = $targetRootPath . "projects\\";
if (! -d $targetPath) {
  mkdir($targetPath, 0777);
}

createWizards("7.1");
createWizards("8.0");
createWizards("9.0");



sub createWizards {
  my($ver) = @_;
  createWizard($srcRootPath . "Items\\Qt4Class\\", $targetRootPath . "wizards\\$ver\\");
  createWizard($srcRootPath . "Items\\Qt4GuiClass\\", $targetRootPath . "wizards\\$ver\\");

  createWizard($srcRootPath . "projects\\Qt4ActiveQtServerProject\\", $targetRootPath . "projects\\$ver\\");
  createWizard($srcRootPath . "projects\\Qt4ConsoleProject\\", $targetRootPath . "projects\\$ver\\");
  createWizard($srcRootPath . "projects\\Qt4DesignerPluginProject\\", $targetRootPath . "projects\\$ver\\");
  createWizard($srcRootPath . "projects\\Qt4GuiProject\\", $targetRootPath . "projects\\$ver\\");
  createWizard($srcRootPath . "projects\\Qt4LibProject\\", $targetRootPath . "projects\\$ver\\");
  
  if ($ver != "7.1") {
    createWizard($srcRootPath . "projects\\Qt4WinCELibProject\\", $targetRootPath . "projects\\$ver\\");
    createWizard($srcRootPath . "projects\\Qt4WinCEProject\\", $targetRootPath . "projects\\$ver\\");
  }
}


sub createWizard {
  my($src, $dest) = @_;
  
  if (! -d $dest) {
    mkdir($dest, 0777);
  }

  my $className = "";
  if ($src =~ m/\\([^\\]+)\\$/) {
    $className = $1;
  } else {
    return;
  }
  
  my $version = "";
  if ($dest =~ m/\\([^\\]+)\\$/) {
    $version = $1;
  } else {
    return;
  }
  
  copy ($src . "$className.ico", $dest . "$className.ico");
  copy ($src . "$className.vsdir", $dest . "$className.vsdir");
  createVSZFile($src, $dest, $className, $version);
}


sub createVSZFile {
  my($inPath, $outPath, $className, $version) = @_;      
  my $fileName = $outPath . $className . ".vsz";
  
  $inPath =~ s/\\$//;
  
  if (-e $fileName) {
    unlink($fileName) or die $!;
  }
  open FILE, ">$fileName" or die $!;
  
  print FILE "VSWIZARD 7.1\n";
  print FILE "Wizard=VSWizard.VsWizardEngine.$version\n";
  print FILE "PARAM=\"WIZARD_NAME = $className\"\n";
  print FILE "PARAM=\"ABSOLUTE_PATH = $inPath\"\n";
  print FILE "PARAM=\"FALLBACK_LCID = 1033\"\n";
  close FILE;
}

sub copyFile {
  my($inFile, $outFile) = @_;

  print "$inFile\n";
  open IN, "<$inFile" or die $!;
  open OUT, ">$outFile" or die $!;
  
  while (<IN>) {
    print OUT "$_";    
  }
  close(IN);
  close(OUT);
}

