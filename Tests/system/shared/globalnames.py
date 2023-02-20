####################################################################################################
# Copyright (C) 2023 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
####################################################################################################

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
