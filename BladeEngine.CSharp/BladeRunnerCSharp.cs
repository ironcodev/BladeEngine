using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using BladeEngine.Core;
using Newtonsoft.Json;
using BladeEngine.Core.Base.Exceptions;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace BladeEngine.CSharp
{
    public class BladeRunnerCSharp : BladeRunner<BladeEngineCSharp, BladeEngineConfigCSharp>
    {
        public BladeRunnerCSharp(ILogger logger, BladeEngineOptions options) : base(logger, options)
        { }
        object GetModel(string model)
        {
            object result = default;

            if (!string.IsNullOrEmpty(model))
            {
                try
                {
                    result = JsonConvert.DeserializeObject(model);
                }
                catch (Exception e)
                {
                    Logger.LogLn("Error deserializing model");
                    Logger.Log(e);
                }
            }

            return result;
        }
        string Md5(string s)
        {
            var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            var bytes = System.Text.Encoding.UTF8.GetBytes(s);
            bytes = md5.ComputeHash(bytes);
            var buff = new System.Text.StringBuilder();

            foreach (byte ba in bytes)
            {
                buff.Append(ba.ToString("x2").ToLower());
            }

            return buff.ToString();
        }
        Assembly CompileOrLoadAssembly(string code)
        {
            Assembly result = null;
            var md5 = Md5(RenderedTemplate);
            var currentPath = AppDomain.CurrentDomain.BaseDirectory;

            if (!Directory.Exists(currentPath + "\\cache"))
            {
                try
                {
                    Directory.CreateDirectory(currentPath + "\\cache");

                    if (Options.Debug)
                    {
                        Logger.DebugLn("Cache directory created at '" + currentPath + "\\cache'");
                    }
                }
                catch (Exception e)
                {
                    if (Options.Debug)
                    {
                        Logger.DebugLn("Creating Cache directory at '" + currentPath + "\\cache' failed.");
                        Logger.Log(e);
                    }
                }
            }

            var existingCompiledAssembly = Path.Combine(currentPath + "\\cache", md5 + ".dll");
            var ok = true;

            if (File.Exists(existingCompiledAssembly))
            {
                try
                {
                    result = Assembly.LoadFrom(existingCompiledAssembly);
                    ok = false;

                    if (Options.Debug)
                    {
                        Logger.DebugLn("Existing cached assembly at '" + existingCompiledAssembly + "' successfully loaded.");
                    }
                }
                catch (Exception e)
                {
                    if (Options.Debug)
                    {
                        Logger.DebugLn("Loading existing cached assembly from '" + existingCompiledAssembly + "' failed.");
                        Logger.Log(e);
                    }
                }
            }
            
            if (ok)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(RenderedTemplate);
                var assemblyName = Path.GetRandomFileName();

                var refPaths = new List<string> {
                    typeof(System.Object).GetTypeInfo().Assembly.Location,
                    typeof(Console).GetTypeInfo().Assembly.Location,
                    typeof(DynamicAttribute).GetTypeInfo().Assembly.Location,
                    typeof(WebUtility).GetTypeInfo().Assembly.Location,
                    typeof(StringBuilder).GetTypeInfo().Assembly.Location,
                    typeof(MD5CryptoServiceProvider).GetTypeInfo().Assembly.Location,
                    typeof(MD5).GetTypeInfo().Assembly.Location,
                    typeof(HashAlgorithm).GetTypeInfo().Assembly.Location,
                    Assembly.Load(new AssemblyName("System.Security.Cryptography.Algorithms")).Location,
                    Assembly.Load(new AssemblyName("Microsoft.CSharp")).Location,
                    Assembly.Load(new AssemblyName("netstandard")).Location,
                    Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll")
                };

                if (Config.References != null && Config.References.Count > 0)
                {
                    foreach (var reference in Config.References.Where(r => !string.IsNullOrEmpty(r)))
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

                MetadataReference[] references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

                if (Options.Debug)
                {
                    Logger.DebugLn("Adding references ...");

                    foreach (var r in refPaths)
                    {
                        Logger.DebugLn(r);
                    }

                    Logger.DebugLn(Environment.NewLine + "Compiling ...");
                }

                CSharpCompilation compilation = CSharpCompilation.Create(
                    assemblyName,
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                using (var ms = new MemoryStream())
                {
                    EmitResult er = compilation.Emit(ms);

                    if (!er.Success)
                    {
                        Logger.DangerLn("Failed!" + Environment.NewLine);

                        IEnumerable<Diagnostic> failures = er.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error);

                        foreach (Diagnostic diagnostic in failures)
                        {
                            Logger.DangerLn($"\t{diagnostic.Id}: {diagnostic.GetMessage()}");
                        }
                    }
                    else
                    {
                        if (Options.Debug)
                        {
                            Logger.SuccessLn("Succeeded");
                            Logger.DebugLn($"Saving compiled assembly into cache '{existingCompiledAssembly}' ...");
                        }

                        ms.Seek(0, SeekOrigin.Begin);

                        result = AssemblyLoadContext.Default.LoadFromStream(ms);

                        try
                        {
                            ms.Seek(0, SeekOrigin.Begin);

                            using (var file = new FileStream(existingCompiledAssembly, FileMode.Create, System.IO.FileAccess.Write))
                            {
                                var bytes = new byte[ms.Length];

                                ms.Read(bytes, 0, (int)ms.Length);

                                file.Write(bytes, 0, bytes.Length);

                                ms.Close();
                            }

                            if (Options.Debug)
                            {
                                Logger.SuccessLn("Succeeded");
                            }
                        }
                        catch (Exception e)
                        {
                            if (Options.Debug)
                            {
                                Logger.DangerLn("Failed");
                                Logger.Log(e);
                            }
                        }
                    }
                }
            }

            return result;
        }
        protected override string Execute()
        {
            var result = "";

            if (string.IsNullOrEmpty(RenderedTemplate))
            {
                throw new BladeEngineException("BladeEngineCSharp did not produce any code to be executed!");
            }
            else
            {
                var model = GetModel(Options.GivenModel);
                var assembly = CompileOrLoadAssembly(RenderedTemplate);

                if (assembly != null)
                {
                    if (Options.Debug)
                    {
                        Logger.DebugLn("Instantiating and executing the code ...");
                    }

                    var templateMainClass = Config.Namespace + "." + Template.GetMainClassName();
                    var templateType = assembly.GetType(templateMainClass);
                    var templateInstance = assembly.CreateInstance(templateMainClass);
                    var method = templateType.GetMethod("Render");

                    if (method != null)
                    {
                        result = method.Invoke(templateInstance, new object[] { model })?.ToString();
                        
                        if (Options.Debug)
                        {
                            Logger.SuccessLn("Done");
                        }
                    }
                    else
                    {
                        throw new BladeEngineException("Generated class does not have a method named Render().");
                    }
                }
                else
                {
                    Logger.LogLn("Runner failed. Use debug switch for error details.");
                }
            }

            return result;
        }
    }
}
