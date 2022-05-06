using CourseRecorder.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using WebSocketSharp;

namespace CourseRecorder.Course
{
    public class CourseManager
    {
        public Guid CourseId;
        public string ServerAddress;
        public string ShortCode;
        public string JoinURL;
        public string StreamServer;
        private WebSocket ws;
        private CourseStreamer streamer;
        private DateTime LastScreenshotTime;
        
        public CourseManager(string serverAddress)
        {
            ServerAddress = serverAddress;
            ws = new WebSocket("wss://" + serverAddress + "/ws");
            ws.OnOpen += (sender, e) =>
            {
                State = CourseState.ServerConnected;
                if (CourseId != Guid.Empty)
                {
                    Debug.WriteLine("Rejoining...");
                    ws.Send(JsonConvert.SerializeObject(new { action = "recorderRejoin", courseId = CourseId }));
                }
                else
                {
                    ws.Send(JsonConvert.SerializeObject(new { action = "recorderRegister" }));
                }

            };
            ws.OnMessage += WsMessageHandler;
            ws.OnClose += WsCloseHandler;
            ws.OnError += WsErrorHandler;
        }
        public void Connect()
        {
            ws.Connect();
        }
        public void Disconnect()
        {
            State = CourseState.BeforeEnd;
            if (ws.ReadyState == WebSocketState.Open)
            {
                ws.Close();
            }
            if(streamer != null)
            {
                streamer.Stop();
            }
        }
        public void WsMessageHandler(object sender, MessageEventArgs e)
        {
            var wsMsg = JsonConvert.DeserializeObject<Dictionary<string, object>>(e.Data);
            switch (wsMsg["action"])
            {
                case "registered":
                    CourseId = Guid.Parse((string)wsMsg["courseId"]);
                    ShortCode = wsMsg["shortCode"].ToString();
                    JoinURL = wsMsg["joinURL"].ToString();
                    StreamServer = wsMsg["streamServer"].ToString();

                    streamer = new CourseStreamer();
                    streamer.AudioInputDevice = "";
                    streamer.StreamURL = "rtmp://" + StreamServer + "/live/" + Guid.NewGuid().ToString() + "?courseId=" + CourseId.ToString();

                    State = CourseState.CourseRegistered;
                    break;
                case "rejoined":
                case "courseBegin":
                    Program.cep.CourseEvent += CourseEventHandler;
                    State = CourseState.CourseStarted;
                    break;
                case "rejoinFailed":
                    MessageBox.Show("断线重连失败，请重新运行" + Application.ProductName + "开课", Application.ProductName);
                    Application.Exit();
                    break;
                default:
                    Debug.WriteLine("Unhandled message: " + e.Data);
                    break;
            }
        }

        public void WsCloseHandler(object sender, CloseEventArgs e)
        {
            Program.cep.CourseEvent -= CourseEventHandler;
            if (State != CourseState.BeforeEnd)
            { // accidently closed
                State = CourseState.TryReconnect;
                Debug.WriteLine("Try to reconnect after 5 seconds");
                Thread.Sleep(5000);
                Connect();
            }
        }

        public void StreamChangeInput(string inputDevice)
        {
            if (streamer != null)
            {
                streamer.AudioInputDevice = inputDevice;
                streamer.Restart();
            }
        }

        public void WsErrorHandler(object sender, ErrorEventArgs e)
        {
            Program.cep.CourseEvent -= CourseEventHandler;
            State = CourseState.TryReconnect;
        }

        public async void CourseEventHandler(object sender, CourseEventArgs e)
        {
            if (State != CourseState.CourseStarted) return;
            switch (e)
            {
                case KeyboardEventArgs KeyboardEvent:
                    string KeyboardEventScreenshot = await TakeScreenshotAndUpload(KeyboardEvent.EventId.ToString());
                    if (KeyboardEventScreenshot != null)
                    {
                        AttachmentArgs attachment = new AttachmentArgs(KeyboardEventScreenshot, "screenshot");
                        ws.Send(JsonConvert.SerializeObject(new EventMessage(KeyboardEvent, attachment)));
                        _ = Task.Delay(250).ContinueWith((task) =>
                        {
                            // take another screenshot after 250ms
                            CourseEventHandler(sender, new TriggerOnDemandEventArgs("screenshot"));
                        });
                        _ = Task.Delay(1000).ContinueWith((task) =>
                        {
                            // well, another screenshot after 1s
                            CourseEventHandler(sender, new TriggerOnDemandEventArgs("screenshot"));
                        });
                    }
                    break;
                case MouseEventArgs MouseEvent:
                string MouseEventScreenshot = await TakeScreenshotAndUpload(MouseEvent.EventId.ToString());
                if (MouseEventScreenshot != null)
                {
                    AttachmentArgs attachment = new AttachmentArgs(MouseEventScreenshot, "screenshot");
                    ws.Send(JsonConvert.SerializeObject(new EventMessage(MouseEvent, attachment)));
                    _ = Task.Delay(250).ContinueWith((task) =>
                    {
                        // take another screenshot after 250ms
                        CourseEventHandler(sender, new TriggerOnDemandEventArgs("screenshot"));
                    });
                    _ = Task.Delay(1000).ContinueWith((task) =>
                    {
                        // well, another screenshot after 1s
                        CourseEventHandler(sender, new TriggerOnDemandEventArgs("screenshot"));
                    });
                    }
                    break;
                case DocumentEventArgs DocumentEvent:
                    string DocumentFileId = await TakeScreenshotAndUpload(DocumentEvent.EventId.ToString());
                    if (DocumentFileId != null)
                    {
                        AttachmentArgs attachment = new AttachmentArgs(DocumentFileId, "screenshot");
                        ws.Send(JsonConvert.SerializeObject(new EventMessage(DocumentEvent, attachment)));
                    }
                    else
                    {
                        ws.Send(JsonConvert.SerializeObject(new EventMessage(DocumentEvent)));
                    }
                    break;
                case TriggerOnDemandEventArgs TriggerOnDemandEvent:
                    switch (TriggerOnDemandEvent.Action)
                    {
                        case "screenshot":
                            string ScreenshotOnDemandFileId = await TakeScreenshotAndUpload(TriggerOnDemandEvent.EventId.ToString());
                            if (ScreenshotOnDemandFileId != null)
                            {
                                AttachmentArgs attachment = new AttachmentArgs(ScreenshotOnDemandFileId, "screenshot");
                                ws.Send(JsonConvert.SerializeObject(new EventMessage(TriggerOnDemandEvent, attachment)));
                            }
                            break;
                        default:
                            Debug.WriteLine("Unhandled message: " + TriggerOnDemandEvent.Action);
                            break;
                    }
                    break;

                default:
                    break;
            }
        }

        private async Task<string> TakeScreenshotAndUpload(string eventId)
        {
            //throttle
            if (LastScreenshotTime.AddMilliseconds(200) > DateTime.Now)
            {
                return null;
            }
            LastScreenshotTime = DateTime.Now;
            //Take screenshot
            using (WebP webp = new WebP())
            {
                try
                {
                    DEVMODE dm = new DEVMODE();
                    dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                    EnumDisplaySettings(Screen.AllScreens[0].DeviceName, -1, ref dm);
                    Bitmap ps = new Bitmap(dm.dmPelsWidth, dm.dmPelsHeight);
                    Graphics graphics = Graphics.FromImage(ps as Image);
                    graphics.CopyFromScreen(0, 0, 0, 0, ps.Size);
                    byte[] rawWebP = webp.EncodeLossy(ps, 75);
                    //Upload to server
                    using (HttpClient httpClient = new HttpClient())
                    {
                        MultipartFormDataContent form = new MultipartFormDataContent();
                        form.Add(new ByteArrayContent(rawWebP, 0, rawWebP.Length), "file", "screenshot.webp");
                        HttpResponseMessage response = await httpClient.PostAsync($"https://{this.ServerAddress}/api/uploadfile?courseId={this.CourseId}&eventId={eventId}&fileType=screenshot", form);
                        response.EnsureSuccessStatusCode();
                        string res = await response.Content.ReadAsStringAsync();
                        //Debug.WriteLine(res);
                        var httpMsg = JsonConvert.DeserializeObject<Dictionary<string, object>>(res);
                        if ((bool)httpMsg["success"] == true)
                        {
                            return (string)httpMsg["fileName"];
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    return null;
                }
            }


        }

        public event Action OnStateChanged;
        private CourseState _state;
        public CourseState State
        {
            get
            {
                return _state;
            }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnStateChanged?.Invoke();
                }
            }
        }
        public enum CourseState
        {
            ServerNotConnected,
            ServerConnected,
            TryReconnect,
            CourseRegistered,
            CourseStarted,
            BeforeEnd,
            End
        }
        public class EventMessage
        {
            public string action = "eventUpdate";
            public AttachmentArgs attachment;
            public CourseEventArgs eventData;
            public EventMessage(CourseEventArgs d, AttachmentArgs userAttachment = null)
            {
                eventData = d;
                attachment = userAttachment;
            }
        }
        public class AttachmentArgs
        {
            public string fileName;
            public string attachmentType;
            public AttachmentArgs(string fileName, string attachmentType)
            {
                this.fileName = fileName;
                this.attachmentType = attachmentType;
            }

        }
        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 0x20;
            private const int CCHFORMNAME = 0x20;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

    }

}
