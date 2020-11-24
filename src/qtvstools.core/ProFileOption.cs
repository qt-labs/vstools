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
            name = optname;
            astype = AssignType.AT_PlusEquals;
            comment = null;
            shortComment = "Default";
            incComment = false;
            newOpt = " \\\r\n    ";
            list = new List<string>();
        }

        public override string ToString()
        {
            return shortComment;
        }

        public string Comment
        {
            get
            {
                return comment;
            }
            set
            {
                comment = value;
            }
        }

        public string ShortComment
        {
            get
            {
                return shortComment;
            }
            set
            {
                shortComment = value;
            }
        }

        public AssignType AssignSymbol
        {
            get
            {
                return astype;
            }
            set
            {
                astype = value;
            }
        }

        public string NewOption
        {
            get
            {
                return newOpt;
            }
            set
            {
                newOpt = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public List<string> List
        {
            get
            {
                return list;
            }
        }

        public bool IncludeComment
        {
            get
            {
                return incComment;
            }
            set
            {
                incComment = value;
            }
        }

        public enum AssignType
        {
            AT_Equals = 1,
            AT_PlusEquals = 2, // default
            AT_MinusEquals = 3,
            AT_Function = 4
        }

        private AssignType astype;
        private string shortComment;
        private bool incComment;
        private string comment;
        private string newOpt;
        private string name;
        private List<string> list;
    }
}
