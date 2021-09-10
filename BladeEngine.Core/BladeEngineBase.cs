using BladeEngine.Core.Base.Exceptions;
using BladeEngine.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Core
{
    public abstract partial class BladeEngineBase
    {
        private BladeEngineConfigBase _config;
        public BladeEngineConfigBase Config
        {
            get
            {
                if (_config == null)
                {
                    _config = new BladeEngineConfigBase();
                }

                return _config;
            }
            set
            {
                _config = value;
            }
        }
        public BladeEngineBase()
        { }
        public BladeEngineBase(BladeEngineConfigBase config)
        {
            _config = config;
        }
        protected abstract string WriteLiteral(string literal);
        protected abstract string WriteValue(string value);
        protected string MergeDependencies(string currentDependencies,
                                            string newDependencies,
                                            string keyword,
                                            string separator,
                                            Func<List<string>, string, string> dependencyRefiner = null)
        {
            List<string> init(string dependencies)
            {
                var result = new List<string>();

                foreach (var line in dependencies.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
                {
                    foreach (var @using in line.Split(separator, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var current = @using.Trim();

                        if (current.Length > 0)
                        {
                            if (current.StartsWith(keyword, StringComparison.Ordinal))
                            {
                                if (char.IsWhiteSpace(current[keyword.Length]))
                                {
                                    current = current.Substring(keyword.Length).Trim();
                                }
                            }

                            if (dependencyRefiner != null)
                            {
                                current = dependencyRefiner(result, current);
                            }

                            if (!result.Contains(current))
                            {
                                result.Add(current);
                            }
                        }
                    }
                }

                return result;
            };

            var _currentDependencies = init(currentDependencies);
            var _newDependencies = init(newDependencies);

            foreach (var dependency in _newDependencies)
            {
                if (!_currentDependencies.Contains(dependency, StringComparer.Ordinal))
                {
                    _currentDependencies.Add(dependency);
                }
            }

            var final = _currentDependencies.Aggregate("", (prev, current) => prev + (string.IsNullOrEmpty(prev) ? "" : Environment.NewLine) + keyword + " " + current + separator);

            return final;
        }
        protected abstract string MergeDependencies(string currentDependencies, string newDependencies);
        protected virtual string MergeExternalCode(string currentExternalCodes, string newExternalCodes)
        {
            return currentExternalCodes + Environment.NewLine + newExternalCodes;
        }
        protected abstract BladeTemplateBase CreateTemplate();
        #region Encode/Decode
        protected virtual string HtmlEncode(string s)
        {
            return WriteValue($"{(Config.CamelCase ? "h" : "H")}tmlEncode({s})");
        }
        protected virtual string HtmlDecode(string s)
        {
            return WriteValue($"{(Config.CamelCase ? "h" : "H")}tmlDecode({s})");
        }
        protected virtual string UrlEncode(string s)
        {
            return WriteValue($"{(Config.CamelCase ? "u" : "U")}rlEncode({s})");
        }
        protected virtual string FullUrlEncode(string s)
        {
            return WriteValue($"{(Config.CamelCase ? "f" : "F")}ullUrlEncode({s})");
        }
        protected virtual string UrlDecode(string s)
        {
            return WriteValue($"{(Config.CamelCase ? "u" : "U")}rlDecode({s})");
        }
        protected virtual string FullUrlDecode(string s)
        {
            return WriteValue($"{(Config.CamelCase ? "f" : "F")}ullUrlDecode({s})");
        }
        protected virtual string MD5(string s)
        {
            return WriteValue($"{(Config.CamelCase ? "m" : "M")}d5({s})");
        }
        protected virtual string Base64Encode(string s)
        {
            return WriteValue($"{(Config.CamelCase ? "b" : "B")}ase64Encode({s})");
        }
        protected virtual string Base64Decode(string s)
        {
            return WriteValue($"{(Config.CamelCase ? "b" : "B")}ase64Decode({s})");
        }
        #endregion
        public BladeTemplateBase Parse(string template)
        {
            var result = CreateTemplate();
            var reader = new CharReader(template);
            var body = new StringBuilder();
            var functions = new StringBuilder();
            var state = BladeTemplateParseState.Start;
            var prevState = state;
            var buffer = new CharBuffer();
            var functionStarted = false;
            var functionShouldClose = false;
            var stringStart = default(char);

            Action<string, string> throwBladeEngineInvalidCharacterException = (type, expected) =>
            {
                throw new BladeEngineInvalidCharacterException(reader.Current, reader.Position, reader.Row, reader.Col, type, state.ToString(), expected);
            };

            Action<string> append = s =>
            {
                if (functionStarted)
                {
                    functions.Append(s);
                }
                else
                {
                    body.Append(s);
                }
            };

            foreach (var ch in reader)
            {
                switch (state)
                {
                    case BladeTemplateParseState.Start:
                        if (ch == '<')
                        {
                            state = BladeTemplateParseState.LT;
                        }
                        else
                        {
                            buffer.Append(ch);
                        }

                        break;
                    case BladeTemplateParseState.LT:
                        if (ch == '%')
                        {
                            var literal = buffer.Flush();

                            if (literal != Environment.NewLine || !Config.SkipExcessiveNewLines)
                            {
                                append(WriteLiteral(literal));
                            }

                            state = BladeTemplateParseState.StartTag;
                        }
                        else
                        {
                            buffer.Append("<" + ch);

                            state = BladeTemplateParseState.Start;
                        }

                        break;
                    case BladeTemplateParseState.StartTag:
                        switch (ch)
                        {
                            case '@':
                                state = BladeTemplateParseState.DependencyStart;
                                break;
                            case '`':
                                state = BladeTemplateParseState.TemplateNameStart;
                                break;
                            case '!':
                                state = BladeTemplateParseState.ExternalCodeStart;
                                break;
                            case '*':
                                state = BladeTemplateParseState.CommentStart;
                                break;
                            case '#':
                                state = BladeTemplateParseState.IncludeStart;
                                break;
                            case '=':
                                state = BladeTemplateParseState.PlainWriteStart;
                                break;
                            case '~':
                                state = BladeTemplateParseState.IsHtmlDecode;
                                break;
                            case '?':
                                state = BladeTemplateParseState.IsUrlEncode;
                                break;
                            case '$':
                                state = BladeTemplateParseState.IsMD5;
                                break;
                            case '^':
                                state = BladeTemplateParseState.IsFullUrlDecode;
                                break;
                            case '&':
                                state = BladeTemplateParseState.IsUrlDecode;
                                break;
                            case ':':
                                state = BladeTemplateParseState.IsBase64Encode;
                                break;
                            case '.':
                                state = BladeTemplateParseState.IsBase64Decode;
                                break;
                            default:
                                state = BladeTemplateParseState.BodyCodeStart;
                                reader.Store();
                                break;
                        }

                        break;
                    case BladeTemplateParseState.BackSlash:
                        if (ch == '%')
                        {
                            state = BladeTemplateParseState.BackSlashPercent;
                        }
                        else
                        {
                            buffer.Append("\\" + ch);

                            state = prevState;
                        }

                        break;
                    case BladeTemplateParseState.BackSlashPercent:
                        if (ch == '>')
                        {
                            buffer.Append("%>");

                            state = BladeTemplateParseState.BackSlashPercent;
                        }
                        else
                        {
                            buffer.Append("\\%" + ch);

                            state = prevState;
                        }

                        break;
                    case BladeTemplateParseState.DependencyStart:
                        if (ch == '%')
                        {
                            state = BladeTemplateParseState.DependencyEnd;
                        }
                        else
                        {
                            if (ch == '\\')
                            {
                                state = BladeTemplateParseState.BackSlash;

                                prevState = state;
                            }
                            else
                            {
                                buffer.Append(ch);
                            }
                        }

                        break;
                    case BladeTemplateParseState.DependencyEnd:
                        if (ch == '>')
                        {
                            result.Dependencies += Environment.NewLine + buffer.Flush();

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            reader.Store();

                            state = BladeTemplateParseState.DependencyStart;
                        }

                        break;
                    case BladeTemplateParseState.TemplateNameStart:
                        if (ch == '`')
                        {
                            state = BladeTemplateParseState.TemplateNameEnding;
                        }
                        else if (Char.IsLetterOrDigit(ch) || ch == '_')
                        {
                            buffer.Append(ch);
                        }
                        else
                        {
                            throwBladeEngineInvalidCharacterException("InvalidCharacterInTemplateName", "num|alpha|_");
                        }

                        break;
                    case BladeTemplateParseState.TemplateNameEnding:
                        if (ch == '%')
                        {
                            state = BladeTemplateParseState.TemplateNameEnd;
                        }
                        else
                        {
                            throwBladeEngineInvalidCharacterException("TemplateNameEndTagError", "%");
                        }

                        break;
                    case BladeTemplateParseState.TemplateNameEnd:
                        if (ch == '>')
                        {
                            var name = buffer.Flush();

                            if (string.IsNullOrEmpty(name))
                            {
                                throw new BladeEngineMissingTemplateNameException();
                            }

                            result.SetMainClassName(name);

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            throwBladeEngineInvalidCharacterException("TemplateNameEndTagError", ">");
                        }

                        break;
                    case BladeTemplateParseState.ExternalCodeStart:
                        switch (ch)
                        {
                            case '=':
                                state = BladeTemplateParseState.FullUrlEncodeStart;
                                break;
                            case '%':
                                state = BladeTemplateParseState.ExternalCodeEnd;
                                break;
                            case '\\':
                                prevState = state;
                                state = BladeTemplateParseState.BackSlash;
                                break;
                            default:
                                buffer.Append(ch);
                                break;
                        }

                        break;
                    case BladeTemplateParseState.ExternalCodeEnd:
                        if (ch == '>')
                        {
                            result.ExternalCode += Environment.NewLine + buffer.Flush();

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            state = BladeTemplateParseState.ExternalCodeStart;

                            reader.Store();
                        }

                        break;
                    case BladeTemplateParseState.FullUrlEncodeStart:
                        switch (ch)
                        {
                            case '%':
                                state = BladeTemplateParseState.FullUrlEncodeEnd;
                                break;
                            case '\\':
                                prevState = state;
                                state = BladeTemplateParseState.BackSlash;
                                break;
                            default:
                                buffer.Append(ch);
                                break;
                        }

                        break;
                    case BladeTemplateParseState.FullUrlEncodeEnd:
                        if (ch == '>')
                        {
                            append(FullUrlEncode(buffer.Flush()));

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            state = BladeTemplateParseState.FullUrlEncodeStart;

                            reader.Store();
                        }

                        break;
                    case BladeTemplateParseState.CommentStart:
                        switch (ch)
                        {
                            case '*':
                                state = BladeTemplateParseState.CommentEnding;
                                break;
                            case '\\':
                                prevState = state;
                                state = BladeTemplateParseState.BackSlash;
                                break;
                            default:
                                break;
                        }

                        break;
                    case BladeTemplateParseState.CommentEnding:
                        if (ch == '%')
                        {
                            state = BladeTemplateParseState.CommentEnd;
                        }
                        else
                        {
                            state = BladeTemplateParseState.CommentStart;

                            reader.Store();
                        }

                        break;
                    case BladeTemplateParseState.CommentEnd:
                        if (ch == '>')
                        {
                            buffer.Flush();

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            state = BladeTemplateParseState.CommentStart;

                            reader.Store();
                        }

                        break;
                    case BladeTemplateParseState.IncludeStart:
                        if (ch == '=')
                        {
                            state = BladeTemplateParseState.HtmlEncodeStart;
                        }
                        else if (Char.IsWhiteSpace(ch))
                        {
                            state = BladeTemplateParseState.IncludeStart;
                        }
                        else if (ch == '"' || ch == '\'')
                        {
                            stringStart = ch;

                            state = BladeTemplateParseState.IncludePathStart;
                        }
                        else
                        {
                            throwBladeEngineInvalidCharacterException("IncludeError", "string starter character (' or \")");
                        }

                        break;
                    case BladeTemplateParseState.IncludePathStart:
                        if (ch == stringStart)
                        {
                            state = BladeTemplateParseState.IncludePathEnd;
                        }
                        else if (ch == '\r' || ch == '\n')
                        {
                            throwBladeEngineInvalidCharacterException("UnTerminatedIncludePath", "string starter character (' or \")");
                        }
                        else
                        {
                            buffer.Append(ch);
                        }

                        break;
                    case BladeTemplateParseState.IncludePathEnd:
                        if (ch == '%')
                        {
                            state = BladeTemplateParseState.IncludeEnd;
                        }
                        else if (!char.IsWhiteSpace(ch))
                        {
                            throwBladeEngineInvalidCharacterException("IncludeEndError", "whitespace or %");
                        }

                        break;
                    case BladeTemplateParseState.IncludeEnd:
                        if (ch == '>')
                        {
                            var path = buffer.Flush();

                            path = Path.Combine(Environment.CurrentDirectory, path);

                            if (!File.Exists(path))
                            {
                                throw new BladeIncludeFileNotFoundException(path);
                            }

                            var content = Try(() => File.ReadAllText(path), e => new BladeEngineIncludeFileReadException(path, e));
                            var ir = Try(() => Parse(content), e => new BladeEngineIncludeFileParseException(path, e));
                            
                            result.Dependencies = Try(() => MergeDependencies(result.Dependencies, ir.Dependencies), e => new BladeEngineMergeDependenciesException(path, e));
                            result.ExternalCode += Environment.NewLine + ir.ExternalCode + Environment.NewLine + ir.Body;

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            throwBladeEngineInvalidCharacterException("IncludePathEndTagError", ">");
                        }

                        break;
                    case BladeTemplateParseState.HtmlEncodeStart:
                        switch (ch)
                        {
                            case '%':
                                state = BladeTemplateParseState.HtmlEncodeEnd;
                                break;
                            case '\\':
                                prevState = state;
                                state = BladeTemplateParseState.BackSlash;
                                break;
                            default:
                                buffer.Append(ch);
                                break;
                        }

                        break;
                    case BladeTemplateParseState.HtmlEncodeEnd:
                        if (ch == '>')
                        {
                            append(HtmlEncode(buffer.Flush()));

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            state = BladeTemplateParseState.HtmlEncodeStart;

                            reader.Store();
                        }

                        break;
                    case BladeTemplateParseState.PlainWriteStart:
                        switch (ch)
                        {
                            case '%':
                                state = BladeTemplateParseState.PlainWriteEnd;
                                break;
                            case '\\':
                                prevState = state;
                                state = BladeTemplateParseState.BackSlash;
                                break;
                            default:
                                buffer.Append(ch);
                                break;
                        }

                        break;
                    case BladeTemplateParseState.PlainWriteEnd:
                        if (ch == '>')
                        {
                            append(WriteValue(buffer.Flush()));

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            state = BladeTemplateParseState.PlainWriteStart;

                            reader.Store();
                        }

                        break;
                    case BladeTemplateParseState.IsHtmlDecode:
                        if (ch == '=')
                        {
                            state = BladeTemplateParseState.HtmlDecodeStart;
                        }
                        else
                        {
                            reader.Store();

                            if (!functionStarted)
                            {
                                functionStarted = true;

                                buffer.Append(Environment.NewLine);
                            }

                            state = BladeTemplateParseState.FunctionStart;
                        }

                        break;
                    case BladeTemplateParseState.HtmlDecodeStart:
                        switch (ch)
                        {
                            case '%':
                                state = BladeTemplateParseState.HtmlDecodeEnd;

                                break;
                            case '\\':
                                prevState = state;

                                state = BladeTemplateParseState.BackSlash;

                                break;
                            default:
                                buffer.Append(ch);

                                break;
                        }

                        break;
                    case BladeTemplateParseState.HtmlDecodeEnd:
                        if (ch == '>')
                        {
                            append(HtmlDecode(buffer.Flush()));

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            state = BladeTemplateParseState.HtmlDecodeStart;

                            reader.Store();
                        }

                        break;
                    case BladeTemplateParseState.IsUrlEncode:
                        if (ch == '=')
                        {
                            state = BladeTemplateParseState.UrlEncodeStart;
                        }
                        else
                        {
                            reader.Store();
                            state = BladeTemplateParseState.BodyCodeStart;
                        }

                        break;
                    case BladeTemplateParseState.UrlEncodeStart:
                        switch (ch)
                        {
                            case '%':
                                state = BladeTemplateParseState.UrlEncodeEnd;
                                break;
                            case '\\':
                                prevState = state;
                                state = BladeTemplateParseState.BackSlash;
                                break;
                            default:
                                buffer.Append(ch);
                                break;
                        }

                        break;
                    case BladeTemplateParseState.UrlEncodeEnd:
                        if (ch == '>')
                        {
                            append(UrlEncode(buffer.Flush()));

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            state = BladeTemplateParseState.UrlEncodeStart;

                            reader.Store();
                        }

                        break;
                    case BladeTemplateParseState.IsMD5:
                        if (ch == '=')
                        {
                            state = BladeTemplateParseState.MD5Start;
                        }
                        else
                        {
                            reader.Store();

                            state = BladeTemplateParseState.BodyCodeStart;
                        }

                        break;
                    case BladeTemplateParseState.MD5Start:
                        switch (ch)
                        {
                            case '%':
                                state = BladeTemplateParseState.MD5End;
                                break;
                            case '\\':
                                prevState = state;
                                state = BladeTemplateParseState.BackSlash;
                                break;
                            default:
                                buffer.Append(ch);
                                break;
                        }

                        break;
                    case BladeTemplateParseState.MD5End:
                        if (ch == '>')
                        {
                            append(MD5(buffer.Flush()));

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            state = BladeTemplateParseState.MD5Start;

                            reader.Store();
                        }

                        break;
                    case BladeTemplateParseState.IsFullUrlDecode:
                        if (ch == '=')
                        {
                            state = BladeTemplateParseState.FullUrlDecodeStart;
                        }
                        else
                        {
                            reader.Store();

                            state = BladeTemplateParseState.BodyCodeStart;
                        }

                        break;
                    case BladeTemplateParseState.FullUrlDecodeStart:
                        switch (ch)
                        {
                            case '%':
                                state = BladeTemplateParseState.FullUrlDecodeEnd;
                                break;
                            case '\\':
                                prevState = state;
                                state = BladeTemplateParseState.BackSlash;
                                break;
                            default:
                                buffer.Append(ch);
                                break;
                        }

                        break;
                    case BladeTemplateParseState.FullUrlDecodeEnd:
                        if (ch == '>')
                        {
                            append(FullUrlDecode(buffer.Flush()));

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            state = BladeTemplateParseState.FullUrlDecodeStart;

                            reader.Store();
                        }

                        break;
                    case BladeTemplateParseState.IsUrlDecode:
                        if (ch == '=')
                        {
                            state = BladeTemplateParseState.UrlDecodeStart;
                        }
                        else
                        {
                            reader.Store();

                            state = BladeTemplateParseState.BodyCodeStart;
                        }

                        break;
                    case BladeTemplateParseState.UrlDecodeStart:
                        switch (ch)
                        {
                            case '%':
                                state = BladeTemplateParseState.UrlDecodeEnd;
                                break;
                            case '\\':
                                prevState = state;
                                state = BladeTemplateParseState.BackSlash;
                                break;
                            default:
                                buffer.Append(ch);
                                break;
                        }

                        break;
                    case BladeTemplateParseState.UrlDecodeEnd:
                        if (ch == '>')
                        {
                            append(UrlDecode(buffer.Flush()));

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            state = BladeTemplateParseState.UrlDecodeStart;

                            reader.Store();
                        }

                        break;
                    case BladeTemplateParseState.IsBase64Encode:
                        if (ch == '=')
                        {
                            state = BladeTemplateParseState.Base64EncodeStart;
                        }
                        else
                        {
                            reader.Store();

                            state = BladeTemplateParseState.BodyCodeStart;
                        }

                        break;
                    case BladeTemplateParseState.Base64EncodeStart:
                        switch (ch)
                        {
                            case '%':
                                state = BladeTemplateParseState.Base64EncodeEnd;
                                break;
                            case '\\':
                                prevState = state;
                                state = BladeTemplateParseState.BackSlash;
                                break;
                            default:
                                buffer.Append(ch);
                                break;
                        }

                        break;
                    case BladeTemplateParseState.Base64EncodeEnd:
                        if (ch == '>')
                        {
                            append(Base64Encode(buffer.Flush()));

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            state = BladeTemplateParseState.Base64EncodeStart;

                            reader.Store();
                        }

                        break;
                    case BladeTemplateParseState.IsBase64Decode:
                        if (ch == '=')
                        {
                            state = BladeTemplateParseState.Base64DecodeStart;
                        }
                        else
                        {
                            reader.Store();

                            state = BladeTemplateParseState.BodyCodeStart;
                        }

                        break;
                    case BladeTemplateParseState.Base64DecodeStart:
                        switch (ch)
                        {
                            case '%':
                                state = BladeTemplateParseState.Base64DecodeEnd;
                                break;
                            case '\\':
                                prevState = state;
                                state = BladeTemplateParseState.BackSlash;
                                break;
                            default:
                                buffer.Append(ch);
                                break;
                        }

                        break;
                    case BladeTemplateParseState.Base64DecodeEnd:
                        if (ch == '>')
                        {
                            append(Base64Decode(buffer.Flush()));

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            state = BladeTemplateParseState.Base64DecodeStart;

                            reader.Store();
                        }

                        break;
                    case BladeTemplateParseState.BodyCodeStart:
                        switch (ch)
                        {
                            case '~':
                                state = BladeTemplateParseState.IsBodyCodeEnd;
                                break;
                            case '%':
                                state = BladeTemplateParseState.BodyCodeEnd;
                                break;
                            case '\\':
                                prevState = state;
                                state = BladeTemplateParseState.BackSlash;
                                break;
                            default:
                                buffer.Append(ch);
                                break;
                        }

                        break;
                    case BladeTemplateParseState.IsBodyCodeEnd:
                        if (ch == '%')
                        {
                            state = BladeTemplateParseState.BodyCodeEnd;
                        }
                        else
                        {
                            buffer.Append("~" + ch);

                            state = BladeTemplateParseState.BodyCodeStart;
                        }

                        break;
                    case BladeTemplateParseState.BodyCodeEnd:
                        if (ch == '>')
                        {
                            var code = buffer.Flush();

                            append(code);

                            if (reader.HasEndedWith("~%>"))
                            {
                                if (!functionStarted)
                                {
                                    throw new BladeEngineException("Encountered function end tag without a previous function start tag.");
                                }

                                functionStarted = false;
                                functionShouldClose = false;
                            }

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            state = BladeTemplateParseState.BodyCodeStart;

                            reader.Store();
                        }

                        break;
                    case BladeTemplateParseState.FunctionStart:
                        switch (ch)
                        {
                            case '~':
                                state = BladeTemplateParseState.IsFunctionEnd;
                                break;
                            case '%':
                                state = BladeTemplateParseState.FunctionEnd;
                                break;
                            case '\\':
                                prevState = state;
                                state = BladeTemplateParseState.BackSlash;
                                break;
                            default:
                                buffer.Append(ch);
                                break;
                        }

                        break;
                    case BladeTemplateParseState.IsFunctionEnd:
                        if (ch == '%')
                        {
                            state = BladeTemplateParseState.FunctionEnd;
                        }
                        else
                        {
                            state = BladeTemplateParseState.FunctionStart;
                            reader.Store();
                        }

                        break;
                    case BladeTemplateParseState.FunctionEnd:
                        if (ch == '>')
                        {
                            append(buffer.Flush());

                            if (reader.HasEndedWith("~%>") || functionShouldClose)
                            {
                                functionStarted = false;
                                functionShouldClose = false;
                            }
                            else
                            {
                                if (!functionShouldClose)
                                {
                                    functionShouldClose = true;
                                }
                            }

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            state = BladeTemplateParseState.FunctionStart;
                            reader.Store();
                        }

                        break;
                }
            }

            if (state != BladeTemplateParseState.Start)
            {
                var currentTag = "";

                if (state == BladeTemplateParseState.StartTag ||
                    state == BladeTemplateParseState.IsBase64Decode || state == BladeTemplateParseState.IsBase64Encode ||
                    state == BladeTemplateParseState.IsFullUrlDecode || state == BladeTemplateParseState.IsHtmlDecode ||
                    state == BladeTemplateParseState.IsMD5 ||
                    state == BladeTemplateParseState.IsUrlDecode || state == BladeTemplateParseState.IsUrlEncode
                    )
                {
                    currentTag = "tag";
                }
                else if (state == BladeTemplateParseState.DependencyStart || state == BladeTemplateParseState.DependencyEnd ||
                        (state == BladeTemplateParseState.BackSlash && prevState == BladeTemplateParseState.DependencyStart) ||
                        (state == BladeTemplateParseState.BackSlashPercent && prevState == BladeTemplateParseState.DependencyStart))
                {
                    currentTag = "dependency <%@ ...";
                }
                else if (state == BladeTemplateParseState.TemplateNameStart || state == BladeTemplateParseState.TemplateNameEnding ||
                    state == BladeTemplateParseState.TemplateNameEnd)
                {
                    currentTag = "template name <%` ...";
                }
                else if (state == BladeTemplateParseState.ExternalCodeStart || state == BladeTemplateParseState.ExternalCodeEnd ||
                        (state == BladeTemplateParseState.BackSlash && prevState == BladeTemplateParseState.ExternalCodeStart) ||
                        (state == BladeTemplateParseState.BackSlashPercent && prevState == BladeTemplateParseState.ExternalCodeStart))
                {
                    currentTag = "external code <%! ...";
                }
                else if (state == BladeTemplateParseState.FullUrlEncodeStart || state == BladeTemplateParseState.FullUrlEncodeEnd ||
                        (state == BladeTemplateParseState.BackSlash && prevState == BladeTemplateParseState.FullUrlEncodeStart) ||
                        (state == BladeTemplateParseState.BackSlashPercent && prevState == BladeTemplateParseState.FullUrlEncodeStart))
                {
                    currentTag = "fullUrlEncode <%!= ...";
                }
                else if (state == BladeTemplateParseState.CommentStart || state == BladeTemplateParseState.CommentEnding || state == BladeTemplateParseState.CommentEnd ||
                        (state == BladeTemplateParseState.BackSlash && prevState == BladeTemplateParseState.CommentStart) ||
                        (state == BladeTemplateParseState.BackSlashPercent && prevState == BladeTemplateParseState.CommentStart))
                {
                    currentTag = "comment <%* ...";
                }
                else if (state == BladeTemplateParseState.IncludeStart || state == BladeTemplateParseState.IncludeEnd ||
                        state == BladeTemplateParseState.IncludePathStart || state == BladeTemplateParseState.IncludePathEnd ||
                        (state == BladeTemplateParseState.BackSlash && prevState == BladeTemplateParseState.IncludeStart) ||
                        (state == BladeTemplateParseState.BackSlashPercent && prevState == BladeTemplateParseState.IncludeStart))
                {
                    currentTag = "include <%# ...";
                }
                else if (state == BladeTemplateParseState.HtmlEncodeStart || state == BladeTemplateParseState.HtmlEncodeEnd ||
                        (state == BladeTemplateParseState.BackSlash && prevState == BladeTemplateParseState.HtmlEncodeStart) ||
                        (state == BladeTemplateParseState.BackSlashPercent && prevState == BladeTemplateParseState.HtmlEncodeStart))
                {
                    currentTag = "htmlEncode <%#= ...";
                }
                else if (state == BladeTemplateParseState.PlainWriteStart || state == BladeTemplateParseState.PlainWriteEnd ||
                        (state == BladeTemplateParseState.BackSlash && prevState == BladeTemplateParseState.PlainWriteStart) ||
                        (state == BladeTemplateParseState.BackSlashPercent && prevState == BladeTemplateParseState.PlainWriteStart))
                {
                    currentTag = "write <%= ...";
                }
                else if (state == BladeTemplateParseState.HtmlDecodeStart || state == BladeTemplateParseState.HtmlDecodeEnd ||
                        (state == BladeTemplateParseState.BackSlash && prevState == BladeTemplateParseState.HtmlDecodeStart) ||
                        (state == BladeTemplateParseState.BackSlashPercent && prevState == BladeTemplateParseState.HtmlDecodeStart))
                {
                    currentTag = "htmlDecode <%~= ...";
                }
                else if (state == BladeTemplateParseState.UrlEncodeStart || state == BladeTemplateParseState.UrlEncodeEnd ||
                        (state == BladeTemplateParseState.BackSlash && prevState == BladeTemplateParseState.UrlEncodeStart) ||
                        (state == BladeTemplateParseState.BackSlashPercent && prevState == BladeTemplateParseState.UrlEncodeStart))
                {
                    currentTag = "urlEncode <%?= ...";
                }
                else if (state == BladeTemplateParseState.UrlDecodeStart || state == BladeTemplateParseState.UrlDecodeEnd ||
                        (state == BladeTemplateParseState.BackSlash && prevState == BladeTemplateParseState.UrlDecodeStart) ||
                        (state == BladeTemplateParseState.BackSlashPercent && prevState == BladeTemplateParseState.UrlDecodeStart))
                {
                    currentTag = "urlDecode <%&= ...";
                }
                else if (state == BladeTemplateParseState.FullUrlDecodeStart || state == BladeTemplateParseState.FullUrlDecodeEnd ||
                        (state == BladeTemplateParseState.BackSlash && prevState == BladeTemplateParseState.FullUrlDecodeStart) ||
                        (state == BladeTemplateParseState.BackSlashPercent && prevState == BladeTemplateParseState.FullUrlDecodeStart))
                {
                    currentTag = "fullUrlDecode <%^= ...";
                }
                else if (state == BladeTemplateParseState.MD5Start || state == BladeTemplateParseState.MD5End ||
                        (state == BladeTemplateParseState.BackSlash && prevState == BladeTemplateParseState.MD5Start) ||
                        (state == BladeTemplateParseState.BackSlashPercent && prevState == BladeTemplateParseState.MD5Start))
                {
                    currentTag = "md5 <%$= ...";
                }
                else if (state == BladeTemplateParseState.Base64EncodeStart || state == BladeTemplateParseState.Base64EncodeEnd ||
                        (state == BladeTemplateParseState.BackSlash && prevState == BladeTemplateParseState.Base64EncodeStart) ||
                        (state == BladeTemplateParseState.BackSlashPercent && prevState == BladeTemplateParseState.Base64EncodeStart))
                {
                    currentTag = "base64Encode <%:= ...";
                }
                else if (state == BladeTemplateParseState.Base64DecodeStart || state == BladeTemplateParseState.Base64DecodeEnd ||
                        (state == BladeTemplateParseState.BackSlash && prevState == BladeTemplateParseState.Base64DecodeStart) ||
                        (state == BladeTemplateParseState.BackSlashPercent && prevState == BladeTemplateParseState.Base64DecodeStart))
                {
                    currentTag = "base64Decode <%.= ...";
                }
                else if (state == BladeTemplateParseState.BodyCodeStart || state == BladeTemplateParseState.BodyCodeEnd || state == BladeTemplateParseState.IsBodyCodeEnd ||
                        (state == BladeTemplateParseState.BackSlash && prevState == BladeTemplateParseState.BodyCodeStart) ||
                        (state == BladeTemplateParseState.BackSlashPercent && prevState == BladeTemplateParseState.BodyCodeStart))
                {
                    currentTag = "block <% ... ";
                }
                else if (state == BladeTemplateParseState.FunctionStart || state == BladeTemplateParseState.IsFunctionEnd || state == BladeTemplateParseState.FunctionEnd ||
                        (state == BladeTemplateParseState.BackSlash && prevState == BladeTemplateParseState.FunctionStart) ||
                        (state == BladeTemplateParseState.BackSlashPercent && prevState == BladeTemplateParseState.FunctionStart))
                {
                    currentTag = "function <%~ ... ";
                }

                throw string.IsNullOrEmpty(currentTag) ? new BladeEngineException($"Unknown error. Current state is {state}") : new BladeEngineUnterminatedTagException(currentTag);
            }
            else
            {
                if (functionStarted)
                {
                    throw new BladeEngineUnterminatedFunctionException();
                }

                var literal = buffer.Flush();

                if (literal != Environment.NewLine || !Config.SkipExcessiveNewLines)
                {
                    append(WriteLiteral(literal));
                }
            }

            result.Functions = functions.ToString();
            result.Body = body.ToString();

            return result;
        }
    }

    public abstract class BladeEngineBase<T> : BladeEngineBase
        where T : BladeEngineConfigBase
    {
        public BladeEngineBase(T config) : base(config)
        { }
        public T StrongConfig
        {
            get
            {
                return (T)Config;
            }
            set
            {
                Config = value;
            }
        }
    }
}
