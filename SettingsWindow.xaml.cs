using System.Linq;
using System.Windows;
using NAudio.CoreAudioApi;

namespace Shozom {

	public partial class SettingsWindow : Window {

		private record Device(string Id, string Name);

		private static readonly MMDeviceEnumerator Enumerator = new();

		public SettingsWindow() {
			InitializeComponent();

			DevicePicker.SelectedValuePath = "Id";
			DevicePicker.DisplayMemberPath = "Name";
			DevicePicker.SelectionChanged += DevicePicker_OnSelectionChanged;
		}

		public void Load() {
			var devices = Enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).Select(device => {
				return new Device(device.ID, (device.DataFlow switch { DataFlow.Capture => "IN – ", DataFlow.Render => "OUT – ", _ => "UNK – " }) + device.FriendlyName);
			}).ToList();

			DevicePicker.ItemsSource = devices;
			DevicePicker.SelectedValue = Config.Object.Device;
		}

		private void DevicePicker_OnSelectionChanged(object sender, RoutedEventArgs e) {
			var item = (Device) DevicePicker.SelectedItem;

			Config.Object.Device = item.Id;
			Config.Save();
		}

	}

}
