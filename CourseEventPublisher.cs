using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Diagnostics;
using PowerPoint = Microsoft.Office.Interop.PowerPoint;
using System.Collections;
using CourseRecorder.Helpers;

namespace CourseRecorder
{
    public class CourseEventPublisher
    {
        public Queue EventQueue = new Queue();
        public CourseEventPublisher()
        {
            // Register mouse and keyboard hooks
            HookHelper SystemHook = new HookHelper();
            SystemHook.SetUpMouseHook(MouseHookCallback);
            SystemHook.SetUpKeyboardHook(KeyboardHookCallback);
            
            // Register PowerPoint Application Event Hooks
            PowerPoint.Application PowerPointApp;
            PowerPointApp = new Microsoft.Office.Interop.PowerPoint.Application();
            try
            {
                PowerPointApp.SlideShowNextSlide += PowerPointSlideShowNextSlide;
            }
            catch (COMException)
            {
                Debug.WriteLine("PPT钩子错误");
            }
            
        }
        private void MouseHookCallback(int x, int y, MouseMessage message)
        {
            if (message != MouseMessage.WM_MOUSEMOVE)
            {
                EventQueue.Enqueue(new MouseEventArgs(x, y, message));
            }
        }
        private void KeyboardHookCallback(int keyCode)
        {
            EventQueue.Enqueue(new KeyboardEventArgs(keyCode));
        }

        private void PowerPointSlideShowNextSlide(PowerPoint.SlideShowWindow Wn)
        {
            EventQueue.Enqueue(new DocumentEventArgs(Wn.Presentation.Name, Wn.Presentation.FullName, "PowerPointNextSlide", Wn.View.Slide.SlideIndex));
        }        

    }

    
        
    interface CourseEventArgs
    {
        long Timestamp { get; set; }
        Guid EventId { get; set; }
        string EventType { get; set; }
    }
    class KeyboardEventArgs : CourseEventArgs
    {
        public long Timestamp { get; set; }
        public Guid EventId { get; set; }
        public string EventType { get; set; }

        public int KeyCode;
        public KeyboardEventArgs(int keyCode)
        {
            KeyCode = keyCode;
            Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            EventId = Guid.NewGuid();
            EventType = "KeyboardEvent";
        }
    }
    class MouseEventArgs : CourseEventArgs
    {
        public long Timestamp { get; set; }
        public Guid EventId { get; set; }
        public string EventType { get; set; }

        public int X;
        public int Y;
        public int Button;
        public MouseEventArgs(int x, int y, MouseMessage message)
        {
            this.X = x;
            this.Y = y;
            Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            EventId = Guid.NewGuid();
            EventType = "MouseEvent";
            switch (message)
            {
                case MouseMessage.WM_LBUTTONUP:
                case MouseMessage.WM_LBUTTONDOWN:
                case MouseMessage.WM_LBUTTONDBLCLK:
                    Button = 1;
                    break;
                case MouseMessage.WM_RBUTTONUP:
                case MouseMessage.WM_RBUTTONDOWN:
                case MouseMessage.WM_RBUTTONDBLCLK:
                    Button = 2;
                    break;
                case MouseMessage.WM_MBUTTONDOWN:
                    Button = 3;
                    break;
                default:
                    Button = 0;
                    break;
            }
        }
    }



    class DocumentEventArgs : CourseEventArgs
    {
        public long Timestamp { get; set; }
        public Guid EventId { get; set; }
        public string EventType { get; set; }

        public string Name;
        public string FullPath;
        public string Action;
        public int Index;
        public DocumentEventArgs(string name, string fullPath, string action, int index)
        {
            Name = name;
            FullPath = fullPath;
            Action = action;
            Index = index;
            Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            EventId = Guid.NewGuid();
            EventType = "DocumentEvent";
        }
    }
}
