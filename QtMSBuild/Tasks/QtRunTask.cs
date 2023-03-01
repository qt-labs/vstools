/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#region Task TaskName="QtRunTask"

#region Reference
//System.Runtime
#endregion

#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
#endregion

#region Comment
/////////////////////////////////////////////////////////////////////////////////////////////////
/// TASK QtRunTask
/////////////////////////////////////////////////////////////////////////////////////////////////
// Execute a task from the specified assembly, taking as input a list of items. Each item
// metadata is copied to a task property with the same name. After the task is executed, the
// result for each item is then stored in a new metadata for that item. The final output is the
// list of modified items.
// Parameters /////////////////////////////////////////////////////////////////////////////////
//  in ITaskItem[]   Items........................List of source items.
//  in String        AssemblyPath.................Path to assembly containing the task.
//  in String        TaskName.....................Full name of task, including namespace.
//  in String        TaskInput....................Name of task input property.
//  in String        TaskOutput (optional)........Name of task output property.
//    If unspecified, task output is ignored.
//  in String        NewMetadata (optional).......Name of new item metadata to store result.
//    If unspecified, defaults to TaskOutput.
// out ITaskItem[]   Result.......................Output list of modified items.
//    If TaskOutput is unspecified, Result will be empty.
// Example //////////////////////////////////////////////////////////////////////////////////////
//  Items = {
//      ClCompile {
//          Identity = "main.cpp",
//          ...
//          EnforceTypeConversionRules = False,
//          ...
//      }
//  }
//  AssemblyPath = "$(VCTargetsPath)\Microsoft.Build.CPPTasks.Common.dll"
//  TaskName = "Microsoft.Build.CPPTasks.CLCommandLine"
//  TaskInput = "Sources"
//  TaskOutput = "CommandLines"
//  NewMetadata = "CommandLine"
//  Result = {
//      ClCompile_Modified {
//          Identity = "main.cpp",
//          ...
//          EnforceTypeConversionRules = False,
//          ...
//          CommandLine = "... /Zc:rvalueCast- ..."
//      }
//  }
#endregion

namespace QtVsTools.QtMsBuild.Tasks
{
    public static class QtRunTask
    {
        public static QtMSBuild.ITaskLoggingHelper Log { get; set; }
        public static IBuildEngine BuildEngine { get; set; }
        public static ITaskHost HostObject { get; set; }

        public static bool Execute(
        #region Parameters
            Microsoft.Build.Framework.ITaskItem[] Items,
            System.String AssemblyPath,
            System.String TaskName,
            System.String TaskInput,
            out Microsoft.Build.Framework.ITaskItem[] Result,
            System.String TaskOutput = null,
            System.String NewMetadata = null)
        #endregion
        {
            #region Code
            var reserved = new HashSet<string>
            {
                "AccessedTime", "CreatedTime", "DefiningProjectDirectory",
                "DefiningProjectExtension", "DefiningProjectFullPath", "DefiningProjectName",
                "Directory", "Extension", "Filename", "FullPath", "Identity", "ModifiedTime",
                "RecursiveDir", "RelativeDir", "RootDir",
            };

            // Output default values
            Result = null;

            // Load specified assembly
            var taskAssembly = Assembly.LoadFile(AssemblyPath);
            if (taskAssembly == null)
                throw new ArgumentException("AssemblyPath");

            // Access task type
            var taskType = taskAssembly.GetType(TaskName);
            if (taskType == null)
                throw new ArgumentException("TaskName");

            // Task type has the following requirements:
            //  * Must be a descendant of the ToolTask type
            //  * Cannot be an abstract class
            //  * Cannot have generic type arguments
            //  * Must have a public default constructor
            if (!typeof(ToolTask).IsAssignableFrom(taskType))
                throw new NotSupportedException("Not a ToolTask derived type");
            if (taskType.IsAbstract)
                throw new NotSupportedException("Abstract class");
            if (taskType.ContainsGenericParameters)
                throw new NotSupportedException("Generic class");
            var ctorInfo = ((TypeInfo)taskType).DeclaredConstructors
                .FirstOrDefault(x => x.GetParameters().Length == 0);
            if (ctorInfo == null)
                throw new NotSupportedException("No default constructor");

            // Access input property of task type
            var inputProperty = taskType.GetProperty(TaskInput);
            if (inputProperty == null)
                throw new ArgumentException("TaskInput");
            if (inputProperty.PropertyType != typeof(ITaskItem)
                && inputProperty.PropertyType != typeof(ITaskItem[])) {
                throw new NotSupportedException("Input property type is not supported");
            }

            // If output was specified, access corresponding property of task type
            PropertyInfo outputProperty = null;
            if (TaskOutput != null) {
                outputProperty = taskType.GetProperty(TaskOutput);
                if (outputProperty == null)
                    throw new ArgumentException("TaskOutput");
                if (outputProperty.PropertyType != typeof(string)
                    && outputProperty.PropertyType != typeof(string[])
                    && outputProperty.PropertyType != typeof(ITaskItem[])) {
                    throw new NotSupportedException("Output property type is not supported");
                }
                if (NewMetadata == null)
                    NewMetadata = TaskOutput;
            }

            var resultItems = new List<ITaskItem>();
            foreach (var item in Items) {
                // For each source item ...

                // Instantiate task
                var task = ctorInfo.Invoke(Array.Empty<object>()) as ToolTask;
                task.BuildEngine = BuildEngine;
                task.HostObject = HostObject;

                // Set task input property to the source item
                var inputPropertyType = inputProperty.PropertyType;
                if (inputPropertyType == typeof(ITaskItem)) {
                    inputProperty.SetValue(task, item);
                } else if (inputPropertyType == typeof(ITaskItem[])) {
                    inputProperty.SetValue(task, new ITaskItem[] { item });
                }

                var names = item.MetadataNames.Cast<string>()
                    .Where(x => !reserved.Contains(x));
                foreach (var name in names) {
                    // For each metadata in the source item ...

                    // Try to obtain a task property with the same name
                    var taskProperty = taskType.GetProperty(name);
                    if (taskProperty != null) {
                        // If the property exists, set it to the metadata value
                        string metadataValue = item.GetMetadata(name);
                        var propertyType = taskProperty.PropertyType;
                        if (propertyType == typeof(bool)) {
                            taskProperty.SetValue(task, metadataValue.Equals("true",
                                StringComparison.InvariantCultureIgnoreCase));
                        } else if (propertyType == typeof(string)) {
                            taskProperty.SetValue(task, metadataValue);
                        } else if (propertyType == typeof(string[])) {
                            taskProperty.SetValue(task, metadataValue.Split(';'));
                        }
                    }
                }

                // Run task
                if (!task.Execute())
                    throw new Exception(string.Format("Task failed ({0})", item.ItemSpec));

                // Record task output
                if (TaskOutput != null) {
                    // Create output item as copy of source item
                    var resultItem = new TaskItem(item);

                    // Set new metadata and add output item to the result list
                    string outputValue;
                    object propertyValue = outputProperty.GetValue(task);
                    if (propertyValue == null)
                        outputValue = string.Empty;
                    else if (outputProperty.PropertyType == typeof(string))
                        outputValue = propertyValue as string;
                    else if (outputProperty.PropertyType == typeof(string[]))
                        outputValue = (propertyValue as string[]).FirstOrDefault();
                    else if (outputProperty.PropertyType == typeof(ITaskItem[]))
                        outputValue = (propertyValue as ITaskItem[]).FirstOrDefault().ItemSpec;
                    else
                        outputValue = string.Empty;
                    if (NewMetadata != null)
                        resultItem.SetMetadata(NewMetadata, outputValue);
                    resultItems.Add(resultItem);
                }
            }

            // Return the list of output items
            Result = resultItems.ToArray();

            #endregion

            return true;
        }
    }
}
#endregion
