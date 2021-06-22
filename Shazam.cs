using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Shozom.Magic;

namespace Shozom {

	public static class Shazam {

		private static readonly MMDeviceEnumerator _enumerator = new();
		private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(3) };

		private static readonly string _deviceId = Guid.NewGuid().ToString();

		public static async Task<ShozomMatch> IdentifyAsync(string deviceId, CancellationToken cancel) {
			var device = _enumerator.GetDevice(deviceId);
			if (device == null || device.State != DeviceState.Active) throw new ArgumentException("Device not available");
			
			using var capture = device.DataFlow switch {
				DataFlow.Capture => new WasapiCapture(device),
				DataFlow.Render => new WasapiLoopbackCapture(device)
			};

			var buffer = new BufferedWaveProvider(capture.WaveFormat) { ReadFully = false, DiscardOnBufferOverflow = true };
			var samples = new MediaFoundationResampler(buffer, new WaveFormat(16000, 16, 1)).ToSampleProvider();

			capture.DataAvailable += (s, e) => { buffer.AddSamples(e.Buffer, 0, e.BytesRecorded); };
			capture.StartRecording();

			var analyser = new Analyser();
			var finder = new Landmarker(analyser);

			var retryMs = 3000;

			while (true) {
				if (cancel.IsCancellationRequested) {
					capture.StopRecording();
					throw new OperationCanceledException("Took longer than expected");
				}

				if (buffer.BufferedDuration.TotalSeconds < 1) {
					Thread.Sleep(100);
					continue;
				}
				
				analyser.ReadChunk(samples);

				if (analyser.StripeCount > 2 * Landmarker.RADIUS_TIME) finder.Find(analyser.StripeCount - Landmarker.RADIUS_TIME - 1);
				if (analyser.ProcessedMs < retryMs) continue;

				var body = new ShazamRequest {
					Signature = new ShazamSignature {
						Uri = "data:audio/vnd.shazam.sig;base64," + Convert.ToBase64String(Signature.Create(Analyser.SAMPLE_RATE, analyser.ProcessedSamples, finder)),
						SampleMs = analyser.ProcessedMs
					}
				};

				var res = await _http.PostAsync($"https://amp.shazam.com/discovery/v5/en/US/android/-/tag/{_deviceId}/{Guid.NewGuid()}", new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"), cancel);
				var data = JsonSerializer.Deserialize<ShazamResponse>(await res.Content.ReadAsStringAsync(cancel));

				if (data.RetryMs != null) {
					if (data.RetryMs == 0) return null;
					retryMs = (int) data.RetryMs;
					continue;
				}

				capture.StopRecording();

				if (data.Track == null) return null;
				return new ShozomMatch {
					Title = data.Track.Title,
					Artist = data.Track.Subtitle,
					Link = data.Track.Share.Link,
					Cover = data.Track?.Images?.CoverHQ ?? data.Track?.Images?.Cover ?? data.Track.Share.Image
				};
			}
		}

	}

}
