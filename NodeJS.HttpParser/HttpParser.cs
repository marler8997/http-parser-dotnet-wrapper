using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using More;

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
    public enum HttpMethod : byte
    {
        DELETE      = 0,
        GET         = 1,
        HEAD        = 2,
        POST        = 3,
        PUT         = 4,
        CONNECT     = 5,
        OPTIONS     = 6,
        TRACE       = 7,
        COPY        = 8,
        LOCK        = 9,
        MKCOL       = 10,
        MOVE        = 11,
        PROPFIND    = 12,
        PROPPATCH   = 13,
        SEARCH      = 14,
        UNLOCK      = 15,
        BIND        = 16,
        REBIND      = 17,
        UNBIND      = 18,
        ACL         = 19,
        REPORT      = 20,
        MKACTIVITY  = 21,
        CHECKOUT    = 22,
        MERGE       = 23,
        MSEARCH     = 24,
        NOTIFY      = 25,
        SUBSCRIBE   = 26,
        UNSUBSCRIBE = 27,
        PATCH       = 28,
        PURGE       = 29,
        MKCALENDAR  = 30,
        LINK        = 31,
        UNLINK      = 32,
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
        [DllImport("nodejs_http_parser", EntryPoint = "http_parser_init")]
        public static unsafe extern void Init(ref http_parser parser, http_parser_type type);

        [DllImport("nodejs_http_parser", EntryPoint = "http_parser_settings_init")]
        public static unsafe extern void SettingsInit(http_parser_settings* settings);

        [DllImport("nodejs_http_parser", EntryPoint = "http_parser_execute")]
        public static unsafe extern NativeSizeT Execute(http_parser* parser,
            http_parser_settings* settings, byte* data, NativeSizeT len);
        [DllImport("nodejs_http_parser", EntryPoint = "http_parser_execute")]
        public static unsafe extern NativeSizeT Execute(http_parser* parser,
            http_parser_settings* settings, byte[] data, NativeSizeT len);

        [DllImport("nodejs_http_parser", EntryPoint = "http_parser_execute")]
        public static unsafe extern NativeSizeT Execute(http_parser* parser,
            HttpParserSettings settings, byte* data, NativeSizeT len);
        [DllImport("nodejs_http_parser", EntryPoint = "http_parser_execute")]
        public static unsafe extern NativeSizeT Execute(http_parser* parser,
            HttpParserSettings settings, byte[] data, NativeSizeT len);

        [DllImport("nodejs_http_parser", EntryPoint = "http_parser_execute")]
        public static unsafe extern NativeSizeT Execute(ref http_parser parser,
            http_parser_settings* settings, byte* data, NativeSizeT len);
        [DllImport("nodejs_http_parser", EntryPoint = "http_parser_execute")]
        public static unsafe extern NativeSizeT Execute(ref http_parser parser,
            http_parser_settings* settings, byte[] data, NativeSizeT len);

        [DllImport("nodejs_http_parser", EntryPoint = "http_parser_execute")]
        public static unsafe extern NativeSizeT Execute(ref http_parser parser,
            HttpParserSettings settings, byte* data, NativeSizeT len);
        [DllImport("nodejs_http_parser", EntryPoint = "http_parser_execute")]
        public static unsafe extern NativeSizeT Execute(ref http_parser parser,
            HttpParserSettings settings, byte[] data, NativeSizeT len);
    }

    public enum http_parser_type { Request, Response, Both };

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate NativeInt HttpDataCallback(ref http_parser httpParser, BytePtr at, NativeSizeT length);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate NativeInt HttpCallback(ref http_parser httpParser);
    
    [StructLayout(LayoutKind.Sequential)]
    public struct http_parser
    {
        //NativeUnsignedInt type;         /* enum http_parser_type */
        //NativeUnsignedInt flags;        /* F_* values from 'flags' enum; semi-public */
        //NativeUnsignedInt state;        /* enum state from http_parser.c */
        //NativeUnsignedInt header_state; /* enum header_state from http_parser.c */
        //NativeUnsignedInt index;        /* index into current matcher */
        //NativeUnsignedInt lenient_http_headers;
        UInt32 privateBitFieldValues;

        UInt32 nread;          /* # bytes read in various scenarios */
        UInt64 content_length; /* # bytes in body (0 if no Content-Length header) */

        public readonly NativeUnsignedShort http_major;
        public readonly NativeUnsignedShort http_minor;
        public readonly UInt16 status_code; /* responses only */
        public readonly HttpMethod method;       /* requests only */
        //public readonly NativeUnsignedInt http_errno;
        public readonly Byte http_errno_and_upgrade;
        /* 1 = Upgrade header was present and the parser has exited because of that.
        * 0 = No upgrade header present.
        * Should be checked when http_parser_execute() returns in addition to
        * error checking.
        */
        //NativeUnsignedInt upgrade;

        /// <summary>
        /// A pointer to get hook to the "connection" or "socket" object
        /// </summary>
        public GCHandle data;
    }

    public struct HttpNativeCallback
    {
        public IntPtr ptr;
        public void Set(HttpCallback callback)
        {
            this.ptr = Marshal.GetFunctionPointerForDelegate(callback);
        }
    }
    public struct HttpNativeDataCallback
    {
        public IntPtr ptr;
        public void Set(HttpDataCallback callback)
        {
            this.ptr = Marshal.GetFunctionPointerForDelegate(callback);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct http_parser_settings
    {
        public HttpNativeCallback on_message_begin;
        public HttpNativeDataCallback on_url;
        public HttpNativeDataCallback on_status;
        public HttpNativeDataCallback on_header_field;
        public HttpNativeDataCallback on_header_value;
        public HttpNativeCallback on_headers_complete;
        public HttpNativeDataCallback on_body;
        public HttpNativeCallback on_message_complete;
        // When on_chunk_header is called, the current chunk length is stored in parser->content_length.
        public HttpNativeCallback on_chunk_header;
        public HttpNativeCallback on_chunk_complete;
    }
    public struct HttpParserSettings : IDisposable
    {
        unsafe http_parser_settings* ptr;
        public unsafe HttpParserSettings(bool initialize)
        {
            this.ptr = (http_parser_settings*)Marshal.AllocHGlobal(sizeof(http_parser_settings));
            if (initialize)
            {
                HttpParser.SettingsInit(this.ptr);
            }
        }
        public unsafe void SetOnMessageBegin(HttpCallback cb)
        {
            ptr->on_message_begin.Set(cb);
        }
        public unsafe void SetOnUrl(HttpDataCallback cb)
        {
            ptr->on_url.Set(cb);
        }
        public unsafe void SetOnStatus(HttpDataCallback cb)
        {
            ptr->on_status.Set(cb);
        }
        public unsafe void SetOnHeaderField(HttpDataCallback cb)
        {
            ptr->on_header_field.Set(cb);
        }
        public unsafe void SetOnHeaderValue(HttpDataCallback cb)
        {
            ptr->on_header_value.Set(cb);
        }
        public unsafe void SetOnHeadersComplete(HttpCallback cb)
        {
            ptr->on_headers_complete.Set(cb);
        }
        public unsafe void SetOnBody(HttpDataCallback cb)
        {
            ptr->on_body.Set(cb);
        }
        public unsafe void SetOnMessageComplete(HttpCallback cb)
        {
            ptr->on_message_complete.Set(cb);
        }
        public unsafe void SetOnChunkHeader(HttpCallback cb)
        {
            ptr->on_chunk_header.Set(cb);
        }
        public unsafe void SetOnChunkComplete(HttpCallback cb)
        {
            ptr->on_chunk_complete.Set(cb);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        unsafe void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                if (ptr != null)
                {
                    Marshal.FreeHGlobal((IntPtr)ptr);
                    ptr = null;
                }
            }
        }
    }
}


//
// Copied from the More Library https://github.com/marler8997/More.git
//
namespace More
{
    public static class Unicode
    {
        public static unsafe void Utf8ToUtf16(Byte* src, Char* dest, UInt32 length)
        {
            for (int i = 0; i < length; i++)
            {
                // TODO: should I be checking this?
                if (src[i] > 127)
                {
                    throw new FormatException("Called AsciiToUtf16 on a string with non-ascii characters");
                }
                dest[i] = (Char)src[i];
            }
        }

        public static unsafe void Utf16ToAscii(String source, Byte* dest)
        {
            for (int i = 0; i < source.Length; i++)
            {
                // TODO: should I be checking this?
                if (source[i] > 127)
                {
                    throw new FormatException("Called Utf16ToAscii on a string with non-ascii characters");
                }
                dest[i] = (Byte)source[i];
            }
        }
    }
    public struct BytePtr
    {
        //public static unsafe readonly BytePtr Null = new BytePtr(null);
        public unsafe void SetToNull()
        {
            this.ptr = null;
        }

        public unsafe Byte* ptr;
        public unsafe BytePtr(Byte* ptr)
        {
            this.ptr = ptr;
        }

        //
        // Implicit Cast Operators
        //
        public static unsafe implicit operator BytePtr(Byte* ptr)
        {
            return new BytePtr(ptr);
        }
        public static unsafe implicit operator BytePtr(IntPtr ptr)
        {
            return new BytePtr((Byte*)ptr.ToPointer());
        }
        public static unsafe implicit operator IntPtr(BytePtr ptr)
        {
            return new IntPtr(ptr.ptr);
        }
        public static unsafe implicit operator BytePtr(GCHandle handle)
        {
            return GCHandle.ToIntPtr(handle);
        }
        public static unsafe implicit operator GCHandle(BytePtr ptr)
        {
            return GCHandle.FromIntPtr(ptr);
        }
        //
        // Explicit Cast Operators
        //
        public static unsafe explicit operator UInt32(BytePtr ptr)
        {
            return (UInt32)ptr.ptr;
        }
        public static unsafe explicit operator Int32(BytePtr ptr)
        {
            return (Int32)ptr.ptr;
        }

        //
        // Comparison Operators
        //
        public static unsafe Boolean operator ==(BytePtr a, Byte* p)
        {
            return a.ptr == p;
        }
        public static unsafe Boolean operator !=(BytePtr a, Byte* p)
        {
            return a.ptr != p;
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static unsafe Boolean operator <(BytePtr a, BytePtr b)
        {
            return a.ptr < b.ptr;
        }
        public static unsafe Boolean operator >(BytePtr a, BytePtr b)
        {
            return a.ptr > b.ptr;
        }
        public static unsafe Boolean operator <=(BytePtr a, BytePtr b)
        {
            return a.ptr <= b.ptr;
        }
        public static unsafe Boolean operator >=(BytePtr a, BytePtr b)
        {
            return a.ptr >= b.ptr;
        }
        public static unsafe BytePtr operator +(BytePtr a, UInt32 b)
        {
            return a.ptr + b;
        }

        public unsafe Byte this[UInt32 i]
        {
            get
            {
                return ptr[i];
            }
        }


        public unsafe UInt32 DiffWithSmallerNumber(BytePtr smaller)
        {
            return (UInt32)(this.ptr - smaller.ptr);
        }


        public unsafe override string ToString()
        {
            return new IntPtr(ptr).ToString();
        }

        // TODO: use native memcpy
        public unsafe void CopyTo(BytePtr dest, UInt32 length)
        {
            for (uint i = 0; i < length; i++)
            {
                dest.ptr[i] = this.ptr[i];
            }
        }
    }
}