using DXHook.Hook.DX11;
using DXHook.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace DXHook.Hook.Common
{
    [Serializable]
    public class ImageElement : Element
    {
        internal DXImage Image;
        internal Bitmap _initialBmp;

        /// <summary>
        /// Update current bitmap with supplied one
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="dispose">Whether to dispose given bitmap after use</param>
        public void SetBitmap(Bitmap bmp,bool dispose=false)
        {
            lock (this)
            {
                if(_initialBmp == null || Image == null)
                {
                    _initialBmp = bmp;
                }
                else
                {
                    Image.Update(bmp);
                }
                if (dispose) { bmp?.Dispose(); }
            }
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

        bool _ownsBitmap;

        public ImageElement() { }

        public ImageElement(string filename) :
            this(new Bitmap(filename), true)
        {
            Filename = filename;
        }

        public ImageElement(Bitmap bitmap, bool ownsImage = false)
        {
            Tint = Color.White;
            SetBitmap(bitmap, ownsImage);
            _ownsBitmap = ownsImage;
            Scale = 1.0f;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (_ownsBitmap)
                {
                    _initialBmp?.Dispose();
                }
            }
        }
    }
}
