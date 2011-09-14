#!/usr/bin/perl
use strict;
use warnings;
use Cwd;
use File::Basename;
use File::Path;
use Getopt::Long;

my $qhprepairPath = "qhprepair/release/qhprepair.exe";

sub showUsage
{
    print "usage: createdoc {--qt | --vs | --vsonline}\n";
}

sub getQtSourceDir
{
    my $qtBuildDir = $_[0];
    if (-f "$qtBuildDir/configure.exe") {
        return $qtBuildDir;
    }

    my $filename = "$qtBuildDir/.qmake.cache";
    open(FILE, $filename) or die("Could not open $filename\n");
    foreach my $line (<FILE>) {
        if ($line =~ /^QT_SOURCE_TREE/) {
            $line =~ s/.*=\s*//;
            $line =~ s/\\\\/\\/g;
            $line =~ s/\n//;
            close(FILE);
            return $line;
        }
    }

    close(FILE);
    die "Could not find QT_SOURCE_TREE in $filename\n";
}

sub getAddinVersion
{
    my $addinVersion = "1.0.0";
    my $addinVersionMajor = 1;
    my $addinVersionMinor = 0;
    my $addinVersionPatch = 0;
    my $srcdir = $_[0] . "\\Qt4VS2003\\Qt4VSAddin";
    opendir(DIR, $srcdir) or die "Cannot open directory $srcdir: $!";
    while (defined(my $file = readdir(DIR))) {
        if ($file =~ m/Changes-(([0-9]|\.)+)/) {
            my $v = $1;
            my @version = split('\\.', $v);
            if ( $version[0] > $addinVersionMajor ||
                ($version[0] == $addinVersionMajor && $version[1] > $addinVersionMinor) ||
                ($version[0] == $addinVersionMajor && $version[1] == $addinVersionMinor && $version[2] > $addinVersionPatch))
            {
                $addinVersionMajor = $version[0];
                $addinVersionMinor = $version[1];
                $addinVersionPatch = $version[2];
                $addinVersion = $v;
            }
        }
    }
    closedir(DIR);
    return $addinVersion;
}

#--main--

if (!@ARGV) {
    showUsage();
    exit;
}

my %options = ();
if (!GetOptions("qt" => \$options{"qt"},
                "vs" => \$options{"vs"},
                "vsonline" => \$options{"vsonline"}))
{
    showUsage();
    exit;
}

my $qtDir = $ENV{QTDIR};
if (!$qtDir) {
    die "%QTDIR% is not set.\n";
}

if (! -f "$qtDir/bin/qmake.exe") {
    die "%QTDIR% doesn't point to a valid Qt installation.\n";
}
$ENV{PATH} = "$qtDir\\bin;" . $ENV{PATH};

my $qtvsDir = dirname(dirname(__FILE__));
my $qtVersion = `"$qtDir\\bin\\qmake.exe" -query QT_VERSION`;
$qtVersion =~ s/\n//;
if (!$qtVersion) {
    die "Can't determine the Qt version.\n";
}

my $vsHelpBuilder = $qtvsDir . "\\help\\VsHelpGeneratorQhp\\release\\vshelpbuilder.exe";
if (! -f $vsHelpBuilder) {
    die "Can't find $vsHelpBuilder\n";
}

if ($options{qt}) {
    if (! -f $qhprepairPath) {
        die "The tool $qhprepairPath does not exist\n";
    }
    my $qhpFile = "$qtDir/doc-build/html-qt/qt.qhp";
    if (! -f $qhpFile) {
        die "File $qhpFile does not exit. Did you build the Qt docs?\n";
    }

    system("$qhprepairPath", $qhpFile);
    die "qhprepair failed\n" if ($?);
    rmtree("$qtvsDir/help/test");
    mkdir "$qtvsDir/help/test";
    print "Running the VsHelpGenerator on Qt...\n";
    system($vsHelpBuilder, "/in:$qtDir\\doc-build\\html-qt", "/out:$qtvsDir\\help\\test", "/title:Qt Reference Documentation ($qtVersion)", "/version:$qtVersion");
    die "vshelpbuilder failed\n" if ($?);
} elsif ($options{vs} || $options{vsoline}) {
    print "Creating VS Add-in HTML documentation...\n";
    my $qdoc3 = "$qtDir\\bin\\qdoc3.exe";
    if (! -f $qdoc3) {
        die "Cannot find $qdoc3\n";
    }

    my $addinVersion = getAddinVersion($qtvsDir);
    my $qtSourceDir = getQtSourceDir($qtDir);

    print "add-in version:   $addinVersion.\n";
    print "Qt source dir:    $qtSourceDir.\n";

    my $oldCWD = getcwd();
    chdir "$qtvsDir\\Qt4VS2003\\Doc";

    open(FILE, ">stupid_include_hack.qdocconf") or die "Can't open stupid_include_hack.qdocconf for writing.\n";
    print FILE "include($qtSourceDir\\tools\\qdoc3\\test\\qt-project.qdocconf)\n";
    close(FILE);

    $ENV{QT_BUILD_TREE} = $qtDir;
    $ENV{QT_SOURCE_TREE} = $qtSourceDir;
    $ENV{QTVSDIR} = $qtvsDir;
    $ENV{SRCDIR} = "$qtvsDir\\Qt4VS2003\\Doc";
    system($qdoc3, "vs-addin.qdocconf");
    die "qdoc3 failed\n" if ($?);
    print "Add-in documentation created.\n";

    if ($options{vsoline}) {
        system("zip -r9 vsonlinedocs.zip $qtvsDir\\Qt4VS2003\\Doc\\html\\*");
        die "zip failed\n" if ($?);
    } else {
        print "Running the VsHelpGenerator on the Add-in documentation...\n";
        system($vsHelpBuilder, "/in:$qtvsDir\\Qt4VS2003\\Doc\\html", "/out:$qtvsDir\\help\\test", "/title:Qt Add-in for Visual Studio", "/version:$addinVersion", "/type:VS");
        die "vshelpbuilder failed\n" if ($?);
    }

    chdir $oldCWD;
}

