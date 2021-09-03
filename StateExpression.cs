/*
*/

using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Hecate {
	/// <summary>
	/// Description of StateExpression.
	/// Uses Pratt's Top Down Recursion Parser
	/// </summary>
	public class StateExpression {
		private readonly Token[] tokens;
		private int currentToken;
		private bool condition;
		private StateNode rootNode;
		private StateNode? ruleNode;
		private SymbolManager symbolManager;
		private StoryGenerator generator;
		private bool createSubvars = false;
		private string original_text = "";

		private static StateNode localNode;
		private static List<StateNode> localStack = new List<StateNode>();

		private static Regex literalRegex = new Regex("^(\".*\"|[0-9]+([.][0-9]+)?)$");

		private static bool debugging = false;

		public StateExpression(string text, StoryGenerator generator, SymbolManager symbolManager, Rule rule) {
			DebugLog(text + "\n");
			this.generator = generator;
			this.symbolManager = symbolManager;

			// Translate into executable script
			tokens = Token.Tokenize(text, symbolManager);

			// Even one conditional token marks this expression as a condition
			condition = false;

			foreach (Token t in tokens) {
				if (SymbolManager.IsConditionalOperator(t.type)) {
					condition = true;

					break;
				}
			}

			currentToken = 0;
			original_text = text;

			if (rule != null) {
				ruleNode = rule.GetNode();
			}
		}

		// Evaluates the expression
		public StateNode? Evaluate(StoryGenerator generator, StateNode rootNode) {
			this.rootNode = rootNode;
			currentToken = 0;

			try {
				return Expression();
			} catch (Exception ex) {
				throw new Exception("Error in expression: " + original_text + "\n" + ex.Message);
			}
		}

		// The expression parser
		private StateNode? Expression(int rightBindingPower = 0) {
			int t = currentToken;
			currentToken++;
			StateNode? left = NullDenotation(tokens[t]);

			while (rightBindingPower < LeftBindingPower(tokens[currentToken])) {
				t = currentToken;
				currentToken++;
				left = LeftDenotation(tokens[t], left);
			}

			return left;
		}

		// Advances the parser only if the current token is of the specified type
		private void Advance(int type) {
			if (tokens[currentToken].type != type) {
				throw new Exception("Syntax error: missing token");
			}

			currentToken++;
		}

		// Returns the left binding power of the token
		private int LeftBindingPower(Token token) {
			switch (token.type) {
				case SymbolManager.ASSIGN:
				case SymbolManager.REPLACE:
				case SymbolManager.ADD_TO:
				case SymbolManager.SUB_TO:
				case SymbolManager.MULTIPLY_TO:
				case SymbolManager.DIVIDE_TO:
				case SymbolManager.LET:
				case SymbolManager.DEL:
				case SymbolManager.AND:
				case SymbolManager.OR:
					return 10;

				case SymbolManager.EQUALS:
				case SymbolManager.NOT_EQUALS:
				case SymbolManager.LESS_THAN:
				case SymbolManager.GREATER_THAN:
				case SymbolManager.LESS_OR:
				case SymbolManager.GREATER_OR:
					return 40;

				case SymbolManager.ADD:
				case SymbolManager.SUB:
				case SymbolManager.CAPITALIZE:
					return 50;

				case SymbolManager.MULTIPLY:
				case SymbolManager.DIVIDE:
					return 60;

				case SymbolManager.LEFT_PAREN:
					return 70;

				case SymbolManager.DOT:
				case SymbolManager.CALL:
				case SymbolManager.MODIFIER:
					return 80;

				case SymbolManager.END_OF_EXPRESSION:
					return 0;

				default: return 0;
			}
		}

		// The value of the literal
		private StateNode? NullDenotation(Token token) {
			StateNode? expr;

			switch (token.type) {
				case SymbolManager.NULL: return null;
				case SymbolManager.LITERAL: return token.literal;
				case SymbolManager.VARIABLE: return rootNode.GetSubvariable(token.literal, createSubvars);
				case SymbolManager.THIS: return ruleNode;
				case SymbolManager.LOCAL: return localNode.GetSubvariable(token.literal, createSubvars);
				case SymbolManager.ADD: return Expression(100);
				case SymbolManager.SUB: return -Expression(100);
				case SymbolManager.CAPITALIZE: {
					string val = Expression(10);

					if (val.Length > 0) {
						return val.Substring(0, 1).ToUpper() + val.Substring(1);
					}

					return val;
				}
				case SymbolManager.NEGATE: {
					StateNode? result = Expression(100);

					if (result == null) {
						return 1;
					}

					return result > 0 ? 0 : 1;
				}
				case SymbolManager.LEFT_PAREN: {
					bool temp = createSubvars;
					createSubvars = false;
					expr = Expression();
					createSubvars = temp;
					Advance(SymbolManager.RIGHT_PAREN);

					return expr;
				}
				case SymbolManager.LET: {
					createSubvars = true;
					expr = Expression(79);
					createSubvars = false;

					return expr;
				}
				case SymbolManager.DEL: {
					StateNode? node = Expression(79);

					return node.RemoveFromParent();
				}
				case SymbolManager.CALL: {
					int rule = Expression(79);
					List<StateNode?> parameters = new List<StateNode?>();
					int current = tokens[currentToken].type;

					if (current == SymbolManager.LEFT_PAREN) {
						currentToken += 1;
						current = tokens[currentToken].type;

						while (current != SymbolManager.END_OF_EXPRESSION && current != SymbolManager.RIGHT_PAREN) {
							StateNode? param = Expression(0);
							parameters.Add(param);
							current = tokens[currentToken].type;
						}

						currentToken += 1;
					}

					return generator.ExecuteRule(rule, parameters.ToArray());
				}
				default: throw new Exception("Syntax error: Invalid value!");
			}
		}

		// The left denotation of a token
		private StateNode? LeftDenotation(Token token, StateNode? left) {
			StateNode? right = Expression(LeftBindingPower(token));

			return token.type switch {
				SymbolManager.ADD => StateNode.Add(left, right),
				SymbolManager.SUB => left - right,
				SymbolManager.MULTIPLY => left * right,
				SymbolManager.DIVIDE => left / right,
				SymbolManager.EQUALS => StateNode.EqualsWithNull(left, right) ? 1 : 0,
				SymbolManager.NOT_EQUALS => StateNode.EqualsWithNull(left, right) ? 0 : 1,
				SymbolManager.LESS_THAN => left < right ? 1 : 0,
				SymbolManager.GREATER_THAN => left > right ? 1 : 0,
				SymbolManager.LESS_OR => left <= right ? 1 : 0,
				SymbolManager.GREATER_OR => left >= right ? 1 : 0,
				SymbolManager.DOT => left?.GetSubvariable(right, createSubvars),
				SymbolManager.REPLACE => left.ReplaceWith(right),
				SymbolManager.ASSIGN => right == null ? left.ReplaceWith(null) : left.SetValue(right.GetValue()),
				SymbolManager.ADD_TO => left.SetValue((int)left + (int)right),
				SymbolManager.SUB_TO => left.SetValue(left - right),
				SymbolManager.MULTIPLY_TO => left.SetValue(left * right),
				SymbolManager.DIVIDE_TO => left.SetValue(left / right),
				SymbolManager.AND => (left > 0) && (right > 0) ? 1 : 0,
				SymbolManager.OR => (left > 0) || (right > 0) ? 1 : 0,
				SymbolManager.MODIFIER => right != null ? RunModifier(right, left) : throw new Exception("Syntax error: No modifier specified! "),
				_ => throw new Exception("Syntax error: Invalid operator! " + token.type)
			};
		}

		private StateNode RunModifier(StateNode modifier, StateNode? value) {
			// TODO: Do we need to set its parent?
			return generator.ExecuteModifer(modifier, value);
		}

		public bool IsCondition() {
			return condition;
		}

		public static StateExpression[] StringArrayToExpressionArray(string[] strings, StoryGenerator generator, SymbolManager symbolManager, Rule rule) {
			StateExpression[] exprs = new StateExpression[strings.Length];

			for (int i = 0; i < strings.Length; i++) {
				exprs[i] = new StateExpression(strings[i].Trim(), generator, symbolManager, rule);
			}

			return exprs;
		}

		// Add a layer of scope insulation for a rule
		public static void PushLocalStack() {
			localStack.Add(localNode);
			localNode = new StateNode(0, null, 0);
		}

		// Remove a layer of scope insulation when a rule ends
		public static void PopLocalStack() {
			int last_index = localStack.Count - 1;
			localNode = localStack[last_index];
			localStack.RemoveAt(last_index);
		}

		private static void DebugLog(string text) {
			if (debugging) {
				Console.Write(text);
			}
		}
	}
}
