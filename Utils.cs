using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Shozom {


    public static class Utils {

		private static float[] ComputeGaussianKernel1D(int radius, float sigma) {
			var kernel = new float[radius * 2 + 1];
			float sum = 0;
			for (var i = 0; i < kernel.Length; i++) sum += kernel[i] = (float) Math.Exp(-.5 * Math.Pow((i - radius) / sigma, 2));
			for (var i = 0; i < kernel.Length; i++) kernel[i] /= sum;
			return kernel;
		}

		public static unsafe void GaussianBlur(this Bitmap image, int radius, float sigma) {
			var raw = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, image.PixelFormat);
			var data = new byte[raw.Stride * image.Height];
			Marshal.Copy(raw.Scan0, data, 0, data.Length);
			
			var kernel = ComputeGaussianKernel1D(radius, sigma);

			for (var yy = 0; yy < image.Height; yy++) {
				for (var xx = 0; xx < image.Width; xx++) {
					float r = 0, g = 0, b = 0, a = 0, c = 0;

					for (var i = -radius; i < radius + 1; i++) {
						var x = xx + i;
						if (x < 0 || x >= image.Width) continue;

						var offset = yy * raw.Stride + x * 4;
						var weight = kernel[i + radius];

						b += data[offset] * weight;
						g += data[offset + 1] * weight;
						r += data[offset + 2] * weight;
						a += data[offset + 3] * weight;
						c += weight;
					}

					var newOffset = yy * raw.Stride + xx * 4;
					data[newOffset] = (byte) Math.Round(b / c);
					data[newOffset + 1] = (byte) Math.Round(g / c);
					data[newOffset + 2] = (byte) Math.Round(r / c);
					data[newOffset + 3] = (byte) Math.Round(a / c);
				}

			}

			for (var xx = 0; xx < image.Width; xx++) {
				for (var yy = 0; yy < image.Height; yy++) {
					float r = 0, g = 0, b = 0, a = 0, c = 0;

					for (var i = -radius; i < radius + 1; i++) {
						var y = yy + i;
						if (y < 0 || y >= image.Height) continue;

						var offset = y * raw.Stride + xx * 4;
						var weight = kernel[i + radius];

						b += data[offset] * weight;
						g += data[offset + 1] * weight;
						r += data[offset + 2] * weight;
						a += data[offset + 3] * weight;
						c += weight;
					}

					var newOffset = yy * raw.Stride + xx * 4;
					data[newOffset] = (byte) Math.Round(b / c);
					data[newOffset + 1] = (byte) Math.Round(g / c);
					data[newOffset + 2] = (byte) Math.Round(r / c);
					data[newOffset + 3] = (byte) Math.Round(a / c);
				}

			}

			Marshal.Copy(data, 0, raw.Scan0, data.Length);
			image.UnlockBits(raw);
		}

		public static void PlaceImage(this Bitmap srcImage, Bitmap image, int x, int y, int width, int height, int inset = 0) {
			using var g = Graphics.FromImage(srcImage);
			g.CompositingMode = CompositingMode.SourceOver;
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;
			g.SmoothingMode = SmoothingMode.HighQuality;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;

			using var wrapMode = new ImageAttributes();
			wrapMode.SetWrapMode(WrapMode.TileFlipXY);

			g.DrawImage(image, new Rectangle(x, y, width, height), inset, inset, image.Width - inset * 2, image.Height - inset * 2, GraphicsUnit.Pixel, wrapMode);
		}

	}

}
