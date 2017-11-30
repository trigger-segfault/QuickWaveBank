using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickWaveBank.Util;

namespace QuickWaveBank.Xap {
	/**<summary>An Xact3 Xap project file.</summary>*/
	public class XapFile {
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The characters reserved in Xap files.</summary>*/
		public static readonly char[] FormatCharacters = {
			'{', '}', ';', '='
		};

		#endregion
		//=========== MEMBERS ============
		#region Members

		/**<summary>The root group.</summary>*/
		public XapGroup Root { get; private set; }
		/**<summary>True if spaces are used instead of tabs.</summary>*/
		public bool UseSpaces { get; set; }

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the xap file.</summary>*/
		public XapFile() {
			Root = new XapGroup("");
			UseSpaces = false;
		}

		#endregion
		//=========== LOADING ============
		#region Loading

		/**<summary>Loads the Xap file from the filepath.</summary>*/
		public void Load(string filepath) {
			Root.Clear();
			var stream = new FileStream(filepath, FileMode.Open);
			Load(stream);
			stream.Close();
		}
		/**<summary>Loads the Xap file from the stream.</summary>*/
		public void Load(Stream stream) {
			Root.Clear();
			StreamReader reader = new StreamReader(stream);
			Parse(reader);
		}
		/**<summary>Saves the Xap file to the filepath.</summary>*/
		public void Save(string filepath) {
			var stream = new FileStream(filepath, FileMode.OpenOrCreate);
			Save(stream);
			stream.Close();
		}
		/**<summary>Saves the Xap file to the stream.</summary>*/
		public void Save(Stream stream) {
			stream.SetLength(0);
			StreamWriter writer = new StreamWriter(stream);
			Write(writer);
			writer.Flush();
		}

		#endregion
		//=========== PARSING ============
		#region Parsing

		/**<summary>Parses the Xap file.</summary>*/
		private void Parse(StreamReader reader) {
			string[] tokens = ReadTokens(reader);
			int index = 0;
			Root = ParseGroup(tokens, ref index, "");
		}
		/**<summary>Parses a group.</summary>*/
		private XapGroup ParseGroup(string[] tokens, ref int index, string name) {
			XapGroup group = new XapGroup(name);
			string currentText = null;
			string variableName = null;
			for (; index < tokens.Length; index++) {
				if (IsFormatCharacter(tokens[index].First())) {
					switch (tokens[index].First()) {
					case '=':
						if (variableName != null)
							throw new ArgumentException("Invalid token '='. Variable name already declared.");
						if (currentText != null)
							variableName = currentText;
						else
							throw new ArgumentException("Invalid token '='. No variable name.");
						break;
					case ';':
						if (variableName != null) {
							if (currentText != null) {
								group.AddVariable(variableName, currentText);
								variableName = null;
							}
							else
								throw new ArgumentException("Invalid token ';'. No variable value.");
						}
						else {
							throw new ArgumentException("Invalid token ';'. No variable name or value.");
						}
						break;
					case '{':
						if (variableName != null) {
							throw new ArgumentException(
								"Invalid token '{'. " + (currentText == null ?
								"Expected variable value." :
								"Expected ';'.")
							);
						}
						else {
							if (currentText != null) {
								index++;
								group.AddGroup(ParseGroup(tokens, ref index, currentText));
							}
							else
								throw new ArgumentException("Invalid token '{'. No group name.");
						}
						break;
					case '}':
						if (variableName != null) {
							throw new ArgumentException(
								"Unexpected end of group. " + (currentText == null ?
								"Expected variable value." :
								"Expected ';'.")
							);
						}
						else if (currentText != null) {
							throw new ArgumentException("Unexpected end of group. Leftover text.");
						}
						else if (name == "")
							throw new ArgumentException("Invalid token '}'. Not in group.");
						else
							return group;
					}
					currentText = null;
				}
				else {
					currentText = tokens[index];
				}
			}
			if (variableName != null) {
				throw new ArgumentException(
					"Unexpected end of file. " + (currentText == null ?
					"Expected variable value." :
					"Expected ';'.")
				);
			}
			else if (currentText != null) {
				throw new ArgumentException("Unexpected end of file. Leftover text.");
			}
			else if (name != "") {
				throw new ArgumentException("Unexpected end of file. Expected '}'.");
			}
			return group;
		}
		/**<summary>Splits up the Xap file into tokens to read.</summary>*/
		private string[] ReadTokens(StreamReader reader) {
			char[] buffer = new char[1024];
			int charsRead = reader.Read(buffer, 0, buffer.Length);
			string text = new string(buffer).Substring(0, charsRead);
			List<string> tokens = new List<string>();
			string token;
			for (int i = 0; i < text.Length; i++) {
				char c = text[i];
				if (IsFormatCharacter(c)) {
					token = text.Substring(0, i).Trim();
					if (token.Length > 0)
						tokens.Add(token);
					tokens.Add(new string(c, 1));
					text = text.Substring(i + 1);
					i = -1;
				}
				if (i + 1 == text.Length) {
					charsRead = reader.Read(buffer, 0, buffer.Length);
					text += new string(buffer).Substring(0, charsRead);
				}
			}

			token = text.Trim();
			if (token.Length > 0)
				tokens.Add(token);

			return tokens.ToArray();
		}

		#endregion
		//=========== WRITING ============
		#region Writing

		/**<summary>Writes the Xap file to the stream.</summary>*/
		private void Write(StreamWriter writer) {
			WriteGroup(Root, writer);
		}
		/**<summary>Writes the Xap group to the stream.</summary>*/
		private void WriteGroup(XapGroup currentGroup, StreamWriter writer, int depth = 0) {
			foreach (XapVariable variable in currentGroup.Variables) {
				writer.WriteLine(GetTab(depth) + variable.Name + " = " + variable.Value + ";");
			}
			if (currentGroup.Variables.Count > 0 && currentGroup.Groups.Count > 0)
				writer.WriteLine();
			bool first = true;
			foreach (XapGroup group in currentGroup.Groups) {
				if (!first) writer.WriteLine();
				else first = false;

				writer.WriteLine(GetTab(depth) + group.Name);
				writer.WriteLine(GetTab(depth) + "{");
				WriteGroup(group, writer, depth + 1);
				writer.WriteLine(GetTab(depth) + "}");
			}
		}
		/**<summary>Gets indentation based on the tabs setting.</summary>*/
		private string GetTab(int depth) {
			if (UseSpaces)
				return new string(' ', depth * 4);
			else
				return new string('\t', depth);
		}

		#endregion
		//============ STATIC ============
		#region Static

		/**<summary>Gets if the string contains any format characters.</summary>*/
		public static bool ContainsFormatCharacter(string s) {
			foreach (char c in s) {
				if (IsFormatCharacter(c))
					return true;
			}
			return false;
		}
		public static string RemoveFormatCharacters(string s) {
			foreach (char specialChar in FormatCharacters) {
				s = s.Replace(new string(specialChar, 1), "");
			}
			return s;
		}
		/**<summary>Gets if the character is a format character.</summary>*/
		public static bool IsFormatCharacter(char c) {
			foreach (char specialChar in FormatCharacters) {
				if (c == specialChar)
					return true;
			}
			return false;
		}

		#endregion
	}
}
