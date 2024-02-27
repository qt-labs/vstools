/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Collections.Generic;
using System.Linq;

namespace QtVsTools.Core.Common
{
    using static SyntaxAnalysis.RegExpr;

    using Env = Dictionary<string, string>;
    using Var = KeyValuePair<string, string>;

    public static partial class Utils
    {
        public static Env ParseEnvironment(string envString)
        {
            try {
                return EnvParser.Parse(envString).GetValues<Env>("ENV").FirstOrDefault();
            } catch (ParseErrorException) {
                return null;
            }
        }

        private static Parser EnvParser => StaticLazy.Get(() => EnvParser, () =>
            new Token("ENV",
                new Token("VAR", VertSpace,
                    new Token("NAME", (~Char['=']).Repeat(atLeast: 1)) & "=" &
                    new Token("VALUE", Line).Optional())
                {
                    new Rule<Var>
                    {
                        Create("NAME", (string name) => new Var(name, string.Empty)),
                        Transform("VALUE", (Var v, string value) => new Var(v.Key, value))
                    }
                }
                .Repeat())
            {
                new Rule<Env>
                {
                    Transform("VAR", (Env env, Var v) =>
                    {
                        env ??= new Env(CaseIgnorer);
                        env[v.Key] = v.Value;
                        return env;
                    })
                }
            }
            .Render());
    }
}
