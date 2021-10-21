using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using BladeEngine.Core;
using BladeEngine.Core.Exceptions;
using static BladeEngine.Core.Utils.LanguageConstructs;
using BladeEngine.Core.Utils.Logging;
using Newtonsoft.Json.Linq;

namespace BladeEngine.CSharp
{
    public class BladeRunnerCSharp : BladeRunner<BladeEngineCSharp, BladeEngineConfigCSharp>
    {
        public BladeRunnerCSharp(ILogger logger) : base(logger)
        { }
        bool GetModel(BladeRunnerOptions options, BladeRunnerRunResult runnerResult, Type modelType)
        {
            var result = false;

            if (options.Model != null)
            {
                if (IsSomeString(StrongEngine.StrongConfig.StrongModelType, true))
                {
                    var givenModelType = options.Model.GetType();

                    if (givenModelType == typeof(JObject) && modelType != typeof(JObject))
                    {
                        if (!Logger.Try($"Mapping deserialized JObject model to {givenModelType.Name} ...", () =>
                        {
                            options.Model = ((JObject)options.Model).ToObject(modelType);
                            return true;
                        }, out Exception ex))
                        {
                            runnerResult.TrySetStatus("MappingModelTypeFailed");
                            runnerResult.Exception = new BladeEngineException($"Converting deserialized model to '{StrongEngine.StrongConfig.StrongModelType}' failed.", ex);
                        }
                    }
                    else
                    {
                        if (!(givenModelType == modelType || givenModelType.DescendsFrom(modelType)))
                        {
                            runnerResult.SetStatus("InvalidModelType");
                            runnerResult.Exception = new BladeEngineException($"Expected a '{StrongEngine.StrongConfig.StrongModelType}', but a '{givenModelType.Name}' model is given.");
                        }
                    }
                }
                else
                {
                    result = true;
                }
            }
            else
            {
                Logger.Log("No model specified. 'null' will be used as model.", options.Debug);

                result = true;
            }

            return result;
        }
        Assembly CompileOrLoadAssembly(BladeRunnerOptions options, BladeRunnerRunResult runnerResult, out Type modelType)
        {
            Assembly result = null;
            var md5 = runnerResult.RenderedTemplate.ToMD5();

            if (!Directory.Exists(options.CacheDir))
            {
                Logger.Try($"Creating cache directory '{options.CacheDir}' ...", options.Debug, () => Directory.CreateDirectory(options.CacheDir));
            }

            var existingCompiledAssembly = Path.Combine(options.CacheDir, md5 + ".dll");
            var createAssembly = true;

            if (File.Exists(existingCompiledAssembly))
            {
                result = Logger.Try($"Loading existing assembly at '" + existingCompiledAssembly + "'", options.Debug, () => Assembly.LoadFrom(existingCompiledAssembly));

                createAssembly = result == null;
            }

            var refPaths = new List<string> {
                    typeof(object).GetTypeInfo().Assembly.Location,
                    typeof(DynamicAttribute).GetTypeInfo().Assembly.Location,           // required due to the use of 'dynamic' in Render() method that BladeTemplateCSharp generates
                    typeof(WebUtility).GetTypeInfo().Assembly.Location,                 // used in the class that BladeTemplateCSharp generates
                    typeof(MD5CryptoServiceProvider).GetTypeInfo().Assembly.Location,   //      "                   "                   "
                    typeof(MD5).GetTypeInfo().Assembly.Location,                        //      "                   "                   "
                    typeof(HashAlgorithm).GetTypeInfo().Assembly.Location,              //      "                   "                   "
                    Assembly.Load(new AssemblyName("System.Security.Cryptography.Algorithms")).Location,
                    Assembly.Load(new AssemblyName("Microsoft.CSharp")).Location,
                    Assembly.Load(new AssemblyName("netstandard")).Location,
                    Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll")
                };

            if (StrongEngine.StrongConfig.References != null && StrongEngine.StrongConfig.References.Count > 0)
            {
                foreach (var reference in StrongEngine.StrongConfig.References.Where(r => IsSomeString(r, true)))
                {
                    var toAdd = "";

                    if (reference.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || reference.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Path.IsPathRooted(reference))
                        {
                            toAdd = reference;
                        }
                        else
                        {
                            toAdd = Path.Combine(Environment.CurrentDirectory, reference);
                        }
                    }
                    else
                    {
                        toAdd = Assembly.Load(new AssemblyName(reference)).Location;
                    }

                    if (!refPaths.Contains(toAdd, StringComparer.OrdinalIgnoreCase))
                    {
                        refPaths.Add(toAdd);
                    }
                }
            }

            if (createAssembly)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(runnerResult.RenderedTemplate);
                var assemblyName = Path.GetRandomFileName();
                var references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

                if (options.Debug)
                {
                    Logger.Log("Adding references ...");

                    foreach (var r in refPaths)
                    {
                        Logger.Debug(r);
                    }

                    Logger.Log(Environment.NewLine + "Compiling ...");
                }

                var compilation = CSharpCompilation.Create(
                    assemblyName,
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                using (var ms = new MemoryStream())
                {
                    var er = compilation.Emit(ms);

                    if (!er.Success)
                    {
                        Logger.Danger("Failed!" + Environment.NewLine);

                        var failures = er.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                        foreach (Diagnostic diagnostic in failures)
                        {
                            Logger.Danger($"\t{diagnostic.Id}: {diagnostic.GetMessage()}");
                        }
                    }
                    else
                    {
                        if (options.Debug)
                        {
                            Logger.Success("Succeeded");
                        }

                        ms.Seek(0, SeekOrigin.Begin);

                        result = AssemblyLoadContext.Default.LoadFromStream(ms);

                        Logger.Try($"Saving compiled assembly into cache '{existingCompiledAssembly}' ...", options.Debug, () =>
                        {
                            ms.Seek(0, SeekOrigin.Begin);

                            using (var file = new FileStream(existingCompiledAssembly, FileMode.Create, System.IO.FileAccess.Write))
                            {
                                var bytes = new byte[ms.Length];

                                ms.Read(bytes, 0, (int)ms.Length);

                                file.Write(bytes, 0, bytes.Length);

                                ms.Close();
                            }
                        });
                    }
                }
            }

            modelType = null;

            if (StrongEngine.StrongConfig.UseStrongModel)
            {
                if (IsSomeString(StrongEngine.StrongConfig.StrongModelType, true))
                {
                    foreach (var reference in refPaths)
                    {
                        try
                        {
                            var asm = Assembly.LoadFrom(reference);

                            if (IsSomeString(StrongEngine.StrongConfig.StrongModelType, true) && modelType == null)
                            {
                                modelType = asm.GetType(StrongEngine.StrongConfig.StrongModelType);

                                if (modelType != null)
                                {
                                    break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if (options.Debug)
                            {
                                Logger.Log($"Loading assembly {reference} failed.{Environment.NewLine + "\t"}{e.ToString("\t" + Environment.NewLine)}");
                            }
                        }
                    }

                    if (modelType == null)
                    {
                        result = null;

                        runnerResult.SetStatus("ModelTypeNotFound");

                        throw new BladeEngineException($"Model type '{StrongEngine.StrongConfig.StrongModelType}' was not found in references");
                    }
                }
                else
                {
                    result = null;

                    runnerResult.SetStatus("ModelTypeNotSpecified");

                    throw new BladeEngineException($"'UseStrongModel' is requested, but no strong model type is specified");
                }
            }

            return result;
        }
        protected override bool Execute(BladeRunnerOptions options, BladeRunnerRunResult runnerResult, out string result)
        {
            result = "";

            if (string.IsNullOrEmpty(runnerResult.RenderedTemplate))
            {
                throw new BladeEngineException("No code is produced to be executed!");
            }
            else
            {
                var assembly = CompileOrLoadAssembly(options, runnerResult, out Type modelType);

                if (assembly != null)
                {
                    if (GetModel(options, runnerResult, modelType))
                    {
                        var templateMainClass = runnerResult.Template.GetFullMainClassName(); // StrongEngine.StrongConfig.Namespace + "." + runnerResult.Template.GetMainClassName();
                        var templateType = assembly.GetType(templateMainClass);

                        if (templateType != null)
                        {
                            var templateInstance = Logger.Try($"Instantiating from {templateMainClass} ...", options.Debug, () => assembly.CreateInstance(templateMainClass));

                            if (templateInstance != null)
                            {
                                var method = templateType.GetMethod("Render");

                                if (method != null)
                                {
                                    Logger.Log($"Invoking Render() on {templateMainClass} instance ...", options.Debug);

                                    var parameters = method.GetParameters();

                                    if (parameters.Length == 0)
                                    {
                                        result = method.Invoke(templateInstance, new object[] { })?.ToString();
                                    }
                                    else if (parameters.Length == 1)
                                    {
                                        result = method.Invoke(templateInstance, new object[] { (object)options.Model })?.ToString();
                                    }
                                    else
                                    {
                                        throw new BladeEngineException($"Cannot execute {templateMainClass}.Render() since it requires more than one argument.");
                                    }
                                }
                                else
                                {
                                    throw new BladeEngineException("Generated class does not have a method named Render().");
                                }
                            }
                        }
                        else
                        {
                            throw new BladeEngineException($"Cannot access {templateMainClass} in generated assembly.");
                        }
                    }
                }
            }

            return true;
        }
    }
}
