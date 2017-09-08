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
	/// Interaction logic for VolumeDialog.xaml
	/// </summary>
	public partial class VolumeDialog : Window {
		private Action volumeCallback;
		private bool loaded = false;

		public VolumeDialog(Action volumeCallback) {
			InitializeComponent();
			this.volumeCallback = volumeCallback;
			sliderVolume.Value = Config.Volume * 100.0;
			checkboxMuted.IsChecked = Config.Muted;
			labelVolume.Content = "Volume: " + ((int)(Config.Volume * 100)).ToString() + "%";
			UpdateIcon();
		}

		public static void Show(Window owner, Action volumeCallback) {
			VolumeDialog dialog = new VolumeDialog(volumeCallback);
			dialog.Owner = owner;
			dialog.ShowDialog();
		}

		private void OnMuteChecked(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.Muted = checkboxMuted.IsChecked.Value;
			UpdateIcon();
			volumeCallback?.Invoke();
		}

		private void OnVolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			if (!loaded)
				return;
			Config.Volume = sliderVolume.Value / 100.0;
			labelVolume.Content = "Volume: " + ((int)(Config.Volume * 100)).ToString() + "%";
			UpdateIcon();
			volumeCallback?.Invoke();
		}

		private void OnOKClicked(object sender, RoutedEventArgs e) {
			Close();
		}

		private void UpdateIcon() {
			if (Config.Muted)
				this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/VolumeMute.png"));
			else if (Config.Volume == 0.0)
				this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/VolumeNone.png"));
			else if (Config.Volume < 0.5)
				this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/VolumeLow.png"));
			else
				this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/VolumeHigh.png"));
		}

		private void OnWindowLoaded(object sender, RoutedEventArgs e) {
			loaded = true;
		}
	}
}
