using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using QuickWaveBank.Windows;
using QuickWaveBank.Util;
using System.Globalization;
using System.IO;

namespace QuickWaveBank {
	/**<summary>The WPF application.</summary>*/
	public partial class App : Application {
		//=========== MEMBERS ============
		#region Members

		/**<summary>Needs to be out here otherwise it can't server its purpose.</summary>*/
		private Mutex m;
		/**<summary>The last exception. Used to prevent multiple error windows for the same error.</summary>*/
		private static object lastException = null;
		
		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the WPF app.</summary>*/
		public App() {
			// Setup embedded assembly resolving
			AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssemblies;

			// We only want to run one instance at a time. Otherwise the temporary
			// output files will be modified by two or more programs.
			bool isNew;
			m = new Mutex(true, "Global\\" + GUID, out isNew);
			if (!isNew) {
				TriggerMessageBox.Show(null, MessageIcon.Warning, "Quick Wave Bank is already running. Cannot run more than one instance at the same time.", "Already Running");
				Environment.Exit(0);
			}
		}

		#endregion
		//============ EVENTS ============
		#region Events

		private Assembly OnResolveAssemblies(object sender, ResolveEventArgs args) {
			var executingAssembly = Assembly.GetExecutingAssembly();
			var assemblyName = new AssemblyName(args.Name);

			string path = assemblyName.Name + ".dll";
			if (assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture) == false) {
				path = string.Format(@"{0}\{1}", assemblyName.CultureInfo, path);
			}

			using (Stream stream = executingAssembly.GetManifestResourceStream(path)) {
				if (stream == null)
					return null;

				byte[] assemblyRawBytes = new byte[stream.Length];
				stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
				return Assembly.Load(assemblyRawBytes);
			}
		}
		private void OnAppStartup(object sender, StartupEventArgs e) {
			// Catch exceptions not in a UI thread
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnAppDomainUnhandledException);
			TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
		}
		private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
			if (e.Exception != lastException) {
				lastException = e.Exception;
				if (ErrorMessageBox.Show(e.Exception))
					Environment.Exit(0);
				e.Handled = true;
			}
		}
		private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e) {
			if (e.ExceptionObject != lastException) {
				lastException = e.ExceptionObject;
				Dispatcher.Invoke(() => {
					if (ErrorMessageBox.Show(e.ExceptionObject))
						Environment.Exit(0);
				});
			}
		}
		private void OnTaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e) {
			if (e.Exception != lastException) {
				lastException = e.Exception;
				Dispatcher.Invoke(() => {
					if (ErrorMessageBox.Show(e.Exception))
						Environment.Exit(0);
				});
			}
		}

		#endregion
		//=========== HELPERS ============
		#region Helpers

		/**<summary>Gets the GUID of the application.</summary>*/
		private static string GUID {
			get {
				object[] assemblyObjects =
				Assembly.GetEntryAssembly().GetCustomAttributes(typeof(GuidAttribute), true);

				if (assemblyObjects.Length > 0) {
					return new Guid(((GuidAttribute)assemblyObjects[0]).Value).ToString();
				}
				return "";
			}
		}

		#endregion
	}
}
