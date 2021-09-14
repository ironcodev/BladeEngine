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
using Microsoft.CodeAnalysis.Emit;
using Newtonsoft.Json;
using BladeEngine.Core;
using BladeEngine.Core.Base.Exceptions;
using static BladeEngine.Core.Utils.LanguageConstructs;
using BladeEngine.Core.Utils;
using BladeEngine.Core.Utils.Logging;

namespace BladeEngine.CSharp
{
    public class BladeRunnerCSharp : BladeRunner<BladeEngineCSharp, BladeEngineConfigCSharp>
    {
        public BladeRunnerCSharp(ILogger logger, BladeEngineOptions options) : base(logger, options)
        { }
        object GetModel(string model)
        {
            object result = default;

            if (IsSomeString(model, true))
            {
                try
                {
                    result = JsonConvert.DeserializeObject(model);
                }
                catch (Exception e)
                {
                    Logger.Log("Error deserializing model");
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
            var currentPath = AppPath.ExecDir;

            if (!Directory.Exists(currentPath + "\\cache"))
            {
                Logger.Try($"Creating cache directory at '" + currentPath + "\\cache'", Options.Debug, () => Directory.CreateDirectory(currentPath + "\\cache"));
            }

            var existingCompiledAssembly = Path.Combine(currentPath + ".\\cache", md5 + ".dll");
            var createAssembly = true;

            if (File.Exists(existingCompiledAssembly))
            {
                result = Logger.Try($"Loading existing assembly at '" + existingCompiledAssembly + "'", Options.Debug, () => Assembly.LoadFrom(existingCompiledAssembly));

                createAssembly = result == null;
            }
            
            if (createAssembly)
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

                if (StrongConfig.References != null && StrongConfig.References.Count > 0)
                {
                    foreach (var reference in StrongConfig.References.Where(r => IsSomeString(r, true)))
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
                    Logger.Debug("Adding references ...");

                    foreach (var r in refPaths)
                    {
                        Logger.Debug(r);
                    }

                    Logger.Debug(Environment.NewLine + "Compiling ...");
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
                        Logger.Danger("Failed!" + Environment.NewLine);

                        IEnumerable<Diagnostic> failures = er.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error);

                        foreach (Diagnostic diagnostic in failures)
                        {
                            Logger.Danger($"\t{diagnostic.Id}: {diagnostic.GetMessage()}");
                        }
                    }
                    else
                    {
                        if (Options.Debug)
                        {
                            Logger.Success("Succeeded");
                        }

                        ms.Seek(0, SeekOrigin.Begin);

                        result = AssemblyLoadContext.Default.LoadFromStream(ms);

                        Logger.Try($"Saving compiled assembly into cache '{existingCompiledAssembly}' ...", Options.Debug, () =>
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

            return result;
        }
        protected override bool Execute(out string result)
        {
            result = "";

            if (string.IsNullOrEmpty(RenderedTemplate))
            {
                throw new BladeEngineException("No code is produced to be executed!");
            }
            else
            {
                var model = GetModel(Options.GivenModel);
                var assembly = CompileOrLoadAssembly(RenderedTemplate);

                if (assembly != null)
                {
                    var templateMainClass = StrongConfig.Namespace + "." + Template.GetMainClassName();
                    var templateType = assembly.GetType(templateMainClass);

                    if (templateType != null)
                    {
                        var templateInstance = Logger.Try($"Instantiating from {templateMainClass} ...", Options.Debug, () => assembly.CreateInstance(templateMainClass));

                        if (templateInstance != null)
                        {
                            var method = templateType.GetMethod("Render");

                            if (method != null)
                            {
                                var parameters = method.GetParameters();

                                if (parameters.Length == 0)
                                {
                                    result = method.Invoke(templateInstance, new object[] { })?.ToString();
                                }
                                else if (parameters.Length == 1)
                                {
                                    result = method.Invoke(templateInstance, new object[] { model })?.ToString();
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

            return true;
        }
    }
}
