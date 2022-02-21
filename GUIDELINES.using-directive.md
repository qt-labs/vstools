# Coding guidelines -- `using` directives

Three kinds of `using` directives are available:
  * **Reference**: `using <namespace>` -- _namespace_ can be omitted when referencing types.
  * **Alias**: `using <alias> = <namespace|type>` -- _alias_ can be used in place of _namespace_
    or _type_.
  * **Static**: `using static <type>` -- all static members of _type_ are accessible without
    having to specify the type name.

The following conventions apply to `using` directives in the Qt VS Tools code:
  * `using` directives are grouped by root namespace, and ordered alphabetically within each group.
  * Directives that reference external namespaces are located at the start of the source file,
    before the local namespace declaration.
  * The order of external namespace groups is as follows:
      1. `System*` namespaces.
      2. `Microsoft*` namespaces.
      3. `EnvDTE*` namespaces.
      4. All other external namespaces.
  * Directives referencing in-solution namespaces (i.e. `QtVsTools*` or `QtVsTest*` namespaces) are
    nested within the local namespace block.
  * In-solution reference directives will use an abbreviated form of the  namespace, whenever
    possible.
  * Alias and static directives can be specified either at top-level or nested in the local
    namespace.
  * Alias directives are specified after all reference directives.
  * Static directives are specified after all reference directives and all alias directives.
  * Optionally, directives of different kinds can be separated with an empty line.

## Example
```csharp
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using EnvDTE;
using EnvDTE80;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools
{
    using Core;
    using QtMsBuild;

    using RegExprParser = SyntaxAnalysis.RegExpr.Parser;

    using static SyntaxAnalysis.RegExpr;

    class ...
```
