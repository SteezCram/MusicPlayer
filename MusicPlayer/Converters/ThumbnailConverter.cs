using System;
using System.Collections.Generic;
//using System.Drawing;
//using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MusicPlayer.Converters
{
    internal class ThumbnailConverter : IValueConverter
    {
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
            if (value == null)
                return null;


            byte width = 16;
            byte height = 16;
            //List<byte> pixels = (List<byte>)value;
            return BitmapSource.Create(width, height, 72, 72, PixelFormats.Rgb24, null, ((List<byte>)value).ToArray(), width * 3);

            //Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            // Github copilot solution
            // Read a 1D RGB array as a 2D array
            //for (int i = 0; i < pixels.Count; i += 3)
            //{
            //    int x = i / 3 % width;
            //    int y = i / 3 / width;
            //    //bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(pixels[i], pixels[i + 1], pixels[i + 2]));
            //}

            // My solution
            //int x = 0;
            //int y = 0;

            //for (int i = 0; i < pixels.Count; i += 3)
            //{
            //    if (x == width)
            //    {
            //        x = 0;
            //        y++;
            //    }

            //    int r = pixels[i];
            //    int g = pixels[i + 1];
            //    int b = pixels[i + 2];

            //    bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(r, g, b));

            //    x++;
            //}


            //BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

            //BitmapSource bitmapSource = BitmapSource.Create(
            //    bitmapData.Width, bitmapData.Height,
            //    bitmap.HorizontalResolution, bitmap.VerticalResolution,
            //    PixelFormats.Bgr24, null,
            //    bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            //bitmap.UnlockBits(bitmapData);

            //return bitmapSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
            throw new NotImplementedException();
        }

        private byte[,,] IncreaseRgbArray(List<byte> rgbArray, int width, int height)
        {
            byte[,,] pixels = new byte[width, height, 3];
            
            int x = 0;
            int y = 0;

            for (int i = 0; i < rgbArray.Count; i += 3)
            {
                if (x == width)
                {
                    x = 0;
                    y++;
                }

                pixels[y, x, 0] = rgbArray[i];
                pixels[y, x, 1] = rgbArray[i + 1];
                pixels[y, x, 2] = rgbArray[i + 2];

                x++;
            }

            return pixels;
        }
    }
}
