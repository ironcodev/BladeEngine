﻿namespace BladeEngine.Core
{
    public enum BladeTemplateParseState
    {
        Start,
        LT,
        StartTag,
        BackSlash,
        BackSlashPercent,
        DependencyStart,
        IsDependencyEnd,
        DependencyEnd,
        TemplateNameStart,
        TemplateNameEnding,
        TemplateNameEnd,
        ExternalCodeStart,
        ExternalCodeEnd,
        FullUrlEncodeStart,
        FullUrlEncodeEnd,
        CommentStart,
        CommentEnding,
        CommentEnd,
        IncludeStart,
        IncludePathStart,
        IncludePathEnd,
        IncludeEnd,
        HtmlEncodeStart,
        HtmlEncodeEnd,
        PlainWriteStart,
        PlainWriteEnd,
        IsHtmlDecode,
        HtmlDecodeStart,
        HtmlDecodeEnd,
        IsUrlEncode,
        UrlEncodeStart,
        UrlEncodeEnd,
        IsMD5,
        MD5Start,
        MD5End,
        IsFullUrlDecode,
        FullUrlDecodeStart,
        FullUrlDecodeEnd,
        IsUrlDecode,
        UrlDecodeStart,
        UrlDecodeEnd,
        IsBase64Encode,
        Base64EncodeStart,
        Base64EncodeEnd,
        IsBase64Decode,
        Base64DecodeStart,
        Base64DecodeEnd,
        BodyCodeStart,
        IsBodyCodeEnd,
        BodyCodeEnd,
        FunctionStart,
        IsFunctionEnd,
        FunctionEnd
    }
}
