using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

using More;
using More.Net;
using NodeJS.HttpParser;

// Todo: support apache configuration
//
// <ListenEndPoint>
//   ListenPort <port>
//   ListenAddress <addr>
//   ListenBacklog <backlog>
//
// </ListenEndPoint>
//
// * Default Index File
// * Logging options
// * Virtual Hosts?
class FastWebServer
{
    public static readonly HttpParserSettings ParserSettings = new HttpParserSettings(true);

    static Buf globalSafeBuffer;
    public static Buf sharedResponseBuffer;
    static String rootDirectory;
    static String defaultIndexFile;

    public static TextWriter Logger = Console.Out;
    public static Boolean VerboseParserOutput = false;

    static void Main(string[] args)
    {
        HttpParser.VerifyInteropTypes();

        //
        // Configuration
        //
        rootDirectory = Environment.CurrentDirectory;
        defaultIndexFile = "index.html";
        Int32 listenBacklog = 32;
        List<EndPoint> listenEndPoints = new List<EndPoint>();
        listenEndPoints.Add(new IPEndPoint(IPAddress.Any, 81));

        //
        // Setup Parser Settings
        //
        ParserSettings.SetOnMessageBegin(OnMessageBegin);
        ParserSettings.SetOnUrl(OnUrl);
        ParserSettings.SetOnHeaderField(OnHeaderField);
        ParserSettings.SetOnHeaderValue(OnHeaderValue);
        ParserSettings.SetOnHeadersComplete(OnHeadersComplete);
        ParserSettings.SetOnBody(OnBody);
        ParserSettings.SetOnMessageComplete(OnMessageComplete);
        ParserSettings.SetOnChunkHeader(OnChunkHeader);
        ParserSettings.SetOnChunkComplete(OnChunkComplete);

        //
        // Setup/Run Server
        //
        globalSafeBuffer = new Buf(1024, 1024);
        sharedResponseBuffer = new Buf(1024, 1024);
        SelectServer server = new SelectServer(false, globalSafeBuffer);

        foreach (var endpoint in listenEndPoints)
        {
            Socket listenSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(endpoint);
            listenSocket.Listen(listenBacklog);
            Logger.WriteLine("Listening on {0}", endpoint);
            server.control.AddListenSocket(listenSocket, AcceptCallback);
        }

        server.Run();
    }

    static void AcceptCallback(ref SelectControl selectControl, Socket listenSocket, Buf safeBuffer)
    {
        Socket newSocket = listenSocket.Accept();
        if (!newSocket.Connected)
        {
            newSocket.Close();
            return;
        }

        String newSocketLogString = newSocket.RemoteEndPoint.ToString();
        Logger.WriteLine("[{0}] NewClient", newSocketLogString);
        selectControl.AddReceiveSocket(newSocket, new OpenHttpClient(newSocketLogString, newSocket).RecvHandler);
    }



    //
    // Http Parser Callbacks
    //
    static int OnMessageBegin(ref http_parser parser)
    {
        OpenHttpClient client = (OpenHttpClient)((GCHandle)parser.data).Target;

        if (VerboseParserOutput)
        {
            Logger.WriteLine("[{0}] [Parser] HeadersComplete", client.logString);
        }

        return 0;
    }
    static int OnUrl(ref http_parser parser, BytePtr at, UInt32 length)
    {
        OpenHttpClient client = (OpenHttpClient)((GCHandle)parser.data).Target;
        client.url = at.AsciiToString(length);
        Logger.WriteLine("[{0}] {1} {2}", client.logString, (HttpMethod)parser.method, client.url);

        return 0;
    }
    static int OnHeaderField(ref http_parser parser, BytePtr at, UInt32 length)
    {
        OpenHttpClient client = (OpenHttpClient)((GCHandle)parser.data).Target;

        // Find header
        // TODO: make this lookup more efficient then just looping through all the known headers
        client.currentHeader = HttpHeader.UnknownHeader;
        for (int i = 0; i < HttpHeader.KnownHeaderDefinitions.Length; i++)
        {
            HttpHeader knownHeader = HttpHeader.KnownHeaderDefinitions[i];
            if (at.AsciiEquals(length, knownHeader.name))
            {
                client.currentHeader = knownHeader;
                break;
            }
        }

        if (VerboseParserOutput)
        {
            Logger.Write("[{0}] [Parser] HeaderField ({1}) \"", client.logString,
                (client.currentHeader == HttpHeader.UnknownHeader) ? "Unknown" : "Known");
            for (uint i = 0; i < length; i++)
            {
                Logger.Write((Char)at[i]);
            }
            Logger.WriteLine("\"");
        }

        return 0;
    }
    static int OnHeaderValue(ref http_parser parser, BytePtr at, UInt32 length)
    {
        OpenHttpClient client = (OpenHttpClient)((GCHandle)parser.data).Target;

        if (VerboseParserOutput)
        {
            Logger.Write("[{0}] [Parser] HeaderValue \"", client.logString);
            for (uint i = 0; i < length; i++)
            {
                Logger.Write((Char)at[i]);
            }
            Logger.WriteLine("\"");
            client.currentHeader.handler(client, at, length);
        }

        return 0;
    }
    static int OnHeadersComplete(ref http_parser parser)
    {
        OpenHttpClient client = (OpenHttpClient)((GCHandle)parser.data).Target;

        if (VerboseParserOutput)
        {
            Logger.WriteLine("[{0}] [Parser] HeadersComplete", client.logString);
        }
        return 0;
    }
    static int OnBody(ref http_parser parser, BytePtr at, UInt32 length)
    {
        OpenHttpClient client = (OpenHttpClient)((GCHandle)parser.data).Target;

        Logger.WriteLine("[{0}] [Parser] OnBody", client.logString);
        return 0;
    }

    const String HttpResponseOk = "200 OK";
    const String HttpResponseBadRequest = "400 Bad Request";
    const String HttpResponseNotFound = "404 Not Found";
    const String HttpResponseMethodNotAllowed = "405 Method Not Allowed";

    static int OnMessageComplete(ref http_parser parser)
    {
        OpenHttpClient client = (OpenHttpClient)((GCHandle)parser.data).Target;

        if (VerboseParserOutput)
        {
            Logger.WriteLine("[{0}] [Parser] MessageComplete", client.logString);
        }

        //
        // Handle the request
        //
        if (parser.method == HttpMethod.GET)
        {
            String url = client.url;

            /*
            // Remove protocol/host from url
            {
                int indexOfProto = url.IndexOf("://");
                if(indexOfProto >= 0)
                {
                    url = url.Substring(
                }
            }
            */

            if (url[0] == '/')
            {
                url = url.Substring(1);
            }
            if (url.Length == 0)
            {
                url = defaultIndexFile;
            }

            String absolutePath = Path.Combine(rootDirectory, url);
            Logger.WriteLine("Uri '{0}' > '{1}'", client.url, absolutePath);
            if (!File.Exists(absolutePath))
            {
                client.Respond(ref parser, HttpResponseNotFound, "", String.Format("Url '{0}' was not found", client.url));
            }
            else
            {
                FileInfo info = new FileInfo(absolutePath);
                var longFileSize = info.Length;
                if (longFileSize > UInt32.MaxValue)
                {
                    client.Respond(ref parser, "400 Bad Request", "", String.Format("File size {0} is too large", longFileSize));
                }
                UInt32 fileSize = (UInt32)longFileSize;

                // TODO: with big files read and send the file in chunks

                UInt32 headersLength = OpenHttpClient.CalculateHeadersLength(HttpResponseOk, "", fileSize);
                UInt32 totalLength = headersLength + fileSize;
                sharedResponseBuffer.EnsureCapacityNoCopy(totalLength);
                Byte[] responseBuffer = sharedResponseBuffer.array;
                UInt32 offset = OpenHttpClient.GenerateHeaders(responseBuffer, 0, ref parser, HttpResponseOk, "", fileSize);

                if (offset != headersLength)
                {
                    throw new InvalidOperationException(String.Format("CodeBug: Expected headers length to be {0} but was {1}", headersLength, offset));
                }

                using (FileStream stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    stream.ReadFullSize(responseBuffer, (int)offset, (int)fileSize);
                }

                Logger.WriteLine("[{0}] {1} (TotalResponse {2} bytes)", client.logString, HttpResponseOk, totalLength);
                client.socket.Send(responseBuffer, (int)totalLength, SocketFlags.None);
            }
        }
        else
        {
            client.Respond(ref parser, HttpResponseMethodNotAllowed, "", String.Format("Method '{0}' is not implemented", parser.method));
        }

        return 0;
    }
    static int OnChunkHeader(ref http_parser parser)
    {
        OpenHttpClient client = (OpenHttpClient)((GCHandle)parser.data).Target;

        Logger.WriteLine("[{0}] OnChunkHeader", client.logString);
        return 0;
    }
    static int OnChunkComplete(ref http_parser parser)
    {
        OpenHttpClient client = (OpenHttpClient)((GCHandle)parser.data).Target;

        Logger.WriteLine("[{0}] OnChunkComplete", client.logString);
        return 0;
    }
}
public static class Extensions
{
    // TODO: in the future, I will want to implement a logger that does not
    // require strings, but can use ascii/utf8 encoded strings
    public static unsafe String AsciiToString(this BytePtr ascii, UInt32 length)
    {
        Char* stackBuffer = stackalloc Char[(int)length];
        for (uint i = 0; i < length; i++)
        {
            stackBuffer[(int)i] = (Char)ascii.ptr[i];
        }
        return new string(stackBuffer, 0, (int)length);
    }
    public static unsafe Boolean AsciiEquals(this BytePtr ascii, UInt32 length, String cmp)
    {
        if (length != cmp.Length)
        {
            return false;
        }
        for (int i = 0; i < length; i++)
        {
            if ((Char)ascii.ptr[i] != cmp[i])
            {
                return false;
            }
        }
        return true;
    }
    public static UInt32 AppendString(this Byte[] buffer, UInt32 offset, String s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            buffer[offset + i] = (Byte)s[i];
        }
        return (uint)(offset + s.Length);
    }

    static void Reverse(this Byte[] buffer, UInt32 startOffset, UInt32 lastOffset)
    {
        while (startOffset < lastOffset)
        {
            Byte b = buffer[startOffset];
            buffer[startOffset] = buffer[lastOffset];
            buffer[lastOffset] = b;
            startOffset++;
            lastOffset--;
        }

    }
    public static UInt32 AppendUnsigned(this Byte[] buffer, UInt32 offset, UInt32 value)
    {
        if(value == 0)
        {
            buffer[offset] = (Byte)'0';
            return offset + 1;
        }

        UInt32 originalOffset = offset;
        while(value > 0)
        {
            buffer[offset++] = (Byte)((value % 10) + '0');
            value /= 10;
        }
        Reverse(buffer, originalOffset, offset - 1);
        return offset;
    }
    public static void ReadFullSize(this Stream stream, Byte[] buffer, Int32 offset, Int32 size)
    {
        int lastBytesRead;

        do
        {
            lastBytesRead = stream.Read(buffer, offset, size);
            size -= lastBytesRead;

            if (size <= 0) return;

            offset += lastBytesRead;
        } while (lastBytesRead > 0);

        throw new IOException(String.Format("Reached end of stream but still expected {0} bytes", size));
    }
}
enum HttpHeaderID
{
    Unknown,
    Host,
    Connection,
    CacheControl,
    Accept,
    UserAgent,
    AcceptEncoding,
    AcceptLanguage,
}
delegate void HeaderHandler(OpenHttpClient client, BytePtr at, UInt32 length);
class HttpHeader
{
    public readonly HttpHeaderID id;
    public readonly String name;
    public readonly HeaderHandler handler;
    public HttpHeader(HttpHeaderID id, String name, HeaderHandler handler)
    {
        this.id = id;
        this.name = name;
        this.handler = handler;
    }

    //
    // Static Header Configuration
    //
    public static readonly HttpHeader UnknownHeader = new HttpHeader(HttpHeaderID.Unknown, "?", HandleUnknownHeader);
    public static readonly HttpHeader[] KnownHeaderDefinitions = new HttpHeader[] {
        new HttpHeader(HttpHeaderID.Host, "Host", HandleHostHeader),
    };
    static void HandleUnknownHeader(OpenHttpClient client, BytePtr at, UInt32 length)
    {
        // ignore it for now
    }
    static void HandleHostHeader(OpenHttpClient client, BytePtr at, UInt32 length)
    {
        if (client.host != null)
        {
            throw new FormatException("Host header appeared twice!");
        }
        client.host = at.AsciiToString(length);
        FastWebServer.Logger.WriteLine("[{0}] Got Host Header '{1}'", client.logString, client.host);
    }
}
class OpenHttpClient
{
    public readonly String logString;
    public readonly Socket socket;
    public http_parser parser;
    public String url;
    public String host;
    public HttpHeader currentHeader;

    public OpenHttpClient(String logString, Socket socket)
    {
        this.logString = logString;
        this.socket = socket;
        this.parser = new http_parser();
        HttpParser.Init(ref this.parser, http_parser_type.Request);
        parser.data = GCHandle.Alloc(this);
    }
    public void RecvHandler(ref SelectControl selectControl, Socket socket, Buf safeBuffer)
    {
        Int32 bytesReceived;
        try
        {
            bytesReceived = socket.Receive(safeBuffer.array);
        }
        catch (SocketException e)
        {
            FastWebServer.Logger.WriteLine("[{0}] Closed(error={1}): {2}", logString, e.ErrorCode, e.Message);
            selectControl.RemoveReceiveSocket(socket);
            return;
        }
        if (bytesReceived <= 0)
        {
            FastWebServer.Logger.WriteLine("[{0}] Closed", logString);
            selectControl.RemoveReceiveSocket(socket);
            return;
        }

        uint parsed = HttpParser.Execute(ref parser, FastWebServer.ParserSettings, safeBuffer.array, (uint)bytesReceived);
        if (parsed != bytesReceived)
        {
            Respond(ref parser, "400 Bad Request", "", "Failed to parse http request");
        }
    }

    static UInt32 UnsignedCharLength(UInt32 value)
    {
        if (value <= 9) return 1;
        if (value <= 99) return 2;
        if (value <= 999) return 3;
        if (value <= 9999) return 4;
        if (value <= 99999) return 5;
        if (value <= 999999) return 6;
        if (value <= 9999999) return 7;
        if (value <= 99999999) return 8;
        if (value <= 999999999) return 9;
        return 10;
    }

    
    public static UInt32 CalculateHeadersLength(String httpResponse, String extraHeaders, UInt32 contentLength)
    {
        // 1. Calculate Length
        //   Http Version
        //   Space
        //   httpResponse
        //   Newline
        //   foreach header
        //     header field
        //     colon, space
        //     header value
        //     newline
        //   ExtraHeaders
        //   newline
        //   body
        //
        // In order to calculate length we need:
        //  1. Http Version Major and Minor (sizeof numbers)
        //  2. Length of ResponseCode
        //  3. Length of ResponseMessage
        //  4. Number of headers and all the header fields/values
        //  5. body length
        //
        // 2. Allocate buffer (maybe on the stack?)
        // 3. Populate Packet
        // 4. Send packet
        return (UInt32)(
            "HTTP/X.X ".Length +
            httpResponse.Length +
            "\r\n".Length +
            // No headers for now except 0 content length
            "Content-Length: ".Length +
            UnsignedCharLength(contentLength) +
            "\r\n".Length +
            extraHeaders.Length +
            "\r\n".Length);
    }

    // Assumes byte array will hold headers
    // Returns offset to the end of headers
    public static UInt32 GenerateHeaders(Byte[] responseBuffer, UInt32 offset, ref http_parser parser,
        String httpResponse, String extraHeaders, UInt32 contentLength)
    {
        offset = responseBuffer.AppendString(offset, "HTTP/");
        responseBuffer[offset++] = (Byte)('0' + parser.http_major);
        responseBuffer[offset++] = (Byte)('/');
        responseBuffer[offset++] = (Byte)('0' + parser.http_minor);
        responseBuffer[offset++] = (Byte)(' ');
        offset = responseBuffer.AppendString(offset, httpResponse);
        offset = responseBuffer.AppendString(offset, "\r\nContent-Length: ");
        offset = responseBuffer.AppendUnsigned(offset, contentLength);
        offset = responseBuffer.AppendString(offset, "\r\n");
        offset = responseBuffer.AppendString(offset, extraHeaders);
        offset = responseBuffer.AppendString(offset, "\r\n");
        return offset;
    }

    // httpResponse = Response code and message, i.e. "200 OK"
    public void Respond(ref http_parser parser, String httpResponse, String extraHeaders, String body)
    {
        UInt32 expectedHeadersLength = CalculateHeadersLength(httpResponse, extraHeaders, (uint)body.Length);

        FastWebServer.sharedResponseBuffer.EnsureCapacityNoCopy(expectedHeadersLength + (uint)body.Length);
        Byte[] responseBuffer = FastWebServer.sharedResponseBuffer.array;

        UInt32 offset = GenerateHeaders(responseBuffer, 0, ref parser, httpResponse, extraHeaders, (uint)body.Length);

        if (offset != expectedHeadersLength)
        {
            throw new InvalidOperationException(String.Format("CodeBug: Expected headers length to be {0} but was {1}", expectedHeadersLength, offset));
        }

        offset = responseBuffer.AppendString(offset, body);

        OpenHttpClient client = (OpenHttpClient)((GCHandle)parser.data).Target;
        FastWebServer.Logger.WriteLine("[{0}] {1} (TotalResponse {2} bytes)", logString, httpResponse, offset);
        client.socket.Send(responseBuffer, (int)offset, SocketFlags.None);
    }
}