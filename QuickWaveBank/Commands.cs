using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QuickWaveBank {
	public static class Commands {
		public static readonly RoutedUICommand MySaveAs = new RoutedUICommand(
			"Save List As", "MySaveAs", typeof(Commands),
			new InputGestureCollection() { new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift) }
		);
		public static readonly RoutedUICommand Exit = new RoutedUICommand(
			"Exit", "Exit", typeof(Commands),
			new InputGestureCollection() { new KeyGesture(Key.W, ModifierKeys.Control) }
		);
		public static readonly RoutedUICommand Build = new RoutedUICommand(
			"Build Wave Bank", "Build", typeof(Commands),
			new InputGestureCollection() { new KeyGesture(Key.F5) }
		);
	}
}
