using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NAudio.CoreAudioApi;

namespace Shozom {

	public partial class SettingsWindow : Window {

		private record Device(string Id, string Name);

		private static readonly Key[] DisallowedKeys = new[] { Key.LeftAlt, Key.RightAlt, Key.LeftCtrl, Key.RightCtrl, Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin };

		private static readonly MMDeviceEnumerator Enumerator = new();

		private ModifierKeys _modifiers;

		private readonly App _app;

		internal SettingsWindow(App app) {
			InitializeComponent();
			_app = app;

			var dupes = new HashSet<Key>();
			foreach (Key key in Enum.GetValues(typeof(Key))) {
				if (dupes.Contains(key)) continue;
				dupes.Add(key);

				if (!DisallowedKeys.Contains(key)) HotkeyPicker.Items.Add(key);
			}

			HotkeyCtrl.IsChecked = Config.Object.Hotkey.Mod.HasFlag(ModifierKeys.Ctrl);
			HotkeyAlt.IsChecked = Config.Object.Hotkey.Mod.HasFlag(ModifierKeys.Alt);
			HotkeyShift.IsChecked = Config.Object.Hotkey.Mod.HasFlag(ModifierKeys.Shift);
			HotkeyWin.IsChecked = Config.Object.Hotkey.Mod.HasFlag(ModifierKeys.Win);
			HotkeyPicker.SelectedValue = Config.Object.Hotkey.Key;

			HotkeyCtrl.Checked += HotkeyMod_OnChecked_OnUnchecked;
			HotkeyCtrl.Unchecked += HotkeyMod_OnChecked_OnUnchecked;
			HotkeyAlt.Checked += HotkeyMod_OnChecked_OnUnchecked;
			HotkeyAlt.Unchecked += HotkeyMod_OnChecked_OnUnchecked;
			HotkeyShift.Checked += HotkeyMod_OnChecked_OnUnchecked;
			HotkeyShift.Unchecked += HotkeyMod_OnChecked_OnUnchecked;
			HotkeyWin.Checked += HotkeyMod_OnChecked_OnUnchecked;
			HotkeyWin.Unchecked += HotkeyMod_OnChecked_OnUnchecked;
		}

		public void ReloadDevices() {
			DevicePicker.ItemsSource = Enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).Select(device => {
				return new Device(device.ID, (device.DataFlow switch { DataFlow.Capture => "IN – ", DataFlow.Render => "OUT – ", _ => "UNK – " }) + device.FriendlyName);
			}).ToList();

			DevicePicker.SelectedValue = Config.Object.Device;
		}

		private void DevicePicker_OnSelectionChanged(object sender, RoutedEventArgs e) {
			Config.Object.Device = ((Device) DevicePicker.SelectedItem).Id;
			Config.Save();
		}

		private void HotkeyPicker_OnSelectionChanged(object sender, RoutedEventArgs e) {
			Config.Object.Hotkey.Key = (Key) HotkeyPicker.SelectedItem;
			Config.Save();
			_app.UpdateHotkey();
		}

		private void HotkeyMod_OnChecked_OnUnchecked(object sender, RoutedEventArgs e) {
			var key = ((CheckBox) sender).Name switch {
				"HotkeyCtrl" => ModifierKeys.Ctrl,
				"HotkeyAlt" => ModifierKeys.Alt,
				"HotkeyShift" => ModifierKeys.Shift,
				"HotkeyWin" => ModifierKeys.Win
			};

			if (e.RoutedEvent.Name == "Checked") _modifiers |= key;
			else _modifiers ^= key;

			Config.Object.Hotkey.Mod = _modifiers;
			Config.Save();
			_app.UpdateHotkey();
		}

	}

}
