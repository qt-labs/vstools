<!--
Copyright (C) 2026 The Qt Company Ltd.
SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GFDL-1.3-no-invariants-only
-->

# Contributing to Qt Visual Studio Tools

Contributions to Qt VS Tools are welcome. This GitHub repository is a mirror of the repository
hosted on the Qt Project's Gerrit server. Changes are reviewed and merged there and synchronized
to GitHub afterwards.

## Reporting bugs

Open an issue on GitHub using the Qt VS Tools bug report form. Reports are triaged and moved to
the [Qt bug tracker](https://qt-project.atlassian.net/browse/QTVSADDINBUG), where they are tracked
and fixed. You can also report bugs and request features directly there.

For questions and general help, use the
[Qt Creator and other tools category](https://forum.qt.io/category/13/tools) on the Qt Forum.

## Contributing code

Pull requests on GitHub cannot be merged, because the mirror is overwritten on every
synchronization. Submit your change for review on
[Gerrit](https://codereview.qt-project.org/q/project:qt-labs/vstools) instead:

1. Read the [Qt Contribution Guidelines](https://wiki.qt.io/Qt_Contribution_Guidelines).
2. Sign the Qt Contribution License Agreement and set up your account as described in
   [Setting up Gerrit](https://wiki.qt.io/Setting_up_Gerrit).
3. Clone the repository from `https://codereview.qt-project.org/qt-labs/vstools` with
   `--recurse-submodules`, commit your change on top of `dev`, and push it for review to
   `refs/for/dev`.

See the [Qt VS Tools README](../QtVsTools/README.md) for build instructions.
