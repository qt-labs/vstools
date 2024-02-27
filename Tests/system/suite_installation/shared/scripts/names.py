####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
####################################################################################################

# encoding: UTF-8

from objectmaphelper import *
import globalnames
extensions_Manage_Extensions_MenuItem = {"container": globalnames.extensions_MenuItem,
                                         "text": RegularExpression("^Manage Extensions(\.\.\.)?$"),
                                         "type": "MenuItem"}
manage_Extensions_Window = {"text": Wildcard("*Extensions*"), "type": "Window"}
extensionManager_UI_InstalledExtItem_Qt_Label = {"text": Wildcard("The Qt VS Tools for Visual Studio *"), "type": "Label"}
manage_Extensions_Close_Button = {"container": manage_Extensions_Window, "text": "Close", "type": "Button"}
extensions_and_Updates_lvw_Extensions_ListView = {"container": manage_Extensions_Window, "name": "lvw_Extensions", "type": "ListView"}
lvw_Extensions_Microsoft_VisualStudio_ExtensionManager_UI_InstalledExtensionItem_ListViewItem = {"container": extensions_and_Updates_lvw_Extensions_ListView, "id": "Qt Visual Studio Tools", "text": "Microsoft.VisualStudio.ExtensionManager.UI.InstalledExtensionItem", "type": "ListViewItem"}
extensions_and_Updates_Version_Label = {"container": manage_Extensions_Window, "text": "Version:", "type": "Label"}
manage_Extensions_Version_Label = {"leftObject": extensions_and_Updates_Version_Label, "type": "Label"}
help_MenuItem = {"container": globalnames.microsoft_Visual_Studio_MenuBar, "text": "Help", "type": "MenuItem"}
help_About_Microsoft_Visual_Studio_MenuItem = {"container": help_MenuItem,
                                               "text": "About Microsoft Visual Studio",
                                               "type": "MenuItem"}
o_Microsoft_Visual_Studio_OK_Button = {"container": globalnames.microsoft_Visual_Studio_Window, "text": "OK", "type": "Button"}
about_Microsoft_Visual_Studio_Window = {"text": "About Microsoft Visual Studio", "type": "Window"}
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
