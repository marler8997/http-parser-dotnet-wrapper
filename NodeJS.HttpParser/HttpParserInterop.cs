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
    internal struct http_parser_sizes_and_offsets
    {
        public NativeSizeT sizeofShort;
        public NativeSizeT sizeofInt;
        public NativeSizeT sizeofLong;

        // HttpParser offsetofs
        public NativeSizeT sizeofHttpParser;
        public NativeSizeT offsetOfHttpParserNRead;
        public NativeSizeT offsetOfHttpParserContentLength;
        public NativeSizeT offsetOfHttpParserHttpMajor;
        public NativeSizeT offsetOfHttpParserHttpMinor;
        public NativeSizeT offsetOfHttpParserData;

        // HttpParserSettings offsetofs
        // Getting offest of 1st, 2nd and last should be good enough
        public NativeSizeT sizeofHttpParserSettings;
        public NativeSizeT offsetOfHttpParserSettingsOnMessageBegin;
        public NativeSizeT offsetOfHttpParserSettingsOnUrl;
        public NativeSizeT offsetOfHttpParserSettingsOnChunkComplete;
    };

    /// <summary>
    /// These native methods are in addition to the native http-parser api.  These methods
    /// are meant to verify that the managed and native api's are in sync.
    /// </summary>
    public static partial class HttpParser
    {
        [DllImport("nodejs_http_parser")]
        internal static extern NativeSizeT http_parser_sizeof_size_t();

        [DllImport("nodejs_http_parser")]
        internal static unsafe extern void http_parser_get_sizes_and_offsets(http_parser_sizes_and_offsets* sizeofTypes);

        static void AssertMatch(String name, UInt32 nativeSize, UInt32 managedSize)
        {
            if (nativeSize != managedSize)
            {
                throw new InteropMismatchException(name, nativeSize, managedSize);
            }
        }

        public static unsafe void VerifyInteropTypes()
        {
            if (sizeof(IntPtr) != sizeof(GCHandle))
            {
                throw new InvalidOperationException(String.Format(
                    "This library assumes that sizeof(IntPtr) {0} is equal to sizeof(GCHandle) {1} but aren't",
                    sizeof(IntPtr), sizeof(GCHandle)));
            }

            //
            // Verify that size_t is matched first
            //
            NativeSizeT sizeofSizeT = http_parser_sizeof_size_t();
            AssertMatch("sizeof(size_t)", sizeofSizeT, sizeof(NativeSizeT));

            //
            // Request all the sizes and offsets from the native code
            //
            http_parser_sizes_and_offsets sizesAndOffsets = new http_parser_sizes_and_offsets();
            http_parser_get_sizes_and_offsets(&sizesAndOffsets);

            //
            // Verify the rest of the sizes and offsets
            //
            AssertMatch("sizeof(short)", sizesAndOffsets.sizeofShort, sizeof(NativeShort));
            AssertMatch("sizeof(int)"  , sizesAndOffsets.sizeofInt, sizeof(NativeInt));
            AssertMatch("sizeof(long)" , sizesAndOffsets.sizeofLong, sizeof(NativeLong));

            // Verify http_parser
            AssertMatch("sizeof(http_parser)", sizesAndOffsets.sizeofHttpParser, (uint)sizeof(http_parser));
            AssertMatch("offsetof(http_parser, nread)", sizesAndOffsets.offsetOfHttpParserNRead,
                (uint)Marshal.OffsetOf(typeof(http_parser), "nread").ToInt32());
            AssertMatch("offsetof(http_parser, content_length)", sizesAndOffsets.offsetOfHttpParserContentLength,
                (uint)Marshal.OffsetOf(typeof(http_parser), "content_length").ToInt32());
            AssertMatch("offsetof(http_parser, http_major)", sizesAndOffsets.offsetOfHttpParserHttpMajor,
                (uint)Marshal.OffsetOf(typeof(http_parser), "http_major").ToInt32());
            AssertMatch("offsetof(http_parser, http_minor)", sizesAndOffsets.offsetOfHttpParserHttpMinor,
                (uint)Marshal.OffsetOf(typeof(http_parser), "http_minor").ToInt32());
            AssertMatch("offsetof(http_parser, data)", sizesAndOffsets.offsetOfHttpParserData,
                (uint)Marshal.OffsetOf(typeof(http_parser), "data").ToInt32());

            // Verify http_parser_settings
            AssertMatch("sizeof(http_parser_settings)", sizesAndOffsets.sizeofHttpParserSettings, (uint)sizeof(http_parser_settings));
            AssertMatch("offsetof(http_parser, on_message_begin)", sizesAndOffsets.offsetOfHttpParserSettingsOnMessageBegin,
                (uint)Marshal.OffsetOf(typeof(http_parser_settings), "on_message_begin").ToInt32());
            AssertMatch("offsetof(http_parser, on_url)", sizesAndOffsets.offsetOfHttpParserSettingsOnUrl,
                (uint)Marshal.OffsetOf(typeof(http_parser_settings), "on_url").ToInt32());
            AssertMatch("offsetof(http_parser, on_chunk_complete)", sizesAndOffsets.offsetOfHttpParserSettingsOnChunkComplete,
                (uint)Marshal.OffsetOf(typeof(http_parser_settings), "on_chunk_complete").ToInt32());
        }
    }

    public class InteropMismatchException : InvalidOperationException
    {
        public InteropMismatchException(String name, UInt32 nativeValue, UInt32 managedValue)
            : base(String.Format("{0} mismatch (native={1}, managed={2})", name, nativeValue, managedValue))
        {
        }
    }
}