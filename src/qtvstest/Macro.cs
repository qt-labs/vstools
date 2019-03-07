/****************************************************************************
**
** Copyright (C) 2019 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Tools.
**
** $QT_BEGIN_LICENSE:GPL-EXCEPT$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see https://www.qt.io/terms-conditions. For further
** information use the contact form at https://www.qt.io/contact-us.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3 as published by the Free Software
** Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
** included in the packaging of this file. Please review the following
** information to ensure the GNU General Public License requirements will
** be met: https://www.gnu.org/licenses/gpl-3.0.html.
**
** $QT_END_LICENSE$
**
****************************************************************************/

using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CSharp;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace QtVsTest.Macros
{
    /// <summary>
    /// Macros are snippets of C# code provided by a test client at runtime. They are compiled
    /// on-the-fly and may run once after compilation or stored and reused later by other macros.
    /// Macros may also include special statements in comment lines starting with '//#'. These will
    /// be expanded into the corresponding code ahead of C# compilation.
    /// </summary>
    class Macro
    {
        /// <summary>
        /// Global variable, shared between macros
        /// </summary>
        class GlobalVar
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string InitialValueExpr { get; set; }
            public FieldInfo FieldInfo { get; set; }
            public PropertyInfo InitInfo { get; set; }
        }

        /// <summary>
        /// Reference to Visual Studio SDK service
        /// </summary>
        class VSServiceRef
        {
            public string Name { get; set; }
            public string Interface { get; set; }
            public string Type { get; set; }
            public FieldInfo RefVar { get; set; }
            public Type ServiceType { get; set; }
        }

        /// <summary>
        /// Name of reusable macro
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// True if macro compilation was successful
        /// </summary>
        public bool Ok { get; private set; }

        /// <summary>
        /// Result of macro compilation and execution
        /// </summary>
        public string Result { get; private set; }

        /// <summary>
        /// True if macro will run immediately after compilation
        /// </summary>
        public bool AutoRun { get; private set; }

        /// <summary>
        /// True if Visual Studio should be closed after macro execution
        /// </summary>
        public bool QuitWhenDone { get; private set; }

        AsyncPackage Package { get; set; }
        JoinableTaskFactory JoinableTaskFactory { get; set; }
        CancellationToken ServerLoop { get; set; }

        string Message { get; set; }

        static MacroParser Parser { get; set; }
        MacroLines MacroLines { get; set; }

        IEnumerable<string> RefAssemblies { get; set; }
        IEnumerable<string> RefNamespaces { get; set; }
        string CSharpMethodCode { get; set; }
        string CSharpClassCode { get; set; }

        IEnumerable<GlobalVar> GlobalVars { get; set; }
        IEnumerable<VSServiceRef> VSServiceRefs { get; set; }

        CompilerResults CompilerResults { get; set; }
        Type Type { get; set; }
        FieldInfo ResultField { get; set; }
        Func<Task> Run { get; set; }

        readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
        const BindingFlags PUBLIC_STATIC = BindingFlags.Public | BindingFlags.Static;
        const string BR = "\r\n";

        static ConcurrentDictionary<string, Macro> Macros
            = new ConcurrentDictionary<string, Macro>();


        /// <summary>
        /// Macro constructor
        /// </summary>
        /// <param name="package">QtVSTest extension package</param>
        /// <param name="joinableTaskFactory">Task factory, enables joining with UI thread</param>
        /// <param name="serverLoop">Server loop cancellation token</param>
        public Macro(
            AsyncPackage package,
            JoinableTaskFactory joinableTaskFactory,
            CancellationToken serverLoop)
        {
            Package = package;
            JoinableTaskFactory = joinableTaskFactory;
            ServerLoop = serverLoop;
            Error("Uninitialized");
        }

        /// <summary>
        /// Compile macro code
        /// </summary>
        /// <param name="msg">Message from client containing macro code</param>
        public async Task<bool> CompileAsync(string msg)
        {
            if (MacroLines != null)
                return Warning("Macro already compiled");

            try {
                Message = msg;

                if (!ParseMessage())
                    return false;

                if (!CompileMacro())
                    return false;

                if (string.IsNullOrEmpty(CSharpMethodCode))
                    return true;

                if (!CompileClass())
                    return false;

                await GetServicesAsync();

                return true;
            } catch (Exception e) {
                return Error(e);
            }
        }

        /// <summary>
        /// Run macro
        /// </summary>
        public async Task RunAsync()
        {
            if (!Ok)
                return;

            try {
                InitGlobalVars();
                await Run();
                await SwitchToWorkerThreadAsync();
                Result = ResultField.GetValue(null) as string;
            } catch (Exception e) {
                Error(e);
            }
        }

        /// <summary>
        /// Parse message text into sequence of macro statements
        /// </summary>
        /// <returns></returns>
        bool ParseMessage()
        {
            if (Parser == null) {
                var parser = MacroParser.Get();
                if (parser == null)
                    return Error("Parser error");
                Parser = parser;
            }

            var macroLines = Parser.Parse(Message);
            if (macroLines == null)
                return Error("Parse error");

            MacroLines = macroLines;

            return NoError();
        }

        /// <summary>
        /// Expand macro statements into C# code
        /// </summary>
        /// <returns></returns>
        bool CompileMacro()
        {
            const StringComparison IGNORE_CASE = StringComparison.InvariantCultureIgnoreCase;

            var selectedAssemblies = new List<string>(MSBuild.MetaInfo.QtVsTest.Reference)
            {
                ExecutingAssembly.FullName,
                "System.Core",
                "EnvDTE",
                "EnvDTE80",
            };

            var selectedNamespaces = new List<string>
            {
                "System",
                "Task = System.Threading.Tasks.Task",
                "System.Linq",
                "System.Reflection",
                "EnvDTE",
                "EnvDTE80",
            };

            string macroName = string.Empty;
            var serviceRefs = new Dictionary<string, VSServiceRef> {
                    { "DTE", new VSServiceRef {
                        Name = "DTE",
                        Interface = "DTE2",
                        Type = "DTE",
                    }},
                };
            var globalVars = new Dictionary<string, GlobalVar>
                {
                    { "Result", new GlobalVar {
                        Type = "string",
                        Name = "Result",
                        InitialValueExpr = "string.Empty"
                    }},
                };
            var csharpCode = new StringBuilder();
            bool quitWhenDone = false;

            foreach (var line in MacroLines) {
                if (quitWhenDone)
                    return Error("No code allowed after #quit");

                if (line is CodeLine) {
                    var codeLine = line as CodeLine;
                    csharpCode.Append(codeLine.Code);
                    continue;
                }

                var s = line as Statement;
                switch (s.Type) {

                    case StatementType.Quit:
                        quitWhenDone = true;
                        break;

                    case StatementType.Macro:
                        if (csharpCode.Length > 0)
                            return Error("#macro must be first statement");
                        if (!string.IsNullOrEmpty(macroName))
                            return Error("Only one #macro statement allowed");
                        if (s.Args.Count < 1)
                            return Error("Missing macro name");
                        macroName = s.Args[0];
                        break;

                    case StatementType.Thread:
                        if (s.Args.Count < 1)
                            return Error("Missing thread id");
                        if (s.Args[0].Equals("ui", IGNORE_CASE))
                            csharpCode.Append("await SwitchToUIThread();");
                        else if (s.Args[0].Equals("default", IGNORE_CASE))
                            csharpCode.Append("await SwitchToWorkerThread();");
                        else
                            return Error("Unknown thread id");
                        break;

                    case StatementType.Reference:
                        if (!s.Args.Any())
                            return Error("Missing args for #reference");
                        selectedAssemblies.Add(s.Args.First());
                        foreach (var ns in s.Args.Skip(1))
                            selectedNamespaces.Add(ns);
                        break;

                    case StatementType.Using:
                        if (!s.Args.Any())
                            return Error("Missing args for #using");
                        foreach (var ns in s.Args)
                            selectedNamespaces.Add(ns);
                        break;

                    case StatementType.Var:
                        if (s.Args.Count < 2)
                            return Error("Missing args for #var");
                        var typeName = s.Args[0];
                        var varName = s.Args[1];
                        var initValue = s.Code;
                        if (varName.Where(c => char.IsWhiteSpace(c)).Any())
                            return Error("Wrong var name");
                        globalVars[varName] = new GlobalVar
                        {
                            Type = typeName,
                            Name = varName,
                            InitialValueExpr = initValue
                        };
                        break;

                    case StatementType.Service:
                        if (s.Args.Count <= 1)
                            return Error("Missing args for #service");
                        var serviceVarName = s.Args[0];
                        if (serviceVarName.Where(c => char.IsWhiteSpace(c)).Any())
                            return Error("Wrong service var name");
                        if (serviceRefs.ContainsKey(serviceVarName))
                            return Error("Duplicate service var name");
                        serviceRefs.Add(serviceVarName, new VSServiceRef
                        {
                            Name = serviceVarName,
                            Interface = s.Args[1],
                            Type = s.Args.Count > 2 ? s.Args[2] : s.Args[1]
                        });
                        break;

                    case StatementType.Call:
                        if (s.Args.Count < 1)
                            return Error("Missing args for #call");
                        var calleeName = s.Args[0];
                        var callee = GetMacro(calleeName);
                        if (callee == null)
                            return Error("Undefined macro");
                        csharpCode.AppendFormat("await CallMacro(\"{0}\");", calleeName);
                        foreach (var globalVar in callee.GlobalVars) {
                            if (globalVars.ContainsKey(globalVar.Name))
                                continue;
                            globalVars[globalVar.Name] = new GlobalVar
                            {
                                Type = globalVar.Type,
                                Name = globalVar.Name
                            };
                        }
                        break;

                    case StatementType.Wait:
                        if (string.IsNullOrEmpty(s.Code))
                            return Error("Missing args for #wait");
                        var expr = s.Code;
                        uint timeout = uint.MaxValue;
                        if (s.Args.Count > 0 && !uint.TryParse(s.Args[0], out timeout))
                            return Error("Timeout format error in #wait");
                        if (s.Args.Count > 2) {
                            var evalVarType = s.Args[1];
                            var evalVarName = s.Args[2];
                            csharpCode.AppendFormat(
                                "{0} {1} = default({0});" +
                                "await WaitExpr({2}, () => {1} = {3});",
                                evalVarType, evalVarName, timeout, expr);
                        } else {
                            csharpCode.AppendFormat(
                                "await WaitExpr({0}, () => {1});",
                                timeout, expr);
                        }
                        break;
                }
            }

            if (csharpCode.Length > 0)
                CSharpMethodCode = csharpCode.ToString();

            if (string.IsNullOrEmpty(macroName))
                Name = "Macro_" + Path.GetRandomFileName().Replace(".", "");
            else if (!SaveMacro(macroName))
                return Error("Macro already defined");

            GlobalVars = globalVars.Values;
            VSServiceRefs = serviceRefs.Values;
            foreach (var sv in VSServiceRefs.Where(x => string.IsNullOrEmpty(x.Type)))
                sv.Type = sv.Interface;

            AutoRun = string.IsNullOrEmpty(macroName);
            QuitWhenDone = quitWhenDone;

            var selectedAssemblyNames = selectedAssemblies
                .Select(x => new AssemblyName(x))
                .GroupBy(x => x.FullName)
                .Select(x => x.First());

            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .GroupBy(x => x.GetName().Name)
                .ToDictionary(x => x.Key, x => x.AsEnumerable(),
                    StringComparer.InvariantCultureIgnoreCase);

            var refAssemblies = selectedAssemblyNames
                .GroupBy(x => allAssemblies.ContainsKey(x.Name))
                .SelectMany(x => x.Key
                    ? x.SelectMany(y => allAssemblies[y.Name])
                    : x.Select(y =>
                    {
                        try {
                            return Assembly.Load(y);
                        } catch {
                            return null;
                        }
                    }));

            RefAssemblies = refAssemblies
                .Where(x => x != null)
                .Select(x => x.Location);

            RefNamespaces = selectedNamespaces;

            return NoError();
        }

        /// <summary>
        /// Generate and compile C# class for macro
        /// </summary>
        /// <returns></returns>
        bool CompileClass()
        {
            var methodName = string.Format("_Run_{0}_Async", Name);
            const string serviceTypePrefix = "_ServiceType_";
            const string initPrefix = "_Init_";

            var csharpClass = new StringBuilder();
            foreach (var ns in RefNamespaces)
                csharpClass.AppendFormat("using {0};", ns);
            csharpClass.Append("namespace QtVsTest.Macros");
            csharpClass.Append("{");
            csharpClass.AppendFormat("public class {0}", Name);
            csharpClass.Append("{");
            foreach (var serviceRef in VSServiceRefs) {
                csharpClass.AppendFormat("public static {0} {1};",
                    serviceRef.Interface, serviceRef.Name);
                csharpClass.AppendFormat("public static readonly Type {0}{1} = typeof({2});",
                    serviceTypePrefix, serviceRef.Name, serviceRef.Type);
            }
            foreach (var globalVar in GlobalVars) {
                csharpClass.AppendFormat("public static {0} {1};", globalVar.Type, globalVar.Name);
                csharpClass.AppendFormat(
                    "public static {0} {1}{2} {{ get {{ return ({3}); }} }}",
                    globalVar.Type, initPrefix, globalVar.Name, globalVar.InitialValueExpr);
            }
            csharpClass.Append("public static Func<string, Assembly> GetAssembly;");
            csharpClass.Append("public static Func<Task> SwitchToUIThread;");
            csharpClass.Append("public static Func<Task> SwitchToWorkerThread;");
            csharpClass.Append("public static Func<string, Task> CallMacro;");
            csharpClass.Append("public static Func<int, Func<object>, Task> WaitExpr;");
            csharpClass.AppendFormat("public static async Task {0}()", methodName);
            csharpClass.Append("{" + CSharpMethodCode + "}");
            csharpClass.Append("} /*class*/ } /*namespace*/");

            CSharpClassCode = csharpClass.ToString();

            var dllUri = new Uri(ExecutingAssembly.EscapedCodeBase);
            var dllPath = Uri.UnescapeDataString(dllUri.AbsolutePath);
            var macroDllPath = Path.Combine(Path.GetDirectoryName(dllPath), Name + ".dll");

            if (File.Exists(macroDllPath))
                File.Delete(macroDllPath);

            var cscParams = new CompilerParameters()
            {
                GenerateInMemory = false,
                OutputAssembly = macroDllPath
            };
            cscParams.ReferencedAssemblies.AddRange(RefAssemblies.ToArray());

            var cSharpProvider = new CSharpCodeProvider();

            CompilerResults = cSharpProvider.CompileAssemblyFromSource(cscParams, CSharpClassCode);
            if (CompilerResults.Errors.Count > 0) {
                if (File.Exists(macroDllPath))
                    File.Delete(macroDllPath);
                return Error(string.Join(BR,
                    CompilerResults.Errors.Cast<CompilerError>()
                        .Select(x => x.ErrorText)));
            }

            var assembly = AppDomain.CurrentDomain.Load(File.ReadAllBytes(macroDllPath));
            var type = assembly.GetType(string.Format("QtVsTest.Macros.{0}", Name));

            if (File.Exists(macroDllPath))
                File.Delete(macroDllPath);

            foreach (var serviceVar in VSServiceRefs) {
                serviceVar.RefVar = type.GetField(serviceVar.Name, PUBLIC_STATIC);
                var serviceType = type.GetField(serviceTypePrefix + serviceVar.Name, PUBLIC_STATIC);
                serviceVar.ServiceType = (Type)serviceType.GetValue(null);
            }

            ResultField = type.GetField("Result", PUBLIC_STATIC);
            foreach (var globalVar in GlobalVars) {
                globalVar.FieldInfo = type.GetField(globalVar.Name, PUBLIC_STATIC);
                globalVar.InitInfo = type.GetProperty(initPrefix + globalVar.Name, PUBLIC_STATIC);
            }

            Run = (Func<Task>)Delegate.CreateDelegate(typeof(Func<Task>),
                type.GetMethod(methodName, PUBLIC_STATIC));

            type.GetField("GetAssembly", PUBLIC_STATIC)
                .SetValue(null, new Func<string, Assembly>(GetAssembly));

            type.GetField("SwitchToUIThread", PUBLIC_STATIC)
                .SetValue(null, new Func<Task>(SwitchToUIThreadAsync));

            type.GetField("SwitchToWorkerThread", PUBLIC_STATIC)
                .SetValue(null, new Func<Task>(SwitchToWorkerThreadAsync));

            type.GetField("CallMacro", PUBLIC_STATIC)
                .SetValue(null, new Func<string, Task>(CallMacroAsync));

            type.GetField("WaitExpr", PUBLIC_STATIC)
                .SetValue(null, new Func<int, Func<object>, Task>(WaitExprAsync));

            return NoError();
        }

        public static Assembly GetAssembly(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => x.GetName().Name == name)
                .FirstOrDefault();
        }

        public async Task SwitchToUIThreadAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(ServerLoop);
        }

        public async Task SwitchToWorkerThreadAsync()
        {
            await TaskScheduler.Default;
        }

        public async Task CallMacroAsync(string macroName)
        {
            var callee = GetMacro(macroName);
            if (callee == null)
                throw new FileNotFoundException("Unknown macro");

            callee.InitGlobalVars();
            callee.CopyGlobalVarsFrom(this);
            await callee.Run();
            CopyGlobalVarsFrom(callee);
        }

        public async Task WaitExprAsync(int timeout, Func<object> expr)
        {
            var tMax = TimeSpan.FromMilliseconds(timeout);
            var tRemaining = tMax;
            var t = Stopwatch.StartNew();
            object value = await Task.Run(() => expr()).WithTimeout(tRemaining);
            bool ok = !IsDefaultValue(value);

            while (!ok && (tRemaining = (tMax - t.Elapsed)) > TimeSpan.Zero) {
                await Task.Delay(10);
                value = await Task.Run(() => expr()).WithTimeout(tRemaining);
                ok = !IsDefaultValue(value);
            }

            if (!ok)
                throw new TimeoutException();
        }

        bool IsDefaultValue(object obj)
        {
            if (obj == null)
                return true;
            else if (obj.GetType().IsValueType)
                return obj == Activator.CreateInstance(obj.GetType());
            else
                return false;
        }

        void InitGlobalVars()
        {
            var globalVarsInit = GlobalVars
                .Where(x => x.FieldInfo != null && !string.IsNullOrEmpty(x.InitialValueExpr));
            foreach (var globalVar in globalVarsInit)
                globalVar.FieldInfo.SetValue(null, globalVar.InitInfo.GetValue(null));
        }

        void CopyGlobalVarsFrom(Macro src)
        {
            var globalVars = GlobalVars
                .Join(src.GlobalVars,
                    DstVar => DstVar.Name, SrcVar => SrcVar.Name,
                    (DstVar, SrcVar) => new { DstVar, SrcVar })
                .Where(x => x.SrcVar.FieldInfo != null && x.DstVar.FieldInfo != null
                    && x.DstVar.FieldInfo.FieldType
                        .IsAssignableFrom(x.SrcVar.FieldInfo.FieldType));

            foreach (var globalVar in globalVars) {
                globalVar.DstVar.FieldInfo
                    .SetValue(null, globalVar.SrcVar.FieldInfo.GetValue(null));
            }
        }

        async Task<bool> GetServicesAsync()
        {
            foreach (var serviceRef in VSServiceRefs.Where(x => x.RefVar != null)) {
                serviceRef.RefVar.SetValue(null,
                    await Package.GetServiceAsync(serviceRef.ServiceType));
            }
            return await Task.FromResult(NoError());
        }

        bool SaveMacro(string name)
        {
            if (Macros.ContainsKey(name))
                return false;
            return Macros.TryAdd(Name = name, this);
        }

        static Macro GetMacro(string name)
        {
            Macro macro;
            if (!Macros.TryGetValue(name, out macro))
                return null;
            return macro;
        }

        bool NoError()
        {
            Result = "(ok)";
            return (Ok = true);
        }

        bool Error()
        {
            Result = "(error)";
            return (Ok = false);
        }

        bool Error(string errorMsg)
        {
            Result = "(error)" + BR + errorMsg;
            return (Ok = false);
        }

        bool Error(Exception e)
        {
            Result = string.Format("(error)" + BR +
                "{0}" + BR + "\"{1}\"" + BR + "{2}",
                e.GetType().Name, e.Message, e.StackTrace);
            return (Ok = false);
        }

        bool Warning(string warnMsg)
        {
            Result = "(warn)" + BR + warnMsg;
            return (Ok = true);
        }
    }
}
