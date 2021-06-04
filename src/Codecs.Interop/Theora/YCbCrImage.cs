using System;
using System.Runtime.InteropServices;
using Flettu.Util;

namespace Codecs.Interop.Theora
{
    public class YCbCrImage : IDisposable
    {
        internal th_img_plane Y;
        internal th_img_plane Cb;
        internal th_img_plane Cr;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public PixelFormat PixelFormat { get; private set; }
        public bool IsLast { get; private set; }

        public YCbCrImage(int width, int height, PixelFormat pixelFormat, bool isLast)
        {
            this.Width = width;
            this.Height = height;
            this.PixelFormat = pixelFormat;
            this.IsLast = isLast;

            this.Y = new th_img_plane();
            this.Y.width = this.Width;
            this.Y.height = this.Height;
            this.Y.stride = this.Width;
            this.Y.data = Marshal.AllocHGlobal(this.Y.stride * this.Y.height);

            this.Cb = new th_img_plane();
            this.Cb.width = this.PixelFormat == PixelFormat.TH_PF_444 ? this.Width : this.Width >> 1;
            this.Cb.height = this.PixelFormat == PixelFormat.TH_PF_420 ? this.Height >> 1 : this.Height;
            this.Cb.stride = this.Cb.width;
            this.Cb.data = Marshal.AllocHGlobal(this.Cb.stride * this.Cb.height);

            this.Cr = new th_img_plane();
            this.Cr.width = this.Cb.width;
            this.Cr.height = this.Cb.height;
            this.Cr.stride = this.Cb.stride;
            this.Cr.data = Marshal.AllocHGlobal(this.Cb.stride * this.Cb.height);
        }

        ~YCbCrImage()
        {
            // Finalizer calls Dispose(false)
            this.Dispose(false);
        }

        public static YCbCrImage CreateFromRGB24(byte[] frameData, int width, int height, PixelFormat pixelFormat, bool isLast)
        {
            Guard.CheckNull(frameData, "CreateFromRGB24(frameData)");

            var frameDataHandle = GCHandle.Alloc(frameData, GCHandleType.Pinned);
            try
            { return YCbCrImage.CreateFromRGB24(frameDataHandle.AddrOfPinnedObject(), width, height, pixelFormat, isLast); }
            finally
            { frameDataHandle.Free(); }
        }

        public static YCbCrImage CreateFromRGB24(IntPtr pframeData, int width, int height, PixelFormat pixelFormat, bool isLast)
        {
            if (pframeData == IntPtr.Zero)
                throw new ArgumentException("pframeData cannot be zero");

            int stride = width * 3;
            YCbCrImage image = new YCbCrImage(width, height, pixelFormat, isLast);

            unsafe
            {
                byte* frameData = (byte*)pframeData.ToPointer();

                byte* pimageY = (byte*)image.Y.data.ToPointer();
                byte* pimageCb = (byte*)image.Cb.data.ToPointer();
                byte* pimageCr = (byte*)image.Cr.data.ToPointer();

                switch (pixelFormat)
                {
                    case PixelFormat.TH_PF_420:
                        int ci = 0;
                        for (int y = 0; y < height; y += 2)
                            for (int x = 0; x < width; x += 2)
                            {
                                int xi = 3 * x;
                                int yi = height - y - 1;
                                int xyi = yi * stride + xi;
                                int yxi = (yi - 1) * stride + xi;

                                byte r00 = frameData[xyi + 0];
                                byte g00 = frameData[xyi + 1];
                                byte b00 = frameData[xyi + 2];

                                byte r01 = frameData[xyi + 3];
                                byte g01 = frameData[xyi + 4];
                                byte b01 = frameData[xyi + 5];

                                byte r10 = frameData[yxi + 0];
                                byte g10 = frameData[yxi + 1];
                                byte b10 = frameData[yxi + 2];

                                byte r11 = frameData[yxi + 3];
                                byte g11 = frameData[yxi + 4];
                                byte b11 = frameData[yxi + 5];

                                pimageY[y * width + x] = (byte)(((66 * r00 + 129 * g00 + 25 * b00 + 128) >> 8) + 16);
                                pimageY[y * width + x + 1] = (byte)(((66 * r01 + 129 * g01 + 25 * b01 + 128) >> 8) + 16);
                                pimageY[(y + 1) * width + x] = (byte)(((66 * r10 + 129 * g10 + 25 * b10 + 128) >> 8) + 16);
                                pimageY[(y + 1) * width + x + 1] = (byte)(((66 * r11 + 129 * g11 + 25 * b11 + 128) >> 8) + 16);

                                byte cb00 = (byte)(((112 * r00 - 94 * g00 - 18 * b00 + 128) >> 8) + 128);
                                byte cb01 = (byte)(((112 * r01 - 94 * g01 - 18 * b01 + 128) >> 8) + 128);
                                byte cb10 = (byte)(((112 * r10 - 94 * g10 - 18 * b10 + 128) >> 8) + 128);
                                byte cb11 = (byte)(((112 * r11 - 94 * g11 - 18 * b11 + 128) >> 8) + 128);

                                byte cr00 = (byte)(((-38 * r00 - 74 * g00 + 112 * b00 + 128) >> 8) + 128);
                                byte cr01 = (byte)(((-38 * r01 - 74 * g01 + 112 * b01 + 128) >> 8) + 128);
                                byte cr10 = (byte)(((-38 * r10 - 74 * g10 + 112 * b10 + 128) >> 8) + 128);
                                byte cr11 = (byte)(((-38 * r11 - 74 * g11 + 112 * b11 + 128) >> 8) + 128);

                                byte cbAverage = (byte)((cb00 + cb01 + cb10 + cb11) / 4);
                                byte crAverage = (byte)((cr00 + cr01 + cr10 + cr11) / 4);

                                pimageCb[ci] = cbAverage;
                                pimageCr[ci++] = crAverage;
                            }
                        break;
                    default:
                        throw new NotSupportedException(string.Format("PixelFormat not supported: {0}", pixelFormat));
                }

            }

            return image;
        }

        public static YCbCrImage CreateFromRGB32(byte[] frameData, int width, int height, PixelFormat pixelFormat, bool isLast)
        {
            Guard.CheckNull(frameData, "CreateFromRGB32(frameData)");

            var frameDataHandle = GCHandle.Alloc(frameData, GCHandleType.Pinned);
            try
            { return YCbCrImage.CreateFromRGB32(frameDataHandle.AddrOfPinnedObject(), width, height, pixelFormat, isLast); }
            finally
            { frameDataHandle.Free(); }
        }

        public static YCbCrImage CreateFromRGB32(IntPtr pframeData, int width, int height, PixelFormat pixelFormat, bool isLast)
        {
            if (pframeData == IntPtr.Zero)
                throw new ArgumentException("pframeData cannot be zero");

            int stride = width * 4;
            YCbCrImage image = new YCbCrImage(width, height, pixelFormat, isLast);

            unsafe
            {
                byte* frameData = (byte*)pframeData.ToPointer();

                byte* pimageY = (byte*)image.Y.data.ToPointer();
                byte* pimageCb = (byte*)image.Cb.data.ToPointer();
                byte* pimageCr = (byte*)image.Cr.data.ToPointer();

                switch (pixelFormat)
                {
                    case PixelFormat.TH_PF_420:
                        int ci = 0;
                        for (int y = 0; y < height; y += 2)
                            for (int x = 0; x < width; x += 2)
                            {
                                int xi = 4 * x;
                                int yi = height - y - 1;
                                int xyi = yi * stride + xi;
                                int yxi = (yi - 1) * stride + xi;

                                byte r00 = frameData[xyi + 0];
                                byte g00 = frameData[xyi + 1];
                                byte b00 = frameData[xyi + 2];

                                byte r01 = frameData[xyi + 4];
                                byte g01 = frameData[xyi + 5];
                                byte b01 = frameData[xyi + 6];

                                byte r10 = frameData[yxi + 0];
                                byte g10 = frameData[yxi + 1];
                                byte b10 = frameData[yxi + 2];

                                byte r11 = frameData[yxi + 4];
                                byte g11 = frameData[yxi + 5];
                                byte b11 = frameData[yxi + 6];

                                pimageY[y * width + x] = (byte)(((66 * r00 + 129 * g00 + 25 * b00 + 128) >> 8) + 16);
                                pimageY[y * width + x + 1] = (byte)(((66 * r01 + 129 * g01 + 25 * b01 + 128) >> 8) + 16);
                                pimageY[(y + 1) * width + x] = (byte)(((66 * r10 + 129 * g10 + 25 * b10 + 128) >> 8) + 16);
                                pimageY[(y + 1) * width + x + 1] = (byte)(((66 * r11 + 129 * g11 + 25 * b11 + 128) >> 8) + 16);

                                byte cb00 = (byte)(((112 * r00 - 94 * g00 - 18 * b00 + 128) >> 8) + 128);
                                byte cb01 = (byte)(((112 * r01 - 94 * g01 - 18 * b01 + 128) >> 8) + 128);
                                byte cb10 = (byte)(((112 * r10 - 94 * g10 - 18 * b10 + 128) >> 8) + 128);
                                byte cb11 = (byte)(((112 * r11 - 94 * g11 - 18 * b11 + 128) >> 8) + 128);

                                byte cr00 = (byte)(((-38 * r00 - 74 * g00 + 112 * b00 + 128) >> 8) + 128);
                                byte cr01 = (byte)(((-38 * r01 - 74 * g01 + 112 * b01 + 128) >> 8) + 128);
                                byte cr10 = (byte)(((-38 * r10 - 74 * g10 + 112 * b10 + 128) >> 8) + 128);
                                byte cr11 = (byte)(((-38 * r11 - 74 * g11 + 112 * b11 + 128) >> 8) + 128);

                                byte cbAverage = (byte)((cb00 + cb01 + cb10 + cb11) / 4);
                                byte crAverage = (byte)((cr00 + cr01 + cr10 + cr11) / 4);

                                pimageCb[ci] = cbAverage;
                                pimageCr[ci++] = crAverage;
                            }
                        break;
                    case PixelFormat.TH_PF_444:
                        for (int y = 0; y < height; y++)
                            for (int x = 0; x < width; x++)
                            {
                                int xi = 4 * x;
                                int yi = height - y - 1;
                                int xyi = yi * stride + xi;

                                byte r = frameData[xyi + 0];
                                byte g = frameData[xyi + 1];
                                byte b = frameData[xyi + 2];

                                pimageY[x + y * width] = (byte)((65481 * r + 128553 * g + 24966 * b + 4207500) / 255000);
                                pimageCb[x + y * image.Cb.width] = (byte)((-33488 * r - 65744 * g + 99232 * b + 29032005) / 225930);
                                pimageCr[x + y * image.Cr.width] = (byte)((157024 * r - 131488 * g - 25536 * b + 45940035) / 357510);
                            }

                        break;
                    default:
                        throw new NotSupportedException(string.Format("PixelFormat not supported: {0}", pixelFormat));
                }

            }

            return image;
        }

        public static YCbCrImage CreateFromRGB(IntPtr pframeData, int width, int height, int bits, PixelFormat pixelFormat, bool isLast)
        {
            switch (bits)
            {
                case 24:
                    return YCbCrImage.CreateFromRGB24(pframeData, width, height, pixelFormat, isLast);
                case 32:
                    return YCbCrImage.CreateFromRGB32(pframeData, width, height, pixelFormat, isLast);
                default:
                    throw new ArgumentException(string.Format("Wrong numbers bits per pixel: {0}", bits));
            }
        }

        public static YCbCrImage CreateFromRGB(byte[] frameData, int width, int height, int bits, PixelFormat pixelFormat, bool isLast)
        {
            switch (bits)
            {
                case 24:
                    return YCbCrImage.CreateFromRGB24(frameData, width, height, pixelFormat, isLast);
                case 32:
                    return YCbCrImage.CreateFromRGB32(frameData, width, height, pixelFormat, isLast);
                default:
                    throw new ArgumentException(string.Format("Wrong numbers bits per pixel: {0}", bits));
            }
        }

        public void ToRGB24(Byte[] rgbData)
        {
            Guard.CheckNull(rgbData, "ToRGB24(rgbData)");

            int i = 0;
            int pixels = this.Width * this.Height;
            int stride = this.Width * 3;
            int cbcrWidth = this.Width / 2;
            int cbcrHeight = this.Height / 2;
            int cbcrPixels = cbcrWidth * cbcrHeight;

            byte[] imageY = new byte[this.Y.stride * this.Y.height];
            byte[] imageCb = new byte[this.Cb.stride * this.Cb.height];
            byte[] imageCr = new byte[this.Cr.stride * this.Cr.height];

            Marshal.Copy(this.Y.data, imageY, 0, imageY.Length);
            Marshal.Copy(this.Cb.data, imageCb, 0, imageCb.Length);
            Marshal.Copy(this.Cr.data, imageCr, 0, imageCr.Length);

            for (int yCord = 0; yCord < Height; yCord++)
            {
                for (int xCord = 0; xCord < Width; xCord += 2)
                {
                    int c1 = imageY[yCord * this.Y.stride + xCord] - 16;
                    int c2 = imageY[yCord * this.Y.stride + xCord + 1] - 16;
                    int d = imageCb[yCord / 2 * this.Cb.stride + xCord / 2] - 128;
                    int e = imageCr[yCord / 2 * this.Cr.stride + xCord / 2] - 128;

                    rgbData[i++] = (byte)(Math.Min(255, Math.Max(0, (298 * c1 + 409 * e + 128) >> 8)));//r
                    rgbData[i++] = (byte)(Math.Min(255, Math.Max(0, (298 * c1 - 100 * d - 208 * e + 128) >> 8)));//g
                    rgbData[i++] = (byte)(Math.Min(255, Math.Max(0, (298 * c1 + 516 * d + 128) >> 8)));//b

                    rgbData[i++] = (byte)(Math.Min(255, Math.Max(0, (298 * c2 + 409 * e + 128) >> 8)));//r
                    rgbData[i++] = (byte)(Math.Min(255, Math.Max(0, (298 * c2 - 100 * d - 208 * e + 128) >> 8)));//g
                    rgbData[i++] = (byte)(Math.Min(255, Math.Max(0, (298 * c2 + 516 * d + 128) >> 8)));//b
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            // Freeing unmanaged resources
            if (this.Y.data != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(this.Y.data);
                this.Y.data = IntPtr.Zero;
            }

            if (this.Cb.data != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(this.Cb.data);
                this.Cb.data = IntPtr.Zero;
            }

            if (this.Cr.data != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(this.Cr.data);
                this.Cr.data = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
