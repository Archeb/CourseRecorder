using WebSocketSharp;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Diagnostics;

namespace CourseRecorder.Course
{
    internal class CourseManager
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
                ws.Send(@"{""action"": ""recorderRegister""}");
            };
            ws.OnMessage += MessageHandler;
            ws.Connect();
        }
        
        private void MessageHandler(object sender, MessageEventArgs e)
        {
            var wsMsg = JsonConvert.DeserializeObject<Dictionary<string, object>>(e.Data);
            switch (wsMsg["action"])
            {
                case "registered":
                    CourseId = Guid.Parse((string)wsMsg["courseId"]);
                    State = CourseState.CourseRegistered;
                    break;
                case "courseBegin":
                    var cep = new CourseEventPublisher();
                    cep.CourseEvent += handleCourseEvent;
                    State = CourseState.CourseStarted;
                    break;
                default:
                    Debug.WriteLine("Unhandled message: " + e.Data);
                    break;
            }
        }

        public void handleCourseEvent(object sender, CourseEventArgs e)
        {
            Debug.WriteLine(e);
            switch (e)
            {
                case KeyboardEventArgs KeyboardEvent:
                    ws.Send(JsonConvert.SerializeObject(new EventMessage
                    {
                        action = "keyboard",
                        eventData = KeyboardEvent
                    }));
                    break;
                case MouseEventArgs MouseEvent:
                    ws.Send(JsonConvert.SerializeObject(new EventMessage
                    {
                        action = "mouse",
                        eventData = MouseEvent
                    }));
                    break;
                case DocumentEventArgs DocumentEvent:
                    ws.Send(JsonConvert.SerializeObject(new EventMessage
                    {
                        action = "document",
                        eventData = DocumentEvent
                    }));
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
            public CourseEventArgs eventData { get; set; }
        }
        
    }
        
}
