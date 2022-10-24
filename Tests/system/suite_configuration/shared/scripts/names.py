####################################################################################################
# Copyright (C) 2023 The Qt Company Ltd.
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
