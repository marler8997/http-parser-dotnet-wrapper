using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using NativeShort = System.Int16;
using NativeInt   = System.Int32;
using NativeLong  = System.Int32;

using NativeUnsignedShort = System.UInt16;
using NativeUnsignedInt   = System.UInt32;
using NativeUnsignedLong  = System.UInt32;

using NativeSizeT = System.UInt32;

namespace NodeJS.HttpParser
{
    // Must be kept in sync with http_parser.h
    public enum HttpMethod
    {
        Delete = 0,
        Get    = 1,
        Head   = 2,
        Post   = 3,
        Put    = 4,
    }


    public static partial class HttpParser
    {
        /// <summary>
        /// Returns the library version. Bits 16-23 contain the major version number,
        /// bits 8-15 the minor version number and bits 0-7 the patch level.
        /// Usage example:
        /// 
        ///    NativeUnsignedLong version = HttpParser.Version();
        ///    Byte major = (Byte)(version >> 16);
        ///    Byte minor = (Byte)(version >>  8);
        ///    Byte patch = (Byte)version;
        ///    Console.WriteLine("NodeJS Http Parser v{0}.{1}.{2}\n", major, minor, patch);
        /// 
        /// </summary>
        [DllImport("nodejs_http_parser", EntryPoint = "http_parser_version")]
        public static extern NativeUnsignedLong Version();

        [DllImport("nodejs_http_parser", EntryPoint = "http_parser_init")]
        public static unsafe extern void Init(http_parser* parser, http_parser_type type);

        [DllImport("nodejs_http_parser", EntryPoint = "http_parser_settings_init")]
        public static unsafe extern void SettingsInit(http_parser_settings* settings);

        [DllImport("nodejs_http_parser", EntryPoint = "http_parser_execute")]
        public static unsafe extern NativeSizeT Execute(http_parser* parser,
            http_parser_settings* settings, byte* data, NativeSizeT len);
        [DllImport("nodejs_http_parser", EntryPoint = "http_parser_execute")]
        public static unsafe extern NativeSizeT Execute(http_parser* parser,
            http_parser_settings* settings, byte[] data, NativeSizeT len);

        // Helper Methods
        public static unsafe void Utf16ToAscii(String source, Byte* dest)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] > 127)
                {
                    throw new FormatException("Called Utf16ToAscii on a string with non-ascii characters");
                }
                dest[i] = (Byte)source[i];
            }
        }
        public static unsafe void AsciiToUtf16(Byte* source, Char* dest, UInt32 length)
        {
            for (int i = 0; i < length; i++)
            {
                if (source[i] > 127)
                {
                    throw new FormatException("Called AsciiToUtf16 on a string with non-ascii characters");
                }
                dest[i] = (Char)source[i];
            }
        }
        public static unsafe void AsciiToUtf16(Byte* source, Char[] dest)
        {
            for (int i = 0; i < dest.Length; i++)
            {
                if (source[i] > 127)
                {
                    throw new FormatException("Called AsciiToUtf16 on a string with non-ascii characters");
                }
                dest[i] = (Char)source[i];
            }
        }
    }

    public enum http_parser_type { Request, Response, Both };

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate NativeInt HttpDataCallback(http_parser* httpParser, byte* at, NativeSizeT length);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate NativeInt HttpCallback(http_parser* httpParser);
    
    [StructLayout(LayoutKind.Sequential)]
    public struct http_parser
    {
        /** PRIVATE **/
        //NativeUnsignedInt type;         /* enum http_parser_type */
        //NativeUnsignedInt flags;        /* F_* values from 'flags' enum; semi-public */
        //NativeUnsignedInt state;        /* enum state from http_parser.c */
        //NativeUnsignedInt header_state; /* enum header_state from http_parser.c */
        //NativeUnsignedInt index;        /* index into current matcher */
        //NativeUnsignedInt lenient_http_headers;
        UInt32 privateBitFieldValues;

        UInt32 nread;          /* # bytes read in various scenarios */
        UInt64 content_length; /* # bytes in body (0 if no Content-Length header) */

        /** READ-ONLY **/
        public readonly NativeUnsignedShort http_major;
        public readonly NativeUnsignedShort http_minor;
        public readonly UInt16 status_code; /* responses only */
        public readonly Byte method;       /* requests only */
        //public readonly NativeUnsignedInt http_errno;
        public readonly Byte http_errno_and_upgrade;


        /* 1 = Upgrade header was present and the parser has exited because of that.
        * 0 = No upgrade header present.
        * Should be checked when http_parser_execute() returns in addition to
        * error checking.
        */
        NativeUnsignedInt upgrade;

        /** PUBLIC **/
        public IntPtr data; /* A pointer to get hook to the "connection" or "socket" object */
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct http_parser_settings
    {
        IntPtr on_message_begin;
        IntPtr on_url;
        IntPtr on_status;
        IntPtr on_header_field;
        IntPtr on_header_value;
        IntPtr on_headers_complete;
        IntPtr on_body;
        IntPtr on_message_complete;
        // When on_chunk_header is called, the current chunk length is stored in parser->content_length.
        IntPtr on_chunk_header;
        IntPtr on_chunk_complete;
    }


    /// <summary>
    /// The purpose of this class is to prevent the garbage collector from
    /// cleaning up objects associated with callback methods that were set
    /// to the native callback structure http_parser_settings.
    /// </summary>
    public struct http_parser_settings_references
    {
        /// <summary>
        /// The purpose of this dictionary is to save managed references to the callback that
        /// are set to the native settings struct.
        /// </summary>
        public readonly Delegate[] callbackReferenceMap;
        public http_parser_settings_references(Boolean ignore)
        {
            this.callbackReferenceMap = new Delegate[(byte)CallbackID.EnumLimit];
        }
        public unsafe void SetCallback(http_parser_settings* settings, CallbackID id, HttpCallback callback)
        {
            NativeInt errorCode = HttpParser.TrySetCallback(settings, id, callback);
            if (errorCode != 0)
            {
                throw new InvalidOperationException(String.Format("Failed to set the '{0}' callback (return code {1})", id, errorCode));
            }
            // Save a managed reference to the callback
            // This will also override the old reference if there is one
            callbackReferenceMap[(byte)id] = callback;
        }
        public unsafe void SetCallback(http_parser_settings* settings, DataCallbackID id, HttpDataCallback callback)
        {
            NativeInt errorCode = HttpParser.TrySetCallback(settings, id, callback);
            if (errorCode != 0)
            {
                throw new InvalidOperationException(String.Format("Failed to set the '{0}' callback (return code {1})", id, errorCode));
            }
            // Save a managed reference to the callback
            // This will also override the old reference if there is one
            callbackReferenceMap[(byte)id] = callback;
        }
    }
}