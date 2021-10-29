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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
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
            public bool IsCallOutput { get; set; }
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
        EnvDTE80.DTE2 Dte { get; set; }

        AutomationElement UiRoot => AutomationElement.RootElement;

        AutomationElement _UiVsRoot;
        AutomationElement UiVsRoot
        {
            get
            {
                if (_UiVsRoot == null)
#if VS2022
                    _UiVsRoot = AutomationElement.FromHandle(Dte.MainWindow.HWnd);
#else
                    _UiVsRoot = AutomationElement.FromHandle(new IntPtr(Dte.MainWindow.HWnd));
#endif
                return _UiVsRoot;
            }
        }

        JoinableTaskFactory JoinableTaskFactory { get; set; }
        CancellationToken ServerLoop { get; set; }

        string Message { get; set; }

        static MacroParser Parser { get; set; }
        MacroLines MacroLines { get; set; }

        List<string> SelectedAssemblies { get { return _SelectedAssemblies; } }
        List<string> _SelectedAssemblies =
            new List<string>
            {
                "QtVsTest",
                "System.Core",
            };

        IEnumerable<string> RefAssemblies { get; set; }

        List<string> Namespaces { get { return _Namespaces; } }
        List<string> _Namespaces =
            new List<string>
            {
                "System",
                "System.Linq",
                "System.Reflection",
                "Task = System.Threading.Tasks.Task",
                "System.Windows.Automation",
                "EnvDTE",
                "EnvDTE80",
            };

        Dictionary<string, VSServiceRef> ServiceRefs { get { return _ServiceRefs; } }
        Dictionary<string, VSServiceRef> _ServiceRefs =
            new Dictionary<string, VSServiceRef>
            {
                {
                    "Dte", new VSServiceRef
                    { Name = "Dte", Interface = "DTE2", Type = "DTE" }
                },
            };

        Dictionary<string, GlobalVar> GlobalVars { get { return _GlobalVars; } }
        Dictionary<string, GlobalVar> _GlobalVars =
            new Dictionary<string, GlobalVar>
            {
                {
                    "Result", new GlobalVar
                    { Type = "string", Name = "Result", InitialValueExpr = "string.Empty" }
                },
            };

        string CSharpMethodCode { get; set; }
        string CSharpClassCode { get; set; }

        CompilerResults CompilerResults { get; set; }
        Assembly MacroAssembly { get; set; }
        Type MacroClass { get; set; }
        FieldInfo ResultField { get; set; }
        Func<Task> Run { get; set; }

        const BindingFlags PUBLIC_STATIC = BindingFlags.Public | BindingFlags.Static;
        const StringComparison IGNORE_CASE = StringComparison.InvariantCultureIgnoreCase;

        static ConcurrentDictionary<string, Macro> Macros
            = new ConcurrentDictionary<string, Macro>();

        static ConcurrentDictionary<string, object> Globals
            = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Macro constructor
        /// </summary>
        /// <param name="package">QtVSTest extension package</param>
        /// <param name="joinableTaskFactory">Task factory, enables joining with UI thread</param>
        /// <param name="serverLoop">Server loop cancellation token</param>
        public Macro(
            AsyncPackage package,
            EnvDTE80.DTE2 dte,
            JoinableTaskFactory joinableTaskFactory,
            CancellationToken serverLoop)
        {
            Package = package;
            JoinableTaskFactory = joinableTaskFactory;
            ServerLoop = serverLoop;
            Dte = dte;
            ErrorMsg("Uninitialized");
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

                if (!CompileClass())
                    return false;

                await GetServicesAsync();

                return true;
            } catch (Exception e) {
                return ErrorException(e);
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
                InitGlobals();
                await Run();
                await SwitchToWorkerThreadAsync();
                Result = ResultField.GetValue(null) as string;
                if (string.IsNullOrEmpty(Result))
                    Result = MACRO_OK;
            } catch (Exception e) {
                ErrorException(e);
            }
            UpdateGlobals();
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
                    return ErrorMsg("Parser error");
                Parser = parser;
            }

            var macroLines = Parser.Parse(Message);
            if (macroLines == null)
                return ErrorMsg("Parse error");

            MacroLines = macroLines;

            return NoError();
        }

        /// <summary>
        /// Expand macro statements into C# code
        /// </summary>
        /// <returns></returns>
        bool CompileMacro()
        {
            if (UiVsRoot == null)
                return ErrorMsg("UI Automation not available");

            var csharp = new StringBuilder();

            foreach (var line in MacroLines) {
                if (QuitWhenDone)
                    return ErrorMsg("No code allowed after #quit");

                if (line is CodeLine) {
                    var codeLine = line as CodeLine;
                    csharp.Append(codeLine.Code + "\r\n");
                    continue;
                }

                if (!GenerateStatement(line as Statement, csharp))
                    return false;
            }

            if (csharp.Length > 0)
                CSharpMethodCode = csharp.ToString();

            AutoRun = string.IsNullOrEmpty(Name);
            if (AutoRun)
                Name = "Macro_" + Path.GetRandomFileName().Replace(".", "");
            else if (!SaveMacro(Name))
                return ErrorMsg("Macro already defined");

            foreach (var sv in ServiceRefs.Values.Where(x => string.IsNullOrEmpty(x.Type)))
                sv.Type = sv.Interface;

            var selectedAssemblyNames = SelectedAssemblies
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

            return NoError();
        }

        bool GenerateStatement(Statement s, StringBuilder csharp)
        {
            switch (s.Type) {

                case StatementType.Quit:
                    QuitWhenDone = true;
                    break;

                case StatementType.Macro:
                    if (csharp.Length > 0)
                        return ErrorMsg("#macro must be first statement");
                    if (!string.IsNullOrEmpty(Name))
                        return ErrorMsg("Only one #macro statement allowed");
                    if (s.Args.Count < 1)
                        return ErrorMsg("Missing macro name");
                    Name = s.Args[0];
                    break;

                case StatementType.Thread:
                    if (s.Args.Count < 1)
                        return ErrorMsg("Missing thread id");
                    if (s.Args[0].Equals("ui", IGNORE_CASE)) {

                        csharp.Append(
/** BEGIN generate code **/
@"
            await SwitchToUIThread();"
/** END generate code **/ );

                    } else if (s.Args[0].Equals("default", IGNORE_CASE)) {

                        csharp.Append(
/** BEGIN generate code **/
@"
            await SwitchToWorkerThread();"
/** END generate code **/ );

                    } else {
                        return ErrorMsg("Unknown thread id");
                    }
                    break;

                case StatementType.Reference:
                    if (!s.Args.Any())
                        return ErrorMsg("Missing args for #reference");
                    SelectedAssemblies.Add(s.Args.First());
                    foreach (var ns in s.Args.Skip(1))
                        Namespaces.Add(ns);
                    break;

                case StatementType.Using:
                    if (!s.Args.Any())
                        return ErrorMsg("Missing args for #using");
                    foreach (var ns in s.Args)
                        Namespaces.Add(ns);
                    break;

                case StatementType.Var:
                    if (s.Args.Count < 1)
                        return ErrorMsg("Missing args for #var");
                    string typeName, varName;
                    if (s.Args.Count == 1) {
                        typeName = "object";
                        varName = s.Args[0];
                    } else {
                        typeName = s.Args[0];
                        varName = s.Args[1];
                    }
                    var initValue = s.Code;
                    if (varName.Where(c => char.IsWhiteSpace(c)).Any())
                        return ErrorMsg("Wrong var name");
                    GlobalVars[varName] = new GlobalVar
                    {
                        Type = typeName,
                        Name = varName,
                        InitialValueExpr = initValue
                    };
                    break;

                case StatementType.Service:
                    if (s.Args.Count <= 1)
                        return ErrorMsg("Missing args for #service");
                    var serviceVarName = s.Args[0];
                    if (serviceVarName.Where(c => char.IsWhiteSpace(c)).Any())
                        return ErrorMsg("Invalid service var name");
                    if (ServiceRefs.ContainsKey(serviceVarName))
                        return ErrorMsg("Duplicate service var name");
                    ServiceRefs.Add(serviceVarName, new VSServiceRef
                    {
                        Name = serviceVarName,
                        Interface = s.Args[1],
                        Type = s.Args.Count > 2 ? s.Args[2] : s.Args[1]
                    });
                    break;

                case StatementType.Call:
                    if (s.Args.Count < 1)
                        return ErrorMsg("Missing args for #call");
                    var calleeName = s.Args[0];
                    var callee = GetMacro(calleeName);
                    if (callee == null)
                        return ErrorMsg("Undefined macro");

                    csharp.AppendFormat(
/** BEGIN generate code **/
@"
            await CallMacro(""{0}"");"
/** END generate code **/ , calleeName);

                    foreach (var globalVar in callee.GlobalVars.Values) {
                        if (GlobalVars.ContainsKey(globalVar.Name))
                            continue;
                        GlobalVars[globalVar.Name] = new GlobalVar
                        {
                            Type = globalVar.Type,
                            Name = globalVar.Name,
                            IsCallOutput = true
                        };
                    }
                    break;

                case StatementType.Wait:
                    if (string.IsNullOrEmpty(s.Code))
                        return ErrorMsg("Missing args for #wait");
                    var expr = s.Code;
                    uint timeout = uint.MaxValue;
                    if (s.Args.Count > 0 && !uint.TryParse(s.Args[0], out timeout))
                        return ErrorMsg("Timeout format error in #wait");
                    if (s.Args.Count > 2) {
                        var evalVarType = s.Args[1];
                        var evalVarName = s.Args[2];

                        csharp.AppendFormat(
/** BEGIN generate code **/
@"
            {0} {1} = default({0});
            await WaitExpr({2}, () => {1} = {3});"
/** END generate code **/ , evalVarType,
                            evalVarName,
                            timeout,
                            expr);

                    } else {

                        csharp.AppendFormat(
/** BEGIN generate code **/
@"
            await WaitExpr({0}, () => {1});"
/** END generate code **/ , timeout,
                            expr);

                    }
                    break;

                case StatementType.Ui:
                    if (!GenerateUiStatement(s, csharp))
                        return false;
                    break;
            }
            return true;
        }

        public AutomationElement UiFind(AutomationElement uiContext, object[] path)
        {
            var uiIterator = uiContext;
            foreach (var item in path) {
                var itemType = item.GetType();
                var scope = (uiIterator == UiRoot) ? TreeScope.Children : TreeScope.Subtree;
                if (itemType.IsAssignableFrom(typeof(string))) {
                    // Find element by name
                    var name = (string)item;
                    uiIterator = uiIterator.FindFirst(scope,
                        new PropertyCondition(AutomationElement.NameProperty, name));
                } else if (itemType.IsAssignableFrom(typeof(string[]))) {
                    // Find element by name and type
                    var itemParams = (string[])item;
                    uiIterator = uiIterator.FindFirst(scope,
                        new AndCondition(itemParams.Select((x, i) =>
                        (i == 0) ? new PropertyCondition(
                            AutomationElement.NameProperty, x) :
                        (i == 1) ? new PropertyCondition(
                            AutomationElement.LocalizedControlTypeProperty, x) :
                        (i == 2) ? new PropertyCondition(
                            AutomationElement.AutomationIdProperty, x) :
                        Condition.FalseCondition).ToArray()));
                }
                if (uiIterator == null)
                    throw new Exception(
                        string.Format("Could not find UI element \"{0}\"", item));
            }
            return uiIterator;
        }

        static readonly IEnumerable<string> UI_TYPES = new[]
        {
            "Dock", "ExpandCollapse", "GridItem", "Grid", "Invoke", "MultipleView", "RangeValue",
            "Scroll", "ScrollItem", "Selection", "SelectionItem", "SynchronizedInput", "Text",
            "Transform", "Toggle", "Value", "Window", "VirtualizedItem", "ItemContainer"
        };

        bool GenerateUiGlobals(StringBuilder csharp)
        {
            csharp.Append(@"
        public static Func<AutomationElement, object[], AutomationElement> UiFind;
        public static AutomationElement UiRoot;
        public static AutomationElement UiVsRoot;
        public static AutomationElement UiContext;");
            return true;
        }

        bool InitializeUiGlobals()
        {
            if (MacroClass == null)
                return false;

            MacroClass.GetField("UiFind", PUBLIC_STATIC)
                .SetValue(null, new Func<AutomationElement, object[], AutomationElement>(UiFind));

            MacroClass.GetField("UiRoot", PUBLIC_STATIC)
                .SetValue(null, UiRoot);

            MacroClass.GetField("UiVsRoot", PUBLIC_STATIC)
                .SetValue(null, UiVsRoot);

            MacroClass.GetField("UiContext", PUBLIC_STATIC)
                .SetValue(null, UiVsRoot);

            return true;
        }

        bool GenerateUiStatement(Statement s, StringBuilder csharp)
        {
            if (s.Args.Count == 0)
                return ErrorMsg("Invalid #ui statement");

            if (s.Args[0].Equals("context", IGNORE_CASE)) {
                //# ui context [ VSROOT | DESKTOP ] [_int_] => _string_ [, _string_, ... ]
                //# ui context HWND [_int_] => _int_

                if (s.Args.Count > 3 || string.IsNullOrEmpty(s.Code))
                    return ErrorMsg("Invalid #ui statement");

                bool uiVsRoot = (s.Args.Count > 1 && s.Args[1] == "VSROOT");
                bool uiDesktop = (s.Args.Count > 1 && s.Args[1] == "DESKTOP");
                bool uiHwnd = (s.Args.Count > 1 && s.Args[1] == "HWND");

                string context;
                if (uiVsRoot)
                    context = string.Format("UiFind(UiVsRoot, new object[] {{ {0} }})", s.Code);
                else if (uiDesktop)
                    context = string.Format("UiFind(UiRoot, new object[] {{ {0} }})", s.Code);
                else if (uiHwnd)
                    context = string.Format("AutomationElement.FromHandle((IntPtr)({0}))", s.Code);
                else
                    context = string.Format("UiFind(UiContext, new object[] {{ {0} }})", s.Code);

                int timeout = 3000;
                if (s.Args.Count > 1 && !uiVsRoot && !uiDesktop && !uiHwnd)
                    timeout = int.Parse(s.Args[1]);
                else if (s.Args.Count > 2)
                    timeout = int.Parse(s.Args[2]);

                csharp.AppendFormat(@"
                    await WaitExpr({0}, () => UiContext = {1});", timeout, context);

            } else if (s.Args[0].Equals("pattern", IGNORE_CASE)) {
                //# ui pattern <_TypeName_> <_VarName_> [ => _string_ [, _string_, ... ] ]
                //# ui pattern Invoke [ => _string_ [, _string_, ... ] ]
                //# ui pattern Toggle [ => _string_ [, _string_, ... ] ]

                if (s.Args.Count < 2)
                    return ErrorMsg("Invalid #ui statement");

                string typeName = s.Args[1];
                string varName = (s.Args.Count > 2) ? s.Args[2] : string.Empty;
                if (!UI_TYPES.Contains(typeName))
                    return ErrorMsg("Invalid #ui statement");

                string uiElement;
                if (!string.IsNullOrEmpty(s.Code))
                    uiElement = string.Format("UiFind(UiContext, new object[] {{ {0} }})", s.Code);
                else
                    uiElement = "UiContext";

                string patternTypeId = string.Format("{0}PatternIdentifiers.Pattern", typeName);
                string patternType = string.Format("{0}Pattern", typeName);

                if (!string.IsNullOrEmpty(varName)) {

                    csharp.AppendFormat(@"
                            var {0} = {1}.GetCurrentPattern({2}) as {3};",
                        varName,
                        uiElement,
                        patternTypeId,
                        patternType);

                } else if (typeName == "Invoke" || typeName == "Toggle") {

                    csharp.AppendFormat(@"
                            ({0}.GetCurrentPattern({1}) as {2}).{3}();",
                        uiElement,
                        patternTypeId,
                        patternType,
                        typeName);

                } else {
                    return ErrorMsg("Invalid #ui statement");
                }

            } else {
                return ErrorMsg("Invalid #ui statement");
            }

            return true;
        }

        const string SERVICETYPE_PREFIX = "_ServiceType_";
        const string INIT_PREFIX = "_Init_";
        string MethodName { get { return string.Format("_Run_{0}_Async", Name); } }

        bool GenerateClass()
        {
            var csharp = new StringBuilder();
            foreach (var ns in Namespaces) {
                csharp.AppendFormat(
/** BEGIN generate code **/
@"
using {0};"
/** END generate code **/ , ns);
            }

            csharp.AppendFormat(
/** BEGIN generate code **/
@"
namespace QtVsTest.Macros
{{
    public class {0}
    {{"
/** END generate code **/ , Name);

            foreach (var serviceRef in ServiceRefs.Values) {
                csharp.AppendFormat(
/** BEGIN generate code **/
@"
        public static {2} {1};
        public static readonly Type {0}{1} = typeof({3});"
/** END generate code **/ , SERVICETYPE_PREFIX,
                            serviceRef.Name,
                            serviceRef.Interface,
                            serviceRef.Type);
            }

            foreach (var globalVar in GlobalVars.Values) {
                csharp.AppendFormat(
/** BEGIN generate code **/
@"
        public static {1} {2};
        public static {1} {0}{2} {{ get {{ return ({3}); }} }}"
/** END generate code **/ , INIT_PREFIX,
                            globalVar.Type,
                            globalVar.Name,
                            !string.IsNullOrEmpty(globalVar.InitialValueExpr)
                                ? globalVar.InitialValueExpr
                                : string.Format("default({0})", globalVar.Type));
            }

            csharp.Append(
/** BEGIN generate code **/
@"
        static string MACRO_OK { get { return ""(ok)""; } }
        static string MACRO_ERROR { get { return ""(error)""; } }
        static string MACRO_WARN { get { return ""(warn)""; } }
        static string MACRO_ERROR_MSG(string msg)
            { return string.Format(""{0}\r\n{1}"", MACRO_ERROR, msg); }
        static string MACRO_WARN_MSG(string msg)
            { return string.Format(""{0}\r\n{1}"", MACRO_WARN, msg); }
        public static Func<string, Assembly> GetAssembly;
        public static Func<Task> SwitchToUIThread;
        public static Func<Task> SwitchToWorkerThread;
        public static Func<string, Task> CallMacro;
        public static Func<int, Func<object>, Task> WaitExpr;"
/** END generate code **/ );

            if (!GenerateResultFuncs(csharp))
                return false;

            if (!GenerateUiGlobals(csharp))
                return false;

            csharp.AppendFormat(
/** BEGIN generate code **/
@"
        public static async Task {0}()
        {{
{1}
        }}

    }} /*class*/

}} /*namespace*/"
/** END generate code **/ , MethodName,
                            CSharpMethodCode);

            CSharpClassCode = csharp.ToString();

            return true;
        }

        /// <summary>
        /// Generate and compile C# class for macro
        /// </summary>
        /// <returns></returns>
        bool CompileClass()
        {
            if (!GenerateClass())
                return false;

            var dllUri = new Uri(Assembly.GetExecutingAssembly().EscapedCodeBase);
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
                return ErrorMsg(string.Join("\r\n",
                    CompilerResults.Errors.Cast<CompilerError>()
                        .Select(x => x.ErrorText)));
            }

            MacroAssembly = AppDomain.CurrentDomain.Load(File.ReadAllBytes(macroDllPath));
            MacroClass = MacroAssembly.GetType(string.Format("QtVsTest.Macros.{0}", Name));
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            if (File.Exists(macroDllPath))
                File.Delete(macroDllPath);

            foreach (var serviceVar in ServiceRefs.Values) {
                serviceVar.RefVar = MacroClass.GetField(serviceVar.Name, PUBLIC_STATIC);
                var serviceType = MacroClass.GetField(SERVICETYPE_PREFIX + serviceVar.Name, PUBLIC_STATIC);
                serviceVar.ServiceType = (Type)serviceType.GetValue(null);
            }

            ResultField = MacroClass.GetField("Result", PUBLIC_STATIC);
            foreach (var globalVar in GlobalVars.Values) {
                globalVar.FieldInfo = MacroClass.GetField(globalVar.Name, PUBLIC_STATIC);
                if (!globalVar.IsCallOutput) {
                    globalVar.InitInfo = MacroClass
                        .GetProperty(INIT_PREFIX + globalVar.Name, PUBLIC_STATIC);
                }
            }

            Run = (Func<Task>)Delegate.CreateDelegate(typeof(Func<Task>),
                MacroClass.GetMethod(MethodName, PUBLIC_STATIC));

            MacroClass.GetField("GetAssembly", PUBLIC_STATIC)
                .SetValue(null, new Func<string, Assembly>(GetAssembly));

            MacroClass.GetField("SwitchToUIThread", PUBLIC_STATIC)
                .SetValue(null, new Func<Task>(SwitchToUIThreadAsync));

            MacroClass.GetField("SwitchToWorkerThread", PUBLIC_STATIC)
                .SetValue(null, new Func<Task>(SwitchToWorkerThreadAsync));

            MacroClass.GetField("CallMacro", PUBLIC_STATIC)
                .SetValue(null, new Func<string, Task>(CallMacroAsync));

            MacroClass.GetField("WaitExpr", PUBLIC_STATIC)
                .SetValue(null, new Func<int, Func<object>, Task>(WaitExprAsync));

            if (!InitializeUiGlobals())
                return false;

            return NoError();
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.RequestingAssembly == null || args.RequestingAssembly != MacroAssembly)
                return null;
            var fullName = new AssemblyName(args.Name);
            var assemblyPath = RefAssemblies
                .Where(x => Path.GetFileNameWithoutExtension(x).Equals(fullName.Name, IGNORE_CASE))
                .FirstOrDefault();
            if (string.IsNullOrEmpty(assemblyPath))
                return null;
            if (!File.Exists(assemblyPath))
                return null;
            return Assembly.LoadFrom(assemblyPath);
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

            callee.InitGlobals();
            await callee.Run();
            callee.UpdateGlobals();

            // Refresh caller local copies of globals
            InitGlobals();
        }

        public async Task WaitExprAsync(int timeout, Func<object> expr)
        {
            var tMax = TimeSpan.FromMilliseconds(timeout);
            var tRemaining = tMax;
            var t = Stopwatch.StartNew();
            object value;
            try {
                value = await Task.Run(() => expr()).WithTimeout(tRemaining);
            } catch {
                value = null;
            }
            bool ok = !IsDefaultValue(value);

            while (!ok && (tRemaining = (tMax - t.Elapsed)) > TimeSpan.Zero) {
                await Task.Delay(10);
                try {
                    value = await Task.Run(() => expr()).WithTimeout(tRemaining);
                } catch {
                    value = null;
                }
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
                return obj.Equals(Activator.CreateInstance(obj.GetType()));
            else
                return false;
        }

        void InitGlobals()
        {
            foreach (var globalVar in GlobalVars.Values) {
                string varName = globalVar.Name;
                Type varType = globalVar.FieldInfo.FieldType;
                object value;
                if (Globals.TryGetValue(varName, out value)) {
                    Type valueType = value.GetType();
                    if (!varType.IsAssignableFrom(valueType)) {
                        throw new InvalidCastException(string.Format(
                            "Global variable '{0}': cannot assign '{1}' from '{2}'",
                            varName, varType.Name, valueType.Name));
                    }
                    globalVar.FieldInfo.SetValue(null, value);
                } else {
                    globalVar.FieldInfo.SetValue(null, globalVar.InitInfo.GetValue(null));
                }
            }
        }

        void UpdateGlobals()
        {
            foreach (var globalVar in GlobalVars.Values) {
                object value = globalVar.FieldInfo.GetValue(null);
                Globals.AddOrUpdate(globalVar.Name, value, (key, oldValue) => value);
            }
        }

        async Task<bool> GetServicesAsync()
        {
            foreach (var serviceRef in ServiceRefs.Values.Where(x => x.RefVar != null)) {
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

        public static void Reset()
        {
            Macros.Clear();
            Globals.Clear();
        }

        bool GenerateResultFuncs(StringBuilder csharp)
        {
            csharp.Append(
/** BEGIN generate code **/
@"
        public static string Ok;
        public static string Error;
        public static Func<string, string> ErrorMsg;"
/** END generate code **/ );
            return true;
        }

        bool InitializeResultFuncs()
        {
            if (MacroClass == null)
                return false;

            MacroClass.GetField("Ok", PUBLIC_STATIC)
                .SetValue(null, MACRO_OK);

            MacroClass.GetField("Error", PUBLIC_STATIC)
                .SetValue(null, MACRO_ERROR);

            MacroClass.GetField("ErrorMsg", PUBLIC_STATIC)
                .SetValue(null, new Func<string, string>(MACRO_ERROR_MSG));

            return true;
        }

        string MACRO_OK { get { return "(ok)"; } }
        string MACRO_ERROR { get { return "(error)"; } }
        string MACRO_WARN { get { return "(warn)"; } }
        string MACRO_ERROR_MSG(string msg) { return string.Format("{0}\r\n{1}", MACRO_ERROR, msg); }
        string MACRO_WARN_MSG(string msg) { return string.Format("{0}\r\n{1}", MACRO_WARN, msg); }

        bool NoError()
        {
            Result = MACRO_OK;
            return (Ok = true);
        }

        bool Error()
        {
            Result = MACRO_ERROR;
            return (Ok = false);
        }

        bool ErrorMsg(string errorMsg)
        {
            Result = MACRO_ERROR_MSG(errorMsg);
            return (Ok = false);
        }

        bool ErrorException(Exception e)
        {
            Result = MACRO_ERROR_MSG(string.Format("{0}\r\n\"{1}\"\r\n{2}",
                e.GetType().Name, e.Message, e.StackTrace));
            return (Ok = false);
        }

        bool Warning(string warnMsg)
        {
            Result = MACRO_WARN_MSG(warnMsg);
            return (Ok = true);
        }
    }
}
