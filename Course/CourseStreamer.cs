using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CourseRecorder.Course
{
    internal class CourseStreamer
    {
        public string StreamURL;
        public string AudioInputDevice;
        private Process proc;
        public void Start()
        {
            Task.Run(() =>
            {
                do
                {
                    Debug.WriteLine("Initializing...");
                    proc = new Process();
                    string streamArgument;
                    if (AudioInputDevice != null && StreamURL != null)
                    {
                        streamArgument = $"-f dshow -i audio=\"{AudioInputDevice}\" -c:a aac -ab 32k -f flv {StreamURL}";
                    }
                    else
                    {
                        break;
                    }
                    proc.StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg.exe",
                        Arguments = streamArgument,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    Debug.WriteLine("Starting ffmpeg...");
                    proc.Start();
                    proc.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
                    proc.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                    State = StreamerState.Started;
                    proc.WaitForExit();
                    if (State == StreamerState.RequestRestart)
                    {
                        State = StreamerState.Stopped;
                        Debug.WriteLine("restarting ffmpeg ...");
                        continue;
                    }
                    else if (State == StreamerState.RequestStop)
                    {
                        Debug.WriteLine("ffmpeg quitted as requested");
                        break; 
                    }
                    else
                    {
                        State = StreamerState.Stopped;
                        Debug.WriteLine("ffmpeg accidentally quitted, restarting in 2s...");
                        Thread.Sleep(2000);
                        continue;
                    }

                } while (true);
                State = StreamerState.Exited;
            });
        }
        public void Stop()
        {
            if (State == StreamerState.Started)
            {
                Debug.WriteLine("Stopping ffmpeg...");
                State = StreamerState.RequestStop;
                proc.Kill();
            }
        }
        public void Restart()
        {
            if (State == StreamerState.Started)
            {
                Debug.WriteLine("Restarting ffmpeg...");
                State = StreamerState.RequestRestart;
                proc.Kill();
            }
            if (State == StreamerState.Exited || State == StreamerState.Stopped)
            {
                Start();
            }
        }
        void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            Debug.WriteLine(outLine.Data);
        }

        public event Action OnStateChanged;
        private StreamerState _state = StreamerState.Stopped;
        public StreamerState State
        {
            get
            {
                return _state;
            }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnStateChanged?.Invoke();
                }
            }
        }
        public enum StreamerState
        {
            Started,
            RequestStop,
            RequestRestart,
            Stopped,
            Exited,
        }
    }
}
