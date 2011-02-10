if ($#ARGV != 1) {
  die "\nUsage: createXML.pl <QT-Version> <Destination Path>\n"
}
else
{
  my $Ver = $ARGV[0];
  my $VerNoDot = $Ver;
  $VerNoDot =~ s/\.//g;
  my $VerUnderscore = $Ver;
  $VerUnderscore =~ s/\./_/g;
  my $Path = $ARGV[1];
  $Path =~ s/\\/\//g;
  open(FILE ,">$Path/qt_$VerUnderscore" . "Filter.xml") || die "Destination File could not be opened for writing";
  print FILE "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n";
  print FILE "<SearchFilter xmlns=\"http://schemas.microsoft.com/VisualStudio/2004/08/Help/SearchFilter\" Version=\"0.1.0.0\">\n";
  print FILE "    <FilterAttribute>\n";
  print FILE "      <Id>Technology</Id>\n";
  print FILE "      <Name _locID=\"name.1\">Technology</Name>\n";
  print FILE "      <FilterValue>\n";
  print FILE "          <Id>qt$VerNoDot</Id>\n";
  print FILE "          <Name _locID=\"name.2\">Qt $ARGV[0]</Name>\n";
  print FILE "          <Meaning>\n";
  print FILE "             <LocalFilterString>(\"DocSet\"=\"qtrefdoc$VerUnderscore\")</LocalFilterString>\n";
  print FILE "             <TocInclude></TocInclude>\n";
  print FILE "             <OnlineFilterString>\n";
  print FILE "               <![CDATA[\n";
  print FILE "                 <StringTest Name=\"ExtendedProperty\" Operator=\"Equals\" Value=\"ms0TCRXH\" ExtendedProperty=\"MSCategory\"/>\n";
  print FILE "               ]]>\n";
  print FILE "             </OnlineFilterString>\n";
  print FILE "          </Meaning>\n";
  print FILE "      </FilterValue>\n";
  print FILE "   </FilterAttribute>\n";
  print FILE "</SearchFilter>\n";
  close(FILE);
}
