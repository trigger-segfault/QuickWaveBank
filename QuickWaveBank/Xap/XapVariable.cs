using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickWaveBank.Xap {
	/**<summary>A variable used in Xact3 Xap project files.</summary>*/
	public class XapVariable {
		//=========== MEMBERS ============
		#region Members

		/**<summary>The name of the Xap variable.</summary>*/
		public string Name { get; private set; }
		/**<summary>The value of the Xap variable.</summary>*/
		public string Value { get; set; }

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs an Xap variable with a blank value.</summary>*/
		public XapVariable(string name) {
			Name = name;
			Value = "";
		}
		/**<summary>Constructs an Xap variable with a value.</summary>*/
		public XapVariable(string name, string value) {
			Name = name;
			Value = value;
		}

		#endregion
	}
}
