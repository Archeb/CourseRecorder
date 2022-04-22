using System;
using System.Windows.Forms;
using WebSocketSharp;
using CourseRecorder.Course;
using CourseRecorder.Helpers;
using System.Diagnostics;
using Newtonsoft.Json;
using static CourseRecorder.Course.CourseManager;
using System.Threading;
using System.Drawing;
using System.Drawing.Printing;

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
            Bitmap ps = new Bitmap(Screen.PrimaryScreen.Bounds.Width,Screen.PrimaryScreen.Bounds.Height);
            Graphics graphics = Graphics.FromImage(ps as Image);
            graphics.CopyFromScreen(0, 0, 0, 0, ps.Size);
            using (WebP webp = new WebP())
                webp.Save(ps, "test.webp", 100);
            // start the program folder
            Process.Start(Application.StartupPath);

        }
        
        

        private void button1_Click(object sender, EventArgs e)
        {
            
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
            Program.cm.Disconnect();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Program.cep.Dispose();
            Program.cm.Disconnect();
        }
    }
}
