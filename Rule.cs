﻿/*
*/

using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Hecate {
	/// <summary>
	/// Description of Rule.
	/// </summary>
	public class Rule {
		private int name;
		private string text;
		private StateExpression[] effects;
		private StateExpression[] conditions;
		private StateExpression[] parameters;
		private StateExpression[] insets;
		private StateNode? ruleNode;
		private int rank;
		private int COUNT;

		private StoryGenerator generator;
		private SymbolManager symbolManager;

		private static readonly Regex ruleRegex = new Regex("(.*) => (\"\"|\"(\\\\\"|[^\"])*(?<!\\\\)\")?(,)?(.*)");
		private static readonly Regex flagRegex = new Regex("flag ([A-Za-z0-9_.]+)");
		private static readonly Regex insetRegex = new Regex("(?<!\\\\)\\[[^\\]]*\\]");

		public Rule(int name, string text, StoryGenerator generator, SymbolManager symbolManager) : this(name, text, generator, symbolManager, new string[0], new string[0]) {
		}

		public Rule(int name, string text, StoryGenerator generator, SymbolManager symbolManager, string[] exprs) : this(name, text, generator, symbolManager, exprs, new string[0]) {
		}

		public Rule(int name, string text, StoryGenerator generator, SymbolManager symbolManager, string[] exprs, string[] parameters) {
			Initialize(name, text, generator, symbolManager, exprs, parameters);
		}

		public Rule(string line, StoryGenerator generator, SymbolManager symbolManager) {
			Match ruleMatch = ruleRegex.Match(line);

			if (ruleMatch.Success) {
				// Get the main parts of the rule
				string leftside = ruleMatch.Groups[1].Value.Trim();
				string text = ruleMatch.Groups[2].Value.Trim();
				string evalstring = ruleMatch.Groups[5].Value.Trim();

				// Separate the name from the parameters
				string[] nameAndParameters = SplitHelper(leftside, " ");
				int name = symbolManager.GetInt(nameAndParameters[0]);
				int numberOfParameters = nameAndParameters.Length - 1;
				string[] parameters = new string[numberOfParameters];
				Array.Copy(nameAndParameters, 1, parameters, 0, numberOfParameters);

				// Separate the evaluations (and unpack flag evaluations)
				Match flagMatch = flagRegex.Match(evalstring);

				while (flagMatch.Groups.Count > 1) {
					string flag = flagMatch.Groups[1].Value;
					evalstring = evalstring.Replace("flag " + flag, "flags." + flag + " == null, let flags." + flag);
					flagMatch = flagRegex.Match(evalstring);
				}

				string[] evals = SplitHelper(evalstring, ",");

				// Massage the parameter strings from "X" to "let X = 0"
				for (int i = 0; i < parameters.Length; i++) {
					parameters[i] = "let " + parameters[i] + " = 0";
				}

				// Finalize
				Initialize(name, text, generator, symbolManager, evals, parameters);
			}
		}

		// Initialization method for easier constructor overload
		private void Initialize(int name, string text, StoryGenerator generator, SymbolManager symbolManager, string[] exprs, string[] parameters) {
			this.name = name;
			this.generator = generator;
			this.symbolManager = symbolManager;
			ruleNode = new StateNode(0, null, 0);
			COUNT = this.symbolManager.GetInt("count");
			ruleNode.SetSubvariable(COUNT, 0);

			SetText(text, symbolManager);
			SetExpressions(StateExpression.StringArrayToExpressionArray(exprs, generator, symbolManager, this));
			this.parameters = StateExpression.StringArrayToExpressionArray(parameters, generator, symbolManager, this);
			rank = (conditions.Length + 1);
		}

		// Executes the rule
		public string Execute(StateNode?[] parameters, StoryGenerator generator, StateNode rootNode) {
			string result = text;
			// Push local stack before evaluations
			StateExpression.PushLocalStack();

			// Add 1 to the number of times this rule has ran
			StateNode? count = ruleNode.GetSubvariable(COUNT);
			ruleNode.SetSubvariable(COUNT, count == null ? 1 : count + 1);

			// Bind parameters
			for (int i = 0; i < this.parameters.Length; i++) {
				StateNode? parameter = this.parameters[i].Evaluate(generator, rootNode);
				parameter.ReplaceWith(parameters[i]);
			}

			// Evaluate effects before insets to avoid rabbithole
			foreach (StateExpression e in effects) {
				e.Evaluate(generator, rootNode);
			}

			// Replace all inset expressions with their evals
			for (int i = 0; i < insets.Length; i++) {
				StateNode? eval = insets[i].Evaluate(generator, rootNode);
				result = result.Replace("[" + i + "]", eval);
			}

			// Pop local stack after evaluations
			StateExpression.PopLocalStack();

			// Add special characters
			result = result.Replace("\\n", "\n");
			result = result.Replace("\\\"", "\"");
			result = result.Replace("\\[", "[");

			return result;
		}

		// Returns true when the rule can be executed with these parameters
		public bool Check(StateNode?[] parameters, StateNode rootNode) {
			// Must be given at least as much parameters as required
			if (parameters.Length < this.parameters.Length) {
				return false;
			}

			// Push local stack before evaluations
			StateExpression.PushLocalStack();

			// Bind parameters
			for (int i = 0; i < this.parameters.Length; i++) {
				StateNode? parameter = this.parameters[i].Evaluate(generator, rootNode);
				parameter.ReplaceWith(parameters[i]);
			}

			// If all conditions return true
			foreach (StateExpression c in conditions) {
				int result = c.Evaluate(generator, rootNode);

				if (result == 0) {
					StateExpression.PopLocalStack();

					return false;
				}
			}

			// Pop local stack after evaluations
			StateExpression.PopLocalStack();


			return true;
		}

		public int GetName() {
			return name;
		}

		public StateNode? GetNode() {
			return ruleNode;
		}

		public StateNode GetRank() {
			return rank;
		}

		// Save the text in a more efficient form by separating the inset expressions
		private void SetText(string text, SymbolManager symbolManager) {
			MatchCollection matches = insetRegex.Matches(text);
			insets = new StateExpression[matches.Count];
			int offset = 0;
			int index = 0;

			foreach (Match match in matches) {
				foreach (Capture capture in match.Captures) {
					string replacer = "[" + index + "]";
					string expr = capture.Value.Substring(1, capture.Length - 2);
					expr = expr.Replace("\\\"", "\"");

					text = text.Remove(capture.Index + offset, capture.Length);
					text = text.Insert(capture.Index + offset, replacer);
					offset -= capture.Length - replacer.Length;

					insets[index] = new StateExpression(expr, generator, symbolManager, this);
					index++;
				}
			}

			this.text = text.Length < 2 ? "" : text.Substring(1, text.Length - 2);
		}

		// Save the expressions in two groups; conditions and effects
		private void SetExpressions(StateExpression[] exprs) {
			List<StateExpression> conditions = new List<StateExpression>();
			List<StateExpression> effects = new List<StateExpression>();

			foreach (StateExpression ex in exprs) {
				if (ex.IsCondition()) {
					conditions.Add(ex);
				} else {
					effects.Add(ex);
				}
			}

			this.conditions = conditions.ToArray();
			this.effects = effects.ToArray();
		}

		public static string[] SplitHelper(string text, string separator) {
			return text.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);
		}
	}
}
