using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;
using Loyc.Utilities;
using Loyc.Essentials;
using Loyc.Math;
using Loyc.CompilerCore;
using S = Loyc.CompilerCore.CodeSymbols;
using EP = ecs.EcsPrecedence;

namespace ecs
{
	// This file: code for printing statements
	public partial class EcsNodePrinter
	{
		#region Sets and dictionaries of statements

		// Syntactic categories of statements:
		//
		// | Category            | Syntax example(s)      | Detection method          |
		// |---------------------|------------------------|---------------------------|
		// | Space definition    | struct X : Y {...}     | IsSpaceStatement()        |
		// | Variable decl       | int x = 2;             | IsVariableDecl()          |
		// | Other definitions   | delegate void f();     | Check DefinitionStmts     |
		// | Simple stmt         | goto label;            | Check SimpleStmts list    |
		// | Block stmt with or  | for (...) {...}        | Check BlockStmts list     |
		// |   without args      | try {...} catch {...}  |                           |
		// | Label stmt          | case 2: ... label:     | IsLabelStmt()             |
		// | Block or list       | { ... } or #{ ... }    | Name in (S.List,S.Braces) |
		// | Expression stmt     | x += y;                | When none of the above    |

		// Space definitions are containers for other definitions
		static readonly HashSet<Symbol> SpaceDefinitionStmts = new HashSet<Symbol>(new[] {
			S.Struct, S.Class, S.Trait, S.Enum, S.Alias, S.Interface, S.Namespace
		});
		// Definition statements define types, spaces, methods, properties, events and variables
		static readonly HashSet<Symbol> OtherDefinitionStmts = new HashSet<Symbol>(new[] {
			S.Var, S.Def, S.Delegate, S.Event, S.Property
		});
		// Simple statements have the syntax "keyword;" or "keyword expr;"
		static readonly HashSet<Symbol> SimpleStmts = new HashSet<Symbol>(new[] {
			S.Break, S.Continue, S.Goto, S.GotoCase, S.Return, S.Throw, S.Import,
		});
		// Block statements take block(s) as arguments
		static readonly HashSet<Symbol> TwoArgBlockStmts = new HashSet<Symbol>(new[] {
			S.Do, S.Fixed, S.Lock, S.Switch, S.UsingStmt, S.While
		});
		static readonly HashSet<Symbol> OtherBlockStmts = new HashSet<Symbol>(new[] {
			S.If, S.Checked, S.For, S.ForEach, S.If, S.Try, S.Unchecked
		});
		static readonly HashSet<Symbol> LabelStmts = new HashSet<Symbol>(new[] {
			S.Label, S.Case
		});
		static readonly HashSet<Symbol> BlocksOfStmts = new HashSet<Symbol>(new[] {
			S.List, S.Braces
		});

		//static readonly HashSet<Symbol> StmtsWithWordAttrs = AllNonExprStmts;

		public enum SPResult { Fail, Complete, NeedSemicolon };
		delegate SPResult StatementPrinter(EcsNodePrinter @this, Ambiguity flags);
		static Dictionary<Symbol, StatementPrinter> StatementPrinters = StatementPrinters_();
		static Dictionary<Symbol, StatementPrinter> StatementPrinters_()
		{
			// Build a dictionary of printers for each operator name.
			var d = new Dictionary<Symbol, StatementPrinter>();
			AddAll(d, SpaceDefinitionStmts, "AutoPrintSpaceDefinition");
			AddAll(d, OtherDefinitionStmts, "AutoPrintMethodDefinition");
			d[S.Var]      = OpenDelegate<StatementPrinter>("AutoPrintVarDecl");
			d[S.Event]    = OpenDelegate<StatementPrinter>("AutoPrintEvent");
			d[S.Property] = OpenDelegate<StatementPrinter>("AutoPrintProperty");
			AddAll(d, SimpleStmts, "AutoPrintSimpleStmt");
			AddAll(d, TwoArgBlockStmts, "AutoPrintTwoArgBlockStmt");
			AddAll(d, OtherBlockStmts, "AutoPrintOtherBlockStmt");
			AddAll(d, LabelStmts, "AutoPrintLabelStmt");
			AddAll(d, BlocksOfStmts, "AutoPrintBlockOfStmts");
			d[S.Result] = OpenDelegate<StatementPrinter>("AutoPrintResult");
			return d;
		}
		static void AddAll(Dictionary<Symbol,StatementPrinter> d, HashSet<Symbol> names, string handlerName)
		{
			var method = OpenDelegate<StatementPrinter>(handlerName);
 			foreach(var name in names)
				d.Add(name, method);
		}
		
		#endregion

		void PrintStmt(INodeReader n, Ambiguity flags = 0)
		{
			using (With(n))
				PrintStmt(flags);
		}

		public void PrintStmt(Ambiguity flags = 0)
		{
			_out.BeginStatement();

			var style = _n.BaseStyle;
			if (style != NodeStyle.Expression && style != NodeStyle.PrefixNotation && style != NodeStyle.PurePrefixNotation)
			{
				StatementPrinter printer;
				var name = _n.Name;
				if (_n.IsKeyword && HasSimpleHeadWPA(_n) && StatementPrinters.TryGetValue(name, out printer)) {
					var result = printer(this, flags);
					if (result != SPResult.Fail) {
						if (result == SPResult.NeedSemicolon)
							_out.Write(';', true);
						return;
					}
				}
			}
			PrintExpr(StartStmt);
			_out.Write(';', true);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintResult(Ambiguity flags)
		{
			if (!IsResultExpr(_n) || (flags & Ambiguity.FinalStmt) == 0)
				return SPResult.Fail;
			PrintExpr(_n.TryGetArg(0), StartExpr); // not StartStmt => allows multiplication e.g. a*b by avoiding ptr ambiguity
			return SPResult.Complete;
		}

		// These methods are public but hidden because they are found by reflection 
		// and they should be compatible with a partial-trust environment.
		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintSpaceDefinition(Ambiguity flags)
		{
			// Spaces: S.Struct, S.Class, S.Trait, S.Enum, S.Alias, S.Interface, S.Namespace
			if (!IsSpaceStatement())
				return SPResult.Fail;

			var ifClause = GetIfClause();
			PrintAttrs(StartStmt, AttrStyle.IsDefinition, flags, ifClause);

			INodeReader name = _n.TryGetArg(0), bases = _n.TryGetArg(1), body = _n.TryGetArg(2);
			WriteOperatorName(_n.Name);
			_out.Space();
			PrintExpr(name, ContinueExpr, Ambiguity.InDefinitionName);
			if (bases.CallsMin(S.List, 1))
			{
				Space(SpaceOpt.BeforeBaseListColon);
				WriteThenSpace(':', SpaceOpt.AfterColon);
				for (int i = 0, c = bases.ArgCount; i < c; i++) {
					if (i != 0)
						WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintType(bases.TryGetArg(i), ContinueExpr);
				}
			}
			bool alias = name.Calls(S.Set, 2);
			var name2 = name;
			if (name2.Calls(S.Of) || (alias && (name2 = name.Args[0]).Calls(S.Of)))
				PrintWhereClauses(name2);

			AutoPrintIfClause(ifClause);
			
			if (body == null)
				return SPResult.NeedSemicolon;

			if (_n.Name == S.Enum)
				PrintEnumBody(body);
			else
				PrintBracedBlock(body, NewlineOpt.BeforeSpaceDefBrace);
			return SPResult.Complete;
		}

		void AutoPrintIfClause(INodeReader ifClause)
		{
			if (ifClause != null) {
				if (!Newline(NewlineOpt.BeforeIfClause))
					Space(SpaceOpt.Default);
				_out.Write("if", true);
				Space(SpaceOpt.BeforeKeywordStmtArgs);
				PrintExpr(ifClause.TryGetArg(0), StartExpr, Ambiguity.NoBracedBlock);
			}
		}

		private INodeReader GetIfClause()
		{
			var ifClause = _n.TryGetAttr(S.If);
			if (ifClause != null && !HasPAttrs(ifClause) && HasSimpleHeadWPA(ifClause) && ifClause.ArgCount == 1)
				return ifClause;
			return null;
		}

		private void PrintWhereClauses(INodeReader name)
		{
			if (name.Name != S.Of)
				return;

			// Look for "where" clauses and print them
			bool first = true;
			for (int i = 1, c = name.ArgCount; i < c; i++)
			{
				var param = name.TryGetArg(i);
				for (int a = 0, ac = param.AttrCount; a < ac; a++)
				{
					var where = param.TryGetAttr(a);
					if (where.CallsMin(S.Where, 1))
					{
						using (Indented)
						{
							if (!Newline(first ? NewlineOpt.BeforeWhereClauses : NewlineOpt.BeforeEachWhereClause))
								_out.Space();
							first = false;
							_out.Write("where ", true);
							var paramName = param.Name.Name;
							_out.Write(param.IsKeyword ? paramName.Substring(1) : paramName, true);
							Space(SpaceOpt.BeforeWhereClauseColon);
							WriteThenSpace(':', SpaceOpt.AfterColon);
							bool firstC = true;
							foreach (var constraint in where.Args)
							{
								if (firstC)
									firstC = false;
								else
									WriteThenSpace(',', SpaceOpt.AfterComma);
								if (constraint.IsSimpleSymbol && (constraint.Name == S.Class || constraint.Name == S.Struct))
									WriteOperatorName(constraint.Name);
								else
									PrintExpr(constraint, StartExpr);
							}
						}
					}
				}
			}
		}

		private void PrintEnumBody(INodeReader body)
		{
			if (!Newline(NewlineOpt.BeforeSpaceDefBrace))
				Space(SpaceOpt.Default);
			_out.Write('{', true);
			using (Indented)
			{
				_out.Newline();
				for (int i = 0, c = body.ArgCount; i < c; i++)
				{
					if (i != 0) {
						_out.Write(',', true);
						if (!Newline(NewlineOpt.AfterEachEnumValue))
							Space(SpaceOpt.AfterComma);
					}
					PrintExpr(body.TryGetArg(i), StartExpr);
				}
			}
			_out.Newline();
			_out.Write('}', true);
		}

		private bool PrintBracedBlockOrStmt(INodeReader stmt, NewlineOpt beforeBrace = NewlineOpt.BeforeExecutableBrace)
		{
			var name = stmt.Name;
			if ((name == S.Braces || name == S.List) && !HasPAttrs(stmt) && HasSimpleHeadWPA(stmt))
			{
				PrintBracedBlock(stmt, beforeBrace);
				return true;
			}
			else using (Indented)
			{
				Newline(NewlineOpt.BeforeSingleSubstmt);
				PrintStmt(stmt);
				return false;
			}
		}

		private void PrintBracedBlock(INodeReader body, NewlineOpt before, bool skipFirstStmt = false)
		{
			if (before != 0)
				if (!Newline(before))
					Space(SpaceOpt.Default);
			if (body.Name == S.List)
				_out.Write('#', false);
			_out.Write('{', true);
			using (Indented)
				for (int i = (skipFirstStmt?1:0), c = body.ArgCount; i < c; i++)
					PrintStmt(body.TryGetArg(i), i + 1 == c ? Ambiguity.FinalStmt : 0);
			Newline(NewlineOpt.Default);
			_out.Write('}', true);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintMethodDefinition(Ambiguity flags)
		{
			// S.Def, S.Delegate: #def(#int, Square, #(int x), { return x * x; });
			if (!IsMethodDefinition(true))
				return SPResult.Fail;

			INodeReader retType = _n.TryGetArg(0), name = _n.TryGetArg(1);
			bool isConstructor = retType.Name == S.Missing && retType.IsSimpleSymbol;
			// A cast operator with the structure: #def(Foo, operator`#cast`, #(...))
			// can be printed in a special format: operator Foo(...);
			bool isCastOperator = (name.Name == S.Cast && name.TryGetAttr(S.StyleUseOperatorKeyword) != null);

			var ifClause = PrintTypeAndName(isConstructor, isCastOperator);
			INodeReader args = _n.TryGetArg(2);

			WriteOpenParen(ParenFor.MethodDecl);
			for (int i = 0, c = args.ArgCount; i < c; i++) {
				if (i != 0)
					WriteThenSpace(',', SpaceOpt.AfterComma);
				PrintExpr(args.TryGetArg(i), StartExpr, Ambiguity.AssignmentLhs);
			}
			WriteCloseParen(ParenFor.MethodDecl);
	
			PrintWhereClauses(_n.TryGetArg(1));

			INodeReader body = _n.TryGetArg(3), firstStmt;
			// If this is a constructor where the first statement is this(...) or 
			// base(...), we must change the notation to ": this(...) {...}" as
			// required in plain C#
			if (isConstructor && body.CallsMin(S.Braces, 1) && (
				CallsWPAIH(firstStmt = body.TryGetArg(0), S.This) ||
				CallsWPAIH(firstStmt, S.Base))) {
				using (Indented) {
					if (!Newline(NewlineOpt.BeforeConstructorColon))
						Space(SpaceOpt.BeforeConstructorColon);
					WriteThenSpace(':', SpaceOpt.AfterColon);
					PrintExpr(firstStmt, StartExpr, Ambiguity.NoBracedBlock);
				}
			} else
				firstStmt = null;

			return AutoPrintBodyOfMethodOrProperty(body, ifClause, firstStmt != null);
		}

		private INodeReader PrintTypeAndName(bool isConstructor, bool isCastOperator = false)
		{
			INodeReader retType = _n.TryGetArg(0), name = _n.TryGetArg(1);
			var ifClause = GetIfClause();

			if (retType.HasPAttrs())
				using (With(retType))
					PrintAttrs(StartStmt, AttrStyle.NoKeywordAttrs, 0, null, "return");

			PrintAttrs(StartStmt, AttrStyle.IsDefinition, 0, ifClause);

			if (_n.Name == S.Delegate)
			{
				_out.Write("delegate", true);
				_out.Space();
			}
			if (isCastOperator)
			{
				_out.Write("operator", true);
				_out.Space();
				PrintType(retType, ContinueExpr, Ambiguity.AllowPointer | Ambiguity.DropAttributes);
			}
			else
			{
				if (!isConstructor) {
					PrintType(retType, ContinueExpr, Ambiguity.AllowPointer | Ambiguity.DropAttributes);
					_out.Space();
				}
				if (isConstructor && name.Name == S.New && name.IsSimpleSymbol)
					_out.Write("new", true);
				else
					PrintExpr(name, ContinueExpr, Ambiguity.InDefinitionName);
			}
			return ifClause;
		}
		private SPResult AutoPrintBodyOfMethodOrProperty(INodeReader body, INodeReader ifClause, bool skipFirstStmt = false)
		{
			AutoPrintIfClause(ifClause);

			if (body == null)
				return SPResult.NeedSemicolon;
			if (body.Name == S.Forward)
			{
				Space(SpaceOpt.BeforeForwardArrow);
				_out.Write("==>", true);
				PrefixSpace(EP.Forward);
				PrintExpr(body.TryGetArg(0), EP.Forward.RightContext(StartStmt));
				return SPResult.NeedSemicolon;
			}
			else
			{
				Debug.Assert(body.Name == S.Braces);
				PrintBracedBlock(body, NewlineOpt.BeforeMethodBrace, skipFirstStmt);
				return SPResult.Complete;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintProperty(Ambiguity flags)
		{
			// S.Property: 
			// #property(int, Foo, { 
			//     get({ return _foo; });
			//     set({ _foo = value; });
			// });
			if (!IsPropertyDefinition())
				return SPResult.Fail;

			var ifClause = PrintTypeAndName(false);

			PrintWhereClauses(_n.TryGetArg(1));

			return AutoPrintBodyOfMethodOrProperty(_n.TryGetArg(2), ifClause);
		}
		
		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintVarDecl(Ambiguity flags)
		{
			if (!IsVariableDecl(true, true))
				return SPResult.Fail;
			
			var ifClause = GetIfClause();
			PrintAttrs(StartStmt, AttrStyle.IsDefinition, flags, ifClause);
			PrintVariableDecl(false, StartStmt);
			AutoPrintIfClause(ifClause);
			return SPResult.NeedSemicolon;
		}
		
		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintEvent(Ambiguity flags)
		{
			var eventType = EventDefinitionType();
			if (eventType == null)
				return SPResult.Fail;

			_out.Write("event", true);
			_out.Space();
			var ifClause = PrintTypeAndName(false);
			if (eventType == EventWithBody)
				return AutoPrintBodyOfMethodOrProperty(_n.TryGetArg(2), ifClause);
			else {
				for (int i = 2, c = _n.ArgCount; i < c; i++)
				{
					WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintExpr(_n.TryGetArg(i), ContinueExpr);
				}
				return SPResult.NeedSemicolon;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintSimpleStmt(Ambiguity flags)
		{
			// S.Break, S.Continue, S.Goto, S.GotoCase, S.Return, S.Throw
			if (!IsSimpleStmt())
				return SPResult.Fail;

			PrintAttrs(StartStmt, AttrStyle.AllowWordAttrs, flags);

			if (_n.Name == S.GotoCase)
				_out.Write("goto case", true);
			else
				WriteOperatorName(_n.Name);

			for (int i = 0, c = _n.ArgCount; i < c; i++)
			{
				if (i == 0)
					Space(SpaceOpt.Default);
				else
					WriteThenSpace(',', SpaceOpt.AfterComma);

				PrintExpr(_n.TryGetArg(i), StartExpr);
			}
			return SPResult.NeedSemicolon;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintTwoArgBlockStmt(Ambiguity flags)
		{
			// S.Do, S.Fixed, S.Lock, S.Switch, S.UsingStmt, S.While
			var type = TwoArgBlockStmtType();
			if (type == null)
				return SPResult.Fail;

			if (type == S.Do)
			{
				_out.Write("do", true);
				bool braces = PrintBracedBlockOrStmt(_n.TryGetArg(0), NewlineOpt.BeforeSimpleStmtBrace);
				if (braces)
					Newline(NewlineOpt.BeforeExecutableBrace);
				_out.Write("while", true);
				PrintWithinParens(ParenFor.KeywordCall, _n.TryGetArg(1));
			}
			else
			{
				WriteOperatorName(_n.Name);
				PrintWithinParens(ParenFor.KeywordCall, _n.TryGetArg(0));
				PrintBracedBlockOrStmt(_n.TryGetArg(1));
			}

			return SPResult.Complete;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintOtherBlockStmt(Ambiguity flags)
		{
			// S.If, S.For, S.ForEach, S.Checked, S.Unchecked, S.Try
			// Note: the "if" statement in particular cannot have "word" attributes
			var type = OtherBlockStmtType();
			if (type == null)
				return SPResult.Fail;
			if (type == S.If)
			{
				_out.Write("if", true);
				PrintWithinParens(ParenFor.KeywordCall, _n.TryGetArg(0));
				bool braces = PrintBracedBlockOrStmt(_n.TryGetArg(1));
				var @else = _n.TryGetArg(2);
				if (@else != null) {
					Newline(braces ? NewlineOpt.BeforeExecutableBrace : NewlineOpt.Default);
					_out.Write("else", true);
					PrintBracedBlockOrStmt(@else);
				}
			}
			else if (type == S.For)
			{
				_out.Write("for", true);
				
				WriteOpenParen(ParenFor.KeywordCall);
				for (int i = 0; i < 3; i++) {
					if (i != 0)
						WriteThenSpace(';', SpaceOpt.AfterComma);
					PrintExpr(_n.TryGetArg(i), StartExpr, flags);
				}
				WriteCloseParen(ParenFor.KeywordCall);

				PrintBracedBlockOrStmt(_n.TryGetArg(3));
			}
			else if (type == S.ForEach)
			{
				_out.Write("foreach", true);
				
				WriteOpenParen(ParenFor.KeywordCall);
				PrintExpr(_n.TryGetArg(0), EP.Equals.LeftContext(StartStmt), flags);
				_out.Space();
				_out.Write("in", true);
				_out.Space();
				PrintExpr(_n.TryGetArg(1), ContinueExpr, flags);
				WriteCloseParen(ParenFor.KeywordCall);

				PrintBracedBlockOrStmt(_n.TryGetArg(2));
			}
			else if (type == S.Try)
			{
				_out.Write("try", true);
				bool braces = PrintBracedBlockOrStmt(_n.TryGetArg(0), NewlineOpt.BeforeSimpleStmtBrace);
				for (int i = 1, c = _n.ArgCount; i < c; i++)
				{
					Newline(braces ? NewlineOpt.BeforeExecutableBrace : NewlineOpt.Default);
					var clause = _n.TryGetArg(i);
					INodeReader first = clause.TryGetArg(0), second = clause.TryGetArg(1);
					
					WriteOperatorName(clause.Name);
					if (second != null && !IsSimpleSymbolWPA(first, S.Missing))
						PrintWithinParens(ParenFor.KeywordCall, first);
					braces = PrintBracedBlockOrStmt(second ?? first);
				}
			}
			else if (type == S.Checked) // includes S.Unchecked
			{
				WriteOperatorName(_n.Name);
				PrintBracedBlockOrStmt(_n.TryGetArg(0), NewlineOpt.BeforeSimpleStmtBrace);
			}
			
			return SPResult.Fail;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintLabelStmt(Ambiguity flags)
		{
			if (!IsLabelStmt())
				return SPResult.Fail;

			if (_n.Name == S.Label) {
				PrintExpr(_n.Args[0], StartStmt);
			} else if (_n.Name == S.Case) {
				_out.Write("case", true);
				_out.Space();
				for (int i = 0, c = _n.ArgCount; i < c; i++)
				{
					WriteThenSpace(',', SpaceOpt.AfterComma);
					PrintExpr(_n.TryGetArg(i), StartStmt);
				}
			}
			_out.Write(':', true);
			return SPResult.Complete;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public SPResult AutoPrintBlockOfStmts(Ambiguity flags)
		{
			if (!IsBlockOfStmts(_n))
				return SPResult.Fail;

			PrintAttrs(StartStmt, AttrStyle.AllowKeywordAttrs, flags);
			PrintBracedBlock(_n, 0);
			return SPResult.Complete;
		}
	}
}