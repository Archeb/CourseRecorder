using System;
using System.Windows.Forms;
using WebSocketSharp;
using CourseRecorder.Course;
using CourseRecorder.Helpers;
using System.Net.Http;
using System.Diagnostics;
using Newtonsoft.Json;
using static CourseRecorder.Course.CourseManager;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Printing;
using QRCoder;

using System.Runtime.InteropServices;
using PowerPoint = Microsoft.Office.Interop.PowerPoint;


namespace CourseRecorder
{
    public partial class Form1 : Form
    {
        
        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            Program.cm.OnStateChanged += handleCourseStateChange;
        }

        
        
        

        private void button3_Click(object sender, EventArgs e)
        {
            // exit program
            Application.Exit();
        }


        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Program.cep.Dispose();
            Program.cm.Disconnect();
        }

        private void handleCourseStateChange()
        {
            switch (Program.cm.State)
            {
                case CourseState.ServerNotConnected:
                    label1.Text = "服务器连接状态：未连接";
                    label3.Text = "";
                    break;
                case CourseState.TryReconnect:
                    label1.Text = "服务器连接状态：重连中";
                    label3.Text = "";
                    break;
                case CourseState.ServerConnected:
                    label1.Text = "服务器连接状态：已连接";
                    break;
                case CourseState.CourseRegistered:
                    label1.Text = "服务器连接状态：已连接";
                    label3.Text = Program.cm.ShortCode;
                    //generate qr code
                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(Program.cm.JoinURL, QRCodeGenerator.ECCLevel.Q);
                    QRCode qrCode = new QRCode(qrCodeData);
                    Bitmap qrCodeImage = qrCode.GetGraphic(6,Color.Black,Color.White,false);
                    pictureBox1.Image = qrCodeImage;
                    break;
                case CourseState.CourseStarted:
                    label1.Text = "服务器连接状态：已连接";
                    break;
                case CourseState.BeforeEnd:
                    label1.Text = "服务器连接状态：未连接";
                    label3.Text = "";
                    break;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
