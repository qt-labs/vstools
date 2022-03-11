/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.VisualStudio
{
    public class SearchTask : VsSearchTask
    {
        private readonly Action _clearCallback = null;
        private readonly Func<IEnumerable<string>, uint> _searchCallBack = null;

        public SearchTask(uint cookie, IVsSearchQuery query, IVsSearchCallback callback,
                Action clearCallback, Func<IEnumerable<string>, uint> searchCallBack)
            : base(cookie, query, callback)
        {
            _clearCallback = clearCallback;
            _searchCallBack = searchCallBack;
        }

        protected override void OnStartSearch()
        {
            ErrorCode = VSConstants.S_OK;
            try {
                _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    if (TaskStatus != VSConstants.VsSearchTaskStatus.Stopped) {
                        SearchResults = _searchCallBack(
                            SearchUtilities.ExtractSearchTokens(SearchQuery).Select(token =>
                            {
                                ThreadHelper.ThrowIfNotOnUIThread();
                                return token.ParsedTokenText;
                            })
                        );
                    } else if (TaskStatus == VSConstants.VsSearchTaskStatus.Stopped) {
                        _clearCallback();
                    }
                });
            } catch {
                ErrorCode = VSConstants.E_FAIL;
            }
            base.OnStartSearch();
        }

        protected override void OnStopSearch()
        {
            SearchResults = 0;
        }
    }

    public abstract class VsWindowSearch : IVsWindowSearch
    {
        public abstract IVsSearchTask CreateSearch(uint cookie, IVsSearchQuery query,
                                                   IVsSearchCallback callback);

        public virtual void ClearSearch()
        {}

        public virtual void ProvideSearchSettings(IVsUIDataSource settings)
        {
            Utilities.SetValue(settings, SearchSettingsDataSource.PropertyNames.ControlMaxWidth,
                (uint)10000);
            Utilities.SetValue(settings, SearchSettingsDataSource.PropertyNames.SearchStartType,
                (uint)VSSEARCHSTARTTYPE.SST_INSTANT);
        }

        public virtual bool OnNavigationKeyDown(uint dwNavigationKey, uint dwModifiers) => false;

        public virtual bool SearchEnabled => true;

        public virtual Guid Category => Guid.Empty;

        public virtual IVsEnumWindowSearchFilters SearchFiltersEnum => null;

        public virtual IVsEnumWindowSearchOptions SearchOptionsEnum => null;
    }

    public class ListBoxSearch : VsWindowSearch
    {
        private readonly ListBox _listBox = null;
        private Action<string> _setSearchText = null;

        public ListBoxSearch(ListBox listBox, Action<string> action)
        {
            _listBox = listBox;
            _setSearchText = action;
        }

        public override IVsSearchTask CreateSearch(uint cookie, IVsSearchQuery query,
                                                   IVsSearchCallback callback)
        {
            return new SearchTask(cookie, query, callback, ClearSearch,
                searchCallBack: (IEnumerable<string> tokens) =>
                {
                    _setSearchText(string.Join(" ", tokens));
                    var view = CollectionViewSource.GetDefaultView(_listBox.ItemsSource);

                    view.Refresh();
                    if (_listBox.Items.Count == 1 || _listBox.SelectedItem == null)
                        _listBox.SelectedIndex = 0;
                    return (uint)view.Cast<object>().Count();
                });
        }

        public override void ClearSearch()
        {
            _setSearchText(string.Empty);
            CollectionViewSource.GetDefaultView(_listBox.ItemsSource).Refresh();
        }
    }
}
