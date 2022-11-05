using System;
using System.Diagnostics;
using System.Drawing;
using System.Xml.Linq;
using BitmapUtil;
using SharpDX;
using SharpDX.Direct3D11;
#pragma warning disable CS1591

namespace DXHook.Hook.DX11
{
    public class DXImage : DisposeCollector
    {
        private DeviceContext _deviceContext;
        private Texture2D _tex;
        private ShaderResourceView _texSRV;
        private bool _initialised;

        public int Width => _textDesc.Width;
        public int Height=>_textDesc.Height;

        public Device Device { get; }

        public DXImage(Device device, DeviceContext deviceContext)
        {
            Device = device;
            _deviceContext = deviceContext;
            _tex = null;
            _texSRV = null;


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
        private readonly ShaderResourceViewDescription _srvDesc;

        private readonly object _srvLock = new object();

        public bool Initialise(BitmapInfo bdata)
        {
            return Initialise(bdata.Width, bdata.Height, bdata.Stride, bdata.Scan0);
        }
        public bool Initialise(int width,int height,int stride,IntPtr scan0)
        {
            lock(this)
            {
                RemoveAndDispose(ref _tex);
                RemoveAndDispose(ref _texSRV);
                _tex = null;
                _texSRV = null;

                _textDesc.Width = width;
                _textDesc.Height = height; DataBox data;
                data.DataPointer = scan0;
                data.RowPitch = stride;// _texWidth * 4;
                data.SlicePitch = 0;

                _tex = Collect(new Texture2D(Device, _textDesc, new[] { data }));
                if (_tex == null)
                    return false;

                _texSRV = Collect(new ShaderResourceView(Device, _tex, _srvDesc));
                if (_texSRV == null)
                    return false;
                _initialised = true;
                return true;
            }
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
