/**************************************************************************
**
** This file is part of the Qt VS Add-in
**
** Copyright (c) 2010 Nokia Corporation and/or its subsidiary(-ies).
**
** Contact: Nokia Corporation (qt-info@nokia.com)
**
** Commercial Usage
**
** Licensees holding valid Qt Commercial licenses may use this file in
** accordance with the Qt Commercial License Agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Nokia.
**
** GNU Lesser General Public License Usage
**
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** If you are unsure which license is appropriate for your use, please
** contact the sales department at http://qt.nokia.com/contact.
**
**************************************************************************/

namespace Qt4VSAddin
// --------------------------------------------------------------------------------------
{
    using System.Collections;

    public class Res
    {
	// The CommandBarName must follow the ProgId
	// defined in Connect.cs
    
    public const string CommandBarName = "Qt4VSAddin";
	// The command bar constants.

	// All the constants for the LaunchDesigner command.
	public const int DesignerBitmapID = 1 ;
	public const string 
	    LaunchDesigner		= "LaunchDesigner",
        LaunchDesignerFullCommand = CommandBarName + ".Connect." + LaunchDesigner;

	// All the constants for the LaunchLinguist command.
    public const int LinguistBitmapID = 3;
    public const string
        LaunchLinguist = "LaunchLinguist",
        LaunchLinguistFullCommand = CommandBarName + ".Connect." + LaunchLinguist;

    // All the constants for the ImportProFile command.
    public const int ImportProFileBitmapID = 11;
    public const string
        ImportProFile = "ImportProFile",
        ImportProFileFullCommand = CommandBarName + ".Connect." + ImportProFile;

    // All the constants for the ImportProFile command.
    public const int ImportPriFileBitmapID = 12;
    public const string
        ImportPriFile = "ImportPriFile",
        ImportPriFileFullCommand = CommandBarName + ".Connect." + ImportPriFile;

    // All the constants for the ExportProFile command.
    public const int ExportProFileBitmapID = 13;
    public const string
        ExportProFile = "ExportProFile",
        ExportProFileFullCommand = CommandBarName + ".Connect." + ExportProFile;

    // All the constants for the ExportPriFile command.
    public const int ExportPriFileBitmapID = 14;
    public const string
        ExportPriFile = "ExportPriFile",
        ExportPriFileFullCommand = CommandBarName + ".Connect." + ExportPriFile;

    // All the constants for the ChangeQtVersion command.
    public const int QtBitmapID = 4;
    public const string
        ChangeSolutionQtVersion = "ChangeSolutionQtVersion",
        ChangeSolutionQtVersionFullCommand = CommandBarName + ".Connect." + ChangeSolutionQtVersion;

    // All the constants for the Add/RemoveQtModules command.
    public const int AddRemoveModulesBitmapID = 0;
    public const string
        AddRemoveModules = "AddRemoveModules",
        AddRemoveModulesButtonText = "Add/Remove Qt Modules",
        AddRemoveModulesFullCommand = CommandBarName + ".Connect." + AddRemoveModules;

    // All the constants for the ProjectQtSettings command.
    public const int ProjectQtSettingsBitmapID = 0;
    public const string
        ProjectQtSettings = "ProjectQtSettings",
        ProjectQtSettingsFullCommand = CommandBarName + ".Connect." + ProjectQtSettings;

    // All the constants for the ChangeProjectQtVersion command.
    public const int ChangeProjectQtVersionBitmapID = 0;
    public const string
        ChangeProjectQtVersion = "ChangeProjectQtVersion",
        ChangeProjectQtVersionFullCommand = CommandBarName + ".Connect." + ChangeProjectQtVersion;

    // All the constants for the VSQtOptions command.
    public const int VSQtOptionsBitmapID = 0;
    public const string
        VSQtOptions = "VSQtOptions",
        VSQtOptionsFullCommand = CommandBarName + ".Connect." + VSQtOptions;

    // All the constants for the CreateTranslationFile command.
    public const int CreateNewTranslationFileBitmapID = 0;
    public const string
        CreateNewTranslationFile = "CreateNewTranslationFile",
        CreateNewTranslationFileFullCommand = CommandBarName + ".Connect." + CreateNewTranslationFile;

    // All the constants for the lupdateProject command.
    public const string
        lupdateProject = "lupdateProject",
        lupdateProjectFullCommand = CommandBarName + ".Connect." + lupdateProject;

    // All the constants for the lupdateProject command.
    public const string
        lreleaseProject = "lreleaseProject",
        lreleaseProjectFullCommand = CommandBarName + ".Connect." + lreleaseProject;

    // All the constants for the lupdateSolution command.
    public const string
        lupdateSolution = "lupdateSolution",
        lupdateSolutionFullCommand = CommandBarName + ".Connect." + lupdateSolution;

    // All the constants for the lupdateSolution command.
    public const string
        lreleaseSolution = "lreleaseSolution",
        lreleaseSolutionFullCommand = CommandBarName + ".Connect." + lreleaseSolution;

    // All the constants for the ConvertToQt command.
    public const string
        ConvertToQt = "ConvertToQt",
        ConvertToQtFullCommand = CommandBarName + ".Connect." + ConvertToQt;

    // All the constants for the ConvertToQMake command.
    public const string
        ConvertToQMake = "ConvertToQMake",
        ConvertToQMakeFullCommand = CommandBarName + ".Connect." + ConvertToQMake;
    }
}
