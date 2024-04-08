This directory contains tests for The Qt VS Tools to be run with Squish.

WARNING: Do not run these tests in an environment where you use the Qt VS Tools.
         They will permanently delete the settings you saved, see QTVSADDINBUG-1088

To run these tests:
1. Run Windows with English or German UI.
2. Have Visual Studio with English language pack and the Visual Studio SDK installed.
3. Open at least one of the test suites (suite.conf) in Squish for Windows.
4. In "Test Suite Settings", select devenv.exe as AUT.
5. In Visual Studio, install the Qt VS Tools to be tested. You can do this manually or run the test
   tst_1_install_from_marketplace from suite_installation. It will download the current version
   from the marketplace and start its installation.
6. Run individual tests or the entire test suite.

The tests will run in the experimental environment which you get when starting devenv.exe with
parameters "/RootSuffix SquishTestInstance". Except for the preconditions listed above, each test
is expected to set up what it needs and to clean up after itself. Should that fail for some reason,
you can run tst_0_reset_testinstance from suite_installation to reset the environment. After doing
so, you will have to install Qt VS Tools again.

Some tests require the following environment variables to be set to the correct values:
SQUISH_VSTOOLS_VERSION: The expected version of Qt VS Tools
SQUISH_VSTOOLS_QTDIRS:  A list of Qt installations to be used by the tests, i.e. the paths to the
                        parent directories of the respective Qt versions' bin-directories,
                        separated by semicola.
SQUISH_VSTOOLS_WORKDIR: A directory in which the tests may create projects.
                        Please note: The tests may remove contents from this directory.
