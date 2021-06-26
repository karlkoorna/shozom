using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Input;
using NAudio.CoreAudioApi;

namespace Shozom {

	internal class ConfigObject {

		internal class HotkeyObject {

			[JsonPropertyName("key")]
			public Key Key { get; set; }

			[JsonPropertyName("mod")]
			public ModifierKeys Mod { get; set; }

		}

		[JsonPropertyName("device")]
		public string Device { get; set; }

		[JsonPropertyName("hotkey")]
		public HotkeyObject Hotkey { get; set; }

	}

	internal static class Config {

		private static readonly MMDeviceEnumerator Enumerator = new();

		private static void Create() {
			Object = new ConfigObject {
				Device = Enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID,
				Hotkey = new ConfigObject.HotkeyObject {
					Key = Key.None,
					Mod = 0
				}
			};
		}

		private static void Restore() {
			var device = Enumerator.GetDevice(Object.Device);
			if (device == null || device.State != DeviceState.Active) Object.Device = Enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
		}

		#region Internal

		public static ConfigObject Object { get; private set; }

		public static string Path;

		public static void Load(string path) {
			Path = path;

			try {
				if (Object == null) Object = JsonSerializer.Deserialize<ConfigObject>(File.ReadAllText(Path));
				Restore();
				Save();
			} catch (Exception) {
				Create();
				Save();
				return;
			}
		}

		public static void Save() {
			var str = JsonSerializer.Serialize(Object, typeof(ConfigObject), new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
			var str2 = Regex.Replace(str, "(?<=^|  )(  )", "\t", RegexOptions.Multiline);
			var str3 = Regex.Replace(str2, "\r\n", "\n");
			File.WriteAllText(Path, str3 + "\n", Encoding.UTF8);
		}

		#endregion

	}

}
