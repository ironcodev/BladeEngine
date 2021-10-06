using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using BladeEngine.Core.Exceptions;
using BladeEngine.Core.Utils;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Core
{
    public abstract partial class BladeEngineBase
    {
        public BladeEngineConfigBase Config { get; set; }
        public BladeEngineBase(BladeEngineConfigBase config)
        {
            Config = config;
        }
        protected bool endParseOnFirstEngineNameOccuranceDetection;
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
                    foreach (var dependency in line.Split(separator, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var current = dependency.Trim();

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
        protected abstract bool OnIncludeTemplate(CharReader reader, BladeTemplateBase current, BladeTemplateBase include);
        protected abstract BladeTemplateBase CreateTemplate(BladeTemplateSettings settings);
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
        public static string GetEngineName(string template)
        {
            var engine = new BladeEngineAny(new BladeEngineConfigAny());

            var templateResult = engine.Parse(template);

            return templateResult.EngineName;
        }
        public virtual BladeTemplateBase Parse(string template, BladeTemplateSettings settings = null)
        {
            var result = CreateTemplate(settings);
            var reader = new CharReader(template);
            var body = new StringBuilder();
            var functions = new StringBuilder();
            var state = BladeTemplateParseState.Start;
            var prevState = state;
            var buffer = new CharBuffer();
            var setEngineName = false;
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
                                state = BladeTemplateParseState.IsDependencyStart;
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
                    case BladeTemplateParseState.IsDependencyStart:
                        if (ch == '`')
                        {
                            state = BladeTemplateParseState.TemplateNameStart;
                        }
                        else
                        {
                            state = BladeTemplateParseState.DependencyStart;

                            reader.Store();
                        }

                        break;
                    case BladeTemplateParseState.DependencyStart:
                        if (ch == '%')
                        {
                            state = BladeTemplateParseState.DependencyEnd;
                        }
                        else if (ch == '@')
                        {
                            state = BladeTemplateParseState.IsDependencyEnd;
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
                    case BladeTemplateParseState.IsDependencyEnd:
                        if (ch == '%')
                        {
                            state = BladeTemplateParseState.DependencyEnd;
                            setEngineName = true;
                        }
                        else
                        {
                            reader.Store();

                            state = BladeTemplateParseState.DependencyStart;
                        }

                        break;
                    case BladeTemplateParseState.DependencyEnd:
                        if (ch == '>')
                        {
                            if (setEngineName)
                            {
                                result.EngineName = buffer.Flush();
                            }
                            else
                            {
                                result.Dependencies += Environment.NewLine + buffer.Flush();
                            }

                            setEngineName = false;

                            state = BladeTemplateParseState.Start;

                            if (endParseOnFirstEngineNameOccuranceDetection)
                            {
                                break;
                            }
                        }
                        else
                        {
                            setEngineName = false;

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
                                throw new BladeEngineMissingTemplateNameException(reader.Row, reader.Col);
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
                            var includePath = buffer.Flush();

                            if (string.IsNullOrEmpty(includePath))
                            {
                                throw new BladeEngineIncludePathEmptyException(reader.Row, reader.Col);
                            }

                            if (Path.IsPathRooted(includePath) && !(includePath[0] == '/' || includePath[0] == '\\'))
                            {
                                throw new BladeEngineRootedIncludeFilePreventedException(reader.Row, reader.Col, includePath);
                            }

                            var includeTemplatePath = "";

                            if (includePath[0] == '/' || includePath[0] == '\\')
                            {
                                includeTemplatePath = PathHelper.Refine("." + includePath);
                            }
                            else if (includePath[0] != '~')
                            {
                                includeTemplatePath = PathHelper.Refine(result.Settings.Path + "/" + includePath);
                            }
                            else
                            {
                                if (includePath.Length > 1)
                                {
                                    if (includePath[1] == '/' || includePath[1] == '\\')
                                    {
                                        includeTemplatePath = PathHelper.Refine("." + includePath.Substring(1));
                                    }
                                    else
                                    {
                                        includeTemplatePath = PathHelper.Refine(includePath.Substring(1));
                                    }
                                }
                                else
                                {
                                    includeTemplatePath = ".";
                                }
                            }

                            var finalPath = includePath[0] == '~' ? Path.Combine(AppPath.ProgramDir + "/" + result.EngineName, includeTemplatePath): Path.Combine(Environment.CurrentDirectory, includeTemplatePath);
                            var fileExists = false;

                            try
                            {
                                finalPath = PathHelper.Refine(finalPath, false);
                            }
                            catch (Exception)
                            {
                                throw new BladeEngineIncludePathUnderflowException(reader.Row, reader.Col, includePath);
                            }

                            if (finalPath.EndsWith("/"))
                            {
                                finalPath += "index.blade";
                            }
                            else
                            {
                                if (!File.Exists(finalPath))
                                {
                                    if (Directory.Exists(finalPath))
                                    {
                                        finalPath += "/index.blade";
                                    }
                                    else
                                    {
                                        finalPath += ".blade";
                                    }
                                }
                                else
                                {
                                    fileExists = true;
                                }
                            }

                            if (!fileExists && !File.Exists(finalPath))
                            {
                                throw new BladeIncludeFileNotFoundException(reader.Row, reader.Col, finalPath);
                            }
                            var _settings = new BladeTemplateSettings
                            {
                                Path = includeTemplatePath,
                                AbsolutePath = Path.GetDirectoryName(finalPath),
                                IsLocal = includePath[0] != '~',
                                IsInclude = true
                            };
                            var content = Try(() => File.ReadAllText(finalPath), e => new BladeEngineIncludeFileReadException(reader.Row, reader.Col, finalPath, e));
                            var itr = Try(() => Parse(content, _settings), e => new BladeEngineIncludeFileParseException(reader.Row, reader.Col, finalPath, e));
                            var fullClassName = itr.GetFullMainClassName();

                            if (ClassExists(result, fullClassName))
                            {
                                throw new BladeEngineClassAlreadyIncludedException(reader.Row, reader.Col, fullClassName);
                            }

                            if (OnIncludeTemplate(reader, result, itr))
                            {
                                result.InnerTemplates.Add(itr);
                            }

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
                        else if (ch == '`')
                        {
                            state = BladeTemplateParseState.ModuleNameStart;
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
                    case BladeTemplateParseState.ModuleNameStart:
                        if (ch == '`')
                        {
                            state = BladeTemplateParseState.TemplateNameEnding;
                        }
                        else if (Char.IsLetterOrDigit(ch) || ch == '_' || ch == '.')
                        {
                            buffer.Append(ch);
                        }
                        else
                        {
                            throwBladeEngineInvalidCharacterException("InvalidCharacterInModuleName", "num|alpha|_|.");
                        }

                        break;
                    case BladeTemplateParseState.ModuleNameEnding:
                        if (ch == '%')
                        {
                            state = BladeTemplateParseState.TemplateNameEnd;
                        }
                        else
                        {
                            throwBladeEngineInvalidCharacterException("ModuleeNameEndTagError", "%");
                        }

                        break;
                    case BladeTemplateParseState.ModuleNameEnd:
                        if (ch == '>')
                        {
                            var name = buffer.Flush();

                            if (!string.IsNullOrEmpty(name))
                            {
                                result.SetModuleName(name);
                            }

                            state = BladeTemplateParseState.Start;
                        }
                        else
                        {
                            throwBladeEngineInvalidCharacterException("ModuleeNameEndTagError", ">");
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

                throw string.IsNullOrEmpty(currentTag) ? new BladeEngineException($"Unknown error. Current state is {state}") : new BladeEngineUnterminatedTagException(reader.Row, reader.Col, currentTag);
            }
            else
            {
                if (functionStarted)
                {
                    throw new BladeEngineUnterminatedFunctionException(reader.Row, reader.Col);
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
        protected virtual bool ClassExists(BladeTemplateBase template, string fullClassName)
        {
            var result = false;

            if (template.InnerTemplates != null)
            {
                foreach (var innerTemplate in template.InnerTemplates)
                {
                    if (string.Compare(innerTemplate.GetFullMainClassName(), fullClassName, false) == 0)
                    {
                        result = true;
                        break;
                    }

                    if (innerTemplate.InnerTemplates?.Count > 0)
                    {
                        if (ClassExists(innerTemplate, fullClassName))
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }

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
