﻿/*
*/

using System;
using System.Collections.Generic;

namespace Hecate {
	/// <summary>
	/// Description of StoryGenerator.
	/// </summary>
	public class StoryGenerator {
		private readonly StateNode rootNode = new StateNode(42);
		private readonly Dictionary<int, List<Rule>> rules = new Dictionary<int, List<Rule>>();
		private readonly SymbolManager symbolManager = new SymbolManager();
		private readonly Random random = new Random();
		private readonly Dictionary<int, Func<object, object>> modifiers = new Dictionary<int, Func<object, object>>();

		public string Generate(string symbol) {
			Rule tempRule = new Rule(-3, "\"" + symbol + "\"", this, symbolManager);
			string text = tempRule.Execute(new StateNode?[0], this, rootNode);

			return text;
		}

		// Chooses an applicable rule and executes it
		public string ExecuteRule(int rule, StateNode?[] parameters) {
			// Choose a rule
			if (rules.ContainsKey(rule)) {
				Rule chosen = null;
				int count = 0;

				foreach (Rule r in rules[rule]) {
					if (r.Check(parameters, rootNode)) {
						int rank = r.GetRank();
						count += rank;

						if (random.Next(count) < rank) {
							chosen = r;
						}
					}
				}

				if (chosen != null) {
					return chosen.Execute(parameters, this, rootNode);
				}
			}

			return "";
		}

		public void ParseRuleDirectory(string filepath) {
			string[] files = System.IO.Directory.GetFiles(filepath, "*.hec");

			foreach (string file in files) {
				ParseRuleFile(file);
			}

			Token.ClearCache();
		}

		public void ParseRuleFile(string filename) {
			string? line;
			string rule = "";
			string setBeginning = "";
			string setEnd = "";
			System.IO.StreamReader file = new System.IO.StreamReader(filename);

			while ((line = file.ReadLine()) != null) {
				line = line.Trim();

				// Collect the rule from multiple lines if the line ends with a comma
				rule += line;

				if (line.EndsWith(",")) continue;

				// Begins a set of rules
				if (!rule.Contains("=>") || rule.Contains("+>")) {
					if (rule.Contains("+>")) {
						string[] parts = Rule.SplitHelper(rule, "+>");
						setBeginning = parts[0];
						setEnd = parts[1];
					} else {
						setBeginning = rule;
						setEnd = "";
					}
				}
				// A continued list line
				else if (rule.StartsWith("=>")) {
					ParseRuleString(setBeginning + " " + rule + ", " + setEnd);
				}
				// A self-contained rule
				else {
					ParseRuleString(rule);
				}

				rule = "";
			}

			file.Close();
		}

		public void ParseRuleString(string line) {
			line = line.Trim();

			if (!line.Equals("") && !line.StartsWith("#")) {
				Rule rule = new Rule(line, this, symbolManager);
				int name = rule.GetName();
				AddRule(name, rule);
			}
		}

		private void AddRule(int name, Rule rule) {
			if (!rules.ContainsKey(name)) {
				rules[name] = new List<Rule>();
			}

			rules[name].Add(rule);
		}

		// TODO: This is not a good place for this, refactor!
		protected internal bool GetNextRandomBool() {
			return random.NextDouble() > 0.5;
		}

		/**
		 * We don't rewrite existing values
		 */
		public void SetBaseState(Dictionary<string, object> newState) {
			AddStateDict(newState, rootNode);
		}

		private void AddStateDict(Dictionary<string, object> newState, StateNode node) {
			foreach (var entry in newState) {
				var nameIdx = symbolManager.GetInt(entry.Key);

				if (node.GetSubvariable(nameIdx, createIfNull: false) != null) continue;

				var newVar = node.GetSubvariable(nameIdx, createIfNull: true);

				if (entry.Value is Dictionary<string, object> dict) {
					AddStateDict(dict, newVar);
				} else {
					newVar.SetValue(entry.Value);
				}
			}
		}

		/**
		 * We don't rewrite existing values
		 */
		public void RegisterModifier(string modifierName, Func<object, object> func) {
			var nameIdx = symbolManager.GetInt(modifierName);

			if (modifiers.ContainsKey(nameIdx)) {
				return;
			}

			modifiers[nameIdx] = func;
		}

		public StateNode ExecuteModifer(StateNode modifier, StateNode value) {
			Func<object, object> modifierFunc;

			if (!modifiers.TryGetValue(modifier.GetValue(), out modifierFunc)) {
				throw new Exception($"Unknown modifier '{symbolManager.GetString(modifier.GetValue())}'");
			}

			var newValue = modifierFunc(value.GetValue());

			// TODO: Do we need to properly set its parent?
			return new StateNode(newValue);
		}
	}
}
