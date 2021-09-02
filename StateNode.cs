/*
*/

using System;
using System.Collections.Generic;

namespace Hecate {
	/// <summary>
	/// Description of StateNode.
	/// </summary>
	public class StateNode {
		private readonly Dictionary<int, StateNode> children = new Dictionary<int, StateNode>();
		private dynamic? stateValue;
		private StateNode? parent;
		private int parentName;

		public StateNode(dynamic? stateValue, StateNode? parent = null, int parentName = 0) {
			this.stateValue = stateValue;
			this.parent = parent;
			this.parentName = parentName;
		}

		public dynamic? GetValue() {
			return stateValue;
		}

		public StateNode SetValue(dynamic? setValue) {
			stateValue = setValue;

			return this;
		}

		// Returns the child variable according to the given name
		public StateNode? GetSubvariable(int name, bool createIfNull = false) {
			if (children.ContainsKey(name)) {
				return children[name];
			} else if (createIfNull) {
				children[name] = new StateNode(0, this, name);

				return children[name];
			}

			return null;
		}

		// Sets the value of the child variable
		public StateNode? SetSubvariable(int name, StateNode setValue) {
			if (!children.TryGetValue(name, out var child)) {
				child = new StateNode(setValue.stateValue, this, name);
				children[name] = child;
			} else {
				child.SetValue(setValue.stateValue);
			}

			return child;
		}

		// Replaces the node with the given tree
		public StateNode? SetSubtree(int name, StateNode? tree) {
			if (tree == null) {
				children.Remove(name);
			} else {
				children[name] = tree;
				tree.parent = this;
				tree.parentName = name;
			}

			return tree;
		}

		// Destroys the subvariable (and all named with it)
		public StateNode? RemoveSubvariable(int name) {
			if (children.ContainsKey(name)) {
				StateNode? subvar = children[name];
				children.Remove(name);

				return subvar;
			}

			throw new ArgumentException("Invalid variable.");
		}

		// Removes this node from it's parent
		public StateNode RemoveFromParent() {
			if (parent == null) throw new ArgumentException("Invalid parent variable.");

			parent.RemoveSubvariable(parentName);

			return this;
		}

		// Replaces the node in it's parent's tree
		public StateNode ReplaceWith(StateNode? replacement) {
			if (parent == null) throw new ArgumentException("Invalid parent variable");

			parent.SetSubtree(parentName, replacement);

			return this;
		}

		// Debug print to the console the whole tree
		public void PrintTree(SymbolManager symbolManager, int name, int level = 0) {
			for (int i = 0; i < level; i++) {
				Console.Write(" ");
			}

			Console.WriteLine(symbolManager.GetString(name) + ": " + stateValue);

			foreach (KeyValuePair<int, StateNode> node in children) {
				node.Value.PrintTree(symbolManager, node.Key, level + 1);
			}
		}

		// Compares two instances of StateNode
		public static bool EqualsWithNull(StateNode? a, StateNode? b) {
			if (a == null) {
				if (b == null) {
					return true;
				}

				return false;
			}

			if (b == null) {
				return false;
			}

			return a.Equals(b);
		}


		// Implicit operators
		public override bool Equals(object? obj) {
			StateNode sn = (StateNode)obj;

			if (stateValue is string) {
				return stateValue.Equals(sn);
			}

			return stateValue == sn;
		}

		public static StateNode? Add(StateNode? a, StateNode? b) {
			return a.stateValue + b.stateValue;
		}

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		// Initialize as int
		public static implicit operator StateNode(int stateValue) {
			return new StateNode(stateValue);
		}

		// Initialize as double
		public static implicit operator StateNode(double stateValue) {
			return new StateNode(stateValue);
		}

		// Initialize as string
		public static implicit operator StateNode(string? stateValue) {
			return new StateNode(stateValue);
		}

		// Treated as int
		public static implicit operator int(StateNode? var) {
			return (int)var.stateValue;
		}

		// Treated as double
		public static implicit operator double(StateNode var) {
			return (double)var.stateValue;
		}

		// Treated as string
		public static implicit operator string(StateNode? var) {
			if (var == null) {
				return "";
			}

			return !(var.stateValue is string strValue)
				? "" + var.stateValue
				: strValue;
		}
	}
}
