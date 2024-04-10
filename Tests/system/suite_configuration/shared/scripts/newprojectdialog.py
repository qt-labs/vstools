####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
####################################################################################################

# -*- coding: utf-8 -*-

import globalnames
import squish

project_type_filter_ComboBox = {"container": globalnames.microsoft_Visual_Studio_Window,
                                "id": "ComboBox_3", "type": "ComboBox"}
qt_ComboBoxItem = {"container": project_type_filter_ComboBox, "text": "Qt", "type": "ComboBoxItem"}
microsoft_VS_TemplateList_ListView = {"container": globalnames.microsoft_Visual_Studio_Window,
                                      "name": "TemplateList", "type": "ListView"}
templateList_ListViewItem = {"container": microsoft_VS_TemplateList_ListView,
                             "text": "Microsoft.VisualStudio.NewProjectDialog.VsTemplateViewModel",
                             "type": "ListViewItem"}
microsoft_Visual_Studio_Back_Button = {"container": globalnames.microsoft_Visual_Studio_Window,
                                       "text": "Back", "type": "Button"}
microsoft_Visual_Studio_Close_Button = {"container": globalnames.microsoft_Visual_Studio_Window,
                                        "id": "button_Close", "type": "Button"}


class NewProjectDialog:

    @staticmethod
    def open():
        squish.mouseClick(squish.waitForObject(globalnames.file_MenuItem))
        squish.mouseClick(squish.waitForObject(globalnames.pART_Popup_New_MenuItem))
        squish.mouseClick(squish.waitForObject(globalnames.pART_Popup_Project_MenuItem))

    def __enter__(self):
        self.open()
        return self

    def __exit__(self, _, __, ___):
        squish.clickButton(squish.waitForObject(microsoft_Visual_Studio_Close_Button))

    def filterForQtProjects(self):
        squish.expand(squish.waitForObject(project_type_filter_ComboBox))
        squish.mouseClick(squish.waitForObject(qt_ComboBoxItem))

    def getListedTemplates(self):
        listView = squish.waitForObject(microsoft_VS_TemplateList_ListView)
        for i in range(1, listView.itemCount - 1):  # itemCount is number of shown items + 2
            listItem = templateList_ListViewItem | {"occurrence": i}
            templateName = squish.waitForObjectExists({"container": listItem, "type": "Label"}).text
            yield listItem, templateName

    def goBack(self):
        squish.clickButton(squish.waitForObject(microsoft_Visual_Studio_Back_Button))
