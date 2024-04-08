####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
####################################################################################################

# -*- coding: utf-8 -*-

source("../shared/scripts/config_utils.py")

import os
import re
import shutil

from newprojectdialog import NewProjectDialog
import names


def testCompareRegex(text, pattern, message):
    regex = re.compile(pattern)
    test.verify(regex.match(text), '%s ("%s"/"%s")' % (message, text, pattern))


def listExpectedWrittenFiles(workDir, projectName, templateName, cmakeBased):
    projectDirectory = os.path.join(workDir, projectName, projectName)

    def prependProjectDirectory(fileName):
        return os.path.join(projectDirectory, fileName)

    if cmakeBased:
        projFiles = [os.path.join(workDir, projectName, "CMakeLists.txt"),
                     os.path.join(workDir, projectName, "CMakePresets.json"),
                     os.path.join(workDir, projectName, "CMakeUserPresets.json")]
        projFiles.extend(map(prependProjectDirectory, ["CMakeLists.txt",
                                                       "qt.cmake"]))
    else:
        projFiles = [os.path.join(workDir, projectName, projectName + ".sln")]
        projFiles.extend(map(prependProjectDirectory, [projectName + ".vcxproj",
                                                       projectName + ".vcxproj.filters",
                                                       projectName + ".vcxproj.user"]))

    if templateName == "Qt Designer Custom Widget":
        return projFiles + list(map(prependProjectDirectory, [projectName + ".cpp",
                                                              projectName + ".h",
                                                              projectName + "Plugin.cpp",
                                                              projectName + "Plugin.h",
                                                              projectName.lower() + "plugin.json"]))
    elif templateName == "Qt Console Application":
        return projFiles + [prependProjectDirectory("main.cpp")]
    elif templateName == "Qt ActiveQt Server":
        return projFiles + list(map(prependProjectDirectory, [projectName + ".cpp",
                                                              projectName + ".def",
                                                              projectName + ".h",
                                                              projectName + ".ico",
                                                              projectName + ".rc",
                                                              projectName + ".ui"]))
    elif templateName == "Qt Quick Application":
        if not cmakeBased:
            projFiles.append(prependProjectDirectory("qml.qrc"))
        return projFiles + list(map(prependProjectDirectory, ["main.cpp",
                                                              "main.qml"]))
    elif templateName == "Qt Empty Application":
        return projFiles
    elif templateName == "Qt Class Library":
        return projFiles + list(map(prependProjectDirectory, [projectName + ".cpp",
                                                              projectName + ".h",
                                                              projectName.lower() + "_global.h"]))
    elif templateName == "Qt Widgets Application":
        if not cmakeBased:
            projFiles.append(prependProjectDirectory(projectName + ".qrc"))
        return projFiles + list(map(prependProjectDirectory, ["main.cpp",
                                                              projectName + ".cpp",
                                                              projectName + ".h",
                                                              projectName + ".ui"]))
    else:
        test.fatal("Unexpected template '%s'" % templateName,
                   "You might need to update function listExpectedWrittenFiles()")
        return []


def getExpectedBuiltFile(workDir, projectName, templateName, cmakeBased):
    buildPath = os.path.join(workDir, projectName)
    if cmakeBased:
        buildPath = os.path.join(buildPath, "out", "build", projectName)
    else:
        expand(waitForObject(names.platforms_ComboBox))
        try:
            waitForObjectExists(names.x64_ComboBoxItem, 5000)
            buildPath = os.path.join(buildPath, "x64")
        except:
            pass
        collapse(waitForObject(names.platforms_ComboBox))
        buildPath = os.path.join(buildPath, "Debug")
    if templateName in ["Qt Console Application",
                        "Qt Quick Application",
                        "Qt Widgets Application"]:
        return os.path.join(buildPath, projectName + ".exe")
    elif templateName in ["Qt Class Library",
                          "Qt Designer Custom Widget"]:
        return os.path.join(buildPath, projectName + ".dll")
    elif templateName == "Qt Empty Application":
        return os.path.join(buildPath)
    else:
        test.fatal("Unexpected template '%s'" % templateName,
                   "You might need to update function getExpectedBuiltFile()")
        return ""


def buildSolution(cmakeBased):
    if cmakeBased:
        labelObject = waitForObjectExists(names.selectStartupItemLabel)
        if not waitFor(lambda: labelObject.text != 'Select Startup Item...', 230000):
            test.fail("Could not start building the project.",
                      "Did configuring fail?")  # See QTVSADDINBUG-1162
            return False
    mouseClick(waitForObject(names.build_MenuItem))
    mouseClick(waitForObject(names.build_BuildAll_MenuItem if cmakeBased
                             else names.build_Build_Solution_MenuItem))
    # make sure building finished
    labelObject = waitForObjectExists(names.selectStartupItemLabel)
    waitFor(lambda: not labelObject.enabled, 5000)
    waitFor(lambda: labelObject.enabled, 100000)
    return True


workDir = os.getenv("SQUISH_VSTOOLS_WORKDIR")
createdProjects = set()


def main():
    qtDirs = readQtDirs()
    if not qtDirs:
        test.fatal("No Qt versions known", "Did you set SQUISH_VSTOOLS_QTDIRS correctly?")
        return
    if not workDir:
        test.fatal("No directory for creating projects known",
                   "Did you set SQUISH_VSTOOLS_WORKDIR correctly?")
        return
    version = startAppGetVersion()
    if not version:
        return
    if not configureQtVersions(version, qtDirs):
        closeMainWindow()
        return

    with NewProjectDialog() as dialog:
        dialog.filterForQtProjects()
        listedTemplates = list(dialog.getListedTemplates())

    expectedFiles = {"Qt Designer Custom Widget": "^QtDesigner.*\.cpp$",
                     "Qt Console Application": "^main\.cpp$",
                     "Qt ActiveQt Server": "^ActiveQtServer\d*\.cpp$",
                     "Qt Quick Application": "^main\.qml$",
                     "Qt Empty Application": None,
                     "Qt Class Library": "^QtClassLibrary\d*\.cpp$",
                     "Qt Widgets Application": "^QtWidgets.*\.cpp$"}

    with NewProjectDialog() as dialog:
        for buildSystem in ["Qt Visual Studio Project (Qt/MSBuild)",
                            "CMake Project for Qt (cmake-qt, Qt/CMake helper functions)"]:
            cmakeBased = buildSystem.startswith("CMake")
            if cmakeBased and version == "2019":
                test.warning("MSVS2019 often freezes when opening CMake-based projects, skipping.")
                continue
            with TestSection("Build System: " + buildSystem):
                for listItem, templateName in listedTemplates:
                    with TestSection(templateName):
                        if not templateName in expectedFiles:
                            test.warning("Template %s is not supported, skipping..."
                                         % templateName)
                            continue
                        if (cmakeBased and templateName in ["Qt ActiveQt Server"]):
                            test.log("Skipping '%s' because it does not support CMake."
                                     % templateName)
                            continue
                        mouseClick(waitForObject(listItem))
                        clickButton(waitForObject(names.microsoft_Visual_Studio_Next_Button))
                        type(waitForObject(names.comboBox_Edit), workDir)
                        waitFor(lambda: waitForObject(names.comboBox_Edit).text == workDir)
                        projectName = waitForObjectExists(names.msvs_Project_name_Edit).text
                        createdProjects.add(projectName)
                        clickButton(waitForObject(names.microsoft_Visual_Studio_Create_Button))
                        fixAppContext()
                        clickButton(waitForObject(names.qt_Wizard_Next_Button))
                        if (templateName in ["Qt ActiveQt Server"]):
                            test.verify(not findObject(names.ProjectModel_ComboBox).enabled,
                                        "'%s' should not allow changing its build system"
                                        % templateName)
                            test.compare(findObject(names.ProjectModel_ComboBox).nativeObject.Text,
                                         buildSystem)
                        else:
                            expand(waitForObject(names.ProjectModel_ComboBox))
                            mouseClick(waitForObject(names.projectModelSelection_ComboBoxItem
                                                     | {"text": buildSystem}))
                        if templateName in ["Qt ActiveQt Server",
                                            "Qt Class Library",
                                            "Qt Designer Custom Widget",
                                            "Qt Widgets Application"]:
                            clickButton(waitForObject(names.qt_Wizard_Next_Button))
                        clickButton(waitForObject(names.qt_Wizard_Finish_Button))
                        fixAppContext()
                        if expectedFiles[templateName]:
                            try:
                                testCompareRegex(waitForObjectExists(names.qt_cpp_Label).text,
                                                 expectedFiles[templateName],
                                                 "Was a file with an expected name opened?")
                            except:
                                message = "There was no expected file opened for %s" % templateName
                                test.fail(message)
                        else:
                            test.exception("waitForObjectExists(names.qt_cpp_Label, 10000)",
                                           "No file should be opened for %s" % templateName)
                        test.verify(all(map(os.path.exists,
                                            listExpectedWrittenFiles(workDir, projectName,
                                                                     templateName, cmakeBased))),
                                    "Were all expected files created?")
                        if (templateName != "Qt ActiveQt Server" and buildSolution(cmakeBased)):
                            builtFile = getExpectedBuiltFile(workDir, projectName,
                                                             templateName, cmakeBased)
                            test.verify(waitFor(lambda: os.path.exists(builtFile), 15000),
                                        "Was %s built as expected?" % builtFile)
                        mouseClick(waitForObject(globalnames.file_MenuItem))
                        mouseClick(waitForObject(names.file_Close_Folder_MenuItem if cmakeBased
                                                 else names.file_Close_Solution_MenuItem))
                        # reopens the "New Project" dialog
                        mouseClick(waitForObject(names.msvs_Create_a_new_project_Label))
    clearQtVersions(version)
    closeMainWindow()


def cleanup():
    if workDir:
        for project in createdProjects:
            shutil.rmtree(os.path.join(workDir, project))
