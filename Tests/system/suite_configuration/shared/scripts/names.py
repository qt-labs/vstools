####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
####################################################################################################

# encoding: UTF-8

from objectmaphelper import *
import globalnames
pART_Popup_Qt_Versions_MenuItem = {"text": "Qt Versions", "type": "MenuItem"}
options_Dialog = {"text": "Options", "type": "Dialog"}
options_OK_Button = {"container": options_Dialog, "text": "OK", "type": "Button"}
options_WPFControl = {"class": "System.Windows.Documents.AdornerDecorator",
                      "container": options_Dialog, "type": "WPFControl"}
dataGrid_Table = {"container": options_WPFControl, "name": "DataGrid", "type": "Table"}
options_Cancel_Button = {"container": options_Dialog, "text": "Cancel", "type": "Button"}
You_must_select_a_Qt_version_Label = {"container": globalnames.microsoft_Visual_Studio_Window,
                                      "text": Wildcard("*You must select a Qt version*"),
                                      "type": "Label"}
microsoft_Visual_Studio_Next_Button = {"container": globalnames.microsoft_Visual_Studio_Window,
                                       "text": "Next", "type": "Button"}
microsoft_Visual_Studio_Create_Button = {"container": globalnames.microsoft_Visual_Studio_Window,
                                         "text": "Create", "type": "Button"}
qt_Wizard_Window = {"class": "QtVsTools.Wizards.Common.WizardWindow", "type": "Window"}
qt_Wizard_Next_Button = {"container": qt_Wizard_Window, "text": "Next >", "type": "Button"}
qt_Wizard_Cancel_Button = {"container": qt_Wizard_Window, "text": "Cancel", "type": "Button"}
project_template_name_Label = {"container": globalnames.microsoft_Visual_Studio_Window,
                               "id": "TextBlock_1", "type": "Label"}
qt_Wizard_Welcome_Label = {"class": "System.Windows.Controls.TextBlock",
                           "container": qt_Wizard_Window, "type": "Label"}
outputPathTextBlock_Label = {"container": globalnames.microsoft_Visual_Studio_Window,
                             "id": "outputPathTextBlock", "type": "Label"}
microsoft_Visual_Studio_Project_name_Edit = {"id": "projectNameText", "type": "Edit"}
solutionNameText_Edit = {"container": globalnames.microsoft_Visual_Studio_Window,
                         "id": "solutionNameText", "type": "Edit"}
comboBox_Edit = {"id": "PART_EditableTextBox", "type": "Edit"}
ProjectModel_ComboBox = {"id": "ProjectModelSelection", "container": qt_Wizard_Window, "type": "ComboBox"}
qt_ConfigTable = {"container": qt_Wizard_Window, "name": "ConfigTable", "type": "Table"}
qt_Wizard_Class_Name_Edit = {"container": qt_Wizard_Window, "id": "ClassName", "type": "Edit"}
qt_Wizard_Source_cpp_file_Edit = {"container": qt_Wizard_Window, "id": "ClassSourceFile",
                                  "type": "Edit"}
qt_Wizard_Header_h_file_Edit = {"container": qt_Wizard_Window, "id": "ClassHeaderFile",
                                "type": "Edit"}
no_Qt_version_Label = {"container": qt_Wizard_Window, "id": "ErrorMsg", "type": "Label"}
qt_Wizard_Finish_Button = {"container": qt_Wizard_Window, "text": "Finish", "type": "Button"}
lower_case_file_names_CheckBox = {"container": qt_Wizard_Window, "text": "Lower case file names",
                                  "type": "CheckBox"}
file_Close_Solution_MenuItem = {"container": globalnames.file_MenuItem, "text": "Close Solution",
                                "type": "MenuItem"}
microsoft_Visual_Studio_Create_a_new_project_Label = {"container":
                                                      globalnames.microsoft_Visual_Studio_Window,
                                                      "text": "Create a _new project",
                                                      "type": "Label"}
qt_Microsoft_Visual_Studio_cpp_TabItem = {"container":globalnames.microsoft_Visual_Studio_Window,
                                          "text": RegularExpression(".*Qt.*\.cpp$|^main\.(cpp|qml)$"),
                                          "type": "TabItem"}
qt_cpp_Label = {"container": qt_Microsoft_Visual_Studio_cpp_TabItem,
                "text": RegularExpression(".+\.(cpp|qml)$"), "type": "Label"}
build_MenuItem = {"container": globalnames.microsoft_Visual_Studio_MenuBar, "text": "Build",
                  "type": "MenuItem"}
build_Build_Solution_MenuItem = {"container": build_MenuItem, "text": "Build Solution",
                                 "type": "MenuItem"}
Microsoft_Visual_Studio_ToolBar = {"container": globalnames.microsoft_Visual_Studio_Window,
                                   "type": "ToolBar"}
platforms_ComboBox = {"container": Microsoft_Visual_Studio_ToolBar,
                      "tooltip": "Solution Platforms", "type": "ComboBox"}
x64_ComboBoxItem = {"container": platforms_ComboBox, "id": "x64", "type": "ComboBoxItem"}
