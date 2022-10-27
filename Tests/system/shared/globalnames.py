############################################################################
#
# Copyright (C) 2022 The Qt Company Ltd.
# Contact: https://www.qt.io/licensing/
#
# This file is part of the Qt VS Tools.
#
# $QT_BEGIN_LICENSE:GPL-EXCEPT$
# Commercial License Usage
# Licensees holding valid commercial Qt licenses may use this file in
# accordance with the commercial license agreement provided with the
# Software or, alternatively, in accordance with the terms contained in
# a written agreement between you and The Qt Company. For licensing terms
# and conditions see https://www.qt.io/terms-conditions. For further
# information use the contact form at https://www.qt.io/contact-us.
#
# GNU General Public License Usage
# Alternatively, this file may be used under the terms of the GNU
# General Public License version 3 as published by the Free Software
# Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
# included in the packaging of this file. Please review the following
# information to ensure the GNU General Public License requirements will
# be met: https://www.gnu.org/licenses/gpl-3.0.html.
#
# $QT_END_LICENSE$
#
############################################################################

# encoding: UTF-8

from objectmaphelper import *
microsoft_Visual_Studio_Window = {"text": Wildcard("*Microsoft Visual Studio"), "type": "Window"}
continueWithoutCode_Label = {"container": microsoft_Visual_Studio_Window, "text": "System.Windows.Controls.AccessText Microsoft.VisualStudio.Imaging.CrispImage", "type": "Label"}
microsoft_Visual_Studio_MenuBar = {"container": microsoft_Visual_Studio_Window, "type": "MenuBar"}
file_MenuItem = {"container": microsoft_Visual_Studio_MenuBar, "text": "File", "type": "MenuItem"}
pART_Popup_Popup = {"id": "", "name": "PART_Popup", "type": "Popup"}
pART_Popup_Exit_MenuItem = {"container": pART_Popup_Popup, "text": "Exit", "type": "MenuItem"}
pART_Popup_Qt_VS_Tools_MenuItem = {"container": pART_Popup_Popup, "text": "Qt VS Tools", "type": "MenuItem"}
extensions_MenuItem = {"container": microsoft_Visual_Studio_MenuBar, "text": "Extensions", "type": "MenuItem"}
qt_VS_Tools_MenuItem = {"container": microsoft_Visual_Studio_MenuBar, "text": "Qt VS Tools", "type": "MenuItem"}
