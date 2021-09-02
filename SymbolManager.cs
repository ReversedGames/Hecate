/*
*/

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Hecate {
	/// <summary>
	/// Manages symbols;
	/// each string is given a corresponding int value for optimization
	/// </summary>
	public class SymbolManager {
		private Dictionary<string, int> integers;
		private Dictionary<int, string> strings;
		private int currentSymbolIndex;

		public const int THIS = -4;
		public const int NULL = -3;
		public const int LOCAL = -2;
		public const int VARIABLE = -1;
		public const int LITERAL = 0;
		public const int ADD = 1;
		public const int SUB = 2;
		public const int MULTIPLY = 3;
		public const int DIVIDE = 4;
		public const int ASSIGN = 5;
		public const int NEGATE = 6;
		public const int EQUALS = 7;
		public const int NOT_EQUALS = 8;
		public const int LESS_THAN = 9;
		public const int GREATER_THAN = 10;
		public const int LESS_OR = 11;
		public const int GREATER_OR = 12;
		public const int LEFT_PAREN = 13;
		public const int RIGHT_PAREN = 14;
		public const int DOT = 15;
		public const int ADD_TO = 16;
		public const int SUB_TO = 17;
		public const int MULTIPLY_TO = 18;
		public const int DIVIDE_TO = 19;
		public const int LET = 20;
		public const int DEL = 21;
		public const int CALL = 22;
		public const int REPLACE = 23;
		public const int AND = 24;
		public const int OR = 25;
		public const int CAPITALIZE = 26;
		public const int END_OF_EXPRESSION = 27;

		public const int FIRST_SYMBOL = 28;

		public SymbolManager() {
			integers = new Dictionary<string, int> {
				{ "this", THIS },
				{ "null", NULL },
				{ "+", ADD },
				{ "-", SUB },
				{ "*", MULTIPLY },
				{ "/", DIVIDE },
				{ "=", ASSIGN },
				{ "!", NEGATE },
				{ "==", EQUALS },
				{ "!=", NOT_EQUALS },
				{ "<", LESS_THAN },
				{ ">", GREATER_THAN },
				{ "<=", LESS_OR },
				{ ">=", GREATER_OR },
				{ "(", LEFT_PAREN },
				{ ")", RIGHT_PAREN },
				{ ".", DOT },
				{ "+=", ADD_TO },
				{ "-=", SUB_TO },
				{ "*=", MULTIPLY_TO },
				{ "/=", DIVIDE_TO },
				{ "let", LET },
				{ "del", DEL },
				{ "=>", CALL },
				{ "<-", REPLACE },
				{ "and", AND },
				{ "or", OR },
				{ "^", CAPITALIZE },
				{ "end_of_expression", END_OF_EXPRESSION }
			};

			strings = new Dictionary<int, string>();

			foreach (KeyValuePair<string, int> entry in integers) {
				strings.Add(entry.Value, entry.Key);
			}

			currentSymbolIndex = FIRST_SYMBOL;
		}

		// Returns the symbol for the given string
		public int GetInt(string symbol) {
			if (!integers.ContainsKey(symbol)) {
				return AddSymbol(symbol);
			}

			return integers[symbol];
		}

		// Returns the string for the given symbol
		public string GetString(int index) {
			if (!strings.ContainsKey(index)) {
				throw new IndexOutOfRangeException();
			}

			return strings[index];
		}

		// Adds the given string to the symbol database and returns its index number
		private int AddSymbol(string symbol) {
			if (!integers.ContainsKey(symbol)) {
				strings[currentSymbolIndex] = symbol;
				integers[symbol] = currentSymbolIndex;
				currentSymbolIndex += 1;
			}

			return integers[symbol];
		}

		// Returns true if the symbol is a conditional (>=, ==, <, etc)
		public static bool IsConditionalOperator(int symbol) {
			switch (symbol) {
				case EQUALS:
				case NOT_EQUALS:
				case LESS_THAN:
				case GREATER_THAN:
				case LESS_OR:
				case GREATER_OR:
					return true;

				default: return false;
			}
		}
	}
}
