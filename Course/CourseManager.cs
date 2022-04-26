using WebSocketSharp;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.Net.Http;
using System.Drawing;
using CourseRecorder.Helpers;

namespace CourseRecorder.Course
{
    public class CourseManager
    {
        public CourseState State=CourseState.ServerNotConnected;
        public Guid CourseId;
        public string ServerAddress;
        private WebSocket ws;
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
                else {
                    ws.Send(JsonConvert.SerializeObject(new { action = "recorderRegister"}));
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
            if(ws.ReadyState == WebSocketState.Open)
            {
                ws.Close();
            }
        }
        public void WsMessageHandler(object sender, MessageEventArgs e)
        {
            var wsMsg = JsonConvert.DeserializeObject<Dictionary<string, object>>(e.Data);
            switch (wsMsg["action"])
            {
                case "registered":
                    CourseId = Guid.Parse((string)wsMsg["courseId"]);
                    State = CourseState.CourseRegistered;
                    break;
                case "rejoined":
                case "courseBegin":
                    Program.cep.CourseEvent += CourseEventHandler;
                    State = CourseState.CourseStarted;
                    break;
                case "rejoinFailed":
                    MessageBox.Show("Rejoin has failed, please restart the program.");
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
            if (State != CourseState.BeforeEnd) { // accidently closed
                State = CourseState.ServerNotConnected;
                Debug.WriteLine("Try to reconnect after 5 seconds");
                Thread.Sleep(5000);
                Connect();
            }
        }

        public void WsErrorHandler(object sender, ErrorEventArgs e)
        {
            Program.cep.CourseEvent -= CourseEventHandler;
            State = CourseState.ServerNotConnected;
        }

        public async void CourseEventHandler(object sender, CourseEventArgs e)
        {
            switch (e)
            {
                case KeyboardEventArgs KeyboardEvent:
                    string KeyboardFileId = await TakeScreenshotAndUpload(KeyboardEvent.EventId.ToString());
                    if (KeyboardFileId != null)
                    {
                        ws.Send(JsonConvert.SerializeObject(new EventMessage(KeyboardEvent, KeyboardFileId)));
                    }
                    else
                    {
                        ws.Send(JsonConvert.SerializeObject(new EventMessage(KeyboardEvent)));
                    }
                    break;
                case MouseEventArgs MouseEvent:
                    if (MouseEvent.Button != 0) {
                        string MouseFileId = await TakeScreenshotAndUpload(MouseEvent.EventId.ToString());
                        if (MouseFileId != null)
                        {
                            ws.Send(JsonConvert.SerializeObject(new EventMessage(MouseEvent, MouseFileId)));
                        }
                        else
                        {
                            ws.Send(JsonConvert.SerializeObject(new EventMessage(MouseEvent)));
                        }
                    }
                    break;
                case DocumentEventArgs DocumentEvent:
                    string DocumentFileId = await TakeScreenshotAndUpload(DocumentEvent.EventId.ToString());
                    if (DocumentFileId != null)
                    {
                        ws.Send(JsonConvert.SerializeObject(new EventMessage(DocumentEvent, DocumentFileId)));
                    }
                    else
                    {
                        ws.Send(JsonConvert.SerializeObject(new EventMessage(DocumentEvent)));
                    }
                    break;
                default:
                    break;
            }
        }

        private async Task<string> TakeScreenshotAndUpload(string eventId)
        {
            //throttle
            if (LastScreenshotTime.AddSeconds(1) > DateTime.Now)
            {
                return null;
            }
            LastScreenshotTime = DateTime.Now;
            //Take screenshot
            using (WebP webp = new WebP())
            {
                try
                {
                    Bitmap ps = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                    Graphics graphics = Graphics.FromImage(ps as Image);
                    graphics.CopyFromScreen(0, 0, 0, 0, ps.Size);
                    byte[] rawWebP = webp.EncodeLossy(ps, 75);
                    //Upload to server
                    using (HttpClient httpClient = new HttpClient())
                    {
                        MultipartFormDataContent form = new MultipartFormDataContent();
                        form.Add(new ByteArrayContent(rawWebP, 0, rawWebP.Length), "file", "screenshot.webp");
                        HttpResponseMessage response = await httpClient.PostAsync($"https://{this.ServerAddress}/uploadfile?courseId={this.CourseId}&eventId={eventId}&fileType=screenshot", form);
                        response.EnsureSuccessStatusCode();
                        string res = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine(res);
                        var httpMsg = JsonConvert.DeserializeObject<Dictionary<string, object>>(res);
                        if ((string)httpMsg["status"] == "success")
                        {
                            return (string)httpMsg["fileId"];
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return null;
                }
            }

            
        }
        
        public enum CourseState
        {
            ServerNotConnected,
            ServerConnected,
            CourseRegistered,
            CourseStarted,
            BeforeEnd,
            End
        }
        public class EventMessage
        {
            public string action = "eventUpdate";
            public string attachmentId = null;
            public CourseEventArgs eventData;
            public EventMessage(CourseEventArgs d, string fileId = null)
            {
                eventData = d;
                if (fileId != null)
                {
                    attachmentId = fileId;
                }
            }
        }
        
    }
        
}
