using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;

namespace Shozom {

	[Flags]
	internal enum ModifierKeys : uint {
		Alt = 1,
		Ctrl = 2,
		Shift = 4,
		Win = 8
	}

	internal class HotkeyListener : IDisposable {

		[DllImport("user32.dll")]
		private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

		[DllImport("user32.dll")]
		private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		private class Window : NativeWindow, IDisposable {

			public event Action Activated;

			public Window() {
				CreateHandle(new CreateParams());
			}

			protected override void WndProc(ref Message msg) {
				base.WndProc(ref msg);
				if (msg.Msg == 0x0312) Activated?.Invoke();
			}

			public void Dispose() {
				DestroyHandle();
			}

		}

		private readonly Window _window = new();

		public event Action Activated {
			add { _window.Activated += value; }
			remove { _window.Activated -= value; }
		}

		public void SetHotkey(ModifierKeys mod, Key key) {
			UnregisterHotKey(_window.Handle, 0);
			RegisterHotKey(_window.Handle, 0, (uint) mod, (uint) KeyInterop.VirtualKeyFromKey(key));
		}

		public void Dispose() {
			UnregisterHotKey(_window.Handle, 0);
			_window.Dispose();
		}

	}

}
