using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Shozom {

	public partial class App {

		public const string CONFIG_PATH = "Shozom.json";
		public const int IDENTIFY_TIMEOUT = 30000;

		private readonly SettingsWindow _settingsWindow;
		private readonly NotifyIcon _notifyIcon;

		private bool _isListening;

		private App() {
			new Mutex(true, "Shozom", out var isNew);
			if (!isNew) Environment.Exit(0);

			Config.Load(CONFIG_PATH);
			Toaster.Setup();

			var menuStrip = new ContextMenuStrip();
			menuStrip.Items.Add("Settings", null, OnClickSettings);
			menuStrip.Items.Add("Exit", null, OnClickExit);

			_settingsWindow = new SettingsWindow();
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

		private async void OnClickListen(object sender, EventArgs e) {
			if (e is MouseEventArgs eMouse && eMouse.Button != MouseButtons.Left) return;

			if (_isListening) return;
			var stopAnimating = AnimateTray();

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

			stopAnimating();
		}

		private void OnClickSettings(object sender, EventArgs e) {
			_settingsWindow.Load();
			_settingsWindow.Show();
		}

		private void OnClickExit(object sender, EventArgs e) {
			_notifyIcon.Dispose();
			Environment.Exit(0);
		}

		private Action AnimateTray() {
			_isListening = true;
			_notifyIcon.Icon = Shozom.Properties.Resources.LogoActiveAlt;

			ushort index = 0;
			var timer = new System.Timers.Timer(500);
			timer.Elapsed += (_, _) => {
				_notifyIcon.Icon = index++ % 2 == 0 ? Shozom.Properties.Resources.LogoActive : Shozom.Properties.Resources.LogoActiveAlt;
			};

			timer.Start();
			return () => {
				_isListening = false;
				_notifyIcon.Icon = Shozom.Properties.Resources.Logo;
				timer.Stop();
			};
		}

	}

}
