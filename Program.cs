using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CourseRecorder.Course;

namespace CourseRecorder
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        public static CourseEventPublisher cep = new CourseEventPublisher();
        public static CourseManager cm = new CourseManager("localtest.qwq.moe:3300");
        [STAThread]
        static void Main()
        {
            Thread cmThread = new System.Threading.Thread(Program.cm.Connect);
            cmThread.Name = "cmThread";
            cmThread.Start();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
           
    }
}
