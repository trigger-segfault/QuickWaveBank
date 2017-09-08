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
using System.Windows.Shapes;

namespace QuickWaveBank.Windows {
	/// <summary>
	/// Interaction logic for CompressionDialog.xaml
	/// </summary>
	public partial class CompressionDialog : Window {

		private bool loaded = false;
		private CompressionFormats format;
		private int wmaQuality;
		private ADPCMSamplesPerBlock samplesPerBlock;

		public CompressionDialog() {
			InitializeComponent();

			format = Config.Format;
			wmaQuality = Config.WMAQuality;
			samplesPerBlock = Config.SamplesPerBlock;

			var formats = (CompressionFormats[])Enum.GetValues(typeof(CompressionFormats));
			for (int i = 0; i < formats.Length; i++) {
				comboBoxFormat.Items.Add(formats[i].ToString());
				if (format == formats[i])
					comboBoxFormat.SelectedIndex = i;
			}

			spinnerQuality.Value = Math.Max(1, Math.Min(100, wmaQuality));

			var samples = (ADPCMSamplesPerBlock[])Enum.GetValues(typeof(ADPCMSamplesPerBlock));
			for (int i = 0; i < samples.Length; i++) {
				comboBoxSamplesPerBlock.Items.Add(((int)samples[i]).ToString());
				if (samplesPerBlock == samples[i])
					comboBoxSamplesPerBlock.SelectedIndex = i;
			}
			
			labelSettings.Visibility = Visibility.Hidden;
			spinnerQuality.Visibility = Visibility.Hidden;
			comboBoxSamplesPerBlock.Visibility = Visibility.Hidden;
			switch (format) {
			case CompressionFormats.ADPCM:
				labelSettings.Content = "Samples Per Block";
				labelSettings.Visibility = Visibility.Visible;
				comboBoxSamplesPerBlock.Visibility = Visibility.Visible;
				break;
			case CompressionFormats.xWMA:
				labelSettings.Content = "Quility (1-100)";
				labelSettings.Visibility = Visibility.Visible;
				spinnerQuality.Visibility = Visibility.Visible;
				break;
			}
		}

		private void OnWindowLoaded(object sender, RoutedEventArgs e) {
			loaded = true;
		}

		private void OnFormatChanged(object sender, SelectionChangedEventArgs e) {
			if (!loaded)
				return;
			var formats = (CompressionFormats[])Enum.GetValues(typeof(CompressionFormats));
			for (int i = 0; i < formats.Length; i++) {
				if (((string)comboBoxFormat.SelectedItem) == formats[i].ToString()) {
					format = formats[i];
					break;
				}
			}
			labelSettings.Visibility = Visibility.Hidden;
			spinnerQuality.Visibility = Visibility.Hidden;
			comboBoxSamplesPerBlock.Visibility = Visibility.Hidden;
			switch (format) {
			case CompressionFormats.ADPCM:
				labelSettings.Content = "Samples Per Block";
				labelSettings.Visibility = Visibility.Visible;
				comboBoxSamplesPerBlock.Visibility = Visibility.Visible;
				break;
			case CompressionFormats.xWMA:
				labelSettings.Content = "Quility (1-100)";
				labelSettings.Visibility = Visibility.Visible;
				spinnerQuality.Visibility = Visibility.Visible;
				break;
			}
		}

		private void OnQualityChanged(object sender, Controls.ValueChangedEventArgs<int> e) {
			if (!loaded)
				return;
			wmaQuality = spinnerQuality.Value;
		}

		private void OnSamplesPerBlockChanged(object sender, SelectionChangedEventArgs e) {
			if (!loaded)
				return;
			var samples = (ADPCMSamplesPerBlock[])Enum.GetValues(typeof(ADPCMSamplesPerBlock));
			for (int i = 0; i < samples.Length; i++) {
				if (int.Parse((string)comboBoxSamplesPerBlock.SelectedItem) == ((int)samples[i])) {
					samplesPerBlock = samples[i];
					break;
				}
			}
		}

		public static void Show(Window owner) {
			CompressionDialog dialog = new CompressionDialog();
			dialog.Owner = owner;
			dialog.ShowDialog();
			Config.Format = dialog.format;
			switch (Config.Format) {
			case CompressionFormats.ADPCM:
				Config.SamplesPerBlock = dialog.samplesPerBlock;
				break;
			case CompressionFormats.xWMA:
				Config.WMAQuality = dialog.wmaQuality;
				break;
			}
		}
		private void OnOK(object sender, RoutedEventArgs e) {
			DialogResult = true;
		}
	}
}
