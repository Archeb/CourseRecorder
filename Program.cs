using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CourseRecorder.Course;
using System.Configuration;

namespace CourseRecorder
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        public static CourseEventPublisher cep;
        public static CourseManager cm;
        [STAThread]
        static void Main()
        {
            string ServerAddress = ConfigurationManager.AppSettings["ServerAddress"];
            if (ServerAddress == null)
            {
                MessageBox.Show("请在配置文件中配置服务器地址", Application.ProductName);
                return;
            }
            cm = new CourseManager(ServerAddress);
            cep = new CourseEventPublisher();
            Thread cmThread = new System.Threading.Thread(Program.cm.Connect);
            cmThread.Name = "cmThread";
            cmThread.Start();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
