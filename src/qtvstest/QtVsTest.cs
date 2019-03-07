/****************************************************************************
**
** Copyright (C) 2019 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Tools.
**
** $QT_BEGIN_LICENSE:GPL-EXCEPT$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see https://www.qt.io/terms-conditions. For further
** information use the contact form at https://www.qt.io/contact-us.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3 as published by the Free Software
** Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
** included in the packaging of this file. Please review the following
** information to ensure the GNU General Public License requirements will
** be met: https://www.gnu.org/licenses/gpl-3.0.html.
**
** $QT_END_LICENSE$
**
****************************************************************************/

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace QtVsTest
{
    using Macros;

    [Guid(PackageGuidString)]
    [InstalledProductRegistration(
        productName: "Qt Visual Studio Test",
        productDetails: "Auto-test framework for Qt Visual Studio Tools.",
        productId:"1.0",
        IconResourceID = 400)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]

    public sealed class QtVsTest : AsyncPackage
    {
        public const string PackageGuidString = "0e258dce-fc8a-49a2-81c5-c9e138bfe500";
        MacroServer MacroServer { get; set; }

        public QtVsTest()
        {
            MacroServer = new MacroServer(this, JoinableTaskFactory);
        }

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            // Start macro server loop as background task
            await Task.Run(() => MacroServer.LoopAsync().Forget());
        }

        protected override int QueryClose(out bool canClose)
        {
            // Shutdown macro server when closing Visual Studio
            MacroServer.Loop.Cancel();

            return base.QueryClose(out canClose);
        }
    }
}
