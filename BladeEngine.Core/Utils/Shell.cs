using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BladeEngine.Core.Utils
{
    public class ShellExecuteResponse
    {
        public bool Succeeded { get; set; }
        public string Output { get; set; }
        public string Status { get; set; }
        public int ExitCode { get; set; }
        public Exception Exception { get; set; }
    }
    public class ShellExecuteRequest
    {
        public string FileName { get; set; }
        public string Args { get; set; }
        public string WorkingDirectory { get; set; }
        public ProcessWindowStyle? WindowStyle { get; set; }
    }
    public class Shell
    {
        // source: https://stackoverflow.com/questions/206323/how-to-execute-command-line-in-c-get-std-out-results
        public static ShellExecuteResponse Execute(ShellExecuteRequest request)
        {
            var response = new ShellExecuteResponse();

            do
            {
                if (request == null)
                {
                    response.Status = "NoRequest";
                    break;
                }

                if (string.IsNullOrEmpty(request.FileName))
                {
                    response.Status = "NoFileName";
                    break;
                }

                var process = new Process();

                process.StartInfo.FileName = request.FileName;

                if (!string.IsNullOrEmpty(request.Args))
                {
                    process.StartInfo.Arguments = request.Args;
                }

                if (!string.IsNullOrEmpty(request.WorkingDirectory))
                {
                    process.StartInfo.WorkingDirectory = request.WorkingDirectory;
                }

                process.StartInfo.CreateNoWindow = true;

                if (request.WindowStyle.HasValue)
                {
                    process.StartInfo.WindowStyle = request.WindowStyle.Value;
                }
                else
                {
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;

                var stdOutput = new StringBuilder();

                process.OutputDataReceived += (sender, args) => stdOutput.AppendLine(args.Data);

                string stdError = null;

                try
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    stdError = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                }
                catch (Exception e)
                {
                    response.Exception = e;
                    response.Status = "Failed";
                }

                response.ExitCode = process.ExitCode;

                if (process.ExitCode == 0)
                {
                    response.Succeeded = true;
                    response.Status = "Succeeded";
                    response.Output = stdOutput.ToString();
                }
                else
                {
                    var message = new StringBuilder();

                    if (!string.IsNullOrEmpty(stdError))
                    {
                        message.AppendLine(stdError);
                    }

                    if (stdOutput.Length != 0)
                    {
                        message.AppendLine(stdOutput.ToString());
                    }

                    response.Output = message.ToString();
                }

            } while (false);

            return response;
        }
    }
}
