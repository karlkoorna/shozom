using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

/*
 * Based on:
 * https://github.com/AlekseyMartynov/shazam-for-real
 * https://github.com/marin-m/SongRec
 */

namespace Shozom.Magic {

	internal class Analyser {

		public const int SAMPLE_RATE = 16000;
		public const int CHUNKS_PER_SECOND = 125;
		public const int CHUNK_SIZE = SAMPLE_RATE / CHUNKS_PER_SECOND;
		public const int WINDOW_SIZE = CHUNK_SIZE * 16;
		public const int BIN_COUNT = WINDOW_SIZE / 2 + 1;

		private static readonly float[] Hann = Array.ConvertAll(Window.Hann(WINDOW_SIZE), Convert.ToSingle);

		private readonly float[] _windowRing = new float[WINDOW_SIZE];
		private readonly List<float[]> _stripes = new(3 * CHUNKS_PER_SECOND);

		private readonly Complex32[] _fftBuffer = new Complex32[WINDOW_SIZE];

		public int ProcessedSamples { get; private set; }

		public int ProcessedMs => ProcessedSamples * 1000 / SAMPLE_RATE;

		public int StripeCount => _stripes.Count;

		private int WindowRingPos => ProcessedSamples % WINDOW_SIZE;

		public void ReadChunk(ISampleProvider sampleProvider) {
			if (sampleProvider.Read(_windowRing, WindowRingPos, CHUNK_SIZE) != CHUNK_SIZE) throw new Exception();
			ProcessedSamples += CHUNK_SIZE;
			if (ProcessedSamples >= WINDOW_SIZE) AddStripe();
		}

		private void AddStripe() {
			for (var i = 0; i < WINDOW_SIZE; i++) {
				var waveRingIndex = (WindowRingPos + i) % WINDOW_SIZE;
				_fftBuffer[i] = new Complex32(_windowRing[waveRingIndex] * Hann[i], 0);
			}

			Fourier.Forward(_fftBuffer, FourierOptions.NoScaling);

			var stripe = new float[BIN_COUNT];
			for (var bin = 0; bin < BIN_COUNT; bin++) {
				// Used in official Shazam since 7.11.0.
				// https://github.com/marin-m/SongRec/issues/10#issuecomment-731527377
				stripe[bin] = 2 * _fftBuffer[bin].MagnitudeSquared;
			}

			_stripes.Add(stripe);
		}

		public float GetMagnitudeSquared(int stripe, int bin) {
			return _stripes[stripe][bin];
		}

		public float FindMaxMagnitudeSquared() {
			return _stripes.Max(s => s.Max());
		}

		public static int FreqToBin(float freq) {
			return Convert.ToInt32(freq * WINDOW_SIZE / SAMPLE_RATE);
		}

		public static float BinToFreq(float bin) {
			return bin * SAMPLE_RATE / WINDOW_SIZE;
		}

	}

}
