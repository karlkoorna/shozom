using System.Linq;
using System.Windows;
using NAudio.CoreAudioApi;

namespace Shozom {

	public partial class SettingsWindow : Window {

		private record Device(string Id, string Name);

		public SettingsWindow() {
			InitializeComponent();

			var devices = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).Select(device => {
				return new Device(device.ID, (device.DataFlow switch { DataFlow.Capture => "IN – ", DataFlow.Render => "OUT – ", _ => "UNK – " }) + device.FriendlyName);
			}).ToList();

			DevicePicker.ItemsSource = devices;
			DevicePicker.SelectedValuePath = "Id";
			DevicePicker.DisplayMemberPath = "Name";
			DevicePicker.SelectedValue = Config.Object.Device;
			DevicePicker.SelectionChanged += DevicePicker_OnSelectionChanged;
		}

		private void DevicePicker_OnSelectionChanged(object sender, RoutedEventArgs e) {
			var item = (Device) DevicePicker.SelectedItem;

			Config.Object.Device = item.Id;
			Config.Save();
		}

	}

}
