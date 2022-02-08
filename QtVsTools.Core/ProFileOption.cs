/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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

using System.Collections.Generic;

namespace QtVsTools.Core
{
    internal class ProFileOption
    {
        public ProFileOption(string optname)
        {
            Name = optname;
            AssignSymbol = AssignType.AT_PlusEquals;
            Comment = null;
            ShortComment = "Default";
            IncludeComment = false;
            NewOption = " \\\r\n    ";
            List = new List<string>();
        }

        public override string ToString()
        {
            return ShortComment;
        }

        public string Comment { get; set; }

        public string ShortComment { get; set; }

        public AssignType AssignSymbol { get; set; }

        public string NewOption { get; set; }

        public string Name { get; }

        public List<string> List { get; }

        public bool IncludeComment { get; set; }

        public enum AssignType
        {
            AT_Equals = 1,
            AT_PlusEquals = 2, // default
            AT_MinusEquals = 3,
            AT_Function = 4
        }
    }
}
