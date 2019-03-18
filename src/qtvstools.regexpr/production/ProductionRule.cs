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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace QtVsTools.SyntaxAnalysis
{
    public abstract partial class RegExpr
    {
        [Flags]
        public enum Delimiter { None, Left, Right }

        [Flags]
        public enum Operand { None, Left, Right }

        public interface IProductionRule
        {
            int Priority { get; }
            RuleCallback.Selector Selector { get; }
            RuleCallback.PreCondition PreCondition { get; }
            Token Token { get; set; }
            Delimiter Delimiters { get; }
            Operand Operands { get; }
            object Execute(ParseTree.Node node);
        }

        public abstract class ProductionRule<T> : IProductionRule, IEnumerable
        {
            public int Priority { get; protected set; }
            public RuleCallback.Selector Selector { get; set; }
            public RuleCallback.PreCondition PreCondition { get; set; }

            public Token Token { get; set; }
            public virtual Delimiter Delimiters { get { return Delimiter.None; } }
            public virtual Operand Operands { get { return Operand.None; } }

            protected List<IRuleAction<T>> Actions = new List<IRuleAction<T>>();

            protected void Init(
                int priority, RuleCallback.Selector select, RuleCallback.PreCondition pre)
            {
                Priority = priority;
                Selector = select;
                PreCondition = pre;
            }

            public void Add(IRuleAction<T> action)
            {
                Actions.Add(action);
            }

            protected abstract object[] FetchOperands(Stack<ParseTree.Node> operandStack);

            protected virtual T DefaultProduction(
                string capturedValue,
                Stack<ParseTree.Node> operandStack,
                ProductionObjects productions)
            {
                return CreateInstance();
            }

            protected virtual bool TestPreCondition(
                ParseTree.Node node,
                string capturedValue,
                Stack<ParseTree.Node> operandStack,
                ProductionObjects productions)
            {
                if (PreCondition == null)
                    return true;
                return PreCondition(node);
            }

            public object Execute(ParseTree.Node node)
            {
                if (PreCondition != null && !PreCondition(node))
                    throw new ParseErrorException();

                if (node.Parent == null)
                    return null;

                var operandStack = node.Parent.OperandStack;
                var capturedValue = node.Value;
                var productions = node.ChildProductions;

                // pop-out rule operands from operand stack
                object[] ruleOperands = FetchOperands(operandStack);

                // calculate order of actions taking child productions into account
                var actionScheduling = ScheduleActions(productions);

                // run actions in the calculated order
                T production = default(T);
                foreach (var actionSchedule in actionScheduling) {
                    var a = actionSchedule.Action;
                    var childProduction = actionSchedule.Production;

                    // if production is null and action is update, init with default production
                    if (production == null && a.ActionInfo.ReturnType == typeof(void))
                        production = DefaultProduction(capturedValue, operandStack, productions);

                    // if action uses child production, use it as input instead of the rule operands
                    if (childProduction != null)
                        a.Execute(ref production, capturedValue, childProduction);
                    else
                        a.Execute(ref production, capturedValue, ruleOperands);
                }

                // if no production was created by rule actions, create default production
                if (production == null)
                    production = DefaultProduction(capturedValue, operandStack, productions);

                return production;
            }

            class ActionSchedule
            {
                public IRuleAction<T> Action { get; set; }
                public int ActionIndex { get; set; }
                public object Production { get; set; }
                public int ProductionIndex { get; set; }
            }

            IEnumerable<ActionSchedule> ScheduleActions(ProductionObjects childProductions)
            {
                // actions with order of definition
                var actionsOrder = Actions
                    .Select((a, aIdx) => new
                    {
                        Self = a,
                        Index = aIdx,
                        a.SourceTokenId
                    });
                var dependentActions = actionsOrder
                    .Where(a => !string.IsNullOrEmpty(a.SourceTokenId));
                var independentActions = actionsOrder
                    .Where(a => string.IsNullOrEmpty(a.SourceTokenId));

                // child productions with order of creation
                var productionsOrder = childProductions
                    .Select((p, pIdx) => new
                    {
                        Self = p.Value,
                        Index = pIdx,
                        TokenId = p.Key
                    });

                // schedule actions that depend on child production:
                //  * actions x productions
                //  * sorted by production creation order (inverted)
                //  * and then by action definition order (inverted)
                var dependentActionSchedules = dependentActions
                    .Join(productionsOrder,
                    a => a.SourceTokenId, p => p.TokenId,
                    (a, p) => new ActionSchedule
                    {
                        Action = a.Self,
                        ActionIndex = a.Index,
                        Production = p.Self,
                        ProductionIndex = p.Index,
                    })
                    .OrderByDescending(ap => ap.ProductionIndex)
                    .ThenByDescending(ap => ap.ActionIndex);

                // insert independent actions in the right order
                var scheduled = new Stack<ActionSchedule>();
                IEnumerable<ActionSchedule> toSchedule = dependentActionSchedules;
                foreach (var a in independentActions.OrderByDescending(a => a.Index)) {
                    var scheduleAfter = toSchedule
                        .TakeWhile(ap => ap.ActionIndex > a.Index);
                    foreach (var ap in scheduleAfter)
                        scheduled.Push(ap);
                    scheduled.Push(new ActionSchedule { Action = a.Self });

                    toSchedule = toSchedule.Skip(scheduleAfter.Count());
                }
                if (toSchedule.Any()) {
                    foreach (var ap in toSchedule)
                        scheduled.Push(ap);
                }

                return scheduled;
            }

            public IEnumerator GetEnumerator()
            {
                return Actions.GetEnumerator();
            }

            protected T CreateInstance()
            {
                var type = typeof(T);

                if (type.IsValueType)
                    return default(T);

                if (type == typeof(string))
                    return (T)(object)string.Empty;

                if (!type.IsClass)
                    throw new InvalidOperationException("Not a class: " + type.Name);

                if (type.IsAbstract)
                    throw new InvalidOperationException("Abstract class: " + type.Name);

                if (type.ContainsGenericParameters)
                    throw new InvalidOperationException("Generic class: " + type.Name);

                var ctorInfo = ((TypeInfo)type).DeclaredConstructors
                    .Where(x => x.GetParameters().Length == 0)
                    .FirstOrDefault();

                if (ctorInfo == null)
                    throw new InvalidOperationException("No default constructor: " + type.Name);

                return (T)ctorInfo.Invoke(new object[0]);
            }
        }

        public class Rule<T> : ProductionRule<T>
        {
            public Rule(
                int priority = int.MaxValue,
                RuleCallback.Selector select = null,
                RuleCallback.PreCondition pre = null)
            {
                Init(priority, select, pre);
            }

            protected override object[] FetchOperands(Stack<ParseTree.Node> operandStack)
            {
                return new object[] { };
            }
        }

        public class PrefixRule<TOperand, T> : ProductionRule<T>
        {
            public override Operand Operands { get { return Operand.Right; } }

            public PrefixRule(
                int priority = 0,
                RuleCallback.Selector select = null,
                RuleCallback.PreCondition pre = null)
            {
                Init(priority, select, pre);
            }

            protected override object[] FetchOperands(Stack<ParseTree.Node> operandStack)
            {
                if (operandStack.Count < 1)
                    throw new ParseErrorException();

                var operand = operandStack.Pop();
                if (!(operand.Production is TOperand))
                    throw new ParseErrorException();

                return new object[] { operand.Production };
            }
        }

        public class PostfixRule<TOperand, T> : ProductionRule<T>
        {
            public override Operand Operands { get { return Operand.Left; } }

            public PostfixRule(
                int priority = 0,
                RuleCallback.Selector select = null,
                RuleCallback.PreCondition pre = null)
            {
                Init(priority, select, pre);
            }

            protected override object[] FetchOperands(Stack<ParseTree.Node> operandStack)
            {
                if (operandStack.Count < 1)
                    throw new ParseErrorException();

                var operand = operandStack.Pop();
                if (!(operand.Production is TOperand))
                    throw new ParseErrorException();

                return new object[] { operand.Production };
            }
        }

        public class InfixRule<TLeftOperand, TRightOperand, T> : ProductionRule<T>
        {
            public override Operand Operands { get { return Operand.Left | Operand.Right; } }

            public InfixRule(
                int priority = 0,
                RuleCallback.Selector select = null,
                RuleCallback.PreCondition pre = null)
            {
                Init(priority, select, pre);
            }

            protected override object[] FetchOperands(Stack<ParseTree.Node> operandStack)
            {
                if (operandStack.Count < 2)
                    throw new ParseErrorException();

                var rightOperand = operandStack.Pop();
                if (!(rightOperand.Production is TRightOperand))
                    throw new ParseErrorException();

                var leftOperand = operandStack.Pop();
                if (!(leftOperand.Production is TLeftOperand))
                    throw new ParseErrorException();

                return new object[] { leftOperand.Production, rightOperand.Production };
            }
        }

        public class LeftDelimiterRule<T> : ProductionRule<T>
        {
            public override Delimiter Delimiters { get { return Delimiter.Left; } }

            public LeftDelimiterRule(
                int priority = int.MinValue,
                RuleCallback.Selector select = null,
                RuleCallback.PreCondition pre = null)
            {
                Init(priority, select, pre);
            }

            protected override object[] FetchOperands(Stack<ParseTree.Node> operandStack)
            {
                return new object[] { };
            }
        }

        public class RightDelimiterRule<TLeftDelim, TExpr, T> : ProductionRule<T>
        {
            public override Delimiter Delimiters { get { return Delimiter.Right; } }

            public RightDelimiterRule(
                int priority = int.MaxValue,
                RuleCallback.Selector select = null,
                RuleCallback.PreCondition pre = null)
            {
                Init(priority, select, pre);
            }

            protected override T DefaultProduction(
                string capturedValue,
                Stack<ParseTree.Node> operandStack,
                ProductionObjects productions)
            {
                throw new ParseErrorException();
            }

            protected override object[] FetchOperands(Stack<ParseTree.Node> operandStack)
            {
                if (operandStack.Count < 2)
                    throw new ParseErrorException();

                var delimitedExpr = operandStack.Pop();
                if (!(delimitedExpr.Production is TExpr))
                    throw new ParseErrorException();

                var leftDelimiter = operandStack.Pop();
                if (!(leftDelimiter.Production is TLeftDelim))
                    throw new ParseErrorException();

                return new object[] { leftDelimiter.Production, delimitedExpr.Production };
            }
        }

        public static class RuleCallback
        {
            public delegate bool Selector(ITokenCapture token);
            public delegate bool PreCondition(IOperatorCapture capture);
        }

        public const RuleCallback.Selector Default = null;
    }
}
