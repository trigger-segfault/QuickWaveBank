using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.IO;
using QuickWaveBank.Xap;
using WPF.JoshSmith.ServiceProviders.UI;
using QuickWaveBank.Util;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using QuickWaveBank.Windows;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Interop;
using System.Management;
using Microsoft.Win32;
using System.Timers;
using System.Threading;
using Timer = System.Timers.Timer;
using System.Media;
using QuickWaveBank.Extracting;
using System.Runtime.InteropServices;

namespace QuickWaveBank {
	/**<summary>The main window of the program.</summary>*/
	public partial class MainWindow : Window {
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The path of the temporary folder.</summary>*/
		private static readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "TriggersToolsGames", "QuickWaveBank");
		private static readonly string TempConverting = Path.Combine(TempDirectory, "Converting");
		/**<summary>The path of the temporary XactBld3 executable.</summary>*/
		private static readonly string TempXactBld = Path.Combine(TempDirectory, "XactBld3.exe");
		/**<summary>The path of the temporary BuildConsole executable.</summary>*/
		private static readonly string TempBuildConsole = Path.Combine(TempDirectory, "QuickWaveBankBuildConsole.exe");
		/**<summary>The path of the temporary Xap project file.</summary>*/
		private static readonly string TempProjectFile = Path.Combine(TempDirectory, "WaveBankProject.xap");
		/**<summary>The path of the temporary created wave bank.</summary>*/
		private static readonly string TempWaveBank = Path.Combine(TempDirectory, "Wave Bank.xwb");
		/**<summary>The path of the temporary created wave bank.</summary>*/
		private const int CancelExitCode = -1073741510;
		/**<summary>The path of the temporary created wave bank.</summary>*/
		private const int FailedExitCode = -100;
		/**<summary>The main part of the window title.</summary>*/
		private const string MainTitle = "Quick Wave Bank";
		/**<summary>The list of supported file formats.</summary>*/
		private static readonly Dictionary<string, string> SupportedFormats = new Dictionary<string, string>(){
			{ "Wave Files",					"*.wav" },
			{ "MP3 Files",					"*.mp3;*.mp2;*.mpga" },
			{ "MP4 Files",                  "*.mp4" },
			{ "AVI Files",                  "*.avi" },
			{ "AAC Files",					"*.m4a;*.aac" },
			{ "FLAC Files",					"*.flac" },
			{ "Ogg Vorbis Files",			"*.ogg" },
			{ "Windows Media Audio Files",	"*.wma" },
			{ "Apple AIFF Files",           "*.aif;*.aiff;*aifc" }
		};


		#endregion
		//=========== MEMBERS ============
		#region Members
		//--------------------------------
		#region Wave List

		/**<summary>The drag and drop manager for the wave list view.</summary>*/
		private ListViewDragDropManager<ListViewItem> dropManager;
		/**<summary>The wave list view items.</summary>*/
		private ObservableCollection<ListViewItem> waveListViewItems = new ObservableCollection<ListViewItem>();
		/**<summary>The list of wave files.</summary>*/
		private List<string> waveFiles = new List<string>();
		/**<summary>True if the current wave list is untitiled.</summary>*/
		bool untitled = true;
		/**<summary>The current wave list file.</summary>*/
		string listFile = "";
		/**<summary>True if the wave list has been modified since last saved.</summary>*/
		bool modified = false;

		#endregion
		//--------------------------------
		#region Processing

		/**<summary>The process for the builder.</summary>*/
		private Process buildProcess = null;
		/**<summary>True if building.</summary>*/
		private bool building = false;
		/**<summary>True if the build was canceled.</summary>*/
		private bool buildCanceled = false;
		/**<summary>The last modified time of the wave bank before building.</summary>*/
		private DateTime lastModified;
		/**<summary>The timer for updating the building label.</summary>*/
		private Timer buildingAnimTimer = new Timer(1000);
		/**<summary>The number of dots in the building label animation.</summary>*/
		private int buildingAnimDots = 0;
		/**<summary>True if the console is showing.</summary>*/
		private bool showingLog;
		/**<summary>True if the console is showing.</summary>*/
		private DateTime buildStart;
		/**<summary>The thread runnign the wave bank extraction process.</summary>*/
		private Thread processThread;

		#endregion
		//--------------------------------
		#region Playback

		/**<summary>The player for wave files.</summary>*/
		private MediaPlayer player = new MediaPlayer();
		/**<summary>The timer used for updating the player element.</summary>*/
		private Timer playerTimer = new Timer(500);
		/**<summary>The list view item that is being played.</summary>*/
		private ListViewItem playItem = null;
		/**<summary>True if playback is paused. MediaPlayer doesn't keep track of this annoyingly.</summary>*/
		private bool paused = false;

		#endregion
		//--------------------------------
		#region Other

		/**<summary>True if the window has been loaded.</summary>*/
		private bool loaded = false;

		#endregion
		//--------------------------------
		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the main window.</summary>*/
		public MainWindow() {
			InitializeComponent();

			// Setup temporary stuff
			if (!Directory.Exists(TempDirectory)) {
				Directory.CreateDirectory(TempDirectory);
			}
			if (!Directory.Exists(TempConverting)) {
				Directory.CreateDirectory(TempConverting);
			}
			EmbeddedResources.Extract(TempXactBld, Properties.Resources.XactBld3);
			EmbeddedResources.Extract(TempBuildConsole, "BuildConsole.exe");

			// Setup the wave list
			dropManager = new ListViewDragDropManager<ListViewItem>();
			dropManager.ProcessDrop += OnProcessDrop;
			listView.ItemsSource = waveListViewItems;

			// Setup build process
			ProcessStartInfo start = new ProcessStartInfo();
			start.FileName = TempBuildConsole;
			start.WorkingDirectory = TempDirectory;
			start.WindowStyle = ProcessWindowStyle.Minimized;
			buildProcess = new Process();
			buildProcess.StartInfo = start;
			buildProcess.EnableRaisingEvents = true;
			buildProcess.Exited += OnBuildProcessExited;
			buildingAnimTimer.AutoReset = true;
			buildingAnimTimer.Elapsed += OnBuildingAnimTimerElapsed;
			
			// Hide hidden stuff
			gridBuilding.Visibility = Visibility.Hidden;
			gridExtracting.Visibility = Visibility.Hidden;
			labelDrop.Visibility = Visibility.Hidden;

			// Setup playback stuff
			player.Volume = 0.5;
			playerTimer.AutoReset = true;
			playerTimer.Elapsed += OnPlayerTimerElapsed;

			// Close orphaned build consoles
			Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(TempBuildConsole));
			foreach (Process process in processes) {
				process.KillWithChildren();
			}
			
			// Disable drag/drop text in textboxes so you can scroll their contents easily
			DataObject.AddCopyingHandler(textBoxOutput, OnTextBoxCancelDrag);

			// Remove quotes from "Copy Path" command on paste
			DataObject.AddPastingHandler(textBoxOutput, OnTextBoxQuotesPaste);

			LoadConfig();
			UpdateTitle();
		}

		#endregion
		//============ CONFIG ============
		#region Config

		/**<summary>Loads the config settings and sets up control states.</summary>*/
		private void LoadConfig() {
			Config.Load();

			textBoxOutput.Text = Config.OutputFile;
			menuItemSaveConfirmation.IsChecked = Config.SaveConfirmation;
			menuItemTrackNames.IsChecked = Config.TrackNames;
			menuItemDragAndDrop.IsChecked = Config.DragAndDrop;
			menuItemShowLog.IsChecked = Config.ShowLog;
			menuItemStreaming.IsChecked = Config.Streaming;
			switch (Config.Format) {
			case CompressionFormats.PCM:
				menuItemCompression.Header = "Format: PCM";
				break;
			case CompressionFormats.ADPCM:
				menuItemCompression.Header = "Format: ADPCM, " + ((int)Config.SamplesPerBlock).ToString();
				break;
			case CompressionFormats.xWMA:
				menuItemCompression.Header = "Format: xWMA, " + Config.WMAQuality.ToString();
				break;
			}
			if (Config.DragAndDrop)
				dropManager.ListView = listView;

			menuItemAutoConvert.IsChecked = Config.ConvertOption.HasFlag(ConvertOptions.AutoConvert);
			menuItemAutoOverwrite.IsChecked = Config.ConvertOption.HasFlag(ConvertOptions.AutoOverwrite);
			menuItemWaitTillBuild.IsChecked = Config.ConvertOption.HasFlag(ConvertOptions.WaitTillBuild);

			menuItemAutoConvert.IsEnabled = !Config.ConvertOption.HasFlag(ConvertOptions.WaitTillBuild);
			menuItemAutoOverwrite.IsEnabled = !Config.ConvertOption.HasFlag(ConvertOptions.WaitTillBuild);

			UpdateButtons();
			UpdateVolume();
		}


		#endregion
		//======== WINDOW EVENTS =========
		#region Window Events

		private void OnWindowLoaded(object sender, RoutedEventArgs e) {
			loaded = true;

			string[] args = Environment.GetCommandLineArgs();
			if (args.Length > 1) {
				try {
					LoadWaveList(args[1]);
					listFile = args[1];
					untitled = false;
					UpdateTitle();
				}
				catch (Exception ex) {
					var result2 = TriggerMessageBox.Show(this, MessageIcon.Error, "Failed to load track list! Would you like to see the error?", "Save Failed", MessageBoxButton.YesNo);
					if (result2 == MessageBoxResult.Yes)
						ErrorMessageBox.Show(ex);
				}
			}
		}
		private void OnWindowClosing(object sender, CancelEventArgs e) {
			if (Config.SaveConfirmation && modified) {
				var result = TriggerMessageBox.Show(this, MessageIcon.Question, "Do you want to save changes to the track list before closing?", "Unsaved Changes", MessageBoxButton.YesNoCancel);
				if (result == MessageBoxResult.Yes) {
					if (untitled) {
						SaveFileDialog dialog = new SaveFileDialog();
						dialog.Title = "Save Track List As";
						dialog.DefaultExt = ".txt";
						dialog.Filter = "Text Files|*.txt|All Files|*.*";
						dialog.FilterIndex = 0;
						dialog.AddExtension = true;
						dialog.OverwritePrompt = true;
						dialog.FileName = (untitled ? "Track List.txt" : Path.GetFileName(listFile));
						if (!untitled)
							dialog.InitialDirectory = Path.GetDirectoryName(listFile);
						var result2 = dialog.ShowDialog(this);
						if (result2.HasValue && result2.Value) {
							try {
								SaveWaveList(dialog.FileName);
								listFile = dialog.FileName;
								untitled = false;
								modified = false;
								UpdateTitle();
							}
							catch (Exception ex) {
								result = TriggerMessageBox.Show(this, MessageIcon.Error, "Failed to save track list! Would you like to see the error?", "Save Failed", MessageBoxButton.YesNo);
								if (result == MessageBoxResult.Yes)
									ErrorMessageBox.Show(ex);
							}
						}
						else {
							e.Cancel = true;
						}
					}
					else {
						try {
							OnSaveWaveList(null, null);
						}
						catch (Exception ex) {
							var result2 = TriggerMessageBox.Show(this, MessageIcon.Error, "Failed to save track list! Would you like to see the error?", "Save Failed", MessageBoxButton.YesNo);
							if (result2 == MessageBoxResult.Yes)
								ErrorMessageBox.Show(ex);
						}
					}
				}
				else if (result == MessageBoxResult.Cancel) {
					e.Cancel = true;
				}
			}
			if (building) {
				buildCanceled = true;
				buildProcess.KillWithChildren();
			}
			try {
				Config.Save();
			}
			catch { }
		}
		private void OnWindowResized(object sender, SizeChangedEventArgs e) {
			UpdateListViewItemRange(0, waveListViewItems.Count - 1);
		}
		private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e) {
			// Make text boxes lose focus on click away
			FocusManager.SetFocusedElement(this, this);
		}
		private void OnTextBoxCancelDrag(object sender, DataObjectCopyingEventArgs e) {
			if (e.IsDragDrop)
				e.CancelCommand();
		}
		private void OnTextBoxQuotesPaste(object sender, DataObjectPastingEventArgs e) {
			var isText = e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true);
			if (!isText) return;

			var text = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;
			if (text.StartsWith("\"") || text.EndsWith("\"")) {
				text = text.Trim('"');
				Clipboard.SetText(text);
			}
		}

		#endregion
		//======= WAVE MANAGEMENT ========
		#region Wave Management

		/**<summary>Loads a wave list.</summary>*/
		private void LoadWaveList(string filepath) {
			StreamReader reader = new StreamReader(new FileStream(filepath, FileMode.Open));
			List<string> files = new List<string>();
			while (!reader.EndOfStream) {
				string file = reader.ReadLine();
				try {
					if (File.Exists(file)) {
						files.Add(file);
					}
				}
				catch { }
			}
			reader.Close();
			AddWaves(files.ToArray(), true, true);
		}
		/**<summary>Saves the wave list.</summary>*/
		private void SaveWaveList(string filepath) {
			StreamWriter writer = new StreamWriter(new FileStream(filepath, FileMode.OpenOrCreate));
			foreach (string waveFile in waveFiles) {
				writer.WriteLine(waveFile);
			}

			writer.Close();
		}
		/**<summary>Loads waves from a directory.</summary>*/
		private void AddWaves(string directory, bool clearCurrent, bool silent) {
			List<string> files = new List<string>(Directory.GetFiles(directory));
			for (int i = 0; i < files.Count; i++) {
				bool validAudio = false;
				string ext = Path.GetExtension(files[i]).ToLower();
				foreach (string extensions in SupportedFormats.Values) {
					if (extensions.Contains(ext)) {
						validAudio = true;
						break;
					}
				}
				if (!validAudio) {
					files.RemoveAt(i);
					i--;
				}
			}
			AddWaves(files.ToArray(), clearCurrent, silent);
		}
		/**<summary>Loads waves from a directory.</summary>*/
		private void AddWaves(string[] files, bool clearCurrent, bool silent) {
			if (clearCurrent) {
				OnStop(null, null);
				waveFiles.Clear();
				waveListViewItems.Clear();
				modified = true;
			}

			var result = MessageBoxResult.Yes;
			int convertCount = 0;
			int existsCount = 0;
			bool wait = Config.ConvertOption.HasFlag(ConvertOptions.WaitTillBuild);
			bool convert = Config.ConvertOption.HasFlag(ConvertOptions.AutoConvert) && !silent && !wait;
			bool overwrite = Config.ConvertOption.HasFlag(ConvertOptions.AutoOverwrite) && !silent && !wait;
			List<string> convertErrors = new List<string>();

			if (!silent && !Config.ConvertOption.HasFlag(ConvertOptions.WaitTillBuild)) {
				foreach (string file in files) {
					if (Path.GetExtension(file).ToLower() != ".wav") {
						convertCount++;
						if (File.Exists(Path.ChangeExtension(file, ".wav")))
							existsCount++;
					}
				}
			}

			if (convertCount > 0 && !Config.ConvertOption.HasFlag(ConvertOptions.AutoConvert) && !silent && !wait) {
				result = TriggerMessageBox.Show(this, MessageIcon.Question,
					"Would you like to convert " + convertCount + " audio file" + (convertCount > 1 ? "s" : "") +
					" to wave format? If not, " + (existsCount > 1 ? "they" : "it") + " will be converted at build-time.",
					"Convert to Wave", MessageBoxButton.YesNoCancel
				);
				if (result == MessageBoxResult.Yes)
					convert = true;
			}
			if (result != MessageBoxResult.Cancel) {
				if (convert && existsCount != 0 && !Config.ConvertOption.HasFlag(ConvertOptions.AutoOverwrite)) {
					result = TriggerMessageBox.Show(this, MessageIcon.Question, "Converting: " + existsCount + " wave file" +
						(existsCount != 1 ? "s" : "") + " with the same name already exist" + (existsCount != 1 ? "" : "s") +
						". Would you like to replace " + (existsCount > 1 ? "them" : "it") + " ?",
						"Already Exists", MessageBoxButton.YesNoCancel
					);
					if (result != MessageBoxResult.Yes)
						overwrite = true;
				}
				if (result != MessageBoxResult.Cancel) {
					for (int i = 0; i < files.Length; i++) {
						string audioFile = files[i];
						if (Path.GetExtension(audioFile).ToLower() != ".wav") {
							string waveFile = Path.ChangeExtension(audioFile, ".wav");
							if (convert && (overwrite || !File.Exists(waveFile))) {
								if (FFmpeg.Convert(audioFile, waveFile)) {
									audioFile = waveFile;
								}
								else {
									convertErrors.Add((i + 1).ToString() + Path.GetFileName(audioFile));
									continue;
								}
							}
						}
						waveFiles.Add(audioFile);
						waveListViewItems.Add(MakeListViewItem(waveListViewItems.Count, audioFile));
						modified = true;
					}

				}
			}
			labelWaveEntries.Content = "Wave Entries: " + waveFiles.Count;
			if (listView.SelectedIndex == -1 && waveFiles.Count > 0) {
				listView.SelectedIndex = 0;
			}
			if (!clearCurrent && waveFiles.Count > 0) {
				listView.ScrollIntoView(waveListViewItems[waveListViewItems.Count - 1]);
				listView.SelectedIndex = waveListViewItems.Count - 1;
			}
			UpdateButtons();
			UpdateTitle();
			if (convertErrors.Count > 0) {
				LogError error = new LogError(false, "Failed to convert the following files:", "");
				foreach (string file in convertErrors) {
					error.Message += "\n    " + file;
				}
				ErrorLogWindow.Show(this, new LogError[] { error });
			}
		}
		/**<summary>Adds a single wave.</summary>*/
		private void AddWave(string file, bool silent) {
			string outputFile = Path.ChangeExtension(file, ".wav");
			string ext = Path.GetExtension(file).ToLower();
			var result = MessageBoxResult.Yes;
			if (ext != ".wav" && !silent && !Config.ConvertOption.HasFlag(ConvertOptions.WaitTillBuild)) {
				if (!Config.ConvertOption.HasFlag(ConvertOptions.AutoConvert))
					result = TriggerMessageBox.Show(this, MessageIcon.Question, "Would you like to convert this audio file to wave format? If not, it will be converted at build-time.", "Convert to Wave", MessageBoxButton.YesNoCancel);
				if (result == MessageBoxResult.Yes) {
					if (File.Exists(outputFile) && !Config.ConvertOption.HasFlag(ConvertOptions.AutoOverwrite))
						result = TriggerMessageBox.Show(this, MessageIcon.Question, "Converting: A wave file with the same name already exists. Would you like to replace it?", "Already Exists", MessageBoxButton.YesNoCancel);
					if (result == MessageBoxResult.Yes) {
						if (!FFmpeg.Convert(file, outputFile)) {
							TriggerMessageBox.Show(this, MessageIcon.Error, "Failed to convert audio file to wave format!", "Conversion Error");
							result = MessageBoxResult.Cancel;
						}
						else {
							file = outputFile;
						}
					}
				}
			}
			if (result != MessageBoxResult.Cancel) {
				waveFiles.Add(file);
				ListViewItem item = MakeListViewItem(waveListViewItems.Count, file);
				waveListViewItems.Add(item);
				listView.ScrollIntoView(item);
				labelWaveEntries.Content = "Wave Entries: " + waveFiles.Count;
				listView.SelectedIndex = waveFiles.Count - 1;
				UpdateButtons();
				modified = true;
			}
			UpdateTitle();
		}
		private void OnNewWaveList(object sender, RoutedEventArgs e) {
			var result = TriggerMessageBox.Show(this, MessageIcon.Question, "Are you sure you want to start a new track list?", "New Track List", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.Yes) {
				OnStop(null, null);
				waveFiles.Clear();
				waveListViewItems.Clear();
				UpdateButtons();
				listFile = "";
				untitled = true;
				modified = false;
				UpdateTitle();
			}
		}
		private void OnLoadWaveList(object sender, RoutedEventArgs e) {
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Title = "Load Track List";
			dialog.DefaultExt = ".txt";
			dialog.Filter = "Text Files|*.txt|All Files|*.*";
			dialog.FilterIndex = 0;
			if (!untitled)
				dialog.InitialDirectory = Path.GetDirectoryName(listFile);
			var result = dialog.ShowDialog(this);
			if (result.HasValue && result.Value) {
				try {
					LoadWaveList(dialog.FileName);
					listFile = dialog.FileName;
					untitled = false;
					modified = false;
					UpdateTitle();
				}
				catch (Exception ex) {
					var result2 = TriggerMessageBox.Show(this, MessageIcon.Error, "Failed to load track list! Would you like to see the error?", "Load Failed", MessageBoxButton.YesNo);
					if (result2 == MessageBoxResult.Yes)
						ErrorMessageBox.Show(ex);
				}
			}
		}
		private void OnSaveWaveList(object sender, RoutedEventArgs e) {
			if (untitled) {
				OnSaveWaveListAs(null, null);
			}
			else {
				try {
					SaveWaveList(listFile);
					modified = false;
					UpdateTitle();
				}
				catch (Exception ex) {
					var result2 = TriggerMessageBox.Show(this, MessageIcon.Error, "Failed to save track list! Would you like to see the error?", "Save Failed", MessageBoxButton.YesNo);
					if (result2 == MessageBoxResult.Yes)
						ErrorMessageBox.Show(ex);
				}
			}
		}
		private void OnSaveWaveListAs(object sender, RoutedEventArgs e) {
			SaveFileDialog dialog = new SaveFileDialog();
			dialog.Title = "Save Track List As";
			dialog.DefaultExt = ".txt";
			dialog.Filter = "Text Files|*.txt|All Files|*.*";
			dialog.FilterIndex = 0;
			dialog.AddExtension = true;
			dialog.OverwritePrompt = true;
			dialog.FileName = (untitled ? "Track List.txt" : Path.GetFileName(listFile));
			if (!untitled)
				dialog.InitialDirectory = Path.GetDirectoryName(listFile);
			var result = dialog.ShowDialog(this);
			if (result.HasValue && result.Value) {
				try {
					SaveWaveList(dialog.FileName);
					listFile = dialog.FileName;
					untitled = false;
					modified = false;
					UpdateTitle();
				}
				catch (Exception ex) {
					var result2 = TriggerMessageBox.Show(this, MessageIcon.Error, "Failed to save track list! Would you like to see the error?", "Save Failed", MessageBoxButton.YesNo);
					if (result2 == MessageBoxResult.Yes)
						ErrorMessageBox.Show(ex);
				}
			}
		}
		private ListViewItem MakeListViewItem(int index, string waveFile) {
			ListViewItem item = new ListViewItem();
			item.SnapsToDevicePixels = true;
			item.UseLayoutRounding = true;
			item.Height = 18;
			item.MouseDoubleClick += OnPlayDoubleClick;

			Grid grid = new Grid();
			ColumnDefinition c0 = new ColumnDefinition();
			c0.Width = new GridLength(40);
			ColumnDefinition c1 = new ColumnDefinition();
			c1.Width = new GridLength(1, GridUnitType.Star);
			ColumnDefinition c2 = new ColumnDefinition();
			c2.Width = new GridLength(1, GridUnitType.Auto);
			grid.ColumnDefinitions.Add(c0);
			grid.ColumnDefinitions.Add(c1);
			grid.ColumnDefinitions.Add(c2);
			grid.Width = listView.ActualWidth - 26;

			Image playImage = new Image();
			playImage.Width = 16;
			playImage.Height = 16;
			playImage.HorizontalAlignment = HorizontalAlignment.Left;
			playImage.VerticalAlignment = VerticalAlignment.Center;
			grid.Children.Add(playImage);
			Grid.SetColumn(playImage, 0);

			TextBlock indexText = new TextBlock();
			indexText.HorizontalAlignment = HorizontalAlignment.Left;
			indexText.VerticalAlignment = VerticalAlignment.Center;
			indexText.TextAlignment = TextAlignment.Right;
			indexText.Width = 30;
			indexText.FontWeight = FontWeights.Bold;
			indexText.Text = (index + 1).ToString();
			grid.Children.Add(indexText);
			Grid.SetColumn(indexText, 0);

			TextBlock nameText = new TextBlock();
			nameText.HorizontalAlignment = HorizontalAlignment.Left;
			nameText.VerticalAlignment = VerticalAlignment.Center;
			nameText.Text = Path.GetFileName(waveFile);
			nameText.TextTrimming = TextTrimming.CharacterEllipsis;
			grid.Children.Add(nameText);
			Grid.SetColumn(nameText, 1);

			TextBlock typeText = new TextBlock();
			typeText.HorizontalAlignment = HorizontalAlignment.Right;
			typeText.VerticalAlignment = VerticalAlignment.Center;
			typeText.TextAlignment = TextAlignment.Right;
			typeText.Text = (!Config.TrackNames ? "" : "(" + WaveBankExtractor.GetTrackName(index) + ")");
			typeText.Padding = new Thickness(2, 0, 5, 0);
			typeText.FontSize = 10;
			grid.Children.Add(typeText);
			Grid.SetColumn(typeText, 2);

			item.Content = grid;
			return item;
		}
		private void UpdateListViewItem(ListViewItem item, int index, string waveFile) {
			Grid grid = (Grid)item.Content;
			grid.Width = listView.ActualWidth - 26;

			Image playImage = (Image)grid.Children[0];
			playImage.Source = (item == playItem ?
				new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/" + (paused ? "Pause" : "Play") + "Unflipped.png")) :
				null
			);

			TextBlock indexText = (TextBlock)grid.Children[1];
			indexText.Text = (index + 1).ToString();

			TextBlock nameText = (TextBlock)grid.Children[2];
			nameText.Text = Path.GetFileName(waveFile);
			nameText.FontWeight = (item == playItem ? FontWeights.Bold : FontWeights.Regular);

			TextBlock typeText = (TextBlock)grid.Children[3];
			typeText.Text = (!Config.TrackNames ? "" : "(" + WaveBankExtractor.GetTrackName(index) + ")");
		}
		private void UpdateListViewItemRange(int minIndex, int maxIndex) {
			for (int i = minIndex; i <= maxIndex; i++) {
				UpdateListViewItem(waveListViewItems[i], i, waveFiles[i]);
			}
		}

		#endregion
		//========== PROCESSING ==========
		#region Processing
		//--------------------------------
		#region Project File

		/**<summary>Writes the Xap project file.</summary>*/
		private void WriteXapProject() {
			MemoryStream stream = new MemoryStream(Properties.Resources.WaveBankProject);
			XapFile xapFile = new XapFile();
			xapFile.Load(stream);
			stream.Close();

			XapGroup preset = xapFile.Root.GetGroup("Global Settings").GetGroup("Compression Preset");
			XapGroup waveBank = xapFile.Root.GetGroup("Wave Bank");

			WriteXapCompression(preset);

			if (Config.Streaming)
				waveBank.SetVariable("Streaming", "1");

			for (int i = 0; i < waveFiles.Count; i++) {
				WriteXapWave(waveBank, i);
			}

			xapFile.Save(TempProjectFile);
		}
		/**<summary>Adds a wave to the Xap project file's wave bank.</summary>*/
		private void WriteXapWave(XapGroup waveBank, int index) {
			XapGroup group = new XapGroup("Wave");
			string waveFile = waveFiles[index];
			//TODO: Is numbering each track really necessary for clarity?
			string sanitizedName = XapFile.SanitizeString(Path.GetFileNameWithoutExtension(waveFile), "_", false, false, false);
			group.AddVariable("Name", (index + 1).ToString("D2") + " " + sanitizedName);

			// Use the temporary directory for converted files and files containing format characters
			//FIXME: Currently forced to avoid potentially invalid wave file formats, and invalid non-ascii path characters
			//if (XapFile.ContainsFormatCharacter(waveFile) || Path.GetExtension(waveFile).ToLower() != ".wav") {
				// Relative path is essential for avoiding usernames with special characters
				waveFile = GetTempWaveRelativePath(index);
			//}
			group.AddVariable("File", waveFile);
			waveBank.AddGroup(group);
		}
		/**<summary>Sets the wave bank compression settings.</summary>*/
		private void WriteXapCompression(XapGroup preset) {
			if (Config.Format != CompressionFormats.xWMA) {
				preset.SetVariable("PC Format Tag", ((int)Config.Format).ToString());
				preset.RemoveVariable("WMA Quality", 1);
				if (Config.Format == CompressionFormats.ADPCM)
					preset.AddVariable("Samples Per Block", ((int)Config.SamplesPerBlock).ToString());
			}
			else {
				preset.SetVariable("WMA Quality", Config.WMAQuality.ToString(), 1);
			}
		}

		#endregion
		//--------------------------------
		#region Building

		private string GetTempWaveFileName(int index) {
			return "Track_" + (index+1).ToString("D2") + ".wav";
		}
		private string GetTempWaveRelativePath(int index) {
			//TODO: Add TempConverting relative path as constant
			return Path.Combine(Path.GetFileName(TempConverting), GetTempWaveFileName(index));
		}
		private string GetTempWavePath(int index) {
			return Path.Combine(TempConverting, GetTempWaveFileName(index));
		}
		private void OnBuild(object sender, RoutedEventArgs e) {
			if (waveFiles.Count == 0) {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "You must add audio tracks before building.", "No Tracks");
				return;
			}
			processThread = new Thread(() => {
				WriteXapProject();

				Dispatcher.Invoke(() => {
					try {
						FileStream stream = new FileStream(TempWaveBank, FileMode.OpenOrCreate, FileAccess.Write);
						stream.Close();
					}
					catch (Exception) {
						TriggerMessageBox.Show(this, MessageIcon.Error, "Cannot write to temporary file!", "Write Error");
						return;
					}
					try {
						FileStream stream = new FileStream(Config.OutputFile, FileMode.OpenOrCreate, FileAccess.Write);
						stream.Close();
					}
					catch (Exception) {
						TriggerMessageBox.Show(this, MessageIcon.Error, "Cannot write to output file!", "Write Error");
						return;
					}

					buildStart = DateTime.Now;
					showingLog = Config.ShowLog;
					buildCanceled = false;
					buttonConsole.IsEnabled = false;
					buttonConsole.Content = "Show Log";
					building = true;
					gridBuilding.Visibility = Visibility.Visible;
					gridWindow.IsEnabled = false;
					buildingAnimTimer.Start();
					buildingAnimDots = 0;
					labelBuilding.Content = "Copying / Converting";
					labelBuildTime.Content = "Time: " + (DateTime.Now - buildStart).ToString(@"m\:ss");
				});
				
				List<string> convertErrors = new List<string>();
				List<string> copyErrors = new List<string>();
				for (int i = 0; i < waveFiles.Count; i++) {
					string audioFile = waveFiles[i];
					string waveFile = GetTempWavePath(i);
					//FIXME: Currently forced to avoid potentially invalid wave file formats, and invalid non-ascii path characters
					//if (Path.GetExtension(audioFile).ToLower() != ".wav") {
						if (!FFmpeg.Convert(audioFile, waveFile))
							convertErrors.Add((i + 1).ToString() + " " + Path.GetFileName(audioFile));
					/*}
					else if (XapFile.ContainsFormatCharacter(audioFile)) {
						// Use a temporary directory for files containing format characters in their paths
						try {
							File.Copy(audioFile, waveFile, true);
						}
						catch {
							copyErrors.Add((i + 1).ToString() + " " + Path.GetFileName(audioFile));
						}
					}*/
				}
				List<LogError> errors = new List<LogError>();
				if (convertErrors.Count > 0) {
					LogError error = new LogError(false, "Failed to convert the following files:", "");
					foreach (string file in convertErrors) {
						error.Message += "\n    " + file;
					}
					errors.Add(error);
				}
				if (copyErrors.Count > 0) {
					LogError error = new LogError(false, "Failed to copy the following files to the temporary directory:", "");
					foreach (string file in copyErrors) {
						error.Message += "\n    " + file;
					}
					errors.Add(error);
				}
				if (errors.Count > 0) {
					buildCanceled = true;
					OnBuildProcessExited(null, null);
					Dispatcher.Invoke(() => {
						ErrorLogWindow.Show(this, errors.ToArray());
					});
				}

				if (File.Exists(TempWaveBank))
					lastModified = File.GetLastWriteTime(TempWaveBank);
				else
					lastModified = DateTime.Now;

				buildProcess.StartInfo.WindowStyle = (Config.ShowLog ? ProcessWindowStyle.Normal : ProcessWindowStyle.Minimized);
				buildProcess.Start();
				if (!Config.ShowLog) {
					Thread.Sleep(50);
					buildProcess.Hide();
				}
				Dispatcher.Invoke(() => {
					buttonConsole.IsEnabled = true;
					buttonConsole.Content = (Config.ShowLog ? "Hide" : "Show") + " Log";
					labelBuilding.Content = "Building Wave Bank";
				});
			});
			processThread.Start();
		}
		private void OnCancelBuildProcess(object sender, RoutedEventArgs e) {
			if (building) {
				buildCanceled = true;
				try {
					if (processThread.ThreadState != System.Threading.ThreadState.Stopped) {
						processThread.Abort();
					}
				}
				catch { }
				try {
					buildProcess.KillWithChildren();
					// Cleanup is handled in OnBuildProcessExited
				}
				catch (InvalidOperationException) {
					// Build process was never started. Manually cleanup
					OnBuildProcessExited(null, null);
				}
			}
		}
		private void OnBuildProcessExited(object sender, EventArgs e) {
			building = false;
			Dispatcher.Invoke(() => {
				buildingAnimTimer.Stop();
				try {
					if (buildProcess.ExitCode == CancelExitCode) {
						TriggerMessageBox.Show(this, MessageIcon.Info, "Wave Bank creation canceled.", "Build Canceled");
					}
					else if (buildProcess.ExitCode == FailedExitCode) {
						TriggerMessageBox.Show(this, MessageIcon.Error, "Wave Bank creation failed! See console log for more information.", "Build Failed");
					}
					else if (!buildCanceled) {
						bool error = true;
						try {
							if (File.Exists(TempWaveBank) && File.GetLastWriteTime(TempWaveBank) > lastModified) {
								// Success
								try {
									File.Copy(TempWaveBank, Config.OutputFile, true);
									File.Delete(TempWaveBank);
									error = false;
									var result = TriggerMessageBox.Show(this, MessageIcon.Info, "Wave Bank creation successful! Would you like to open the folder?", "Build Success", MessageBoxButton.YesNo);
									if (result == MessageBoxResult.Yes)
										Process.Start("explorer.exe", "/select, \"" + Config.OutputFile + "\"");
								}
								catch (Exception) {
									// Wave Bank not successfully written
									TriggerMessageBox.Show(this, MessageIcon.Error, "Failed to copy created Wave Bank from temporary directory!", "Copy Failed");
									error = false; // Handled
								}
							}
						}
						catch (Exception) { }
						if (error) {
							TriggerMessageBox.Show(this, MessageIcon.Error, "Wave Bank creation failed! See console log for more information.", "Build Failed");
						}
					}
				}
				catch (InvalidOperationException) {
					// Build process never started
				}

				// Cleanup temp converting wave files
				DirectoryInfo di = new DirectoryInfo(TempConverting);
				foreach (FileInfo file in di.GetFiles()) {
					file.Delete();
				}
				
				gridWindow.IsEnabled = true;
				gridBuilding.Visibility = Visibility.Hidden;
			});
		}
		private void OnBuildingAnimTimerElapsed(object sender, ElapsedEventArgs e) {
			string text = "";
			buildingAnimDots = (buildingAnimDots + 1) % 4;
			for (int i = 0; i < buildingAnimDots; i++) {
				text += " .";
			}
			Dispatcher.Invoke(() => {
				string message = (string)labelBuilding.Content;
				while (message.EndsWith(" ."))
					message = message.Substring(0, message.Length - 2);
				labelBuilding.Content = message + text;
				labelBuildTime.Content = "Time: " + (DateTime.Now - buildStart).ToString(@"m\:ss");
				message = (string)labelExtracting.Content;
				while (message.EndsWith(" ."))
					message = message.Substring(0, message.Length - 2);
				labelExtracting.Content = message + text;
				labelExtractTime.Content = "Time: " + (DateTime.Now - buildStart).ToString(@"m\:ss");
			});
		}
		private void OnShowLog(object sender, RoutedEventArgs e) {
			showingLog = !showingLog;
			if (showingLog) {
				buildProcess.Show();
				buttonConsole.Content = "Hide Log";
			}
			else {
				buildProcess.Hide();
				buttonConsole.Content = "Show Log";
			}
		}

		#endregion
		//--------------------------------
		#region Extracting

		private void OnExtract(object sender, RoutedEventArgs e) {
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Title = "Choose Wave Bank";
			dialog.Filter = "Wave Banks|*.xwb|All Files|*.*";
			dialog.FilterIndex = 0;
			dialog.InitialDirectory = TerrariaLocator.TerrariaContentDirectory ?? "";
			var result = dialog.ShowDialog(this);
			if (result.HasValue && result.Value) {
				string file = dialog.FileName;
				FolderBrowserDialog browser = new FolderBrowserDialog();
				browser.Description = "Choose wave output folder";
				browser.ShowNewFolderButton = true;
				try {
					browser.SelectedPath = Path.GetDirectoryName(file);
				}
				catch { }
				result = browser.ShowFolderBrowser(this);
				if (result.HasValue && result.Value) {
					string directory = browser.SelectedPath;
					browser.Dispose();
					browser = null;
					processThread = new Thread(() => {
						Extract(file, directory);
					});
					gridWindow.IsEnabled = false;
					gridExtracting.Visibility = Visibility.Visible;
					buildingAnimTimer.Start();
					buildingAnimDots = 0;
					buildStart = DateTime.Now;
					labelExtracting.Content = "Extracting Wave Bank";
					labelExtractTime.Content = "Time: " + (DateTime.Now - buildStart).ToString(@"m\:ss");
					processThread.Start();
				}
			}
		}
		private void OnCancelExtract(object sender, RoutedEventArgs e) {
			try {
				buildingAnimTimer.Stop();
				processThread.Abort();
				gridWindow.IsEnabled = true;
				gridExtracting.Visibility = Visibility.Hidden;
			}
			catch { }
			gridWindow.IsEnabled = true;
		}
		/**<summary>The wave bank extraction thread.</summary>*/
		private void Extract(string inputFile, string outputDirectory) {
			try {
				WaveBankExtractor.Extract(inputFile, outputDirectory);
				Dispatcher.Invoke(() => {
					buildingAnimTimer.Stop();
					var result = TriggerMessageBox.Show(this, MessageIcon.Info, "Wave Bank extraction complete! Would you like to open the folder?", "Build Success", MessageBoxButton.YesNo);
					if (result == MessageBoxResult.Yes)
						Process.Start(outputDirectory);
					gridWindow.IsEnabled = true;
					gridExtracting.Visibility = Visibility.Hidden;
				});
			}
			catch (ThreadAbortException) { }
			catch (ThreadInterruptedException) { }
			catch (Exception ex) {
				Dispatcher.Invoke(() => {
					buildingAnimTimer.Stop();
					var result2 = TriggerMessageBox.Show(this, MessageIcon.Error, "An error occurred while extracting the wave bank. Would you like to see the error?", MessageBoxButton.YesNo);
					if (result2 == MessageBoxResult.Yes)
						ErrorMessageBox.Show(ex, true);
					gridWindow.IsEnabled = true;
					gridExtracting.Visibility = Visibility.Hidden;
				});
			}
		}

		#endregion
		//--------------------------------
		#endregion
		//========= OUTPUT FILE ==========
		#region Output File

		private void OnSelectOutputFile(object sender, RoutedEventArgs e) {
			SaveFileDialog dialog = new SaveFileDialog();
			dialog.Title = "Choose Wave Bank Output File";
			dialog.Filter = "Wave Banks|*.xwb|All Files|*.*";
			dialog.FilterIndex = 0;
			dialog.DefaultExt = ".xwb";
			dialog.AddExtension = true;
			try {
				dialog.FileName = Path.GetFileName(Config.OutputFile);
			}
			catch { }
			try {
				dialog.InitialDirectory = Path.GetDirectoryName(Config.OutputFile);
			}
			catch {
				dialog.InitialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			}
			var result = dialog.ShowDialog(this);
			if (result.HasValue && result.Value) {
				Config.OutputFile = dialog.FileName;
				textBoxOutput.Text = dialog.FileName;
			}
		}
		private void OnOutputFileChanged(object sender, TextChangedEventArgs e) {
			Config.OutputFile = textBoxOutput.Text;
		}
		
		#endregion
		//========== FILE DROP ===========
		#region File Drop

		private void OnFileDrop(object sender, DragEventArgs e) {
			labelDrop.Visibility = Visibility.Hidden;
			if (this.OwnedWindows.Count == 0) {
				if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
					string[] initialFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
					List<string> finalFiles = new List<string>();
					for (int i = 0; i < initialFiles.Length; i++) {
						string ext = Path.GetExtension(initialFiles[i]).ToLower();
						foreach (string extensions in SupportedFormats.Values) {
							if (extensions.Contains(ext)) {
								finalFiles.Add(initialFiles[i]);
								break;
							}
						}
					}
					AddWaves(finalFiles.ToArray().ToArray(), false, false);
				}
			}
		}
		private void OnDragEnter(object sender, DragEventArgs e) {
			if (this.OwnedWindows.Count == 0 && (e.Data.GetDataPresent(DataFormats.FileDrop))) {
				labelDrop.Visibility = Visibility.Visible;
				e.Effects = DragDropEffects.Copy;
				e.Handled = true;
			}
			else {
				e.Effects = DragDropEffects.None;
			}
		}
		private void OnDragOver(object sender, DragEventArgs e) {
			if (this.OwnedWindows.Count == 0 && (e.Data.GetDataPresent(DataFormats.FileDrop))) {
				e.Effects = DragDropEffects.Copy;
				e.Handled = true;
			}
			else {
				e.Effects = DragDropEffects.None;
			}
		}
		private void OnDragLeave(object sender, DragEventArgs e) {
			labelDrop.Visibility = Visibility.Hidden;
		}

		#endregion
		//=========== PLAYBACK ===========
		#region Playback

		/**<summary>Plays the wave at the specified index.</summary>*/
		private void PlayIndex(int index) {
			OnStop(null, null);
			try {
				string ext = Path.GetExtension(waveFiles[index]).ToLower();
				if (ext == ".flac" || ext == ".ogg") {
					TriggerMessageBox.Show(this, MessageIcon.Warning, "FLAC and Ogg Vorbis playback is not supported.", "Unsupported Format");
					return;
				}
				player.Stop();
				player.Open(new Uri(waveFiles[index], UriKind.RelativeOrAbsolute));
				player.Play();
				playerTimer.Start();

				playItem = waveListViewItems[index];
				UpdateListViewItem(playItem, index, waveFiles[index]);
			}
			catch (Exception) {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "Failed to play wave file. Format may be unsupported.", "Play Error");
			}
			UpdateButtons();
		}
		private void OnPlayDoubleClick(object sender, MouseButtonEventArgs e) {
			if (playItem == waveListViewItems[listView.SelectedIndex] && !paused)
				OnPause(null, null);
			else
				OnPlay(null, null);
		}
		private void OnPlay(object sender, RoutedEventArgs e) {
			if (playItem == waveListViewItems[listView.SelectedIndex]) {
				player.Play();
				paused = false;
				int index = waveListViewItems.IndexOf(playItem);
				UpdateListViewItem(playItem, index, waveFiles[index]);
				return;
			}
			else {
				OnStop(null, null);
				PlayIndex(listView.SelectedIndex);
			}
		}
		private void OnPause(object sender, RoutedEventArgs e) {
			if (playItem != null) {
				paused = !paused;
				if (paused)
					player.Pause();
				else
					player.Play();
				UpdateButtons();
				int index = waveListViewItems.IndexOf(playItem);
				UpdateListViewItem(playItem, index, waveFiles[index]);
			}
			else {
				buttonPause.IsChecked = false;
			}
		}
		private void OnStop(object sender, RoutedEventArgs e) {
			if (playItem != null) {
				player.Stop();
				playerTimer.Stop();
				paused = false;
				ListViewItem play = playItem;
				playItem = null;
				int index = waveListViewItems.IndexOf(play);
				UpdateListViewItem(play, index, waveFiles[index]);
				UpdateButtons();
			}
			else {
				buttonStop.IsChecked = true;
			}
		}
		private void OnNext(object sender, RoutedEventArgs e) {
			if (playItem != null) {
				int index = waveListViewItems.IndexOf(playItem);
				OnStop(null, null);
				PlayIndex((index + 1) % waveListViewItems.Count);
			}
		}
		private void OnPrevious(object sender, RoutedEventArgs e) {
			if (playItem != null) {
				int index = waveListViewItems.IndexOf(playItem);
				OnStop(null, null);
				PlayIndex((index - 1 + waveListViewItems.Count) % waveListViewItems.Count);
			}
		}
		private void OnPlayerTimerElapsed(object sender, ElapsedEventArgs e) {
			Dispatcher.Invoke(() => {
				if (player.Position >= player.NaturalDuration || player.Position == TimeSpan.Zero) {
					OnStop(null, null);
				}
			});
		}

		#endregion
		//========== WAVE LIST ===========
		#region Wave List

		private void OnAddWave(object sender, RoutedEventArgs e) {
			loaded = false;

			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Title = "Add Wave File";
			string audioFiles = "AudioFiles|";
			string filters = "";
			foreach (KeyValuePair<string, string> pair in SupportedFormats) {
				audioFiles += pair.Value + ";";
				filters += pair.Key + "|" + pair.Value + "|";
			}
			audioFiles = audioFiles.Substring(0, audioFiles.Length - 1) + "|";
			dialog.Filter = audioFiles + filters + "All Files|*.*";
			dialog.FilterIndex = 0;
			var result = dialog.ShowDialog(this);
			if (result.HasValue && result.Value) {
				string file = dialog.FileName;
				AddWave(file, false);
			}
			loaded = true;
		}
		private void OnRemoveWave(object sender, RoutedEventArgs e) {
			loaded = false;

			var result = TriggerMessageBox.Show(this, MessageIcon.Question, "Are you sure you want to remove this wave file?", "Remove Wave", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.Yes) {
				int oldIndex = listView.SelectedIndex;
				if (waveListViewItems[oldIndex] == playItem)
					OnStop(null, null);
				waveFiles.RemoveAt(listView.SelectedIndex);
				waveListViewItems.RemoveAt(listView.SelectedIndex);
				if (waveFiles.Count > 0)
					listView.SelectedIndex = Math.Max(0, oldIndex - 1);
				UpdateListViewItemRange(oldIndex, waveListViewItems.Count - 1);
				UpdateButtons();
			}

			loaded = true;
		}
		private void OnAddWaveFolder(object sender, RoutedEventArgs e) {
			FolderBrowserDialog browser = new FolderBrowserDialog();
			browser.Description = "Choose a folder with wave files";
			browser.SelectedPath = Config.LastFolderBrowser;
			browser.ShowNewFolderButton = false;
			var result = browser.ShowFolderBrowser(this);
			if (result.HasValue && result.Value) {
				Config.LastFolderBrowser = browser.SelectedPath;
				var result2 = MessageBoxResult.Yes;
				if (waveFiles.Count > 0)
					result2 = TriggerMessageBox.Show(this, MessageIcon.Question, "Would you like to remove the current waves?", "Remove Waves", MessageBoxButton.YesNoCancel);
				if (result2 != MessageBoxResult.Cancel) {
					AddWaves(Config.LastFolderBrowser, result2 == MessageBoxResult.Yes, false);
				}
			}
			browser.Dispose();
			browser = null;
		}
		private void OnConvertToWave(object sender, RoutedEventArgs e) {
			int index = listView.SelectedIndex;
			string audioFile = waveFiles[index];
			string waveFile = Path.ChangeExtension(audioFile, ".wav");
			var result = TriggerMessageBox.Show(this, MessageIcon.Question, "Would you like to convert this audio file to wave format now?", "Convert to Wave", MessageBoxButton.YesNoCancel);
			if (result == MessageBoxResult.Yes) {
				if (File.Exists(waveFile) && !Config.ConvertOption.HasFlag(ConvertOptions.AutoOverwrite))
					result = TriggerMessageBox.Show(this, MessageIcon.Question, "Converting: A wave file with the same name already exists. Would you like to replace it?", "Already Exists", MessageBoxButton.YesNoCancel);
				if (result == MessageBoxResult.Yes) {
					if (!FFmpeg.Convert(audioFile, waveFile)) {
						TriggerMessageBox.Show(this, MessageIcon.Error, "Failed to convert audio file to wave format!", "Conversion Error");
						result = MessageBoxResult.Cancel;
					}
					else {
						waveFiles[index] = waveFile;
						UpdateListViewItem((ListViewItem)listView.Items[index], index, waveFile);
						modified = true;
						UpdateTitle();
					}
				}
			}
		}
		private void OnRemoveAllWaves(object sender, RoutedEventArgs e) {
			var result = TriggerMessageBox.Show(this, MessageIcon.Question, "Are you sure you want to remove all waves from the list?", "Remove Waves", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.Yes) {
				OnStop(null, null);
				waveFiles.Clear();
				waveListViewItems.Clear();
				UpdateButtons();
				modified = true;
				UpdateTitle();
			}
		}
		private void OnMoveWaveUp(object sender, RoutedEventArgs e) {
			int oldIndex = listView.SelectedIndex;
			int newIndex = listView.SelectedIndex - 1;
			waveFiles.Move(oldIndex, newIndex);
			waveListViewItems.Move(oldIndex, newIndex);
			UpdateListViewItemRange(newIndex, oldIndex);
			listView.ScrollIntoView(waveListViewItems[newIndex]);
			listView.SelectedIndex = newIndex;
			UpdateButtons();
			modified = true;
			UpdateTitle();
		}
		private void OnMoveWaveDown(object sender, RoutedEventArgs e) {
			int oldIndex = listView.SelectedIndex;
			int newIndex = listView.SelectedIndex + 1;
			waveFiles.Move(oldIndex, newIndex);
			waveListViewItems.Move(oldIndex, newIndex);
			UpdateListViewItemRange(oldIndex, newIndex);
			listView.SelectedIndex = newIndex;
			listView.ScrollIntoView(waveListViewItems[newIndex]);
			UpdateButtons();
			modified = true;
			UpdateTitle();
		}
		private void OnWaveSelectedChanged(object sender, SelectionChangedEventArgs e) {
			UpdateButtons();
		}
		private void OnProcessDrop(object sender, ProcessDropEventArgs<ListViewItem> e) {
			if (e.OldIndex > -1) {
				waveFiles.Move(e.OldIndex, e.NewIndex);
				waveListViewItems.Move(e.OldIndex, e.NewIndex);
				modified = true;
				UpdateTitle();
				UpdateListViewItemRange(Math.Min(e.OldIndex, e.NewIndex), Math.Max(e.OldIndex, e.NewIndex));
			}

			e.Effects = DragDropEffects.Move;
			UpdateButtons();
		}

		#endregion
		//========== MENU ITEMS ==========
		#region Menu Items
		//--------------------------------
		#region File

		private void OnExit(object sender, RoutedEventArgs e) {
			Close();
		}

		#endregion
		//--------------------------------
		#region Options
			
		private void OnSaveConfirmationChecked(object sender, RoutedEventArgs e) {
			Config.SaveConfirmation = menuItemSaveConfirmation.IsChecked;
		}
		private void OnTrackNamesChecked(object sender, RoutedEventArgs e) {
			Config.TrackNames = menuItemTrackNames.IsChecked;
			UpdateListViewItemRange(0, waveListViewItems.Count - 1);
		}
		private void OnDragAndDropChecked(object sender, RoutedEventArgs e) {
			Config.DragAndDrop = menuItemDragAndDrop.IsChecked;
			dropManager.ListView = (Config.DragAndDrop ? listView : null);
		}
		private void OnShowLogChecked(object sender, RoutedEventArgs e) {
			Config.ShowLog = menuItemShowLog.IsChecked;
		}
		private void OnChangeVolume(object sender, RoutedEventArgs e) {
			VolumeDialog.Show(this, UpdateVolume);
		}

		private void OnAutoConvert(object sender, RoutedEventArgs e) {
			Config.ConvertOption = Config.ConvertOption.SetFlag(ConvertOptions.AutoConvert, menuItemAutoConvert.IsChecked);
		}
		private void OnAutoOverwrite(object sender, RoutedEventArgs e) {
			Config.ConvertOption = Config.ConvertOption.SetFlag(ConvertOptions.AutoOverwrite, menuItemAutoOverwrite.IsChecked);
		}
		private void OnWaitTillBuild(object sender, RoutedEventArgs e) {
			Config.ConvertOption = Config.ConvertOption.SetFlag(ConvertOptions.WaitTillBuild, menuItemWaitTillBuild.IsChecked);
			menuItemAutoConvert.IsEnabled = !menuItemWaitTillBuild.IsChecked;
			menuItemAutoOverwrite.IsEnabled = !menuItemWaitTillBuild.IsChecked;
		}

		private void OnStreamingChecked(object sender, RoutedEventArgs e) {
			Config.Streaming = menuItemStreaming.IsChecked;
		}
		private void OnCompression(object sender, RoutedEventArgs e) {
			CompressionDialog.Show(this);
			switch (Config.Format) {
			case CompressionFormats.PCM:
				menuItemCompression.Header = "Format: PCM";
				break;
			case CompressionFormats.ADPCM:
				menuItemCompression.Header = "Format: ADPCM, " + ((int)Config.SamplesPerBlock).ToString();
				break;
			case CompressionFormats.xWMA:
				menuItemCompression.Header = "Format: xWMA, " + Config.WMAQuality.ToString();
				break;
			}
		}

		#endregion
		//--------------------------------
		#region Help

		private void OnAbout(object sender, RoutedEventArgs e) {
			AboutWindow.Show(this);
		}

		private void OnHelp(object sender, RoutedEventArgs e) {
			Process.Start("https://github.com/trigger-death/QuickWaveBank/wiki");
		}

		private void OnCredits(object sender, RoutedEventArgs e) {
			CreditsWindow.Show(this);
		}

		private void OnViewOnGitHub(object sender, RoutedEventArgs e) {
			Process.Start("https://github.com/trigger-death/QuickWaveBank");
		}

		#endregion
		//--------------------------------
		#endregion
		//=========== UPDATING ===========
		#region Updating

		/**<summary>Updates the window title.</summary>*/
		private void UpdateTitle() {
			Title = MainTitle + " - " + (untitled ? "Untitled" : Path.GetFileName(listFile)) + (modified ? "*" : "");
		}
		/**<summary>Updates changes to button states.</summary>*/
		private void UpdateButtons() {
			buttonRemoveWave.IsEnabled = listView.SelectedIndex != -1;
			buttonRemoveAllWaves.IsEnabled = listView.SelectedIndex != -1;
			buttonMoveWaveUp.IsEnabled = listView.SelectedIndex > 0;
			buttonMoveWaveDown.IsEnabled = listView.SelectedIndex + 1 < waveListViewItems.Count;
			buttonPause.IsEnabled = listView.SelectedIndex != -1;
			buttonPlay.IsEnabled = listView.SelectedIndex != -1;
			buttonStop.IsEnabled = listView.SelectedIndex != -1;
			buttonPause.IsChecked = playItem != null && paused;
			buttonPlay.IsChecked = playItem != null && !paused;
			buttonStop.IsChecked = playItem == null;
			buttonNext.IsEnabled = playItem != null;
			buttonPrevious.IsEnabled = playItem != null;
			buttonConvert.IsEnabled = listView.SelectedIndex != -1 &&
				Path.GetExtension(waveFiles[listView.SelectedIndex]).ToLower() != ".wav";
		}
		/**<summary>Updates changes to volume.</summary>*/
		private void UpdateVolume() {
			player.IsMuted = Config.Muted;
			player.Volume = Config.Volume;
			Image image = (Image)menuItemVolume.Icon;
			if (Config.Muted)
				image.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/VolumeMute.png"));
			else if (Config.Volume == 0.0)
				image.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/VolumeNone.png"));
			else if (Config.Volume < 0.5)
				image.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/VolumeLow.png"));
			else
				image.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/VolumeHigh.png"));
			menuItemVolume.Header = "Volume: " + (Config.Muted ? "(muted)" : (((int)(Config.Volume * 100.0)).ToString() + "%"));
		}
		private void OnViewBossList(object sender, RoutedEventArgs e) {
			Process.Start("https://terraria.gamepedia.com/Music_Box");
		}

		#endregion
	}
}
