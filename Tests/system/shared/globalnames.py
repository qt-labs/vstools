####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
####################################################################################################

# encoding: UTF-8

from objectmaphelper import *
microsoft_Visual_Studio_Window = {"text": Wildcard("*Microsoft Visual Studio"), "type": "Window"}
continueWithoutCode_Label = {"container": microsoft_Visual_Studio_Window, "text": "System.Windows.Controls.AccessText Microsoft.VisualStudio.Imaging.CrispImage", "type": "Label"}
microsoft_Visual_Studio_MenuBar = {"container": microsoft_Visual_Studio_Window, "type": "MenuBar"}
file_MenuItem = {"container": microsoft_Visual_Studio_MenuBar, "text": "File", "type": "MenuItem"}
file_Exit_MenuItem = {"container": file_MenuItem, "text": "Exit", "type": "MenuItem"}
extensions_MenuItem = {"container": microsoft_Visual_Studio_MenuBar, "text": "Extensions", "type": "MenuItem"}
extensions_Qt_VS_Tools_MenuItem = {"container": extensions_MenuItem, "text": "Qt VS Tools",
                                   "type": "MenuItem"}
msvs_Skip_this_for_now_Button = {"container": microsoft_Visual_Studio_Window, "text": "Skip this for now.", "type": "Button"}
msvs_Start_Visual_Studio_Button = {"container": microsoft_Visual_Studio_Window, "text": "Start Visual Studio", "type": "Button"}
msvs_Not_now_maybe_later_Label = {"container": microsoft_Visual_Studio_Window, "text": "Not now, maybe later.", "type": "Label"}
Initializing_MenuItem = {"text": RegularExpression(".*Initializing...$"), "type": "MenuItem"}
pART_Popup_New_MenuItem = {"text": "New", "type": "MenuItem"}
pART_Popup_Project_MenuItem = {"text": "Project...", "type": "MenuItem"}
msvs_Account_Settings_Window = {"text": "Microsoft Visual Studio Account Settings", "type": "Window"}
msvs_Account_Close_Button = {"container": msvs_Account_Settings_Window, "text": "Close", "type": "Button"}
