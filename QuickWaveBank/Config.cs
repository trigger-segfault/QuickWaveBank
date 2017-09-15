using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using QuickWaveBank.Properties;

namespace QuickWaveBank {
	/**<summary>The different types of compression formats.</summary>*/
	public enum CompressionFormats {
		PCM = 0,
		ADPCM = 2,
		xWMA = 353
	}
	/**<summary>The available samples per block of the ADPCM format.</summary>*/
	public enum ADPCMSamplesPerBlock {
		S32 = 32,
		S64 = 64,
		S128 = 128,
		S256 = 256,
		S512 = 512
	}
	/**<summary>How to interact with the user when an audio file needs converting.</summary>*/
	[Flags]
	public enum ConvertOptions {
		// Full Flags
		Ask				= 0x0,
		Auto			= 0x3,
		WaitTillBuild	= 0x4,
		
		// Flags
		AskConvert		= 0x0,
		AskOverwrite	= 0x0,
		AutoConvert		= 0x1,
		AutoOverwrite	= 0x2
	}

	/**<summary>The config settings handler.</summary>*/
	public static class Config {
		//=========== MEMBERS ============
		#region Members

		// General
		/**<summary>The output wave bank file.</summary>*/
		public static string OutputFile { get; set; }
		/**<summary>True if drag and drop is enabled.</summary>*/
		public static bool DragAndDrop { get; set; } = true;
		/**<summary>True if the console log is shown by default when building.</summary>*/
		public static bool ShowLog { get; set; } = false;
		/**<summary>True if track names are shown next to each wave entry.</summary>*/
		public static bool TrackNames { get; set; } = true;
		/**<summary>The current volume.</summary>*/
		public static double Volume { get; set; } = 0.5;
		/**<summary>True if the volume is muted.</summary>*/
		public static bool Muted { get; set; } = false;
		/**<summary>How file conversion is handled.</summary>*/
		public static ConvertOptions ConvertOption { get; set; } = ConvertOptions.Ask;
		/**<summary>The last visited directory of the folder browser.</summary>*/
		public static string LastFolderBrowser { get; set; }
		/**<summary>True if quick wave bank asks to save the track list on closing.</summary>*/
		public static bool SaveConfirmation { get; set; } = true;

		// Advanced
		/**<summary>True if the wave bank is a streaming wave bank.</summary>*/
		public static bool Streaming { get; set; } = false;
		/**<summary>The format of the wave bank compression.</summary>*/
		public static CompressionFormats Format { get; set; } = CompressionFormats.xWMA;
		/**<summary>The quality of the xWMA format [1, 100].</summary>*/
		public static int WMAQuality { get; set; } = 60;
		/**<summary>The samples per block of the ADPCM format.</summary>*/
		public static ADPCMSamplesPerBlock SamplesPerBlock { get; set; } = ADPCMSamplesPerBlock.S128;

		#endregion
		//=========== LOADING ============
		#region Loading

		/**<summary>Loads the settings.</summary>*/
		public static void Load() {
			// General
			OutputFile = Settings.Default.OutputFile;
			DragAndDrop = Settings.Default.DragAndDrop;
			ShowLog = Settings.Default.ShowLog;
			TrackNames = Settings.Default.TrackNames;
			Volume = Settings.Default.Volume;
			Muted = Settings.Default.Muted;
			LastFolderBrowser = Settings.Default.LastFolderBrowser;
			SaveConfirmation = Settings.Default.SaveConfirmation;
			if (string.IsNullOrEmpty(LastFolderBrowser)) {
				LastFolderBrowser = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			}
			ConvertOptions convertOption;
			if (Enum.TryParse(Settings.Default.ConvertOption, out convertOption))
				ConvertOption = convertOption;
			else
				ConvertOption = ConvertOptions.Ask;

			if (string.IsNullOrEmpty(OutputFile))
				OutputFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Wave Bank.xwb");

			// Advanced
			Streaming = Settings.Default.Streaming;
			WMAQuality = Settings.Default.WMAQuality;
			SamplesPerBlock = (ADPCMSamplesPerBlock)Settings.Default.ADPCMSamplesPerBlock;
			CompressionFormats format;
			if (Enum.TryParse(Settings.Default.Format, out format))
				Format = format;
			else
				Format = CompressionFormats.xWMA;

		}
		/**<summary>Saves the settings.</summary>*/
		public static void Save() {
			// General
			Settings.Default.OutputFile = OutputFile;
			Settings.Default.DragAndDrop = DragAndDrop;
			Settings.Default.ShowLog = ShowLog;
			Settings.Default.TrackNames = TrackNames;
			Settings.Default.Volume = Volume;
			Settings.Default.Muted = Muted;
			Settings.Default.ConvertOption = ConvertOption.ToString();
			Settings.Default.LastFolderBrowser = LastFolderBrowser;
			Settings.Default.SaveConfirmation = SaveConfirmation;

			// Advanced
			Settings.Default.Streaming = Streaming;
			Settings.Default.Format = Format.ToString();
			Settings.Default.WMAQuality = WMAQuality;
			Settings.Default.ADPCMSamplesPerBlock = (int)SamplesPerBlock;

			Settings.Default.Save();
		}

		#endregion
	}
}
