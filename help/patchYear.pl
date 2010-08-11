#!perl

use File::Path;
use File::Copy;
use Getopt::Long;

&GetOptions('htmlPath:s', \$htmlPath,
    'replace-mshelp-links', \$replace_mshelp_links);

($second, $minute, $hour, $dayOfMonth, $month, $yearOffset, $dayOfWeek, $dayOfYear, $daylightSavings) = localtime();
$year = 1900 + $yearOffset;

opendir SOURCEDIR, $htmlPath or die "Cannot open directory $htmlPath!";
my @files = grep /(\.html$)/, readdir SOURCEDIR;
closedir SOURCEDIR;

foreach (@files) {
	my $file = $htmlPath . "\\" . $_;
	my $tmp = $file . "_";
	
	unless (open IN, "< $file") {
		print "Warning: Cannot open file $file - skipping it!\n";
		next;
	}
	
	unless (open OUT, "> $tmp") {
		next;
	}
	
	while (readline IN) {
		my $line = $_;
		$line =~ s/\$THISYEAR\$/$year/;
		$line =~ s/<title>Qt %VERSION%: /<title>/;

        if ($replace_mshelp_links) {
            $line =~ s/href="ms-help:\/\/.+"/href="http:\/\/doc.qt.nokia.com\/"/;
        }
        print OUT $line;
	}
	close OUT;
	close IN;
	
	copy($tmp, $file);
	unlink($tmp);
}
