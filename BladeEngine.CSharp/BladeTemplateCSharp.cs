using BladeEngine.Core;

namespace BladeEngine.CSharp
{
    public class BladeTemplateCSharp : BladeTemplateBase<BladeEngineCSharp>
    {
        public BladeTemplateCSharp(BladeEngineCSharp engine): base(engine)
        { }
        public override string RenderContent()
        {
            return $@"
{ExternalCode}
namespace {StrongEngine.StrongConfig.Namespace}
{{
    public class {GetMainClassName()}
    {{
        System.Text.StringBuilder _buffer;
        public {GetMainClassName()}()
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
        #endregion
        public string {(StrongEngine.StrongConfig.UseGenericModel ? "Render<T>(T model = default)" : "Render(dynamic model = (object)null)")}
        {{
            {Body}
            var result = _buffer.ToString();

            _buffer.Clear();

            return result;
        }}
        {Functions}
    }}
}}
";
        }
    }
}
