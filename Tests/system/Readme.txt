This directory contains tests for The Qt VS Tools to be run with Squish.

To run these tests:
1. Run Windows with English or German UI.
2. Have Visual Studio with English language pack and Qt VS Tools installed.
3. Have Visual Studio's tool vswhere.exe in PATH.
4. Open at least one of the test suites (suite.conf) in Squish for Windows.
5. In "Test Suite Settings", select devenv.exe as AUT.
6. Run individual tests or the entire test suite.

Please note: The tests will run in your normal working environment. Settings you made may influence
             the tests and vice versa.

Some tests require the following environment variables to be set to the correct values:
SQUISH_VSTOOLS_VERSION: The expected version of Qt VS Tools
