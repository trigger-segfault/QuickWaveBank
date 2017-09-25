using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace QuickWaveBank.Controls {
	/**<summary>Signifies an numeric value change.</summary>*/
	public class ValueChangedEventArgs<T> : RoutedEventArgs {
		//=========== MEMBERS ============
		#region Members

		/**<summary>The previous value.</summary>*/
		public T Previous;
		/**<summary>The new value.</summary>*/
		public T New;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the value change event args.</summary>*/
		public ValueChangedEventArgs(RoutedEvent routedEvent, T previousValue, T newValue) : base(routedEvent) {
			Previous = previousValue;
			New = newValue;
		}

		#endregion
	}

	/**<summary>An integer-based numeric spinner.</summary>*/
	public partial class IntSpinner : UserControl {
		//=========== MEMBERS ============
		#region Members

		/**<summary>The value used when a number can't be parsed.</summary>*/
		private int errorValue = 0;
		/**<summary>The current value.</summary>*/
		private int number = 1;
		/**<summary>The maximum value.</summary>*/
		private int maximum = int.MaxValue;
		/**<summary>The minimum value.</summary>*/
		private int minimum = int.MinValue;
		/**<summary>The spinner increments.</summary>*/
		private int increment = 1;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the numeric spinner.</summary>*/
		public IntSpinner() {
			InitializeComponent();

			UpdateSpinner();
			UpdateTextBox();
		}

		#endregion
		//============ EVENTS ============
		#region Events

		/**<summary>Event handler for value change events.</summary>*/
		public delegate void IntChangedEventHandler(object sender, ValueChangedEventArgs<int> e);
		/**<summary>The value changed routed event.</summary>*/
		public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(IntChangedEventHandler), typeof(IntSpinner));
		/**<summary>Called when the value has been changed.</summary>*/
		public event IntChangedEventHandler ValueChanged {
			add { AddHandler(ValueChangedEvent, value); }
			remove { RemoveHandler(ValueChangedEvent, value); }
		}

		#endregion
		//========== PROPERTIES ==========
		#region Properties

		/**<summary>The value used when a number can't be parsed.</summary>*/
		public int ErrorValue {
			get { return errorValue; }
			set {
				if (value < minimum || value > maximum)
					throw new IndexOutOfRangeException("NumericSpinner ErrorValue outside of Minimum or Maximum range.");
				errorValue = value;
			}
		}
		/**<summary>The current value.</summary>*/
		public int Value {
			get { return number; }
			set {
				if (value < minimum || value > maximum)
					throw new IndexOutOfRangeException("NumericSpinner Value outside of Minimum or Maximum range.");
				if (number != value) {
					int oldValue = number;
					number = value;
					UpdateSpinner();
					UpdateTextBox();
					RaiseEvent(new ValueChangedEventArgs<int>(IntSpinner.ValueChangedEvent, oldValue, number));
				}
			}
		}
		/**<summary>The maximum value.</summary>*/
		public int Maximum {
			get { return maximum; }
			set {
				if (value < minimum)
					throw new IndexOutOfRangeException("NumericSpinner Maximum is less than Minimum range.");
				maximum = value;
				if (number > maximum)
					Value = maximum;
				else
					UpdateSpinner();
				if (errorValue > maximum)
					errorValue = maximum;
			}
		}
		/**<summary>The minimum value.</summary>*/
		public int Minimum {
			get { return minimum; }
			set {
				if (value > maximum)
					throw new IndexOutOfRangeException("NumericSpinner Minimum is greater than Maximum range.");
				minimum = value;
				if (number < minimum)
					Value = minimum;
				else
					UpdateSpinner();
				if (errorValue < minimum)
					errorValue = minimum;
			}
		}
		/**<summary>The up/down increments.</summary>*/
		public int Increment {
			get { return increment; }
			set { increment = value; }
		}
		/**<summary>The text in the text box.</summary>*/
		public string Text {
			get { return textBox.Text; }
		}

		#endregion
		//=========== EDITING ============
		#region Editing

		/**<summary>Selects everything in the spinner.</summary>*/
		public void SelectAll() {
			textBox.Focusable = true;
			textBox.Focus();
			textBox.SelectAll();
		}

		#endregion
		//=========== HELPERS ============
		#region Helpers

		/**<summary>Updates the state of the spinner.</summary>*/
		private void UpdateSpinner() {
			spinner.ValidSpinDirection = ValidSpinDirections.None;
			spinner.ValidSpinDirection |= (number != maximum ? ValidSpinDirections.Increase : ValidSpinDirections.None);
			spinner.ValidSpinDirection |= (number != minimum ? ValidSpinDirections.Decrease : ValidSpinDirections.None);
		}
		/**<summary>Updates the state of the textbox.</summary>*/
		private void UpdateTextBox() {
			int caretIndex = textBox.CaretIndex;
			textBox.Text = number.ToString();
			textBox.CaretIndex = Math.Min(textBox.Text.Length, caretIndex);
			UpdateTextBoxError();
		}
		/**<summary>Updates the state of the textbox.</summary>*/
		private void UpdateTextBox(string newText, int newCaretIndex) {
			int caretIndex = textBox.CaretIndex;
			textBox.Text = newText;
			textBox.CaretIndex = Math.Min(textBox.Text.Length, newCaretIndex);
			UpdateTextBoxError();
		}
		/**<summary>Updates the error state of the textbox.</summary>*/
		private void UpdateTextBoxError() {
			bool error = false;
			try {
				int newNum = int.Parse(Text);
				if (newNum > maximum || newNum < minimum) {
					error = true;
				}
			}
			catch (OverflowException) {
				error = true;
			}
			catch (FormatException) {
				error = true;
			}
			if (error)
				textBox.Foreground = new SolidColorBrush(Color.FromRgb(220, 0, 0));
			else
				textBox.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
		}

		#endregion
		//============ EVENTS ============
		#region Events

		private void OnTextInput(object sender, TextCompositionEventArgs e) {
			int oldValue = number;
			TextBox textBox = sender as TextBox;
			e.Handled = true;
			bool invalidChar = false;
			for (int i = 0; i < e.Text.Length && !invalidChar; i++) {
				if ((e.Text[i] < '0' || e.Text[i] > '9') && (e.Text[i] != '-' || textBox.CaretIndex != 0 || i > 0 || minimum >= 0))
					invalidChar = true;
			}
			if (!invalidChar) {
				string newText = "";
				if (textBox.SelectionLength != 0)
					newText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength).Insert(textBox.SelectionStart, e.Text);
				else
					newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);
				try {
					number = int.Parse(newText);
					if (number > maximum) {
						number = maximum;
						UpdateTextBox();
						textBox.CaretIndex += e.Text.Length;
					}
					else if (number < minimum) {
						number = minimum;
						UpdateTextBox(newText, textBox.CaretIndex + e.Text.Length);
					}
					else {
						UpdateTextBox(newText, textBox.CaretIndex + e.Text.Length);
					}
				}
				catch (OverflowException) {
					if (textBox.Text.Length > 0 && textBox.Text[0] == '-') {
						//Underflow
						number = minimum;
						UpdateTextBox();
						textBox.CaretIndex = textBox.Text.Length;
					}
					else {
						number = maximum;
						UpdateTextBox();
						textBox.CaretIndex = textBox.Text.Length;
					}
				}
				catch (FormatException) {
					if (newText == "-" || newText == "") {
						// Don't worry, the user is just writing a negative number or typing a new number
						number = errorValue;
						UpdateTextBox(newText, textBox.CaretIndex + e.Text.Length);
					}
					else {
						// Shouldn't happen?
						number = errorValue;
						UpdateTextBox();
						textBox.CaretIndex = textBox.Text.Length;
					}
				}
				if (number != oldValue) {
					UpdateSpinner();
					RaiseEvent(new ValueChangedEventArgs<int>(IntSpinner.ValueChangedEvent, oldValue, number));
				}
			}
			UpdateTextBoxError();
		}
		private void OnTextChanged(object sender, TextChangedEventArgs e) {
			int oldValue = number;
			try {
				number = int.Parse(textBox.Text);
				if (number > maximum) {
					number = maximum;
					UpdateTextBox();
					textBox.CaretIndex = textBox.Text.Length;
				}
				else if (number < minimum) {
					number = minimum;
				}
			}
			catch (OverflowException) {
				if (textBox.Text.Length > 0 && textBox.Text[0] == '-') {
					//Underflow
					number = minimum;
					UpdateTextBox();
					textBox.CaretIndex = textBox.Text.Length;
				}
				else {
					number = maximum;
					UpdateTextBox();
					textBox.CaretIndex = textBox.Text.Length;
				}
			}
			catch (FormatException) {
				if (textBox.Text == "-" || textBox.Text == "") {
					// Don't worry, the user is just writing a negative number or typing a new number
					number = errorValue;
				}
				else {
					number = errorValue;
					UpdateTextBox();
					textBox.CaretIndex = textBox.Text.Length;
				}
			}
			if (number != oldValue) {
				UpdateSpinner();
				RaiseEvent(new ValueChangedEventArgs<int>(IntSpinner.ValueChangedEvent, oldValue, number));
			}

			UpdateTextBoxError();
		}
		private void OnSpinnerSpin(object sender, SpinEventArgs e) {
			int oldValue = number;
			if (e.Direction == SpinDirection.Increase)
				number = Math.Min(maximum, number + increment);
			else if (e.Direction == SpinDirection.Decrease)
				number = Math.Max(minimum, number - increment);
			if (number != oldValue) {
				UpdateSpinner();
				UpdateTextBox();
				textBox.CaretIndex = textBox.Text.Length;
				RaiseEvent(new ValueChangedEventArgs<int>(IntSpinner.ValueChangedEvent, oldValue, number));
			}
		}
		private void OnGotFocus(object sender, RoutedEventArgs e) {
			this.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
		}
		private void OnFocusLost(object sender, RoutedEventArgs e) {
			UpdateTextBox();
		}

		#endregion
	}
}
