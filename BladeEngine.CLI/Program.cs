using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BladeEngine.Core;
using BladeEngine.CSharp;
using BladeEngine.Java;

namespace BladeEngine.CLI
{
    class Program
    {
        static ILogger logger => new ConsoleLogger();
        static void Help()
        {
            Console.WriteLine(@"
blade [runner] [engine] [-i input-template] [-o output] [-on] [-r runner-output] [-rn] [-c engine config] [-m model] [-debug]
    runner  :   execute template
    engine  :   language engine to parse the template. supported languages are:
                    c#, java, python, javascript
    -i      :   input balde template to compile
    -o      :   filename to save generated content into.
    -on     :   do not overwrite output if already exists
    -r      :   in case of using 'runner', a filename to save the result of executing generated code
    -rn     :   do not overwrite runner output if already exists
    -c      :   engine config in json format
    -m      :   model in json format or a filename containing model in json format
    -debug  :   execute runner in debug mode

example:
    blade c# -i my-template.blade
    blade c# -i my-template.blade -o my-template.cs
    blade c# -i my-template.blade -c ""{ 'NameSpace': 'MyNS' }""
    blade runner c# -i my-template.blade -m ""{ 'name': 'John Doe' }""
");
        }
        static string GetAssemblyPath(string arg)
        {
            string result;

            do
            {
                result = Path.Combine(Environment.CurrentDirectory, "\\BladeEngine." + arg + ".dll");

                if (File.Exists(result))
                {
                    break;
                }

                result = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "\\BladeEngine." + arg + ".dll");

                if (File.Exists(result))
                {
                    break;
                }

                result = Path.Combine(Environment.CurrentDirectory, "\\" + arg + ".dll");

                if (File.Exists(result))
                {
                    break;
                }

                result = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "\\" + arg + ".dll");

                if (File.Exists(result))
                {
                    break;
                }

                result = null;
            } while (false);

            return result;
        }
        static bool Validate(BladeEngineOptions options, out BladeRunner runner)
        {
            var result = false;

            runner = null;

            do
            {
                if (!string.IsNullOrEmpty(options.EngineLibraryPath))
                {
                    var assembly = logger.Try($"Loading Engine assembly '{options.EngineLibraryPath}' ...", options.Debug, () => Assembly.LoadFrom(options.EngineLibraryPath));

                    if (assembly != null)
                    {
                        var runnerType = logger.Try($"Finding runner ...", options.Debug, () => assembly.GetTypes().FirstOrDefault(t => t.DescendsFrom(typeof(BladeRunner))));

                        if (runnerType == null)
                        {
                            logger.Log($"Could not find a runner in '{options.Engine}' engine assembly that is derived from BladeRunner base class.");
                            break;
                        }
                        else
                        {
                            runner = logger.Try($"Instantiating runner ...", options.Debug, () => (BladeRunner)Activator.CreateInstance(runnerType, logger, options));

                            if (runner == null)
                            {
                                logger.Abort($"Instantiating '{options.Engine}' runner failed", !options.Debug);
                                break;
                            }
                        }
                    }
                    else
                    {
                        logger.Abort($"Loading '{options.Engine}' engine assembly failed", !options.Debug);
                    }
                }
                else
                {
                    logger.Log("No engine specified or specified engine is invalid.");
                    break;
                }

                if (string.IsNullOrEmpty(options.InputFile))
                {
                    logger.Log("No input template is specified to be compiled.");
                    break;
                }

                if (!Path.IsPathRooted(options.InputFile))
                {
                    options.InputFile = Path.Combine(Environment.CurrentDirectory, options.InputFile);
                }

                if (!File.Exists(options.InputFile))
                {
                    logger.Log($"input file '{options.InputFile}' does not exist.");
                    break;
                }

                if (!string.IsNullOrEmpty(options.OutputFile))
                {
                    if (!Path.IsPathRooted(options.OutputFile))
                    {
                        options.OutputFile = Path.Combine(Environment.CurrentDirectory, options.OutputFile);
                    }

                    if (File.Exists(options.OutputFile) && options.DontOverwriteExistingOutputFile)
                    {
                        logger.Log($"output file '{options.OutputFile}' already exists.");
                        break;
                    }
                }
                else
                {
                    var filename = Path.GetFileNameWithoutExtension(options.InputFile);

                    options.OutputFile = Path.Combine(Path.GetDirectoryName(options.InputFile), filename + runner.Config.FileExtension);
                    options.ManualOutput = true;
                }

                if (options.Runner)
                {
                    if (!string.IsNullOrEmpty(options.RunnerOutputFile))
                    {
                        if (!Path.IsPathRooted(options.RunnerOutputFile))
                        {
                            options.RunnerOutputFile = Path.Combine(Environment.CurrentDirectory, options.RunnerOutputFile);
                        }

                        if (File.Exists(options.RunnerOutputFile) && options.DontOverwriteExistingRunnerOutputFile)
                        {
                            logger.Log($"runner output file '{options.RunnerOutputFile}' already exist.");
                            break;
                        }
                    }
                    else
                    {
                        var filename = Path.GetFileNameWithoutExtension(options.InputFile);

                        options.RunnerOutputFile = Path.Combine(Environment.CurrentDirectory, filename + ".output");
                    }

                    if (!string.IsNullOrEmpty(options.GivenModel))
                    {
                        options.GivenModel = options.GivenModel.Trim();

                        if (!(options.GivenModel.StartsWith("{") && options.GivenModel.EndsWith("}")))  // if model is not json, assume it as a file
                        {
                            options.ModelPath = Path.IsPathRooted(options.GivenModel) ? options.GivenModel : Path.Combine(Environment.CurrentDirectory, options.GivenModel);

                            if (!File.Exists(options.ModelPath))
                            {
                                logger.Log($"model file '{options.ModelPath}' not found.");
                                break;
                            }

                            if (!logger.Try($"Reading model file {options.ModelPath}", options.Debug, () =>
                            {
                                options.GivenModel = File.ReadAllText(options.ModelPath);
                            }))
                            {
                                logger.Abort($"Reading model file '{options.ModelPath}' failed", !options.Debug);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    options.RunnerOutputFile = "";
                }

                result = true;
            } while (false);

            return result;
        }
        static BladeEngineOptions GetOptions(string[] args)
        {
            var result = new BladeEngineOptions();

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg == "runner")
                {
                    result.Runner = true;
                    continue;
                }

                if (arg == "-debug")
                {
                    result.Debug = true;
                    continue;
                }

                if (arg == "-on")
                {
                    result.DontOverwriteExistingOutputFile = true;
                    continue;
                }

                if (arg == "-rn")
                {
                    result.DontOverwriteExistingRunnerOutputFile = true;
                    continue;
                }

                if (arg == "-i")
                {
                    if (i < args.Length - 1)
                    {
                        result.InputFile = args[i + 1];
                        continue;
                    }
                }

                if (arg == "-o")
                {
                    if (i < args.Length - 1)
                    {
                        result.OutputFile = args[i + 1];
                        continue;
                    }
                }

                if (arg == "-r")
                {
                    if (i < args.Length - 1)
                    {
                        result.RunnerOutputFile = args[i + 1];
                        continue;
                    }
                }

                if (arg == "-c")
                {
                    if (i < args.Length - 1)
                    {
                        result.GivenConfig = args[i + 1];
                        continue;
                    }
                }

                if (arg == "-m")
                {
                    if (i < args.Length - 1)
                    {
                        result.GivenModel = args[i + 1];
                        continue;
                    }
                }

                result.EngineLibraryPath = GetAssemblyPath(arg);

                if (!string.IsNullOrEmpty(result.EngineLibraryPath))
                {
                    result.Engine = arg.ToLower();
                    continue;
                }
            }

            return result;
        }
        static void Start(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Help();
            }
            else
            {
                var options = GetOptions(args);

                BladeRunner runner;

                if (Validate(options, out runner))
                {
                    runner.Run();
                }
            }
        }
        static void Test(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                foreach (var item in args)
                {
                    Console.WriteLine(item);
                }
            }
        }
        static void Main(string[] args)
        {
            Start(args);
            //Test(args);
        }
    }
}
