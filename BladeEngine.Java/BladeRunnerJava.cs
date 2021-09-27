using System;
using System.IO;
using BladeEngine.Core;
using BladeEngine.Core.Utils;
using BladeEngine.Core.Utils.Logging;
using BladeEngine.Core.Exceptions;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Java
{
    public class BladeRunnerJava: BladeRunner<BladeEngineJava, BladeEngineConfigJava>
    {
        public BladeRunnerJava(ILogger logger) : base(logger)
        { }
        bool ShellExecute(BladeRunnerOptions options, string message, string errorMessage, string filename, Func<ShellExecuteResponse, bool> onExecute, string args = null, string workingDirectory = null, bool throwErrors = false)
        {
            var result = Logger.Try(message + Environment.NewLine + $"command: {filename} {args}", options.Debug, () =>
            {
                var sr = Shell.Execute(new ShellExecuteRequest { FileName = filename, Args = args, WorkingDirectory = workingDirectory });
                
                if (options.Debug)
                {
                    Logger.Log($"Exit Code: {sr.ExitCode}");
                    Logger.Log("Output:");
                    Logger.Debug(sr.Output);

                    if (IsSomeString(sr.Errors))
                    {
                        if (throwErrors)
                        {
                            throw new BladeEngineException($"shell executing '{filename} {args}' failed", sr.Exception);
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
                Logger.Abort(errorMessage, !options.Debug);
            }

            return result;
        }
        protected bool CompileInnerTemplates(string baseDir, BladeRunnerOptions options, BladeRunnerRunResult runnerResult, BladeTemplateBase template)
        {
            bool? result = null;
            Exception ex = null;

            if (template.InnerTemplates != null)
            {
                foreach (var item in template.InnerTemplates)
                {
                    var path = item.Key.Replace("\\", "/");
                    var dirs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var dir in dirs)
                    {
                        if (dir != ".")
                        {
                            var _dir = baseDir + "\\" + dir;

                            if (!Directory.Exists(_dir))
                            {
                                if (!Logger.Try($"\tCreating include dir '" + _dir + "'", options.Debug, () =>
                                {
                                    Directory.CreateDirectory(_dir);

                                    return true;
                                }, out ex))
                                {
                                    runnerResult.SetStatus("CreateIncludeDirFailed");
                                    Logger.Abort($"\tCreating include dir '{_dir}' failed. Executing template aborted", !options.Debug);
                                    result = false;
                                    break;
                                }
                            }
                        }
                    }

                    if (result.HasValue)
                    {
                        break;
                    }

                    var tmpFile = baseDir + "\\" + path + item.Value.GetMainClassName() + ".java";
                    var includeFileContent = "package " + Environment.NewLine + item.Value.Render();
                    var tmpFileHash = baseDir + "\\" + path + includeFileContent.ToMD5() + ".hash";

                    // Save include template and its hash

                    if (!File.Exists(tmpFile) || !File.Exists(tmpFileHash))
                    {
                        if (!Logger.Try($"\tSaving include template '{tmpFile}' ...", options.Debug, () =>
                        {
                            File.WriteAllText(tmpFile, includeFileContent);

                            return true;
                        }, out ex))
                        {
                            runnerResult.SetStatus("SavingIncludeTemplateFailed");
                            Logger.Abort($"\tSaving include template '{tmpFile}' failed. Executing template aborted", !options.Debug);
                            result = false;
                            break;
                        }

                        Logger.Try($"\tSaving include template hash '{tmpFileHash}' ...", options.Debug, () =>
                        {
                            File.WriteAllText(tmpFileHash, "");

                            return true;
                        }, out ex);
                    }

                    if (result.HasValue)
                    {
                        break;
                    }

                    // Compile include template

                    if (!File.Exists(Path.Combine(Path.GetDirectoryName(tmpFile), Path.GetFileName(tmpFile) + ".class")))
                    {
                        if (!ShellExecute(options: options,
                                        message: $"\tCompiling include template {tmpFile} ...",
                                        errorMessage: "\tCompiling include template failed",
                                        filename: "javac.exe",
                                        onExecute: sr => sr.IsSucceeded(@"^\s*$", true, true),
                                        args: $"-cp \".;{Engine.StrongConfig.ClassPath}\" {Path.GetFileName(tmpFile)}",
                                        workingDirectory: Path.GetDirectoryName(tmpFile),
                                        throwErrors: true))
                        {
                            runnerResult.SetStatus("CompilingIncludeTemplateFailed");
                            break;
                        }
                    }

                    if (!CompileInnerTemplates(baseDir, options, runnerResult, item.Value))
                    {
                        result = false;
                        break;
                    }
                }
            }

            if (!result.HasValue)
            {
                result = true;
            }

            if (ex != null)
            {
                runnerResult.Exception = ex;
            }

            return result.Value;
        }
        protected override bool Execute(BladeRunnerOptions options, BladeRunnerRunResult runnerResult, out string result)
        {
            var ok = false;

            result = "";

            Exception ex = null;

            do
            {
                // STEP 1. Check whether JDK is installed

                if (options.Debug)
                {
                    Logger.Log(Environment.NewLine + "STEP 1. Checking if JDK exists ...");
                }

                if (Engine.StrongConfig.RunnerConfig.CheckJdkExistence)
                {
                    // STEP 1.1. Check whether java.exe is executed without any error

                    if (!ShellExecute(options: options,
                                      message: "STEP 1.1. Executing ...",
                                      errorMessage: "Cannot run java. Please make sure JDK is installed and its path is included in PATH environment variable.",
                                      filename: "java.exe",
                                      onExecute: sr => sr.Succeeded))
                    {
                        runnerResult.SetStatus("JVMNotFound");
                        break;
                    }

                    // STEP 1.2. Check whether javac.exe is executed without any error

                    if (!ShellExecute(options: options,
                                      message: "STEP 1.2. Executing javac.exe ...",
                                      errorMessage: "Cannot run javac. Please make sure JDK is installed and its path is included in PATH environment variable.",
                                      filename: "javac.exe",
                                      onExecute: sr => sr.Succeeded))
                    {
                        runnerResult.SetStatus("JavaCompilerNotFound");
                        break;
                    }
                }
                else
                {
                    if (options.Debug)
                    {
                        Logger.Debug("Skipped");
                    }
                }

                // STEP 2. Create /cache dir in Blade folder if not existed

                if (!Directory.Exists(options.CacheDir))
                {
                    if (!Logger.Try($"STEP 2. Creating cache directory at '" + options.CacheDir + "'", options.Debug, () =>
                    {
                        Directory.CreateDirectory(options.CacheDir);

                        return true;
                    }, out ex))
                    {
                        runnerResult.SetStatus("CreateCacheDirFailed");
                        Logger.Abort($"Creating cache directory at '{options.CacheDir}' failed. Executing template aborted", !options.Debug);
                        break;
                    }
                }
                
                // STEP 3. Create a directory based on rendered template

                var tmpDir = options.CacheDir + "\\" + runnerResult.RenderedTemplate.ToMD5();

                if (!Directory.Exists(tmpDir))
                {
                    if (!Logger.Try($"STEP 3. Creating directory for template execution at '" + tmpDir + "'", options.Debug, () =>
                    {
                        Directory.CreateDirectory(tmpDir);

                        return true;
                    }, out ex))
                    {
                        runnerResult.SetStatus("CreateTemplateDirFailed");
                        Logger.Abort($"Creating template directory at {tmpDir} failed. Executing template aborted", !options.Debug);
                        break;
                    }
                }

                // STEP 4. Create package dir

                var tmpPackage = tmpDir + "\\" + Engine.StrongConfig.Package;

                if (!Directory.Exists(tmpPackage))
                {
                    if (!Logger.Try($"STEP 4. Creating package dir {tmpPackage} ...", options.Debug, () =>
                    {
                        Directory.CreateDirectory(tmpPackage);

                        return true;
                    }, out ex))
                    {
                        runnerResult.SetStatus("CreatePackageDirFailed");
                        Logger.Abort($"Creating package dir {tmpPackage} failed. Executing template aborted", !options.Debug);
                        break;
                    }
                }

                var classPath = $".;{Engine.StrongConfig.ClassPath}";

                // STEP 5. Saving rendered template at tmpDir

                var tmpFile = tmpPackage + "\\" + runnerResult.Template.GetMainClassName() + ".java";

                if (!File.Exists(tmpFile))
                {
                    if (!Logger.Try($"STEP 5. Saving rendered template '{tmpFile}' ...", options.Debug, () =>
                    {
                        File.WriteAllText(tmpFile, runnerResult.RenderedTemplate);

                        return true;
                    }, out ex))
                    {
                        runnerResult.SetStatus("SavingRenderedTemplateFailed");
                        Logger.Abort($"Saving rendered template '{tmpFile}' failed. Executing template aborted", !options.Debug);
                        break;
                    }
                }

                // STEP 6. Compile rendered template

                if (!File.Exists(Path.Combine(Path.GetDirectoryName(tmpFile), Path.GetFileName(tmpFile) + ".class")))
                {
                    if (!ShellExecute(options: options,
                                    message: $"STEP 7. Compiling template {Path.GetFileName(tmpFile)} ...",
                                    errorMessage: "Compiling template failed",
                                    filename: "javac.exe",
                                    onExecute: sr => sr.IsSucceeded(@"^\s*$", true, true),
                                    args: $"-cp \"{classPath}\" {Path.GetFileName(tmpFile)}",
                                    workingDirectory: tmpPackage,
                                    throwErrors: true))
                    {
                        runnerResult.SetStatus("CompilingRenderedTemplateFailed");
                        break;
                    }
                }

                CompileInnerTemplates(Path.GetDirectoryName(tmpFile), options, runnerResult, runnerResult.Template);

                // STEP 7. Create and save a runner program to run rendered template at tmpDir

                var tmpProgram = $"{tmpDir}\\Program.java";

                if (!File.Exists(tmpProgram))
                {
                    var program = $@"
import {Engine.StrongConfig.Package}.{runnerResult.Template.GetMainClassName()};

public class Program {{
    public static void main(String[] args) {{
        {runnerResult.Template.GetMainClassName()} t = new {runnerResult.Template.GetMainClassName()}();

        System.out.println(t.render());
    }}
}}
";
                    if (!Logger.Try($"STEP 6. Saving runner Program at '{tmpProgram}' ...", options.Debug, () =>
                    {
                        File.WriteAllText(tmpProgram, program);

                        return true;
                    }, out ex))
                    {
                        runnerResult.SetStatus("CreateRunnerProgramFailed");
                        Logger.Abort($"Saving runner Program at {tmpDir} failed. Executing template aborted", !options.Debug);
                        break;
                    }
                }

                // STEP 8. Compile runner
                if (!File.Exists(Path.Combine(Path.GetDirectoryName(tmpProgram), Path.GetFileName(tmpProgram) + ".class")))
                {
                    if (!ShellExecute(options: options,
                                message: $"STEP 8. Compiling template runner {Path.GetFileName(tmpProgram)} ...",
                                errorMessage: "Compiling template runner failed",
                                filename: "javac.exe",
                                onExecute: sr => sr.IsSucceeded(@"^\s*$", true, true),
                                args: $"-cp \"{classPath}\" {Path.GetFileName(tmpProgram)}",
                                workingDirectory: tmpDir,
                                throwErrors: true))
                    {
                        runnerResult.SetStatus("CompilingTemplateRunnerFailed");
                        break;
                    }
                }

                // STEP 9. Execute runner
                var shellOutput = "";

                if (!ShellExecute(options: options,
                                message: $"STEP 9. Executing template runner ...",
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
                    runnerResult.SetStatus("RunningTemplateRunnerFailed");
                    break;
                }

                result = shellOutput;

                ok = true;
            } while (false);
            
            if (ex != null)
            {
                runnerResult.Exception = ex;
            }

            return ok;
        }
    }
}
