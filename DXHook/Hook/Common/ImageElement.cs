using DXHook.Hook.DX11;
using DXHook.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using BitmapUtil;

#pragma warning disable CS1591

namespace DXHook.Hook.Common
{
    [Serializable]
    public class ImageElement : Element
    {
        [Obsolete]
        internal Bitmap Bitmap => null;

        public int Width => (int)(BitmapInfo.Width * Scale);
        public int Height => (int)(BitmapInfo.Height * Scale);

        /// <summary>
        /// Pointer to this image's underlying buffer area
        /// </summary>
        public IntPtr PtrBuffer => BitmapInfo.Scan0;
        internal object SwapLock = new object();
        internal DXImage Front;
        internal DXImage Back;
        private bool _ownsBuffer;
        public int BufferSize { get; private set; }
        internal BitmapInfo BitmapInfo = new BitmapInfo { BytesPerPixel = 4 };

        /// <summary>
        /// Update current bitmap with supplied one
        /// </summary>
        /// <param name="bmp">The bitmap to replace current one with</param>
        /// <remarks>Image buffer is copied to <see cref="PtrBuffer"/></remarks>
        public unsafe void SetBitmap(Bitmap bmp)
        {
            lock (this)
            {
                BitmapData d = null;
                try
                {
                    d = bmp.LockBits(new Rectangle { Size = bmp.Size }, ImageLockMode.ReadOnly, bmp.PixelFormat);
                    var size = Resize(bmp.Size, d.Stride / d.Width);
                    Buffer.MemoryCopy((void*)d.Scan0, (void*)BitmapInfo.Scan0, size, size);
                    Invalidate();
                }
                finally
                {
                    if (d != null)
                    {
                        bmp.UnlockBits(d);
                    }
                }

            }
        }

        public bool Invalidate()
        {
            if (BitmapInfo.Scan0 == default)
            {
                return false;
            }
            lock (this)
            {
                if (Back?.Initialise(BitmapInfo) != true)
                {
                    return false;
                }

                lock (SwapLock)
                {
                    var t = Front;
                    Front = Back;
                    Back = t;
                }

                return true;
            }
        }

        public int Resize(Size size, int bytesPerPixel)
        {
            BitmapInfo.Size = size;
            BitmapInfo.BytesPerPixel = bytesPerPixel;
            var newSize = BitmapInfo.BufferLength;
            if (newSize > BufferSize || PtrBuffer == default)
            {
                SetBuffer(newSize);
            }
            return newSize;
        }

        public virtual byte Opacity
        {
            get => Tint.A;
            set => Tint = Color.FromArgb(value, Tint.R, Tint.G, Tint.B);
        }

        /// <summary>
        /// This value is multiplied with the source color (e.g. White will result in same color as source image)
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="System.Drawing.Color.White"/>.
        /// </remarks>
        public virtual Color Tint { get; set; } = Color.White;

        /// <summary>
        /// The location of where to render this image element
        /// </summary>
        public virtual Point Location { get; set; }

        public float Angle { get; set; }

        public float Scale { get; set; } = 1.0f;

        public string Filename { get; set; }

        public ImageElement() { }

        public ImageElement(string filename) :
            this(new Bitmap(filename))
        {
            Filename = filename;
        }

        public ImageElement(Bitmap bitmap)
        {
            Tint = Color.White;
            SetBitmap(bitmap);
            Scale = 1.0f;
        }

        public ImageElement(int width, int height, int bytesPerPixel, IntPtr scan0)
        {
            BitmapInfo.BytesPerPixel = bytesPerPixel;
            BitmapInfo.Width = width;
            BitmapInfo.Height = height;
            BitmapInfo.Scan0 = scan0;
            BufferSize = BitmapInfo.BufferLength;
        }

        public void SetBuffer(int bufferSize)
        {
            lock (this)
            {
                _freeBuffer();
                BitmapInfo.Scan0 = Marshal.AllocHGlobal(bufferSize);
                BufferSize = bufferSize;
                _ownsBuffer = true;
            }
        }

        private void _freeBuffer()
        {
            if (!_ownsBuffer)
            {
                return;
            }
            lock (this)
            {
                if (BitmapInfo.Scan0 != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(BitmapInfo.Scan0);
                }
                BitmapInfo.Scan0 = IntPtr.Zero;
                BufferSize = 0;
            }
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _freeBuffer();
                Front?.Dispose();
                Back?.Dispose();
            }
        }
    }
}
