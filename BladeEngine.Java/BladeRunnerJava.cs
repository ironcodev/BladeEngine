using System;
using System.IO;
using System.Reflection;
using BladeEngine.Core;
using BladeEngine.Core.Base.Exceptions;
using BladeEngine.Core.Utils;
using BladeEngine.Core.Utils.Logging;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Java
{
    public class BladeRunnerJava: BladeRunner<BladeEngineJava, BladeEngineConfigJava>
    {
        public BladeRunnerJava(ILogger logger, BladeEngineOptions options) : base(logger, options)
        { }
        bool ShellExecute(string message, string errorMessage, string filename, Func<ShellExecuteResponse, bool> onExecute, string args = null, string workingDirectory = null, bool throwErrors = false)
        {
            var result = Logger.Try(message + Environment.NewLine + $"command: {filename} {args}", Options.Debug, () =>
            {
                var sr = Shell.Execute(new ShellExecuteRequest { FileName = filename, Args = args, WorkingDirectory = workingDirectory });
                
                if (Options.Debug)
                {
                    Logger.Log($"Exit Code: {sr.ExitCode}");
                    Logger.Log("Output:");
                    Logger.Debug(sr.Output);

                    if (IsSomeString(sr.Errors))
                    {
                        if (throwErrors)
                        {
                            throw new BladeEngineException($"executing '{filename} {args}' failed", sr.Exception);
                        }
                        else
                        {
                            Logger.Log("Errors:");
                            Logger.Debug(sr.Errors);
                        }
                    }

                    if (sr.Exception != null)
                    {
                        if (throwErrors)
                        {
                            throw new BladeEngineException($"executing '{filename} {args}' faulted", sr.Exception);
                        }
                        else
                        {
                            Logger.Danger(Environment.NewLine + sr.Exception.ToString(Environment.NewLine) + Environment.NewLine);
                        }
                    }
                }

                return onExecute(sr);
            });

            if (!result)
            {
                Abort(errorMessage);
            }

            return result;
        }
        protected override bool Execute(out string result)
        {
            var ok = false;
            var currentPath = AppPath.ExecDir;

            result = "";

            do
            {
                if (Engine.StrongConfig.RunnerConfig.CheckJdkExistence)
                {
                    // STEP 1. Check whether JDK is installed

                    if (Options.Debug)
                    {
                        Logger.Log(Environment.NewLine + "STEP 1. Checking if JDK exists ...");
                    }

                    // STEP 1.1. Check whether java.exe is executed without any error

                    if (!ShellExecute(message: "STEP 1.1. Executing ...",
                                      errorMessage: "Cannot run java. Please make sure JDK is installed and its path is included in PATH environment variable.",
                                      filename: "java.exe",
                                      onExecute: sr => sr.Succeeded))
                    {
                        break;
                    }

                    // STEP 1.2. Check whether javac.exe is executed without any error

                    if (!ShellExecute(message: "STEP 1.2. Executing javac.exe ...",
                                      errorMessage: "Cannot run javac. Please make sure JDK is installed and its path is included in PATH environment variable.",
                                      filename: "javac.exe",
                                      onExecute: sr => sr.Succeeded))
                    {
                        break;
                    }
                }

                // STEP 2. Create /temp dir in Blade folder if not existed

                if (!Directory.Exists(currentPath + "\\temp"))
                {
                    if (!Logger.Try($"STEP 2. Creating main temp directory at '" + currentPath + "'", Options.Debug, () =>
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
                    if (!Logger.Try($"STEP 3. Creating temporarily directory for template execution at '" + tmpDir + "'", Options.Debug, () =>
                    {
                        Directory.CreateDirectory(tmpDir);

                        return true;
                    }))
                    {
                        Abort($"Creating temp directory at {tmpDir} failed. Executing template aborted");
                        break;
                    }
                }

                // STEP 4. Create package dir

                var tmpPackage = tmpDir + "\\" + Engine.StrongConfig.Package;

                if (!Directory.Exists(tmpPackage))
                {
                    if (!Logger.Try($"STEP 4. Creating package dir {tmpPackage} ...", Options.Debug, () =>
                    {
                        Directory.CreateDirectory(tmpPackage);

                        return true;
                    }))
                    {
                        Abort($"Creating package dir {tmpPackage} failed. Executing template aborted");
                        break;
                    }
                }

                var classPath = $".;{currentPath}\\java;{Engine.StrongConfig.ClassPath}";

                // STEP 5. Saving rendered template at tmpDir

                var tmpFile = tmpPackage + "\\" + Template.GetMainClassName() + ".java";

                if (!Logger.Try($"STEP 5. Saving rendered template '{tmpFile}' ...", Options.Debug, () =>
                {
                    File.WriteAllText(tmpFile, RenderedTemplate);

                    return true;
                }))
                {
                    Abort($"Saving rendered template '{tmpFile}' failed. Executing template aborted");
                    break;
                }

                // STEP 6. Create and save a runner program to run rendered template at tmpDir

                var program = $@"
import {Engine.StrongConfig.Package}.{Template.GetMainClassName()};

public class Program {{
    public static void main(String[] args) {{
        {Template.GetMainClassName()} t = new {Template.GetMainClassName()}();

        System.out.println(t.render());
    }}
}}
";
                var tmpProgram = $"{tmpDir}\\Program.java";

                if (!Logger.Try($"STEP 6. Saving runner Program at '{tmpProgram}' ...", Options.Debug, () => {
                    File.WriteAllText(tmpProgram, program);

                    return true;
                }))
                {
                    Abort($"Saving runner Program at {tmpDir} failed. Executing template aborted");
                    break;
                }

                // STEP 7. Compile rendered template

                if (!ShellExecute(message: $"STEP 7. Compiling template {Path.GetFileName(tmpFile)} ...",
                                errorMessage: "Compiling template failed",
                                filename: "javac.exe",
                                onExecute: sr => sr.IsSucceeded(@"^\s*$", true, true),
                                args: $"-cp \"{classPath}\" {Path.GetFileName(tmpFile)}",
                                workingDirectory: tmpPackage,
                                throwErrors: true))
                {
                    break;
                }

                // STEP 8. Compile runner

                if (!ShellExecute(message: $"STEP 8. Compiling template runner {Path.GetFileName(tmpProgram)} ...",
                                errorMessage: "Compiling template runner failed",
                                filename: "javac.exe",
                                onExecute: sr => sr.IsSucceeded(@"^\s*$", true, true),
                                args: $"-cp \"{classPath}\" {Path.GetFileName(tmpProgram)}",
                                workingDirectory: tmpDir,
                                throwErrors: true))
                {
                    break;
                }

                // STEP 9. Execute runner
                var shellOutput = "";

                if (!ShellExecute(message: $"STEP 9. Executing template runner ...",
                                errorMessage: "Executing template runner failed",
                                filename: "java.exe",
                                onExecute: sr =>
                                {
                                    shellOutput = sr.Output;

                                    return sr.Succeeded;
                                },
                                args: $"-cp \"{classPath}\" {Path.GetFileNameWithoutExtension(tmpProgram)}",
                                workingDirectory: tmpDir,
                                throwErrors: true))
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
