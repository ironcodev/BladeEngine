using BladeEngine.Core;
using System;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.CSharp
{
    public class BladeTemplateCSharp : BladeTemplateBase<BladeEngineCSharp>
    {
        public static string NamespaceRegex => @"^@?[a-z_A-Z]\w*(\.@?[a-z_A-Z]\w*)*$";
        public BladeTemplateCSharp(BladeEngineCSharp engine, BladeTemplateSettings settings = null) : base(engine, settings)
        {
            ModuleNameRegex = NamespaceRegex;

            if (!Settings.IsInclude)
            {
                Dependencies = @"using Blade;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Net;
using System.Xml;
using System.Drawing;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;";
                ExternalCode = $@"
namespace Blade
{{
    public abstract class BladeTemplateCSharpBase
    {{
        protected System.Text.StringBuilder _buffer;
        public BladeTemplateCSharpBase()
        {{
            _buffer = new System.Text.StringBuilder();
        }}
        #region Encode/Decode Helpers
        protected virtual string HtmlEncode(string s)
        {{
            return System.Net.WebUtility.HtmlEncode(s);
        }}
        protected virtual string HtmlDecode(string s)
        {{
            return System.Net.WebUtility.HtmlDecode(s);
        }}
        protected virtual string UrlEncode(string s)
        {{
            if (!string.IsNullOrEmpty(s))
            {{
                var i = s.IndexOf('?');
                var query = s.Substring(i + 1);
                var parts = query.Split(new char[] {{ '&' }});
                var encodedParts = """";

                foreach (var part in parts)
                {{
                    var arr = part.Split('=');

                    encodedParts += (string.IsNullOrEmpty(encodedParts) ? """" : ""&"") + System.Net.WebUtility.UrlEncode(arr[0]) + (arr.Length > 1 ? ""="" + System.Net.WebUtility.UrlEncode(arr[1]) : """");
                }}

                return s.Substring(0, i + 1) + encodedParts;
            }}

            return """";
        }}
        protected virtual string FullUrlEncode(string s)
        {{
            return System.Net.WebUtility.UrlEncode(s);
        }}
        protected virtual string UrlDecode(string s)
        {{
            return System.Net.WebUtility.UrlDecode(s);
        }}
        protected virtual string FullUrlDecode(string s)
        {{
            return System.Net.WebUtility.UrlDecode(s);
        }}
        protected virtual string Md5(string s)
        {{
            var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            var bytes = System.Text.Encoding.UTF8.GetBytes(s);
            bytes = md5.ComputeHash(bytes);
            var buff = new System.Text.StringBuilder();

            foreach (byte ba in bytes)
            {{
                buff.Append(ba.ToString(""x2"").ToLower());
            }}

            return buff.ToString();
        }}
        protected virtual string Base64Encode(string s)
        {{
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s));
        }}
        protected virtual string Base64Decode(string s)
        {{
            return System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(s));
        }}
        {new string[]
            {
                "bool", "char[]", "char", "byte", "sbyte", "int", "uint", "short", "ushort", "long", "ulong",
                "float", "double", "decimal", "object", "string", "System.Text.StringBuilder"
            }.Join(t => $@"
        protected void Write({t} x)
        {{
            _buffer.Append(x);
        }}")}
        protected void WriteLine(object x)
        {{
            _buffer.AppendLine(x?.ToString());
        }}
        #endregion
    }}
}}
";
            }
        }
        public override string RenderContent()
        {
            return $@"
{ExternalCode}
{(IsSomeString(Body + Functions, rejectAllWhitespaceStrings: true) ? $@"
namespace {GetModuleName()}
{{
    public class {GetMainClassName()}: BladeTemplateCSharpBase
    {{
        public string {(StrongEngine.StrongConfig.UseStrongModel && IsSomeString(StrongEngine.StrongConfig.StrongModelType, rejectAllWhitespaceStrings: true) ? $"Render({StrongEngine.StrongConfig.StrongModelType} model = default)" : "Render(dynamic model = (object)null)")}
        {{
            {Body}
            var result = _buffer.ToString();

            _buffer.Clear();

            return result;
        }}
        {Functions}
    }}
}}
": "")}
";
        }
        protected override string GetEngineName()
        {
            return "CSharp";
        }
    }
}
