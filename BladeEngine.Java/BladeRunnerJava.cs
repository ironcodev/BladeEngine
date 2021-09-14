using System;
using System.IO;
using System.Reflection;
using BladeEngine.Core;
using BladeEngine.Core.Utils;
using BladeEngine.Core.Utils.Logging;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Java
{
    public class BladeRunnerJava: BladeRunner<BladeEngineJava, BladeEngineConfigJava>
    {
        public BladeRunnerJava(ILogger logger, BladeEngineOptions options) : base(logger, options)
        { }
        bool ShellExecute(string message, string errorMessage, string filename, out string output, string args = null, string workingDirectory = null, bool strictSuccess = false)
        {
            var _outout = "";
            var result = Logger.Try(message + Environment.NewLine + $"{filename} {args}", Options.Debug, () =>
            {
                var sr = Shell.Execute(new ShellExecuteRequest { FileName = filename, Args = args, WorkingDirectory = workingDirectory });
                
                _outout = sr.Output;

                if (Options.Debug)
                {
                    Logger.Log($"Exit Code: {sr.ExitCode}");
                    Logger.Log("Output:");
                    Logger.Debug(sr.Output);

                    if (sr.Exception != null)
                    {
                        Logger.Danger(Environment.NewLine + sr.Exception.ToString(Environment.NewLine) + Environment.NewLine);
                    }
                }

                return (strictSuccess && sr.IsSucceeded()) || (!strictSuccess && sr.Succeeded && sr.ExitCode.HasValue);
            });

            if (!result)
            {
                Abort(errorMessage);
            }

            output = _outout;

            return result;
        }
        protected override bool Execute(out string result)
        {
            var ok = false;
            var currentPath = Assembly.GetExecutingAssembly().Location;

            result = "";

            do
            {
                // STEP 1. Check whether JDK is installed

                var shellOutput = "";

                if (Options.Debug)
                {
                    Logger.Log("Checking if JDK exists ...");
                }

                // STEP 1.1. Check whether java.exe is executed without any error

                if (!ShellExecute(message: "Executing ...",
                            errorMessage: "Cannot run java. Please make sure JDK is installed and its path is included in PATH environment variable.",
                            filename: "java.exe",
                            output: out shellOutput))
                {
                    break;
                }

                // STEP 1.2. Check whether javac.exe is executed without any error

                if (!ShellExecute(message: "Executing javac.exe ...",
                            errorMessage: "Cannot run javac. Please make sure JDK is installed and its path is included in PATH environment variable.",
                            "javac.exe",
                            output: out shellOutput))
                {
                    break;
                }

                // STEP 2. Create /temp dir in Blade folder if not existed

                if (!Directory.Exists(currentPath + "\\temp"))
                {
                    if (!Logger.Try($"Creating main temp directory at '" + currentPath + "'", Options.Debug, () =>
                    {
                        Directory.CreateDirectory(currentPath + "\\temp");

                        return true;
                    }))
                    {
                        Abort($"Creating main temp directory at {currentPath} failed. Executing template aborted");
                        break;
                    }
                }

                // STEP 3. Create a random directory

                var tmpDir = currentPath + "\\temp\\" + Guid.NewGuid().ToString().Replace("-", "");

                if (!Directory.Exists(tmpDir))
                {
                    if (!Logger.Try($"Creating temporarily directory for template execution at '" + tmpDir + "'", Options.Debug, () =>
                    {
                        Directory.CreateDirectory(tmpDir);

                        return true;
                    }))
                    {
                        Abort($"Creating temp directory at {tmpDir} failed. Executing template aborted");
                        break;
                    }
                }

                var classPath = $".;{currentPath}\\java;{Engine.StrongConfig.ClassPath}";

                // STEP 4. Saving rendered template at tmpDir

                var tmpFile = tmpDir + "\\" + Template.GetMainClassName() + ".java";

                if (!Logger.Try($"Saving rendered template at '{tmpFile}' ...", Options.Debug, () =>
                {
                    File.WriteAllText(tmpFile, RenderedTemplate);

                    return true;
                }))
                {
                    Abort($"Saving rendered template at {tmpDir} failed. Executing template aborted");
                    break;
                }

                // STEP 5. Create and save a runner program to run rendered template at tmpDir

                var program = $@"
import {Template.GetMainClassName()};

public class Program {{
    public static void main(String[] args) {{
        {Template.GetMainClassName()} t = new {Template.GetMainClassName()}();

        System.out.println(t.render());
    }}
}}
";
                var tmpProgram = $"{tmpDir}\\Program.java";

                if (!Logger.Try($"Saving runner Program at '{tmpProgram}' ...", Options.Debug, () => {
                    File.WriteAllText(tmpProgram, program);

                    return true;
                }))
                {
                    Abort($"Saving runner Program at {tmpDir} failed. Executing template aborted");
                    break;
                }

                // STEP 6. Compile rendered template

                if (!ShellExecute(message: $"Compiling template {Path.GetFileName(tmpFile)} ...",
                                errorMessage: "Compiling template failed",
                                filename: "javac.exe",
                                output: out shellOutput,
                                args: $"-cp {classPath} {Path.GetFileName(tmpFile)}",
                                workingDirectory: tmpDir,
                                strictSuccess: true))
                {
                    break;
                }

                // STEP 8. Compile runner

                if (!ShellExecute(message: $"Compiling template runner {Path.GetFileName(tmpProgram)} ...",
                                errorMessage: "Compiling template runner failed",
                                filename: "javac.exe",
                                output: out shellOutput,
                                args: $"-cp {classPath} {Path.GetFileName(tmpProgram)}",
                                workingDirectory: tmpDir,
                                strictSuccess: true))
                {
                    break;
                }

                // STEP 9. Execute runner

                if (!ShellExecute(message: $"Executing template runner ...",
                                errorMessage: "Executing template runner failed",
                                filename: "java.exe",
                                output: out shellOutput,
                                args: $"-cp {classPath} {Path.GetFileNameWithoutExtension(tmpProgram)}",
                                workingDirectory: tmpDir,
                                strictSuccess: true))
                {
                    break;
                }

                result = shellOutput;

                ok = true;
            } while (false);
            
            return ok;
        }
    }
}
