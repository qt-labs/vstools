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
extensions_MenuItem = {"container": microsoft_Visual_Studio_MenuBar, "text": "Extensions", "type": "MenuItem"}
pART_Popup_Popup = {"id": "", "name": "PART_Popup", "type": "Popup"}
pART_Popup_Manage_Extensions_MenuItem = {"container": pART_Popup_Popup, "text": "Manage Extensions", "type": "MenuItem"}
manage_Extensions_Window = {"text": Wildcard("*Extensions*"), "type": "Window"}
extensionManager_UI_InstalledExtItem_Qt_Label = {"text": Wildcard("The Qt VS Tools for Visual Studio *"), "type": "Label"}
manage_Extensions_Close_Button = {"container": manage_Extensions_Window, "text": "Close", "type": "Button"}
file_MenuItem = {"container": microsoft_Visual_Studio_MenuBar, "text": "File", "type": "MenuItem"}
pART_Popup_Exit_MenuItem = {"container": pART_Popup_Popup, "text": "Exit", "type": "MenuItem"}
tools_MenuItem = {"container": microsoft_Visual_Studio_MenuBar, "text": "Tools", "type": "MenuItem"}
pART_Popup_Extensions_and_Updates_MenuItem = {"container": pART_Popup_Popup, "text": "Extensions and Updates...", "type": "MenuItem"}
extensions_and_Updates_lvw_Extensions_ListView = {"container": manage_Extensions_Window, "name": "lvw_Extensions", "type": "ListView"}
lvw_Extensions_Microsoft_VisualStudio_ExtensionManager_UI_InstalledExtensionItem_ListViewItem = {"container": extensions_and_Updates_lvw_Extensions_ListView, "text": "Microsoft.VisualStudio.ExtensionManager.UI.InstalledExtensionItem", "type": "ListViewItem"}
extensionManager_UI_InstalledExtItem_Qt_2017_Label = {"container": lvw_Extensions_Microsoft_VisualStudio_ExtensionManager_UI_InstalledExtensionItem_ListViewItem, "text": Wildcard("The Qt VS Tools for Visual Studio *"), "type": "Label"}
extensions_and_Updates_Version_Label = {"container": manage_Extensions_Window, "text": "Version:", "type": "Label"}
manage_Extensions_Version_Label = {"leftObject": extensions_and_Updates_Version_Label, "type": "Label"}
help_MenuItem = {"container": microsoft_Visual_Studio_MenuBar, "text": "Help", "type": "MenuItem"}
pART_Popup_About_Microsoft_Visual_Studio_MenuItem = {"container": pART_Popup_Popup, "text": "About Microsoft Visual Studio", "type": "MenuItem"}
o_Microsoft_Visual_Studio_OK_Button = {"container": microsoft_Visual_Studio_Window, "text": "OK", "type": "Button"}
about_Microsoft_Visual_Studio_Window = {"text": "About Microsoft Visual Studio", "type": "Window"}
about_Microsoft_Visual_Studio_2017_Label = {"container": about_Microsoft_Visual_Studio_Window, "text": Wildcard("Microsoft Visual Studio *"), "type": "Label"}
about_Microsoft_Visual_Studio_Edit = {"container": about_Microsoft_Visual_Studio_Window, "type": "Edit"}
o_Extensions_ProvidersTree_Tree = {"container": manage_Extensions_Window, "name": "ProvidersTree", "type": "Tree"}
providersTree_Online_TreeItem = {"container": o_Extensions_ProvidersTree_Tree, "text": "Online", "type": "TreeItem"}
o_Extensions_Edit = {"container": manage_Extensions_Window, "type": "Edit"}
lvw_Extensions_Microsoft_VisualStudio_ExtensionManager_UI_OnlineExtensionItem_ListViewItem = {"container": extensions_and_Updates_lvw_Extensions_ListView, "text": "Microsoft.VisualStudio.ExtensionManager.UI.OnlineExtensionItem", "type": "ListViewItem"}
OnlineExtensionItem_Download_Button = {"container": lvw_Extensions_Microsoft_VisualStudio_ExtensionManager_UI_OnlineExtensionItem_ListViewItem, "text": "Download", "type": "Button"}
changes_scheduled_Label = {"container": manage_Extensions_Window, "text": "Your changes will be scheduled.  The modifications will begin when all Microsoft Visual Studio windows are closed.", "type": "Label"}
msvs_ExtensionManager_UI_InstalledExtItem_Uninstall_Label= {"container": lvw_Extensions_Microsoft_VisualStudio_ExtensionManager_UI_InstalledExtensionItem_ListViewItem, "text": "_Uninstall", "type": "Label"}
microsoft_Visual_Studio_Dialog = {"text": "Microsoft Visual Studio", "type": "Dialog"}
microsoft_Visual_Studio_Yes_Button= {"container": microsoft_Visual_Studio_Dialog, "text": RegularExpression("Yes|Ja"), "type": "Button"}
pART_Popup_qt_io_MenuItem = {"text": "qt.io", "type": "MenuItem"}
pART_Popup_Qt_VS_Tools_MenuItem = {"container": pART_Popup_Popup, "text": "Qt VS Tools", "type": "MenuItem"}
qt_VS_Tools_MenuItem = {"container": microsoft_Visual_Studio_MenuBar, "text": "Qt VS Tools", "type": "MenuItem"}
