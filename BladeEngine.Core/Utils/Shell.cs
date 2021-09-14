using System;
using System.Diagnostics;
using System.Text;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Core.Utils
{
    public class ShellExecuteResponse
    {
        public bool Succeeded { get; set; }
        public string Output { get; set; }
        public string Errors { get; set; }
        public string Status { get; set; }
        public int? ExitCode { get; set; }
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

                if (IsSomeString(request.Args, true))
                {
                    process.StartInfo.Arguments = request.Args;
                }

                if (IsSomeString(request.WorkingDirectory, true))
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

                try
                {
                    response.ExitCode = process.ExitCode;
                    response.Succeeded = true;
                    response.Status = "Succeeded";
                }
                catch { }

                if (response.ExitCode.HasValue)
                {
                    if (IsSomeString(stdError, true))
                    {
                        response.Errors = stdError;
                    }

                    if (IsSomeString(stdOutput.ToString(), true))
                    {
                        response.Output = stdOutput.ToString();
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(response.Status))
                    {
                        response.Status = "Errored";
                    }
                }
            } while (false);

            return response;
        }
    }
}
