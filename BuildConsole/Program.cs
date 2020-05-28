using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BuildConsole {
	/**<summary>The main program.</summary>*/
	class Program {
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The path of the temporary folder.</summary>*/
		private static readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "TriggersToolsGames", "QuickWaveBank");
		/**<summary>The path of the temporary XactBld3 executable.</summary>*/
		private static readonly string TempXactBld = Path.Combine(TempDirectory, "XactBld3.exe");
		/**<summary>The path of the temporary BuildConsole executable.</summary>*/
		private static readonly string TempBuildConsole = Path.Combine(TempDirectory, "BuildConsole.exe");
		/**<summary>The path of the temporary Xap project file.</summary>*/
		private static readonly string TempProjectFile = Path.Combine(TempDirectory, "WaveBankProject.xap");
		/**<summary>The path of the temporary created wave bank.</summary>*/
		private static readonly string TempWaveBank = Path.Combine(TempDirectory, "Wave Bank.xwb");
		/**<summary>The path of the temporary created wave bank.</summary>*/
		private const int CancelExitCode = -1073741510;
		/**<summary>The path of the temporary created wave bank.</summary>*/
		private const int FailedExitCode = -100;

		#endregion
		//========= ENTRY POINT ==========
		#region Entry Point

		/**<summary>The entry point of the program.</summary>*/
		static int Main(string[] args) {
			Environment.ExitCode = CancelExitCode;

			Console.Title = "Wave Bank Builder (XactBld3)";
			Console.WriteLine("Building Wave Bank...");
			
			DateTime lastModified = (File.Exists(TempWaveBank) ?
				File.GetLastWriteTime(TempWaveBank) :
				DateTime.Now
			);

			ProcessStartInfo start = new ProcessStartInfo();
			start.FileName = TempXactBld;
			start.Arguments = "/X:REPORT /X:HEADER /X:SOUNDBANK /X:CUELIST /WIN32 /F " +
					"\"" + Path.GetFileName(TempProjectFile) + "\""; //TODO: fix later with a proper constant (needed to avoid special characters in usernames)
			start.UseShellExecute = false;
			start.WorkingDirectory = TempDirectory;
			Process process = Process.Start(start);
			process.WaitForExit();

			if (process.ExitCode != 0 || !File.Exists(TempWaveBank) || File.GetLastWriteTime(TempWaveBank) <= lastModified) {
				Environment.ExitCode = FailedExitCode;

				// Sleep now so the window is not hidden afterwards by QuickWaveBanks's hide log.
				Thread.Sleep(200);
				// Just in case the window is currently hidden.
				ShowWindow();

				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Failed to create wave bank!");
				Console.WriteLine("    See console log for more details.");
				Console.ForegroundColor = ConsoleColor.Gray;
				Console.WriteLine();
				Console.WriteLine("Press any key to close...");

				Console.Read();
			}
			else {
				Environment.ExitCode = 0;
			}
			return Environment.ExitCode;
		}

		#endregion
		//========= DLL IMPORTS ==========
		#region Dll Imports

		private static void ShowWindow() {
			ShowWindow(GetConsoleWindow(), SW_RESTORE);
		}

		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
		[DllImport("kernel32.dll")]
		private static extern IntPtr GetConsoleWindow();

		private const int SW_RESTORE = 9;

		#endregion
	}
}
