using System;
using System.Runtime.InteropServices;
using PowerPoint = Microsoft.Office.Interop.PowerPoint;
using System.Threading.Tasks;
using System.Diagnostics;

using CourseRecorder.Helpers;
using System.Windows;

namespace CourseRecorder
{
    public class CourseEventPublisher
    {
        public event EventHandler<CourseEventArgs> CourseEvent;
        private HookHelper SystemHook;
        private PowerPoint.Application PowerPointApp;
        public CourseEventPublisher()
        {
            // Register mouse and keyboard hooks
            SystemHook = new HookHelper();
            SystemHook.SetUpMouseHook(MouseHookCallback);
            SystemHook.SetUpKeyboardHook(KeyboardHookCallback);

            // Register PowerPoint Application Event Hooks
            RegisterPowerPointEventHandlers();


        }
        private void RegisterPowerPointEventHandlers()
        {
            try
            {
                PowerPointApp = new PowerPoint.Application();
                PowerPointApp.SlideShowNextSlide += PowerPointSlideShowNextSlide;
                PowerPointApp.PresentationOpen += PowerPointPresentationOpen;
                PowerPointApp.PresentationClose += PowerPointPresentationClose;
                Debug.WriteLine("PowerPoint Hooked");
            }
            catch (COMException)
            {
                MessageBox.Show("PowerPoint集成功能注册失败，将影响部分功能使用，请检查PowerPoint是否安装。");
            }
        }
        public void Dispose()
        {
            SystemHook.ClearMouseHook();
            SystemHook.ClearKeyboardHook();
        } 
        private void MouseHookCallback(int x, int y, MouseMessage message)
        {
            if (message != MouseMessage.WM_MOUSEMOVE)
            {
                OnRaiseCourseEvent(new MouseEventArgs(x, y, message));
            }
        }
        private void KeyboardHookCallback(int keyCode)
        {
            OnRaiseCourseEvent(new KeyboardEventArgs(keyCode));
        }
        
        private void PowerPointSlideShowNextSlide(PowerPoint.SlideShowWindow Wn)
        {
            Debug.WriteLine("PowerPoint ShowNextSlide Event");
            OnRaiseCourseEvent(new DocumentEventArgs(Wn.Presentation.Name, Wn.Presentation.FullName, "PowerPointNextSlide", Wn.View.Slide.SlideIndex));
        }
        private void PowerPointPresentationOpen(PowerPoint.Presentation Pres)
        {
            Debug.WriteLine("PowerPoint PresentationOpen Event");
            OnRaiseCourseEvent(new DocumentEventArgs(Pres.Name, Pres.FullName, "PowerPointPresentationOpen", -1));
        }
        private void PowerPointPresentationClose(PowerPoint.Presentation Pres)
        {
            Debug.WriteLine("PowerPoint PresentationClose Event");
            OnRaiseCourseEvent(new DocumentEventArgs(Pres.Name, Pres.FullName, "PowerPointPresentationClose", -1));
        }
        protected virtual void OnRaiseCourseEvent(CourseEventArgs e)
        {
            EventHandler<CourseEventArgs> handler = CourseEvent;
            Task.Run(() => {
                handler?.Invoke(this, e);
            });
        }
    }

    
        
    public class CourseEventArgs
    {
        public long Timestamp;
        public Guid EventId;
        public string EventType;
        
    }
    class KeyboardEventArgs : CourseEventArgs
    {
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

    class TriggerOnDemandEventArgs : CourseEventArgs
    {
        public string Action;
        public TriggerOnDemandEventArgs(string action)
        {
            Action = action;
            Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            EventId = Guid.NewGuid();
            EventType = "TriggerOnDemandEvent";
        }
    }
}
