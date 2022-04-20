using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;


namespace CourseRecorder
{
    public partial class Form1 : Form
    {
        
        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }


        
        public CourseEventPublisher pub;
        private void button2_Click(object sender, EventArgs e)
        {
            pub = new CourseEventPublisher();
            Thread t = new Thread(new ThreadStart(ProcessCourseEvent));
            t.Start();
        }
        void ProcessCourseEvent()
        {
            do
            {
                while (pub.EventQueue.Count != 0)
                {
                    CourseEventArgs e = (CourseEventArgs)pub.EventQueue.Dequeue();
                    switch (e)
                    {
                        case KeyboardEventArgs KeyboardEvent:
                            listBox1.Items.Add(KeyboardEvent.Timestamp + " 按键：" + KeyboardEvent.KeyCode);
                            break;
                        case MouseEventArgs MouseEvent:
                            listBox1.Items.Add(MouseEvent.Timestamp  + " 鼠标按键：" + MouseEvent.Button + " 鼠标位置：" + MouseEvent.X + "," + MouseEvent.Y);
                            break;
                        case DocumentEventArgs DocumentEvent:
                            listBox1.Items.Add(DocumentEvent.Timestamp + " 文档：" + DocumentEvent.Name + " 当前页码：" + DocumentEvent.Index);
                            break;
                        default:
                            break;
                    }
                    listBox1.SelectedIndex = listBox1.Items.Count - 1;
                }
                Thread.Sleep(1000);
            } while (true);
        }

    }
}
