using WebSocketSharp;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

namespace CourseRecorder.Course
{
    public class CourseManager
    {
        public CourseState State=CourseState.ServerNotConnected;
        public Guid CourseId;
        private WebSocket ws;
        public CourseManager(string ServerAddress)
        {
            ws = new WebSocket(ServerAddress);
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

        public void CourseEventHandler(object sender, CourseEventArgs e)
        {
            switch (e)
            {
                case KeyboardEventArgs KeyboardEvent:
                    ws.Send(JsonConvert.SerializeObject(new EventMessage(KeyboardEvent)));
                    break;
                case MouseEventArgs MouseEvent:
                    ws.Send(JsonConvert.SerializeObject(new EventMessage(MouseEvent)));
                    break;
                case DocumentEventArgs DocumentEvent:
                    ws.Send(JsonConvert.SerializeObject(new EventMessage(DocumentEvent)));
                    break;
                default:
                    break;
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
            public CourseEventArgs eventData;
            public EventMessage(CourseEventArgs d)
            {
                eventData = d;
            }
        }
        
    }
        
}
