using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Force.Crc32;

/*
 * Based on:
 * https://github.com/AlekseyMartynov/shazam-for-real
 * https://github.com/marin-m/SongRec
 */

namespace Shozom.Magic {

	static class Signature {

		public static byte[] Create(int sampleRate, int sampleCount, Landmarker landmarker) {
			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream);

			writer.Write(0xCAFE2580);
			writer.Write(-1);
			writer.Write(-1);
			writer.Write(0x94119C00);
			writer.Write(0);
			writer.Write(0);
			writer.Write(0);
			writer.Write(GetSampleRateCode(sampleRate) << 27);
			writer.Write(0);
			writer.Write(0);
			writer.Write(sampleCount);
			writer.Write(0x007C0000);
			writer.Write(0x40000000);
			writer.Write(-1);

			var bandData = GetBandData(landmarker);
			for (var i = 0; i < bandData.Length; i++) {
				writer.Write(0x60030040 + i);
				writer.Write(bandData[i].Length);
				writer.Write(bandData[i]);
			}

			foreach (var i in new[] { 2, 13 }) {
				stream.Position = i * 4;
				writer.Write(stream.Length - 48);
			}

			var crc = Crc32Algorithm.Compute(stream.GetBuffer(), 8, (int) stream.Length - 8);
			stream.Position = 4;
			writer.Write(crc);

			return stream.ToArray();
		}

		private static byte[][] GetBandData(Landmarker finder) {
			return finder.EnumerateBandedLandmarks().Select(GetBandData).ToArray();
		}

		private static byte[] GetBandData(IEnumerable<Landmark> landmarks) {
			using var mem = new MemoryStream();
			using var writer = new BinaryWriter(mem);

			var stripeIndex = 0;

			foreach (var landmark in landmarks) {
				if (landmark.StripeIndex - stripeIndex >= 100) {
					stripeIndex = landmark.StripeIndex;
					writer.Write((byte) 255);
					writer.Write(stripeIndex);
				}

				if (landmark.StripeIndex < stripeIndex)
					throw new InvalidOperationException();

				writer.Write(Convert.ToByte(landmark.StripeIndex - stripeIndex));
				writer.Write(Convert.ToUInt16(landmark.InterpolatedLogMagnitude));
				writer.Write(Convert.ToUInt16(64 * landmark.InterpolatedBin));

				stripeIndex = landmark.StripeIndex;
			}

			while (mem.Length % 4 != 0) writer.Write((byte) 0);
			return mem.ToArray();
		}

		private static int GetSampleRateCode(int sampleRate) {
			return sampleRate switch {
				8000 => 1,
				16000 => 3,
				32000 => 4,
				_ => throw new NotSupportedException()
			};
		}

	}

}
