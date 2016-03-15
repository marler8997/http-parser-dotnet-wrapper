using System;
using System.Runtime.InteropServices;

using NativeShort = System.Int16;
using NativeInt   = System.Int32;
using NativeLong  = System.Int32;

using NativeUnsignedShort = System.UInt16;
using NativeUnsignedInt   = System.UInt32;
using NativeUnsignedLong  = System.UInt32;

using NativeSizeT = System.UInt32;

namespace NodeJS.HttpParser
{
    //
    // These need to be synchronized with http_parser_interop.h
    //
    [StructLayout(LayoutKind.Sequential)]
    internal struct http_parser_sizeof_types
    {
        public NativeSizeT sizeofShort;
        public NativeSizeT sizeofInt;
        public NativeSizeT sizeofLong;
        public NativeSizeT sizeofHttpParser;
        public NativeSizeT sizeofHttpParserSettings;
    };
    
    public enum CallbackID
    {
        MessageBegin      = 0,
        //Url             = 1,
        //Status          = 2,
        //HeaderField     = 3,
        //HeaderValue     = 4,
        HeadersComplete   = 5,
        //Body            = 6,
        MessageComplete   = 7,
        ChunkHeader       = 8,
        ChunkComplete     = 9,
        EnumLimit         = 10,
    }
    public enum DataCallbackID
    {
        // MessageBegin   = 0
        Url               = 1,
        Status            = 2,
        HeaderField       = 3,
        HeaderValue       = 4,
        //HeadersComplete = 5,
        Body            = 6,
        //MessageComplete = 7,
        //ChunkHeader     = 8,
        //ChunkComplete   = 9
        EnumLimit         = 10,
    }


    /// <summary>
    /// These native methods are in addition to the native http-parser api.  These methods
    /// are meant to verify that the managed and native api's are in sync.
    /// </summary>
    public static partial class HttpParser
    {
        [DllImport("nodejs_http_parser")]
        internal static extern NativeSizeT http_parser_sizeof_size_t();

        [DllImport("nodejs_http_parser")]
        internal static unsafe extern void http_parser_get_sizeof_types(http_parser_sizeof_types* sizeofTypes);

        [DllImport("nodejs_http_parser", EntryPoint = "http_parser_set_callback")]
        public static extern unsafe NativeInt TrySetCallback(http_parser_settings* settings, CallbackID callbackID, HttpCallback callback);
        [DllImport("nodejs_http_parser", EntryPoint = "http_parser_set_callback")]
        public static extern unsafe NativeInt TrySetCallback(http_parser_settings* settings, DataCallbackID callbackID, HttpDataCallback callback);

        public static unsafe void SetCallback(http_parser_settings* settings, CallbackID callbackID, HttpCallback callback)
        {
            NativeInt errorCode = TrySetCallback(settings, callbackID, callback);
            if (errorCode != 0)
            {
                throw new InvalidOperationException(String.Format("Failed to set the '{0}' callback (return code {1})", callbackID, errorCode));
            }
        }
        public static unsafe void SetCallback(http_parser_settings* settings, DataCallbackID callbackID, HttpDataCallback callback)
        {
            NativeInt errorCode = TrySetCallback(settings, callbackID, callback);
            if (errorCode != 0)
            {
                throw new InvalidOperationException(String.Format("Failed to set the '{0}' callback (return code {1})", callbackID, errorCode));
            }
        }
        public static unsafe void VerifyInteropTypes()
        {
            NativeSizeT sizeofSizeT = http_parser_sizeof_size_t();
            if (sizeofSizeT != sizeof(NativeSizeT))
            {
                throw new InvalidOperationException(String.Format("native sizeof(size_t) {0} != sizeof(NativeSizeT) {1}",
                    sizeofSizeT, sizeof(NativeSizeT)));
            }
            http_parser_sizeof_types sizeofTypes = new http_parser_sizeof_types();
            http_parser_get_sizeof_types(&sizeofTypes);
            if (sizeofTypes.sizeofShort != sizeof(NativeShort))
            {
                throw new InvalidOperationException(String.Format("native sizeof(short) {0} != sizeof(NativeShort) {1}",
                    sizeofTypes.sizeofShort, sizeof(NativeShort)));
            }
            if (sizeofTypes.sizeofInt != sizeof(NativeInt))
            {
                throw new InvalidOperationException(String.Format("native sizeof(int) {0} != sizeof(NativeInt) {1}",
                    sizeofTypes.sizeofInt, sizeof(NativeInt)));
            }
            if (sizeofTypes.sizeofLong != sizeof(NativeLong))
            {
                throw new InvalidOperationException(String.Format("native sizeof(long) {0} != sizeof(NativeLong) {1}",
                    sizeofTypes.sizeofLong, sizeof(NativeLong)));
            }
            if (sizeofTypes.sizeofHttpParser != sizeof(http_parser))
            {
                throw new InvalidOperationException(String.Format("native sizeof(http_parser) {0} != sizeof(http_parser) {1}",
                    sizeofTypes.sizeofHttpParser, sizeof(http_parser)));
            }
            if (sizeofTypes.sizeofHttpParserSettings != sizeof(http_parser_settings))
            {
                throw new InvalidOperationException(String.Format("native sizeof(http_parser_settings) {0} != sizeof(http_parser_settings) {1}",
                    sizeofTypes.sizeofHttpParserSettings, sizeof(http_parser_settings)));
            }
        }
    }
}