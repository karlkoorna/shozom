using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NAudio.CoreAudioApi;

namespace Shozom {

	internal class ConfigObject {

		[JsonPropertyName("device")]
		public string Device { get; set; }

	}

	internal static class Config {

		private static readonly MMDeviceEnumerator _enumerator = new();

		private static void Create() {
			Object = new ConfigObject {
				Device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID
			};
		}

		private static void Restore() {
			var device = _enumerator.GetDevice(Object.Device);
			if (device == null || device.State != DeviceState.Active) Object.Device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
		}

		#region Internal

		public static ConfigObject Object { get; private set; }

		private static string _path;

		public static void Load(string path) {
			_path = path;

			try {
				if (Object == null) Object = JsonSerializer.Deserialize<ConfigObject>(File.ReadAllText(_path));
				var check = Check();
				Restore();
				if (Check() != check) Save();
			} catch (Exception) {
				Create();
				Save();
				return;
			}
		}

		public static void Save() {
			var str = JsonSerializer.Serialize(Object, typeof(ConfigObject), new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(_path, Regex.Replace(str, "(?<=^|  )(  )", "\t", RegexOptions.Multiline) + "\n");
		}

		private static int Check() {
			return Object.GetType().GetProperties().Select(prop => prop.GetValue(Object))
				.Where(value => value != null).Select(value => value.GetHashCode()).Aggregate((code, nextCode) => code ^ nextCode);
		}

		#endregion

	}

}
