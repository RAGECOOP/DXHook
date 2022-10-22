using System;
using System.Diagnostics;
using System.Drawing;
using SharpDX;
using SharpDX.Direct3D11;

namespace DXHook.Hook.DX11
{
    public class DXImage : DisposeCollector
    {
        Device _device;
        DeviceContext _deviceContext;
        Texture2D _tex;
        ShaderResourceView _texSRV;
        int _texWidth, _texHeight;
        bool _initialised = false;

        public int Width
        {
            get
            {
                return _texWidth;
            }
        }

        public int Height
        {
            get
            {
                return _texHeight;
            }
        }

        public Device Device
        {
            get { return _device; }
        }

        public DXImage(Device device, DeviceContext deviceContext)
        {
            _device = device;
            _deviceContext = deviceContext;
            _tex = null;
            _texSRV = null;
            _texWidth = 0;
            _texHeight = 0;


            _srvDesc = new ShaderResourceViewDescription();
            _srvDesc.Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm;
            _srvDesc.Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D;
            _srvDesc.Texture2D.MipLevels = 1;
            _srvDesc.Texture2D.MostDetailedMip = 0;


            _textDesc = new Texture2DDescription();
            _textDesc.MipLevels = 1;
            _textDesc.ArraySize = 1;
            _textDesc.Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm;
            _textDesc.SampleDescription.Count = 1;
            _textDesc.SampleDescription.Quality = 0;
            _textDesc.Usage = ResourceUsage.Immutable;
            _textDesc.BindFlags = BindFlags.ShaderResource;
            _textDesc.CpuAccessFlags = CpuAccessFlags.None;
            _textDesc.OptionFlags = ResourceOptionFlags.None;
        }

        private Texture2DDescription _textDesc;
        private ShaderResourceViewDescription _srvDesc;

        private object _srvLock = new object();

        public bool Initialise(Bitmap bitmap)
        {
            lock (this)
            {
                _tex?.Dispose();
                _texSRV?.Dispose();
                RemoveAndDispose(ref _tex);
                RemoveAndDispose(ref _texSRV);
                _tex = null;
                _texSRV = null;
                GC.Collect();

                //Debug.Assert(bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                System.Drawing.Imaging.BitmapData bmData;

                _textDesc.Width = _texWidth = bitmap.Width;
                _textDesc.Height = _texHeight = bitmap.Height;

                bmData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, _texWidth, _texHeight), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try
                {

                    SharpDX.DataBox data;
                    data.DataPointer = bmData.Scan0;
                    data.RowPitch = bmData.Stride;// _texWidth * 4;
                    data.SlicePitch = 0;

                    _tex = Collect(new Texture2D(_device, _textDesc, new[] { data }));
                    if (_tex == null)
                        return false;

                    _texSRV = Collect(new ShaderResourceView(_device, _tex, _srvDesc));
                    if (_texSRV == null)
                        return false;
                }
                finally
                {
                    bitmap.UnlockBits(bmData);
                }

                _initialised = true;
                return true;
            }
        }

        public void Update(Bitmap bitmap)
        {
            Initialise(bitmap);
        }

        public ShaderResourceView GetSRV()
        {
            //Debug.Assert(_initialised);
            if (!_initialised) return null;
            lock (_srvLock)
            {
                return _texSRV;
            }
        }

    }
}
