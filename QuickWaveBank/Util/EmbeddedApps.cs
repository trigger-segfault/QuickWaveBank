using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;

namespace QuickWaveBank {
	//https://stackoverflow.com/questions/666799/embedding-unmanaged-dll-into-a-managed-c-sharp-dll
	/// <summary>
	/// A class used by managed classes to managed unmanaged DLLs.
	/// This will extract and load DLLs from embedded binary resources.
	/// 
	/// This can be used with pinvoke, as well as manually loading DLLs your own way. If you use pinvoke, you don't need to load the DLLs, just
	/// extract them. When the DLLs are extracted, the %PATH% environment variable is updated to point to the temporary folder.
	///
	/// To Use
	/// <list type="">
	/// <item>Add all of the DLLs as binary file resources to the project Propeties. Double click Properties/Resources.resx,
	/// Add Resource, Add Existing File. The resource name will be similar but not exactly the same as the DLL file name.</item>
	/// <item>In a static constructor of your application, call EmbeddedDllClass.ExtractEmbeddedDlls() for each DLL that is needed</item>
	/// <example>
	///               EmbeddedDllClass.ExtractEmbeddedDlls("libFrontPanel-pinv.dll", Properties.Resources.libFrontPanel_pinv);
	/// </example>
	/// <item>Optional: In a static constructor of your application, call EmbeddedDllClass.LoadDll() to load the DLLs you have extracted. This is not necessary for pinvoke</item>
	/// <example>
	///               EmbeddedDllClass.LoadDll("myscrewball.dll");
	/// </example>
	/// <item>Continue using standard Pinvoke methods for the desired functions in the DLL</item>
	/// </list>
	/// </summary>
	public static class EmbeddedApps {
		/// <summary>
		/// Extract DLLs from resources to temporary folder
		/// </summary>
		/// <param name="exeName">name of EXE file to create (including exe suffix)</param>
		/// <param name="resourceBytes">The resource name (fully qualified)</param>
		public static string ExtractEmbeddedExe(string exePath, byte[] resourceBytes) {
			// See if the file exists, avoid rewriting it if not necessary
			bool rewrite = true;
			if (File.Exists(exePath)) {
				byte[] existing = File.ReadAllBytes(exePath);
				if (resourceBytes.SequenceEqual(existing)) {
					rewrite = false;
				}
			}
			if (rewrite) {
				File.WriteAllBytes(exePath, resourceBytes);
			}
			return exePath;
		}
	}
}