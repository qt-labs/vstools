# Squish tests for the Qt VS Tools

This directory contains tests for the Qt VS Tools to be run with Squish.

**WARNING:** Do not run these tests in an environment where you use the Qt VS Tools.
They will permanently delete the settings you saved. See [QTVSADDINBUG-1088](https://bugreports.qt.io/browse/QTVSADDINBUG-1088).

## Prerequisites

- Windows with English or German UI
- Visual Studio 2019 or Visual Studio 2022 (English language pack)
- Visual Studio SDK (Tools for building, testing, and deploying Visual Studio extensions)
- Squish for Windows

## To Run These Tests

### 1. Set Environment Variables

Make sure you add the following environment variables:

|Name                    |Description                                                                                                                                                              |
|:-----------------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
|`SQUISH_VSTOOLS_VERSION`| The expected version of the Qt VS Tools.                                                                                                                                |
|`SQUISH_VSTOOLS_QTDIRS` | A list of Qt installations to be used by the tests, i.e., the paths to the parent directories of the respective Qt versions' `bin` directories, separated by semicolons.|
|`SQUISH_VSTOOLS_WORKDIR`| A directory in which the tests may create projects.                                                                                                                     |

**Note:** The tests may remove contents from the `SQUISH_VSTOOLS_WORKDIR`.

### 2. Start Squish

Start Squish from the command line where you have set the environment variables mentioned above.

### 3. Open Test Suites

Open at least one of the test suites (`suite.conf`) in Squish for Windows.

### 4. Configure Test Suite Settings

- In "Test Suite Settings", select `devenv.exe` as AUT (Application Under Test).
- In "Global Scripts", add the `Tests\system\shared` directory using the `Folder icon` with the right-side down arrow.

### 5. Install Qt VS Tools

In Visual Studio, install the Qt VS Tools to be tested.

**Note:** The tools to test need to be installed into the `/RootSuffix SquishTestInstance`.
**Note:** If the installed extension does not work correctly, resetting the `SquishTestInstance` and installing the extension again may help.

#### Installing the Extension

To install the extension you can run the test `tst_1_install_from_marketplace` from `suite_installation`. This test will download the current version from the marketplace and start its installation.

Alternatively, a manual installation is also possible using the following command:

    vsixinstaller /RootSuffix:SquishTestInstance QtVsTools.vsix

#### Uninstalling the Extension

To uninstall the extension you can run the test `tst_8_uninstall` from `suite_installation`.

Alternatively, a manual uninstallation is also possible. To remove the extension installed into VS 2019, run the following command:

    vsixinstaller /RootSuffix:SquishTestInstance /uninstall:QtVsTools.bf3c71c0-ab41-4427-ada9-9b3813d89ff5

To remove the extension installed into VS 2022, run the following command:

    vsixinstaller /RootSuffix:SquishTestInstance /uninstall:QtVsTools.8e827d74-6fc4-40a6-a3aa-faf19652b3b8

#### Resetting the SquishTestInstance

To reset the 'SquishTestInstance' environment, you can run the test `tst_0_reset_testinstance` from `suite_installation`.

### 6. Run Tests

Run individual tests or the entire test suite. Be aware of:

- Depending on if you run a registered Visual Studio version or not, some controls might not be found, and individual tests might pop up some windows.
- Some tests take more time than expected, especially those that open the wizard due to the fact that VS might switch the app context.
- While tests are running, do not turn off the screen. If you do, Visual Studio might not create dialogs correctly, resulting in errors in the tests.

The tests will run in the experimental environment, which you get when starting `devenv.exe` with parameters `/RootSuffix SquishTestInstance`. Except for the preconditions listed above, each test is expected to set up what it needs and to clean up after itself. Should that fail for some reason, you can run `tst_0_reset_testinstance` from `suite_installation` to reset the environment. After doing so, you will have to install the Qt VS Tools again.
