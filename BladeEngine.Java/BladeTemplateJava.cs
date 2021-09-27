﻿using BladeEngine.Core;
using BladeEngine.Core.Exceptions;
using System.Collections.Generic;

namespace BladeEngine.Java
{
    public class BladeTemplateJava : BladeTemplateBase<BladeEngineJava>
    {
        public BladeTemplateJava(BladeEngineJava engine, string path = ".") : base(engine, path)
        {
            var package = StrongEngine.StrongConfig.Package + ((string.IsNullOrEmpty(path) || path == ".") ? "" : path.Replace("/", ".").Replace("\\", "."));

            Dependencies = $@"package {package};
import org.apache.commons.text.StringEscapeUtils;
import java.net.URLEncoder;
import java.net.URLDecoder;
import java.util.Base64;
import javax.xml.bind.DatatypeConverter;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.io.UnsupportedEncodingException;
import java.security.NoSuchAlgorithmException;";

            ClassPath = new List<string>();
        }
        public List<string> ClassPath { get; set; }
        public override string RenderContent()
        {
            return $@"
{ExternalCode}
    public class {GetMainClassName()} {{
        StringBuilder _buffer;
        boolean isNullOrEmpty(String str) {{
            return str == null || str.isEmpty();
        }}
        public {GetMainClassName()}() {{
            _buffer = new StringBuilder();
        }}
        // Encode/Decode Helpers
        protected String htmlEncode(String s) {{
            return StringEscapeUtils.escapeHtml4(s);
        }}
        protected String htmlDecode(String s) {{
            return StringEscapeUtils.unescapeHtml4(s);
        }}
        protected String urlEncode(String s) {{
            try {{
                if (!isNullOrEmpty(s)) {{
                    int i = s.indexOf('?');
                    String query = s.substring(i + 1);
                    String[] parts = query.split(""&"");
                    String encodedParts = """";

                    for (String part : parts) {{
                        String[] arr = part.split(""="");

                        encodedParts += (isNullOrEmpty(encodedParts) ? """" : ""&"") + URLEncoder.encode(arr[0], ""UTF-8"") + (arr.length > 1 ? ""="" + URLEncoder.encode(arr[1], ""UTF-8"") : """");
                    }}

                    return s.substring(0, i + 1) + encodedParts;
                }}
            }} catch (UnsupportedEncodingException e) {{ }}

            return """";
        }}
        protected String fullUrlEncode(String s) {{
            try {{
                return URLEncoder.encode(s, ""UTF-8"");
			}} catch (UnsupportedEncodingException e) {{
				return """";
			}}
        }}
        protected String urlDecode(String s) {{
            try {{
                return URLDecoder.decode(s, ""UTF-8"");
			}} catch (UnsupportedEncodingException e) {{
				return """";
			}}
        }}
        protected String fullUrlDecode(String s) {{
            try {{
                return URLDecoder.decode(s, ""UTF-8"");
			}} catch (UnsupportedEncodingException e) {{
				return """";
			}}
        }}
        protected String md5(String s) {{
            try {{
                MessageDigest md = MessageDigest.getInstance(""MD5"");
                md.update(s.getBytes(StandardCharsets.UTF_8));
                byte[] digest = md.digest();
                String result = DatatypeConverter.printHexBinary(digest);

                return result;
            }} catch (NoSuchAlgorithmException e) {{
				return """";
			}}
        }}
        protected String base64Encode(String s) {{
            return Base64.getEncoder().encodeToString(s.getBytes(StandardCharsets.UTF_8));
        }}
        protected String base64Decode(String s) {{
            return new String(Base64.getDecoder().decode(s));
        }}
        public String render() {{
            {Body}
            String result = _buffer.toString();

            _buffer.setLength(0);

            return result;
        }}
        {Functions}
    }}
";
        }
        protected override string GetEngineName()
        {
            return "java";
        }
    }
}
