using BladeEngine.Core;

namespace BladeEngine.Java
{
    public class BladeTemplateJava : BladeTemplateBase<BladeEngineJava>
    {
        public BladeTemplateJava(BladeEngineJava engine) : base(engine)
        {
            Dependencies = @"import org.apache.commons.text.StringEscapeUtils;
import java.net.URLEncoder;
import java.util.Base64;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;";
        }
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
            if (!isNullOrEmpty(s)) {{
                int i = s.indexOf('?');
                String query = s.substring(i + 1);
                String[] parts = query.split(""&"");
                String encodedParts = """";

                for (String part : parts) {{
                    String[] arr = part.split(""="");

                    encodedParts += (isNullOrEmpty(encodedParts) ? """" : ""&"") + URLEncoder.encode(arr[0]) + (arr.length > 1 ? ""="" + URLEncoder.encode(arr[1]) : """");
                }}

                return s.substring(0, i + 1) + encodedParts;
            }}

            return """";
        }}
        protected String fullUrlEncode(String s) {{
            return URLEncoder.encode(s);
        }}
        protected String urlDecode(String s) {{
            return URLEncoder.decode(s);
        }}
        protected String fullUrlDecode(String s) {{
            return URLEncoder.decode(s);
        }}
        protected String md5(String s) {{
            MessageDigest md = MessageDigest.getInstance(""MD5"");
            md.update(s.getBytes(StandardCharsets.UTF_8));
            byte[] digest = md.digest();
            String result = DatatypeConverter.printHexBinary(digest);

            return result;
        }}
        protected String base64Encode(String s) {{
            return Base64.getEncoder().encodeToString(s.getBytes(StandardCharsets.UTF_8));
        }}
        protected String base64Decode(String s) {{
            return Base64.getDecoder().decode(s);
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
    }
}
