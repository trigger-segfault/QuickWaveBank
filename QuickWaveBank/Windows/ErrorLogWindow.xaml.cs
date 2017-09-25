using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;

namespace QuickWaveBank.Windows {
	/**<summary>A log error.</summary>*/
	public struct LogError {
		/**<summary>True if the log item is a warning and not an error.</summary>*/
		public bool IsWarning;
		/**<summary>The log message.</summary>*/
		public string Message;
		/**<summary>The error/warning reason.</summary>*/
		public string Reason;
		/**<summary>Constructs a log error.</summary>*/
		public LogError(bool isWarning, string message, string reason) {
			IsWarning = isWarning;
			Message = message;
			Reason = reason;
		}
	}

	/**<summary>The log window for showing errors that occurred during file processing.</summary>*/
	public partial class ErrorLogWindow : Window {
		//=========== MEMBERS ============
		#region Members

		/**<summary>The number of lines written so far.</summary>*/
		int lines;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs and sets up the error log window.</summary>*/
		private ErrorLogWindow(LogError[] errors) {
			InitializeComponent();

			lines = 0;
			textBlockMessage.Text = "";
			foreach (LogError log in errors) {
				if (lines >= 300) {
					textBlockMessage.Inlines.Add(new Run("Issues continued in log file..."));
					textBlockMessage.Inlines.Add(new LineBreak());
					break;
				}
				AddError(log);
			}
		}

		#endregion
		//=========== HELPERS ============
		#region Helpers

		/**<summary>Adds an error.</summary>*/
		private void AddError(LogError log) {
			//string[] logs = log.Message.Split('\n', StringSplitOptions.None);
			//if (logs.Length > 0) {
			Run run = new Run((log.IsWarning ? "Warning: " : "Error: ") + log.Message);
			ColorRun(log.IsWarning, run);
			textBlockMessage.Inlines.Add(run);
			textBlockMessage.Inlines.Add(new LineBreak());
			lines++;
			if (log.Reason != String.Empty) {
				run = new Run("    Reason: " + log.Reason);
				ColorRun(log.IsWarning, run);
				textBlockMessage.Inlines.Add(run);
				textBlockMessage.Inlines.Add(new LineBreak());
				lines++;
			}
		}
		/**<summary>Adds a Run with color based on if it is a warning or error.</summary>*/
		private void ColorRun(bool isWarning, Run run) {
			if (isWarning)
				run.Foreground = new SolidColorBrush(Colors.Orange);
			else
				run.Foreground = new SolidColorBrush(Colors.Red);
		}

		#endregion
		//============ EVENTS ============
		#region Events

		#endregion
		//=========== SHOWING ============
		#region Showing

		public static void Show(Window owner, LogError[] errors) {
			ErrorLogWindow window = new ErrorLogWindow(errors);
			if (owner == null || owner.Visibility != Visibility.Visible)
				window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			else
				window.Owner = owner;
			window.ShowDialog();
		}

		#endregion
	}
}
