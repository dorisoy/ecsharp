using System;
using System.Collections.Generic;
using Loyc.Essentials;
using Loyc.Utilities;
using System.Collections;
using Loyc.CompilerCore.ExprParsing;
using System.Diagnostics;

namespace Loyc.CompilerCore.ExprNodes
{
	public class IDOperator : AbstractOperator<AstNode>, IOneOperator<AstNode>
	{
		public IDOperator() : this(Localize.From("identifier"), Tokens.ID, Tokens.ID) { }
		public IDOperator(string name, Symbol exprType, Symbol tokenType)
			: base(name, exprType, new OneOperatorPart[] {
				new OneOperatorPart(tokenType),
			}) {}

		object IOneOperator<AstNode>.Generate(OneOperatorMatch<AstNode> match) { return Generate(match); }
		public AstNode Generate(OneOperatorMatch<AstNode> match)
		{
			Debug.Assert(match.Operator == this);
			Debug.Assert(match.Parts.Length == 1);
			Debug.Assert(!match.Parts[0].MatchedExpr);

			return match.Parts[0].Token.WithType(Tokens.ID);
		}
	}
}
