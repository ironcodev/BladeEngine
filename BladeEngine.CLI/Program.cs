using System;
using System.IO;
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
blade [runner] [engine] [-i input-template] [-o output] [-os] [-r runner-output] [-rs] [-c engine config] [-m model] [-debug]
    runner  :   execute template
    engine  :   language engine to parse the template. supported languages are:
                    c#, java, python, javascript
    -i      :   input balde template to compile
    -o      :   filename to save generated content into.
    -ow     :   do not overwrite output if already exists
    -r      :   in case of using 'runner', a filename to save the result of executing generated code
    -rs     :   do not overwrite runner output if already exists
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
        static string GetEngine(string arg)
        {
            var result = "";

            if (string.Compare(arg, "c#", true) == 0 ||
                string.Compare(arg, "java", true) == 0 ||
                string.Compare(arg, "python", true) == 0 ||
                string.Compare(arg, "javascript", true) == 0)
            {
                result = arg.ToLower();
            }

            return result;
        }
        
        static bool Validate(BladeEngineOptions options)
        {
            var result = false;

            do
            {
                if (string.IsNullOrEmpty(options.Engine))
                {
                    logger.LogLn("No engine specified or specified engine is invalid. Supported engines are: c#, java, python, javascript");
                    break;
                }

                if (string.IsNullOrEmpty(options.InputFile))
                {
                    logger.LogLn("No input template is specified.");
                    break;
                }

                if (!Path.IsPathRooted(options.InputFile))
                {
                    options.InputFile = Path.Combine(Environment.CurrentDirectory, options.InputFile);
                }

                if (!File.Exists(options.InputFile))
                {
                    logger.LogLn($"input file {options.InputFile} does not exist.");
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
                        logger.LogLn($"output file {options.OutputFile} already exist.");
                        break;
                    }
                }
                else
                {
                    var filename = Path.GetFileNameWithoutExtension(options.InputFile);
                    var extension = "";

                    switch (options.Engine)
                    {
                        case "c#":
                            extension = ".cs";
                            break;
                        case "java":
                            extension = ".java";
                            break;
                        case "python":
                            extension = ".py";
                            break;
                        case "javascript":
                            extension = ".js";
                            break;
                        default:
                            extension = ".txt";
                            break;
                    }

                    options.OutputFile = Path.Combine(Path.GetDirectoryName(options.InputFile), filename + extension);
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
                            logger.LogLn($"runner output file {options.RunnerOutputFile} already exist.");
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
                        if (options.GivenModel.IndexOf('{') < 0)
                        {
                            options.ModelPath = Path.IsPathRooted(options.GivenModel) ? options.GivenModel : Path.Combine(Environment.CurrentDirectory, options.GivenModel);
                            
                            if (!File.Exists(options.ModelPath))
                            {
                                logger.LogLn($"model file {options.ModelPath} not found.");
                                break;
                            }

                            try
                            {
                                options.GivenModel = File.ReadAllText(options.ModelPath);
                            }
                            catch (Exception e)
                            {
                                logger.LogLn($"Error reading model file {options.ModelPath}");
                                logger.Log(e);

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
                var engine = GetEngine(arg);

                if (!string.IsNullOrEmpty(engine))
                {
                    result.Engine = engine;
                    continue;
                }

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

                if (arg == "-os")
                {
                    result.DontOverwriteExistingOutputFile = true;
                    continue;
                }

                if (arg == "-rs")
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

                if (Validate(options))
                {
                    BladeRunner runner = null;

                    switch (options.Engine)
                    {
                        case "c#":
                            runner = new BladeRunnerCSharp(logger, options);
                            break;
                        case "java":
                            runner = new BladeRunnerJava(logger, options);
                            break;
                    }

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
