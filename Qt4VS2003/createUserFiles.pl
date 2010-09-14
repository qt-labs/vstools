#!/usr/bin/perl

use Cwd 'abs_path';

my $rootDir = abs_path($0);
$rootDir =~ s/createUserFiles\.pl//;

my $output = `devenv /? 2>&1`;
my $vsVersion = "";
if ($output =~ m/ersion ([\d\.]+)/) {
  $vsVersion = $1;
  $vsVersion =~ s/\.$//;  
}

my $refPath = "";

my $vsMajorVSVersion = 7;
if ($vsVersion =~ m/(\d)/) {
  $vsMajorVSVersion = $1;
}

my $sdkPath = $ENV{'VSINSTALLDIR'} . "\\Common7\\IDE";
unless (-e $sdkPath) {
  die "Cannot find " . $sdkPath . "\n";
}
$sdkPath =~ s/"//g;

if ($vsMajorVSVersion == 8) {
  my $vsPath = $ENV{'VS80COMNTOOLS'};
  $vsPath =~ s/Common7\\Tools/Common7\\IDE/;
  $sdkPath .= "\\VisualStudioIntegration\\Common\\Assemblies";
  $refPath = $vsPath . ";" . $vsPath . "\\PublicAssemblies;" . $sdkPath;
  print "Creating user files for VS 2005.\n";
  print "Assembly Reference Paths: $refPath\n";
  CreateUserFile("QtProjectEngine\\QtProjectEngineLib2005.csproj");
  CreateUserFile("QtProjectLib\\QtProjectLib2005.csproj");
  CreateUserFile("Qt4VSAddin\\Qt4VSAddin2005.csproj");
} elsif ($vsMajorVSVersion == 9) {
  my $vsPath = $ENV{'VS90COMNTOOLS'};
  $vsPath =~ s/Common7\\Tools/Common7\\IDE/;
  $sdkPath .= "\\VisualStudioIntegration\\Common\\Assemblies";
  $refPath = $vsPath . ";" . $vsPath . "\\PublicAssemblies;" . $sdkPath;
  print "Creating user files for VS 2008.\n";
  print "Assembly Reference Paths: $refPath\n";
  CreateUserFile("QtProjectEngine\\QtProjectEngineLib2008.csproj");
  CreateUserFile("QtProjectLib\\QtProjectLib2008.csproj");
  CreateUserFile("Qt4VSAddin\\Qt4VSAddin2008.csproj");
} elsif ($vsMajorVSVersion == 10) {
  my $vsPath = $ENV{'VS100COMNTOOLS'};
  $vsPath =~ s/Common7\\Tools/Common7\\IDE/;
  $sdkPath .= "\\VisualStudioIntegration\\Common\\Assemblies";
  $refPath = $vsPath . ";" . $vsPath . "\\PublicAssemblies;" . $sdkPath;
  print "Creating user files for VS 2010.\n";
  print "Assembly Reference Paths: $refPath\n";
  CreateUserFile("HelperTools\\Qt4VS2003Base\\Qt4VS2010Base.csproj");
  CreateUserFile("HelperTools\\RegistrationTool\\RegistrationTool2010.csproj");
  CreateUserFile("Qt4VS2003\\Qt4VS2010.csproj");
  CreateUserFile("QtProjectEngine\\QtProjectEngineLib2010.csproj");
  CreateUserFile("QtProjectLib\\QtProjectLib2010.csproj");
  CreateUserFile("Qt4VSAddin\\Qt4VSAddin2010.csproj");
}

sub CreateUserFile {
  my ($fName) = @_;
  my $fileName = $rootDir . $fName . ".user";
  if (-e $fileName) {
    unlink($fileName) or die $!;
  }
  
  my $devenv = $ENV{'DevEnvDir'};
  $devenv =~ s/"//g;
  $devenv .= "\\devenv.exe";

  my $args = "";
#  my $args = "/useenv /rootsuffix exp";
#  my $vsMajorVSVersion = 7;
#  if ($vsVersion =~ m/(\d)/) {
#    $vsMajorVSVersion = $1;
#  }
#  if ($vsMajorVSVersion == 9) {
#    $args .= " /ranu";
#  }
  
  open FILE, ">$fileName" or die $!;
  print FILE "<VisualStudioProject>\n";
  print FILE "  <CSHARP LastOpenVersion = \"$vsVersion\" >\n";
  print FILE "    <Build>\n";
  print FILE "      <Settings ReferencePath = \"$refPath\" >\n";  
  print FILE "        <Config\n";
  print FILE "          Name = \"Debug\"\n";
  print FILE "          StartAction = \"Program\"\n";
  print FILE "          StartProgram = \"$devenv\"\n";
  print FILE "          StartArguments = \"$args\"\n";
  print FILE "        />\n";  
  print FILE "      </Settings>\n";
  print FILE "    </Build>\n";
  print FILE "  </CSHARP>\n";
  print FILE "</VisualStudioProject>";
  close FILE;
}

