﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using CPUZ.Model;
using Newtonsoft.Json;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;

using Vector2 = CPUZ.Model.Vector2;
using System.Linq;
using System.Text;
using SharpDX.DirectWrite;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;
using Vector3 = CPUZ.Model.Vector3;

namespace CPUZ
{
    public partial class Cpuz : Form
    {
        public Cpuz()
        {
            InitializeComponent();
            //Make the window's border completely transparant

            #region D3D初始化
            // ANTI BATTLEYE SIG SCAN ;)
            this.Text = Guid.NewGuid().ToString().Replace("-", "");

            // TRANSPARENCY KEY
            this.BackColor = System.Drawing.Color.Black;

            // SETTINGS
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.WindowState = FormWindowState.Maximized;

            // MAKE WINDOW TRANSPARENT
            Win32.SetWindowLong(this.Handle, Win32.GWL_EXSTYLE, (IntPtr)(Win32.GetWindowLong(this.Handle, Win32.GWL_EXSTYLE) ^ Win32.WS_EX_LAYERED ^ Win32.WS_EX_TRANSPARENT));

            // MAKE WINDOW SOLID
            Win32.SetLayeredWindowAttributes(this.Handle, 0, 255, Win32.LWA_ALPHA);

            var targetProperties = new HwndRenderTargetProperties
            {
                Hwnd = this.Handle,
                PixelSize = new Size2(this.Bounds.Right - this.Bounds.Left, this.Bounds.Bottom - this.Bounds.Top),
                PresentOptions = PresentOptions.Immediately
            };

            var prop = new RenderTargetProperties(RenderTargetType.Hardware, new PixelFormat(Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied), 0, 0, RenderTargetUsage.None, FeatureLevel.Level_DEFAULT);

            var d3dFactory = new SharpDX.Direct2D1.Factory();

            var device = new WindowRenderTarget(d3dFactory, prop, targetProperties)
            {
                TextAntialiasMode = TextAntialiasMode.Cleartype,
                AntialiasMode = AntialiasMode.Aliased
            };

            #endregion

            var k = KReader.readPuBase();
            JSON_DATA json_data = null;
            var dxthread = new Thread(() =>
            {
                var brushWhite = new SolidColorBrush(device, RawColorFromColor(Color.White));
                var brushBlack = new SolidColorBrush(device, RawColorFromColor(Color.Black));
                var brushGreen = new SolidColorBrush(device, RawColorFromColor(Color.Green));
                var brushRed = new SolidColorBrush(device, RawColorFromColor(Color.Red));
                var brushPurple = new SolidColorBrush(device, RawColorFromColor(Color.Purple));
                var fontFactory = new SharpDX.DirectWrite.Factory();
                var fontConsolas = new SharpDX.DirectWrite.TextFormat(fontFactory, "Consolas", 15);
                var fontESP = new SharpDX.DirectWrite.TextFormat(fontFactory, "Consolas", 12);

                while (true)
                {
                    // attempt to download JSON data as a string
                    try
                    {
                        json_data = DoGame.getGameDate();

                        if (json_data != null && json_data.players.Count > 0)
                        {
                            mainFrom.STATE = true;
                            device.BeginDraw();
                            device.Clear(null);

                            #region RADAR

                            int radarSize = 600;
                            Vector2 centerpoint = new Vector2(2356, 1210);

                            if (Setting.雷达)
                            {
                                Ellipse el3 = new Ellipse(new RawVector2(2356, 1210), 1, 1);
                                device.DrawEllipse(el3, brushRed);
                                device.FillEllipse(el3, brushRed);

                                // TODO: INTEGRATE INTO MINIMAP
                                //if (Setting.雷达)
                                //{
                                //    var radarOuterRectangle = new RawRectangleF(radarX, radarY, radarX + radarSize,
                                //        radarY + radarSize);
                                //    var radarRectangle = new RawRectangleF(radarX + radarBorder, radarY + radarBorder,
                                //        radarX + radarSize - radarBorder, radarY + radarSize - radarBorder);

                                //    var radarCenterRectangle = new RoundedRectangle()
                                //    {
                                //        RadiusX = 4,
                                //        RadiusY = 4,
                                //        Rect = new RawRectangleF(centerpoint.X, centerpoint.Y, centerpoint.X + 4,
                                //            centerpoint.Y + 4)
                                //    };

                                //    device.FillRectangle(radarRectangle, brushBlack);
                                //    device.DrawRectangle(radarRectangle, brushWhite);

                                //    device.FillRoundedRectangle(radarCenterRectangle, brushGreen);

                                //}
                            }

                            #endregion


                            var vecLocalLocation = new Model.Vector3
                            {
                                X = json_data.camera[1].X,
                                Y = json_data.camera[1].Y,
                                Z = json_data.camera[1].Z
                            };

                            var PlayerCameraManager = new PlayerCameraManager
                            {
                                CameraCache = new FCameraCacheEntry
                                {
                                    POV = new FMinimalViewInfo
                                    {
                                        Fov = json_data.camera[2].X,
                                        Location = new Model.Vector3
                                        {
                                            X = json_data.camera[1].X,
                                            Y = json_data.camera[1].Y,
                                            Z = json_data.camera[1].Z
                                        },
                                        Rotation = new Model.FRotator
                                        {
                                            Pitch = json_data.camera[0].X,
                                            Yaw = json_data.camera[0].Y,
                                            Roll = json_data.camera[0].Z
                                        }
                                    }
                                }
                            };

                            #region 车

                            if (Setting.车辆显示)
                            {
                                foreach (var v in json_data.vehicles)
                                {
                                    var vecActorLocation = new Vector3 { X = v.rx, Y = v.ry, Z = v.rz };
                                    var vecRelativePos = vecLocalLocation - vecActorLocation;
                                    var lDeltaInMeters = vecRelativePos.Length / 100;
                                    if (lDeltaInMeters <= 400)
                                    {
                                        Vector2 screenlocation;
                                        WorldToScreen(vecActorLocation, PlayerCameraManager, out screenlocation);

                                        DrawText($"[{v.v}] {(int)lDeltaInMeters}m", (int)screenlocation.X,
                                            (int)screenlocation.Y, v.v=="Deat"?brushBlack:brushGreen, fontFactory, fontESP, device);
                                    }

                                }
                            }

                            #endregion

                            #region 物品

                            //todo:有BUG
                            if (Setting.物品显示)
                            {
                                foreach (var v in json_data.items)
                                {
                                    var vecActorLocation = new Vector3 { X = v.rx, Y = v.ry, Z = v.rz };
                                    var vecRelativePos = vecLocalLocation - vecActorLocation;
                                    var lDeltaInMeters = vecRelativePos.Length / 100;

                                    Vector2 screenlocation;
                                    WorldToScreen(vecActorLocation, PlayerCameraManager, out screenlocation);

                                    DrawText($"{v.n}", (int)screenlocation.X,
                                        (int)screenlocation.Y, brushWhite, fontFactory, fontESP, device);

                                }
                            }

                            #endregion

                            #region 人物

                            var playerList = json_data.players.OrderBy(z => z.id).ToList();
                            var localPlayer = playerList[0];
                            foreach (var player in playerList)
                            {
                                if (player.health > 0 && player.isInactive > 0 && player.id != 0)
                                {
                                    var vecPlayerLocation = new Vector3 { X = player.rx, Y = player.ry, Z = player.rz };
                                    var vecRelativePos = vecLocalLocation - vecPlayerLocation;
                                    //距离
                                    var lDeltaInMeters = vecRelativePos.Length / 100.0f;


                                    if (lDeltaInMeters >= 750)
                                    {
                                        continue;
                                    }
                                    #region 线条
                                    if (Setting.线条)
                                        if (lDeltaInMeters > 3)
                                            if (
                                            //超过200米不显示
                                            lDeltaInMeters <= 200
                                            //超过40人不显示线
                                            && json_data.players.Count <= 40
                                            //队友不显示
                                            && player.t != localPlayer.t
                                                )
                                            {
                                                Vector2 screenlocation;
                                                WorldToScreen(vecPlayerLocation, PlayerCameraManager, out screenlocation);

                                                device.DrawLine(new RawVector2(2560 / 2, 1440),
                                                    new RawVector2(screenlocation.X, screenlocation.Y), lDeltaInMeters <= 100 ? brushRed : brushWhite);
                                            }
                                    #endregion

                                    #region Distance ESP
                                    if (lDeltaInMeters > 3)

                                        if (Setting.距离和血量)
                                        {
                                            Vector2 screenlocation;
                                            WorldToScreen(vecPlayerLocation, PlayerCameraManager, out screenlocation);
                                            SolidColorBrush brush = brushRed;

                                            if (lDeltaInMeters >= 250)
                                                brush = brushPurple;
                                            if (lDeltaInMeters >= 500)
                                                brush = brushGreen;
                                            if (player.t == localPlayer.t)
                                                brush = brushGreen;

                                            DrawText($"[{player.id}]{(int)player.health} {(int)lDeltaInMeters}m",
                                                (int)screenlocation.X,
                                                (int)screenlocation.Y,
                                                brush,
                                                fontFactory, fontESP, device);
                                        }

                                    #endregion
                                    
                                    #region Radar
                                    if (Setting.雷达)
                                    {
                                        var loclalLocation = new Vector3 { X = playerList[0].x, Y = playerList[0].y, Z = playerList[0].z };
                                        var currentActorLocation = new Vector3 { X = player.x, Y = player.y, Z = player.z };

                                        var relativePos = loclalLocation - currentActorLocation;

                                        if (relativePos.Length / 100.0f <= radarSize / 2 /*DISTANCE FROM CENTER TO EDGE*/)
                                        {
                                            Vector2 screenpos = centerpoint - relativePos.To2D() / 118f;

                                            Ellipse el22 = new Ellipse(new RawVector2(screenpos.X, screenpos.Y), 3, 3);
                                            device.DrawEllipse(el22, brushRed);
                                            device.FillEllipse(el22, brushRed);
                                        }
                                    }
                                    #endregion

                                    #region 骨骼

                                    if (lDeltaInMeters < 150 && lDeltaInMeters>10)
                                    {
                                        DrawSkeleton(player.mesh, PlayerCameraManager, device, brushRed, fontESP);
                                    }
                                    #endregion
                                }

                            }
                            #endregion
                            // DRAW END
                            device.EndDraw();

                        }
                        else
                        {
                            mainFrom.STATE = false;
                        }

                    }
                    catch (Exception ex)
                    {
                        System.IO.File.WriteAllText("c:\\log\\bug_json.txt", JsonConvert.SerializeObject(json_data));
                    }
                    Thread.Sleep(10);
                }
            })
            {
                IsBackground = true
            };
            dxthread.Start();

            #region Web端     
            var webThread = new Thread(() =>
            {
                while (true)
                {
                    using (var webClient = new WebClient())
                    {
                        if (Setting.Web端 && json_data.players.Count > 0 && !webClient.IsBusy)
                        {
                            try
                            {
                                // 指定 WebClient 編碼
                                webClient.Encoding = Encoding.UTF8;
                                // 指定 WebClient 的 Content-Type header
                                webClient.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                                // 指定 WebClient 的 authorization header
                                //webClient.Headers.Add("authorization", "token {apitoken}");
                                // 執行 PUT 動作
                                var result = webClient.UploadString("http://127.0.0.1:3000/api/5", "PUT", JsonConvert.SerializeObject(json_data));
                            }
                            catch (Exception e)
                            {
                                continue;
                            }

                        }
                    }
                    Thread.Sleep(200);
                }
            });

            webThread.IsBackground = true;
            webThread.Start();
            #endregion


            var marg = new Win32.Margins { Left = 0, Top = 0, Right = this.Width, Bottom = this.Height };
            Win32.DwmExtendFrameIntoClientArea(this.Handle, ref marg);
        }



        private void DrawSkeleton(ulong mesh, PlayerCameraManager CameraManager, WindowRenderTarget device, Brush brush, TextFormat font)
        {
            Vector2 vHead, vRoot, vCenter;
            var bRoot = DoGame.GetBoneWithRotation(mesh, Bones.Root);
            var middle = DoGame.GetBoneWithRotation(mesh, Bones.pelvis);

            WorldToScreen(bRoot, CameraManager, out vRoot);
            WorldToScreen(middle, CameraManager, out vCenter);

            var flWidth = Math.Abs(vRoot.Y - vCenter.Y) / 2;
            var flHeight = Math.Abs(vRoot.Y - vCenter.Y);
            vHead.X = vCenter.X - flWidth;
            vHead.Y = vCenter.Y - flHeight;
            //2d box drawing
            //2d box drawing
            DrawLine(vCenter.X - flWidth, vHead.Y, vCenter.X + flWidth, vHead.Y, device,brush); // top
            DrawLine(vCenter.X - flWidth, vRoot.Y, vCenter.X + flWidth, vRoot.Y, device, brush); // bottom
            DrawLine(vCenter.X - flWidth, vHead.Y, vCenter.X - flWidth, vRoot.Y, device, brush); // left
            DrawLine(vCenter.X + flWidth, vRoot.Y, vCenter.X + flWidth, vHead.Y, device, brush); // right


            //Bones[] l = { Bones.forehead, Bones.Head,Bones.neck_01,Bones.hand_l,Bones.hand_r,Bones.foot_l,Bones.foot_r};
            //foreach (var b in l)
            //{
            //    Vector3 pos = DoGame.GetBoneWithRotation(mesh, (Bones)b);
            //    Vector2 h1;
            //    WorldToScreen(pos, CameraManager, out h1);
            //    var radarPlayerRectangle = new RoundedRectangle()
            //    {
            //        RadiusX = 4,
            //        RadiusY = 4,
            //        Rect = new RawRectangleF(h1.X, h1.Y, h1.X + 4,
            //            h1.Y + 4)
            //    };
            //    var fontFactory = new SharpDX.DirectWrite.Factory();
            //    var fontConsolas = new SharpDX.DirectWrite.TextFormat(fontFactory, "Consolas", 15);
            //    var fontESP = new SharpDX.DirectWrite.TextFormat(fontFactory, "Consolas", 12);
            //    DrawText(b.ToString(), (int)h1.X + 4,
            //        (int)(int)h1.Y + 4,
            //        new SolidColorBrush(device, RawColorFromColor(Color.White))
            //        , fontFactory,
            //        fontESP,
            //        device);

            //    device.FillRoundedRectangle(radarPlayerRectangle, brush);

            //}
        }

        private bool WorldToScreen(Vector3 WorldLocation, PlayerCameraManager CameraManager, out Vector2 Screenlocation)
        {
            Screenlocation = new Vector2(0, 0);

            var POV = CameraManager.CameraCache.POV;
            FRotator Rotation = POV.Rotation;

            Vector3 vAxisX, vAxisY, vAxisZ;
            Rotation.GetAxes(out vAxisX, out vAxisY, out vAxisZ);

            Vector3 vDelta = WorldLocation - POV.Location;
            Vector3 vTransformed = new Vector3(Vector3.DotProduct(vDelta, vAxisY), Vector3.DotProduct(vDelta, vAxisZ), Vector3.DotProduct(vDelta, vAxisX));

            if (vTransformed.Z < 1f)
                vTransformed.Z = 1f;

            float FovAngle = POV.Fov;
            float ScreenCenterX = 2560 / 2;
            float ScreenCenterY = 1440 / 2;

            Screenlocation.X = ScreenCenterX + vTransformed.X * (ScreenCenterX / (float)Math.Tan(FovAngle * (float)Math.PI / 360)) / vTransformed.Z;
            Screenlocation.Y = ScreenCenterY - vTransformed.Y * (ScreenCenterX / (float)Math.Tan(FovAngle * (float)Math.PI / 360)) / vTransformed.Z;

            return true;
        }

        private SharpDX.DirectWrite.TextLayout TextLayout(string szText, SharpDX.DirectWrite.Factory factory, SharpDX.DirectWrite.TextFormat font) =>
            new SharpDX.DirectWrite.TextLayout(factory, szText, font, float.MaxValue, float.MaxValue);


        private void DrawLine(float x, float y, float xx, float yy, WindowRenderTarget device, Brush brush)
        {
            RawVector2[] dLine = new RawVector2[2];

            dLine[0].X = x;
            dLine[0].Y = y;

            dLine[1].X = xx;
            dLine[1].Y = yy;
            DrawLines(dLine, device, brush);

        }

        private void DrawLines(RawVector2[] point0, WindowRenderTarget device, Brush brush)
        {
            if (point0.Length < 2)
                return;

            for (int x = 0; x < point0.Length - 1; x++)
                device.DrawLine(point0[x], point0[x + 1], brush);
        }


        private void DrawText(string szText, int x, int y, SharpDX.Direct2D1.Brush foregroundBroush, SharpDX.DirectWrite.Factory fontFactory, SharpDX.DirectWrite.TextFormat font, WindowRenderTarget device)
        {
            var tempTextLayout = TextLayout(szText, fontFactory, font);

            device.DrawTextLayout(new RawVector2(x, y), tempTextLayout, foregroundBroush, DrawTextOptions.NoSnap);

            tempTextLayout.Dispose();
        }

        private RawColor4 RawColorFromColor(Color color) => new RawColor4(color.R, color.G, color.B, color.A);
        //color.ToArgb() >> 16 & 255L, color.ToArgb() >> 8 & 255L, (byte)color.ToArgb() & 255L, color.ToArgb() >> 24 & 255L);

        private static class Win32
        {
            #region Definitions
            public const int GWL_EXSTYLE = -20;

            public const int WS_EX_LAYERED = 0x80000;

            public const int WS_EX_TRANSPARENT = 0x20;

            public const int LWA_ALPHA = 0x2;

            public const int LWA_COLORKEY = 0x1;
            #endregion

            #region Functions
            [DllImport("user32.dll", SetLastError = true)]
            public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll")]
            public static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

            [DllImport("user32.dll")]
            public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

            [DllImport("dwmapi.dll")]
            public static extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMargins);
            #endregion

            #region Structs
            internal struct Margins
            {
                public int Left, Right, Top, Bottom;
            }
            #endregion
        }

        private void OverlayForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
