using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NativeShort = System.Int16;
using NativeInt   = System.Int32;
using NativeLong  = System.Int32;

using NativeUnsignedShort = System.UInt16;
using NativeUnsignedInt   = System.UInt32;
using NativeUnsignedLong  = System.UInt32;

using NativeSizeT = System.UInt32;

namespace NodeJS.HttpParser
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    [DeploymentItem("nodejs_http_parser.dll")]
    public class BasicTests
    {
        [TestMethod]
        public void GetAndPrintVersion()
        {
            Console.WriteLine(Environment.CurrentDirectory);

            NativeUnsignedLong version = HttpParser.Version();
            Byte major = (Byte)(version >> 16);
            Byte minor = (Byte)(version >>  8);
            Byte patch = (Byte)version;
            Console.WriteLine("NodeJS Http Parser v{0}.{1}.{2}\n", major, minor, patch);
        }
        [TestMethod]
        public void VerifyInteropTypes()
        {
            HttpParser.VerifyInteropTypes();
        }
        [TestMethod]
        public unsafe void TestInitializingParserSettingsSafe()
        {
            http_parser_settings settings = new http_parser_settings();
            HttpParser.SettingsInit(&settings);

            // The Unsafe way (does not keep managed references to callbacks)
            HttpParser.SetCallback(&settings, CallbackID.MessageBegin, PrintOnMessageBegin);
            HttpParser.SetCallback(&settings, DataCallbackID.Url     , PrintOnUrl);
        }
        [TestMethod]
        public unsafe void TestInitializingParserSettingsUnsafe()
        {
            http_parser_settings settings = new http_parser_settings();
            HttpParser.SettingsInit(&settings);

            // The Safe Way (keeps a managed dictionary of references to callbacks)
            http_parser_settings_references settingsManager = new http_parser_settings_references(false);
            settingsManager.SetCallback(&settings, CallbackID.MessageBegin, PrintOnMessageBegin);
            settingsManager.SetCallback(&settings, DataCallbackID.Url     , PrintOnUrl);
        }

        static unsafe void ParseExample(http_parser_settings* settings, String example)
        {
            Console.WriteLine("---------------------------------------------------------");
            Console.WriteLine(example);
            Console.WriteLine("---------------------------------------------------------");

            http_parser parser = new http_parser();
            HttpParser.Init(&parser, http_parser_type.Both);

            Byte* exampleBytes = stackalloc Byte[example.Length];
            HttpParser.Utf16ToAscii(example, exampleBytes);

            NativeSizeT parseLength = HttpParser.Execute(&parser, settings,
                exampleBytes, (NativeSizeT)example.Length);
            Console.WriteLine("ParseLength {0}", parseLength);
        }

        [TestMethod]
        public unsafe void ParseSomeExamples()
        {
            http_parser_settings settings = new http_parser_settings();
            HttpParser.SettingsInit(&settings);

            // The Unsafe way (does not keep managed references to callbacks)
            // If you do this, you need to make sure that the objects associated with the
            // callbacks are referenced by something so they do not get garbage collected when you
            // call HttpParser.Execute.
            HttpParser.SetCallback(&settings, CallbackID.MessageBegin, PrintOnMessageBegin);
            HttpParser.SetCallback(&settings, DataCallbackID.Url     , PrintOnUrl);

            // Parse some examples
            ParseExample(&settings, "GET / HTTP/1.1\r\nHost: localhost\r\n\r\n");
            ParseExample(&settings, "HTTP/1.1 200 OK\r\n\r\n");
        }

        static unsafe NativeInt PrintOnMessageBegin(http_parser* parser)
        {
            Console.WriteLine("[ParserCallback] MessageBegin");
            return 0;
        }
        static unsafe NativeInt PrintOnUrl(http_parser* parser, byte* at, NativeSizeT length)
        {
            Console.WriteLine("[ParserCallback] OnUrl (method={0})", parser->method);
            for (NativeSizeT i = 0; i < length; i++)
            {
                Console.WriteLine("[ParserCallback]   [{0}] '{1}' {2}", i, (Char)at[i], at[i]);
            }
            return 0;
        }
        static unsafe NativeInt PrintOnStatus(http_parser* parser, byte* at, NativeSizeT length)
        {
            Console.WriteLine("[ParserCallback] OnStatus (HTTP/{0}.{1})", parser->http_major, parser->http_minor);
            for (NativeSizeT i = 0; i < length; i++)
            {
                Console.WriteLine("[ParserCallback]   [{0}] '{1}' {2}", i, (Char)at[i], at[i]);
            }
            return 0;
        }
    }
}
