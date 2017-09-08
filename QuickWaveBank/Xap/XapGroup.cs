using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickWaveBank.Xap {
	/**<summary>A group used in Xact3 Xap project files.</summary>*/
	public class XapGroup {
		//=========== MEMBERS ============
		#region Members

		/**<summary>The name of the Xap group.</summary>*/
		public string Name { get; private set; }
		/**<summary>The collection of variables in the group.</summary>*/
		public List<XapVariable> Variables { get; private set; }
		/**<summary>The list of sub-groups in the group.</summary>*/
		public List<XapGroup> Groups { get; private set; }

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs an Xap group with a name.</summary>*/
		public XapGroup(string name) {
			Name = name;
			Variables = new List<XapVariable>();
			Groups = new List<XapGroup>();
		}

		#endregion
		//========== VARIABLES ===========
		#region Variables

		/**<summary>adds a variable.</summary>*/
		public void AddVariable(XapVariable variable) {
			Variables.Add(variable);
		}
		/**<summary>Sets or adds a variable.</summary>*/
		public void AddVariable(string name, string value) {
			Variables.Add(new XapVariable(name, value));
		}
		/**<summary>Sets or adds a variable.</summary>*/
		public void SetVariable(string name, string value, int index = 0) {
			Variables.Find((XapVariable v) => {
				if (v.Name == name) {
					if (index == 0)
						return true;
					index--;
				}
				return false;
			}).Value = value;
		}
		/**<summary>Removes a variable.</summary>*/
		public void RemoveVariable(string name, int index = 0) {
			Variables.RemoveAt(Variables.FindIndex((XapVariable v) => {
				if (v.Name == name) {
					if (index == 0)
						return true;
					index--;
				}
				return false;
			}));
		}
		/**<summary>Removes a variable.</summary>*/
		public void RemoveVariables(string name) {
			int index = Variables.FindIndex((XapVariable v) => { return v.Name == name; });
			while (index != -1) {
				Variables.RemoveAt(index);
				index = Variables.FindIndex((XapVariable v) => { return v.Name == name; });
			}
		}
		/**<summary>Sets or adds a variable.</summary>*/
		public XapVariable GetVariable(string name, int index = 0) {
			return Variables.Find((XapVariable v) => {
				if (v.Name == name) {
					if (index == 0)
						return true;
					index--;
				}
				return false;
			});
		}
		/**<summary>Gets all the groups with the specified name.</summary>*/
		public XapVariable[] GetVariables(string name) {
			return Variables.FindAll((XapVariable v) => { return v.Name == name; }).ToArray();
		}
		/**<summary>Checks if a variable exists.</summary>*/
		public bool VariableExists(string name) {
			return Variables.Find((XapVariable v) => { return v.Name == name; }) != null;
		}
		/**<summary>Gets the number of groups with the specified name.</summary>*/
		public int GetVariableCount(string name) {
			return Variables.FindAll((XapVariable v) => { return v.Name == name; }).Count;
		}
		/**<summary>Clears all variables.</summary>*/
		public void ClearVariables() {
			Variables.Clear();
		}

		#endregion
		//============ GROUPS ============
		#region Groups

		/**<summary>Adds a group.</summary>*/
		public void AddGroup(string name) {
			Groups.Add(new XapGroup(name));
		}
		/**<summary>Adds a group.</summary>*/
		public void AddGroup(XapGroup group) {
			Groups.Add(group);
		}
		/**<summary>Removes a group.</summary>*/
		public void RemoveGroup(string name, int index = 0) {
			Groups.RemoveAt(Groups.FindIndex((XapGroup g) => {
				if (g.Name == name) {
					if (index == 0)
						return true;
					index--;
				}
				return false;
			}));
		}
		/**<summary>Removes a group.</summary>*/
		public void RemoveGroups(string name) {
			int index = Groups.FindIndex((XapGroup g) => { return g.Name == name; });
			while (index != -1) {
				Groups.RemoveAt(index);
				index = Groups.FindIndex((XapGroup g) => { return g.Name == name; });
			}
		}
		/**<summary>Gets the group with the specified name at the specified index.</summary>*/
		public XapGroup GetGroup(string name, int index = 0) {
			return Groups.Find((XapGroup g) => {
				if (g.Name == name) {
					if (index == 0)
						return true;
					index--;
				}
				return false;
			});
		}
		/**<summary>Gets all the groups with the specified name.</summary>*/
		public XapGroup[] GetGroups(string name) {
			return Groups.FindAll((XapGroup g) => { return g.Name == name; }).ToArray();
		}
		/**<summary>Checks is the group exists.</summary>*/
		public bool GroupExists(string name) {
			return Groups.Find((XapGroup g) => { return g.Name == name; }) != null;
		}
		/**<summary>Gets the number of groups with the specified name.</summary>*/
		public int GetGroupCount(string name) {
			return Groups.FindAll((XapGroup g) => { return g.Name == name; }).Count;
		}
		/**<summary>Clears all groups.</summary>*/
		public void ClearGroups() {
			Groups.Clear();
		}

		#endregion
		//============= BOTH =============
		#region Groups

		/**<summary>Clears all groups and variables.</summary>*/
		public void Clear() {
			Variables.Clear();
			Groups.Clear();
		}

		#endregion
	}
}
