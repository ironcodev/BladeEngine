using BladeEngine.Core;
using System;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.VisualBasic
{
    public class BladeTemplateVisualBasic : BladeTemplateBase<BladeEngineVisualBasic>
    {
        public static string NamespaceRegex => @"^@?[a-z_A-Z]\w*(\.@?[a-z_A-Z]\w*)*$";
        public BladeTemplateVisualBasic(BladeEngineVisualBasic engine, BladeTemplateSettings settings = null) : base(engine, settings)
        {
            ModuleNameRegex = NamespaceRegex;

            if (!Settings.IsInclude)
            {
                Dependencies = @"Imports Blade
Imports System
Imports System.IO
Imports System.Text
Imports System.Linq
Imports System.Net
Imports System.Xml
Imports System.Drawing
Imports System.Threading
Imports System.Collections
Imports System.Collections.Generic
Imports System.Data.SqlClient
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq";
                ExternalCode = $@"
Namespace Blade
    Public MustInherit Class BladeTemplateVisualBasicBase
        Protected _buffer As StringBuilder

        Public Sub New()
            _buffer = New StringBuilder()
        End Sub
        #Region Encode/Decode Helpers
        Protected Overridable Function HtmlEncode(ByVal s As String) As String
            Return Net.WebUtility.HtmlEncode(s)
        End Function

        Protected Overridable Function HtmlDecode(ByVal s As String) As String
            Return Net.WebUtility.HtmlDecode(s)
        End Function

        Protected Overridable Function UrlEncode(ByVal s As String) As String
            If Not String.IsNullOrEmpty(s) Then
                Dim i = s.IndexOf(""?""c)
                Dim query = s.Substring(i + 1)
                Dim parts = query.Split(New Char() {{""&""c}})
                Dim encodedParts = """"

                For Each part In parts
                    Dim arr = part.Split(""=""c)
                    encodedParts += If(String.IsNullOrEmpty(encodedParts), """", ""&"") & Net.WebUtility.UrlEncode(arr(0)) & If(arr.Length > 1, ""="" & Net.WebUtility.UrlEncode(arr(1)), """")
                Next

                Return s.Substring(0, i + 1) & encodedParts
            End If

            Return """"
        End Function

        Protected Overridable Function FullUrlEncode(ByVal s As String) As String
            Return Net.WebUtility.UrlEncode(s)
        End Function

        Protected Overridable Function UrlDecode(ByVal s As String) As String
            Return Net.WebUtility.UrlDecode(s)
        End Function

        Protected Overridable Function FullUrlDecode(ByVal s As String) As String
            Return Net.WebUtility.UrlDecode(s)
        End Function

        Protected Overridable Function Md5(ByVal s As String) As String
            Dim lMd5 = New Cryptography.MD5CryptoServiceProvider()
            Dim bytes = Encoding.UTF8.GetBytes(s)
            bytes = lMd5.ComputeHash(bytes)
            Dim buff = New StringBuilder()

            For Each ba As Byte In bytes
                buff.Append(ba.ToString(""x2"").ToLower())
            Next

            Return buff.ToString()
        End Function

        Protected Overridable Function Base64Encode(ByVal s As String) As String
            Return Convert.ToBase64String(Encoding.UTF8.GetBytes(s))
        End Function

        Protected Overridable Function Base64Decode(ByVal s As String) As String
            Return Encoding.UTF8.GetString(Convert.FromBase64String(s))
        End Function
        {new string[]
            {
                "Boolean", "Char()", "Char", "Byte", "SByte", "Integer", "UInteger", "Short", "UShort", "Long", "ULong",
                "Single", "Double", "Decimal", "Object", "String", "System.Text.StringBuilder"
            }.Join(t => $@"
        Protected Sub Write(x As {t})
            _buffer.Append(x)
        End Sub")}
        
        Protected Sub WriteLine(ByVal x As Object)
            _buffer.AppendLine(x?.ToString())
        End Sub
        #End Region
    End Class
End Namespace
";
            }
        }
        public override string RenderContent()
        {
            return $@"
{ExternalCode}
{(IsSomeString(Body + Functions, rejectAllWhitespaceStrings: true) ? $@"
Namespace {GetModuleName()}
    Public Class {GetMainClassName()}
        Inherits BladeTemplateVisualBasicBase
        
        Public Function {(StrongEngine.StrongConfig.UseStrongModel && IsSomeString(StrongEngine.StrongConfig.StrongModelType, rejectAllWhitespaceStrings: true) ? $"Render({StrongEngine.StrongConfig.StrongModelType} model = default)" : "Render(dynamic model = (object)null)")} As String
            {Body}
            Dim result As String = _buffer.ToString()

            _buffer.Clear()

            Return result
        End Function
        {Functions}
    End Class
End Namespace
": "")}
";
        }
        protected override string GetEngineName()
        {
            return "VisualBasic";
        }
    }
}
