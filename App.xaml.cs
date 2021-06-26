using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace Shozom {

	public partial class App {

		public const string CONFIG_PATH = "Shozom.json";
		public const int IDENTIFY_TIMEOUT = 30000;

		private readonly SettingsWindow _settingsWindow;
		private readonly NotifyIcon _notifyIcon;

		private readonly HotkeyListener _hotkeyListener;

		private bool _isListening;

		private App() {
			new Mutex(true, "Shozom", out var isNew);
			if (!isNew) Environment.Exit(0);

			Config.Load(CONFIG_PATH);
			Toaster.Setup();

			_hotkeyListener = new HotkeyListener();
			_hotkeyListener.Activated += StartListening;
			UpdateHotkey();

			var menuStrip = new ContextMenuStrip();
			menuStrip.Items.Add("Settings", null, OnClickSettings);
			menuStrip.Items.Add("Exit", null, OnClickExit);

			_settingsWindow = new SettingsWindow(this);
			_settingsWindow.Closing += (_, e) => {
				e.Cancel = true;
				_settingsWindow.Hide();
			};

			_notifyIcon = new NotifyIcon {
				ContextMenuStrip = menuStrip,
				Icon = Shozom.Properties.Resources.Logo,
				Visible = true
			};

			_notifyIcon.Click += OnClickListen;
		}

		private void OnClickListen(object sender, EventArgs e) {
			if (e is System.Windows.Forms.MouseEventArgs eMouse && eMouse.Button == MouseButtons.Left) StartListening();
		}

		private void OnClickSettings(object sender, EventArgs e) {
			_settingsWindow.ReloadDevices();
			_settingsWindow.Show();
		}

		private void OnClickExit(object sender, EventArgs e) {
			_notifyIcon.Dispose();
			Environment.Exit(0);
		}

		public void UpdateHotkey() {
			if (Config.Object.Hotkey.Key != Key.None) _hotkeyListener.SetHotkey(Config.Object.Hotkey.Mod, Config.Object.Hotkey.Key);
		}

		private async void StartListening() {
			if (_isListening) return;
			_isListening = true;
			_notifyIcon.Icon = Shozom.Properties.Resources.LogoActiveAlt;

			ushort frame = 0;
			var timer = new System.Timers.Timer(500);
			timer.Start();
			timer.Elapsed += (_, _) => {
				_notifyIcon.Icon = frame++ % 2 == 0 ? Shozom.Properties.Resources.LogoActive : Shozom.Properties.Resources.LogoActiveAlt;
			};

			try {
				var cancel = new CancellationTokenSource();
				Task.Delay(IDENTIFY_TIMEOUT).ContinueWith((_) => { cancel.Cancel(); });

				var match = await Task.Run(() => Shazam.IdentifyAsync(Config.Object.Device, cancel.Token));

				if (match == null) Toaster.ShowFailure();
				else await Toaster.ShowSuccess(match);
			} catch (OperationCanceledException) {
				Toaster.ShowTimeout();
			} catch (Exception ex) {
				Toaster.ShowError(ex.Message);
			}

			_notifyIcon.Icon = Shozom.Properties.Resources.Logo;
			_isListening = false;
			timer.Stop();
		}

	}

}
