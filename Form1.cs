using System;
using System.Windows.Forms;
using WebSocketSharp;
using CourseRecorder.Course;
using System.Diagnostics;
using Newtonsoft.Json;
using static CourseRecorder.Course.CourseManager;

namespace CourseRecorder
{
    public partial class Form1 : Form
    {
        
        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }


        
        private void button2_Click(object sender, EventArgs e)
        {
        }
        
        

        private void button1_Click(object sender, EventArgs e)
        {
            new CourseManager("wss://localtest.qwq.moe:3300/");
            
        }

        private void handleWsMessage(object sender, MessageEventArgs e)
        {
            listBox1.Items.Add(e.Data);
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // exit program
            Application.Exit();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var cep = new CourseEventPublisher();
            cep.CourseEvent += handleCourseEvent;
        }
        void handleCourseEvent(object sender, CourseEventArgs e)
        {
            Debug.WriteLine(e);
            switch (e)
            {
                case KeyboardEventArgs KeyboardEvent:
                    Debug.WriteLine(JsonConvert.SerializeObject(new EventMessage
                    {
                        eventData = KeyboardEvent
                    }));
                    break;
                case MouseEventArgs MouseEvent:
                    Debug.WriteLine(JsonConvert.SerializeObject(new EventMessage
                    {
                        eventData = MouseEvent
                    }));
                    break;
                case DocumentEventArgs DocumentEvent:
                    Debug.WriteLine(JsonConvert.SerializeObject(new EventMessage
                    {
                        eventData = DocumentEvent
                    }));
                    break;
                default:
                    break;
            }
        }
    }
}
