using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using DXHook;
using DXHook.Hook.DX11;
using DXHook.Hook;
using DXHook.Interface;
using DXHook.Hook.Common;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Threading;

namespace Example
{

    public class Example : Script
    {
        CaptureInterface Interface;
        DXHookD3D11 Hook;


        static readonly Bitmap blah = Image.FromFile(@"M:\SandBox-Shared\Pictures\braw.png") as Bitmap;
        ImageElement _img;
        bool second;
        public Example()
        {
            var hwnd = Process.GetProcessesByName("explorer").First(x => x.MainWindowHandle != IntPtr.Zero).MainWindowHandle;
            KeyDown += OnKeyDown;
            Aborted += OnAborted;

            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(20);
                    _img?.SetBitmap(ScreenCapture.CaptureWindow(hwnd) as Bitmap);
                }
            });
        }

        private void OnAborted(object sender, EventArgs e)
        {
            Hook?.Cleanup();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.L)
            {

                if (Hook != null)
                {
                    Hook.Cleanup();
                    Hook.Dispose();
                    Hook = null;
                    _img?.Dispose();
                    _img = null;
                }
                else
                {
                    Hook = new DXHookD3D11(Interface = new CaptureInterface());
                    Hook.Config = new CaptureConfig
                    {
                        Direct3DVersion = Direct3DVersion.Direct3D11,
                        ShowOverlay = true
                    };
                    Hook.Hook();
                    GTA.UI.Notification.Show(blah.PixelFormat.ToString());
                    Script.Wait(5000);
                    _img = new ImageElement(blah) { Scale = 0.3f };
                    Interface.DrawOverlayInGame(new Overlay
                    {
                        Elements = new List<IOverlayElement>
                        {
                            _img
                        },
                        Hidden = false,
                    });
                }
            }
        }
    }
}
