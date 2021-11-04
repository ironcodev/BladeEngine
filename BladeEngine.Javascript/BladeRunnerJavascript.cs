using System;
using System.IO;
using BladeEngine.Core;
using BladeEngine.Core.Utils;
using BladeEngine.Core.Utils.Logging;
using BladeEngine.Core.Exceptions;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Javascript
{
    public class BladeRunnerJavascript: BladeRunner<BladeEngineJavascript, BladeEngineConfigJavascript>
    {
        public BladeRunnerJavascript(ILogger logger) : base(logger)
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
                            throw new BladeEngineException($"shell executing '{filename} {args}' failed{Environment.NewLine}{sr.Errors}", sr.Exception);
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
        bool? CreatePackageDir(string baseDir, string package, BladeRunnerOptions options, BladeRunnerRunResult runnerResult)
        {
            bool? result = null;
            Exception ex = null;
            var dirs = package.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var _dir = baseDir;

            foreach (var dir in dirs)
            {
                _dir = _dir + "\\" + dir;

                if (!Directory.Exists(_dir))
                {
                    if (!Logger.Try($"\tCreating package dir '{_dir}' at '{baseDir}' ...", options.Debug, () =>
                    {
                        Directory.CreateDirectory(_dir);

                        return true;
                    }, out ex))
                    {
                        runnerResult.SetStatus("CreatePackageDirFailed");
                        Logger.Abort($"\tCreating package dir '{_dir}' at '{baseDir}' failed. Executing template aborted", !options.Debug);
                        result = false;
                        break;
                    }
                }
            }

            if (ex != null)
            {
                runnerResult.Exception = ex;
            }

            return result;
        }
        protected bool CompileTemplateSingle(string baseDir, BladeRunnerOptions options, BladeRunnerRunResult runnerResult, BladeTemplateBase template)
        {
            bool? result = null;
            Exception ex = null;

            if (template != null)
            {
                do
                {
                    result = CreatePackageDir(baseDir, template.GetModuleName(), options, runnerResult);

                    if (result.HasValue)
                    {
                        break;
                    }

                    var tmpDir = $@"{baseDir}\{template.GetModuleName().Replace(".", "\\")}";
                    var tmpFileJavascript = $@"{tmpDir}\{template.GetMainClassName()}.javascript";
                    var tmpFileClass = $@"{tmpDir}\{template.GetMainClassName()}.class";
                    var includeFileContent = $"package {template.GetModuleName()};" + Environment.NewLine + template.Render();
                    var tmpFileHash = $@"{tmpDir}\{includeFileContent.ToMD5()}.hash";

                    // Save include template and its hash

                    if (!File.Exists(tmpFileJavascript) || !File.Exists(tmpFileHash))
                    {
                        if (!Logger.Try($"\tSaving template '{tmpFileJavascript}' ...", options.Debug, () =>
                        {
                            File.WriteAllText(tmpFileJavascript, includeFileContent);

                            return true;
                        }, out ex))
                        {
                            runnerResult.SetStatus("SavingTemplateFailed");
                            Logger.Abort($"\tSaving template '{tmpFileJavascript}' failed. Executing template aborted", !options.Debug);
                            result = false;
                            break;
                        }

                        Logger.Try($"\tSaving template hash '{tmpFileHash}' ...", options.Debug, () =>
                        {
                            File.WriteAllText(tmpFileHash, "");

                            return true;
                        }, out ex);

                        // we should delete old old compiled template so that it is compiled again

                        if (File.Exists(tmpFileClass))
                        {
                            if (!Logger.Try($"\tDeleting old compiled template '{tmpFileClass}' ...", options.Debug, () =>
                            {
                                File.Delete(tmpFileClass);

                                return true;
                            }, out ex))
                            {
                                runnerResult.SetStatus("DeletingOldCompiledTemplateFailed");
                                Logger.Abort($"\tDeleting old compiled template '{tmpFileClass}' failed. Executing template aborted", !options.Debug);
                                result = false;
                                break;
                            }
                        }
                    }

                    if (result.HasValue)
                    {
                        break;
                    }

                    // Compile include template

                    if (!File.Exists(tmpFileClass))
                    {
                        if (!ShellExecute(options: options,
                                        message: $"\tCompiling template {tmpFileJavascript} ...",
                                        errorMessage: "\tCompiling template failed",
                                        filename: "javascriptc.exe",
                                        onExecute: sr => sr.IsSucceeded(@"^\s*$", true, true),
                                        args: $"-cp \".;{AppPath.ProgramDir}\\javascript;{StrongEngine.StrongConfig.ClassPath?.Join(";")}\" {Path.GetFileName(tmpFileJavascript)}",
                                        workingDirectory: Path.GetDirectoryName(tmpFileJavascript),
                                        throwErrors: true))
                        {
                            runnerResult.SetStatus("CompilingTemplateFailed");
                            result = false;
                            break;
                        }
                    }
                } while (false);
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
        protected bool CompileTemplate(string baseDir, BladeRunnerOptions options, BladeRunnerRunResult runnerResult, BladeTemplateBase template)
        {
            var result = CompileTemplateSingle(baseDir, options, runnerResult, template);

            if (result)
            {
                if (template?.InnerTemplates != null && template.InnerTemplates.Count > 0)
                {
                    foreach (var item in template.InnerTemplates)
                    {
                        result = CompileTemplateSingle(baseDir, options, runnerResult, item);

                        if (!result)
                        {
                            break;
                        }
                    }
                }
            }

            return result;
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

                if (StrongEngine.StrongConfig.RunnerConfig.CheckNodeJsExistence)
                {
                    // STEP 1.1. Check whether javascript.exe is executed without any error

                    if (!ShellExecute(options: options,
                                      message: "STEP 1.1. Executing ...",
                                      errorMessage: "Cannot run javascript. Please make sure JDK is installed and its path is included in PATH environment variable.",
                                      filename: "javascript.exe",
                                      onExecute: sr => sr.Succeeded))
                    {
                        runnerResult.SetStatus("JVMNotFound");
                        break;
                    }

                    // STEP 1.2. Check whether javascriptc.exe is executed without any error

                    if (!ShellExecute(options: options,
                                      message: "STEP 1.2. Executing javascriptc.exe ...",
                                      errorMessage: "Cannot run javascriptc. Please make sure JDK is installed and its path is included in PATH environment variable.",
                                      filename: "javascriptc.exe",
                                      onExecute: sr => sr.Succeeded))
                    {
                        runnerResult.SetStatus("JavascriptCompilerNotFound");
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
                    if (!Logger.Try($"STEP 2. Creating cache directory '{options.CacheDir}' ...", options.Debug, () =>
                    {
                        Directory.CreateDirectory(options.CacheDir);

                        return true;
                    }, out ex))
                    {
                        runnerResult.SetStatus("CreateCacheDirFailed");
                        Logger.Abort($"Creating cache directory '{options.CacheDir}' failed. Executing template aborted", !options.Debug);
                        break;
                    }
                }
                
                // STEP 3. Create a directory based on rendered template

                var tmpDir = options.CacheDir + "\\" + runnerResult.RenderedTemplate.ToMD5();

                if (!Directory.Exists(tmpDir))
                {
                    if (!Logger.Try($"STEP 3. Creating directory for template execution '{tmpDir}'", options.Debug, () =>
                    {
                        Directory.CreateDirectory(tmpDir);

                        return true;
                    }, out ex))
                    {
                        runnerResult.SetStatus("CreateTemplateDirFailed");
                        Logger.Abort($"Creating template directory '{tmpDir}' failed. Executing template aborted", !options.Debug);
                        break;
                    }
                }
                
                // STEP 4. Compile rendered template

                if (!CompileTemplate(tmpDir, options, runnerResult, runnerResult.Template))
                {
                    break;
                }

                // STEP 5. Create and save a runner program to run rendered template at tmpDir

                var tmpProgram = $"{tmpDir}\\Program.javascript";

                if (!File.Exists(tmpProgram))
                {
                    var fullMainClass = runnerResult.Template.GetFullMainClassName();
                    var mainClass = runnerResult.Template.GetMainClassName();
                    var program = $@"
import {fullMainClass};

public class Program {{
    public static void main(String[] args) {{
        {mainClass} t = new {mainClass}();

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

                // STEP 6. Compile runner
                if (!File.Exists(Path.Combine(Path.GetDirectoryName(tmpProgram), Path.GetFileName(tmpProgram) + ".class")))
                {
                    if (!ShellExecute(options: options,
                                message: $"STEP 8. Compiling template runner {Path.GetFileName(tmpProgram)} ...",
                                errorMessage: "Compiling template runner failed",
                                filename: "javascriptc.exe",
                                onExecute: sr => sr.IsSucceeded(@"^\s*$", true, true),
                                args: $"-cp \".;{AppPath.ProgramDir}\\javascript;{StrongEngine.StrongConfig.ClassPath?.Join(";")}\" {Path.GetFileName(tmpProgram)}",
                                workingDirectory: tmpDir,
                                throwErrors: true))
                    {
                        runnerResult.SetStatus("CompilingTemplateRunnerFailed");
                        break;
                    }
                }

                // STEP 7. Execute runner
                var shellOutput = "";

                if (!ShellExecute(options: options,
                                message: $"STEP 9. Executing template runner ...",
                                errorMessage: "Executing template runner failed",
                                filename: "javascript.exe",
                                onExecute: sr =>
                                {
                                    shellOutput = sr.Output;

                                    return sr.Succeeded;
                                },
                                args: $"-cp \".;{AppPath.ProgramDir}\\javascript;{StrongEngine.StrongConfig.ClassPath?.Join(";")}\" {Path.GetFileNameWithoutExtension(tmpProgram)}",
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
