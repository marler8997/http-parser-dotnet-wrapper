using System;
using System.Collections.Generic;
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
    public static unsafe http_parser_settings* globalParserSettings;
    static Buf globalSafeBuffer;

    public static void Log(String message)
    {
        Console.WriteLine(message);
        Console.Out.Flush();
    }
    public static void Log(String fmt, params Object[] obj)
    {
        Console.WriteLine(fmt, obj);
        Console.Out.Flush();
    }


    static void Main(string[] args)
    {
        //
        // Configuration
        //
        Int32 listenBacklog = 32;
        List<EndPoint> listenEndPoints = new List<EndPoint>();
        listenEndPoints.Add(new IPEndPoint(IPAddress.Any, 81));


        //
        // Setup Parser Settings
        //
        http_parser_settings parserSettings = new http_parser_settings();
        unsafe
        {
            globalParserSettings = &parserSettings;
            HttpParser.SettingsInit(globalParserSettings);

            HttpParser.SetCallback(globalParserSettings, CallbackID.MessageBegin    , OnMessageBegin);
            HttpParser.SetCallback(globalParserSettings, DataCallbackID.Url         , OnUrl);
            HttpParser.SetCallback(globalParserSettings, DataCallbackID.HeaderField , OnHeaderField);
            HttpParser.SetCallback(globalParserSettings, DataCallbackID.HeaderValue , OnHeaderValue);
            HttpParser.SetCallback(globalParserSettings, CallbackID.HeadersComplete , OnHeadersComplete);
            HttpParser.SetCallback(globalParserSettings, DataCallbackID.Body        , OnBody);
            HttpParser.SetCallback(globalParserSettings, CallbackID.MessageComplete , OnMessageComplete);
            HttpParser.SetCallback(globalParserSettings, CallbackID.ChunkHeader     , OnChunkHeader);
            HttpParser.SetCallback(globalParserSettings, CallbackID.ChunkComplete   , OnChunkComplete);
        }

        //
        // Setup/Run Server
        //
        globalSafeBuffer = new Buf(1024, 1024);
        SelectServer server = new SelectServer(false, globalSafeBuffer);

        foreach (var endpoint in listenEndPoints)
        {
            Socket listenSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(endpoint);
            listenSocket.Listen(listenBacklog);
            FastWebServer.Log("Listening on {0}", endpoint);
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
        FastWebServer.Log("[{0}] NewClient", newSocketLogString);
        selectControl.AddReceiveSocket(newSocket, new OpenHttpClient(newSocketLogString).RecvHandler);
    }



    //
    // Http Parser Callbacks
    //
    static unsafe int OnMessageBegin(http_parser* parser)
    {
        /*
        OpenHttpClient client = (OpenHttpClient)((GCHandle)(parser->data)).Target;
        Console.WriteLine("[{0}] OnMessageBegin", client.logString);
         */
        return 0;
    }
    static unsafe int OnUrl(http_parser* parser, Byte* at, UInt32 length)
    {
        OpenHttpClient client = (OpenHttpClient)((GCHandle)(parser->data)).Target;

        char* stackBuffer = stackalloc char[(int)length];
        client.url = Extensions.NewString(at, length, stackBuffer);

        Console.WriteLine("[{0}] {1} {2}", client.logString, (HttpMethod)parser->method, client.url);

        return 0;
    }
    static unsafe int OnHeaderField(http_parser* parser, Byte* at, UInt32 length)
    {
        OpenHttpClient client = (OpenHttpClient)((GCHandle)(parser->data)).Target;


        // Find header
        client.currentHeader = HttpHeader.UnknownHeader;
        for (int i = 0; i < HttpHeader.Headers.Length; i++)
        {
            HttpHeader header = HttpHeader.Headers[i];
            if(length == header.name.Length)
            {
                Boolean match = true;
                for (int cmp = 0; cmp < length; cmp++)
                {
                    if ((Char)at[cmp] != header.name[cmp])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    client.currentHeader = header;
                    break;
                }
            }
        }

        Console.Write("[{0}] [HeaderField] ({1}) \"", client.logString,
            (client.currentHeader == HttpHeader.UnknownHeader) ? "Unknown" : "Known");
        for (int i = 0; i < length; i++)
        {
            Console.Write((Char)at[i]);
        }
        Console.WriteLine("\"");

        return 0;
    }
    static unsafe int OnHeaderValue(http_parser* parser, Byte* at, UInt32 length)
    {
        OpenHttpClient client = (OpenHttpClient)((GCHandle)(parser->data)).Target;

        Console.Write("[{0}] [HeaderValue] \"", client.logString);
        for (int i = 0; i < length; i++)
        {
            Console.Write((Char)at[i]);
        }
        Console.WriteLine("\"");
        client.currentHeader.handler(client, at, length);

        return 0;
    }
    static unsafe int OnHeadersComplete(http_parser* parser)
    {
        OpenHttpClient client = (OpenHttpClient)((GCHandle)(parser->data)).Target;

        Console.WriteLine("[{0}] HeadersComplete", client.logString);
        return 0;
    }
    static unsafe int OnBody(http_parser* parser, Byte* at, UInt32 length)
    {
        OpenHttpClient client = (OpenHttpClient)((GCHandle)(parser->data)).Target;

        Console.WriteLine("[{0}] OnBody", client.logString);
        return 0;
    }
    static unsafe int OnMessageComplete(http_parser* parser)
    {
        OpenHttpClient client = (OpenHttpClient)((GCHandle)(parser->data)).Target;

        Console.WriteLine("[{0}] MessageComplete", client.logString);


        return 0;
    }
    static unsafe int OnChunkHeader(http_parser* parser)
    {
        OpenHttpClient client = (OpenHttpClient)((GCHandle)(parser->data)).Target;

        Console.WriteLine("[{0}] OnChunkHeader", client.logString);
        return 0;
    }
    static unsafe int OnChunkComplete(http_parser* parser)
    {
        OpenHttpClient client = (OpenHttpClient)((GCHandle)(parser->data)).Target;

        Console.WriteLine("[{0}] OnChunkComplete", client.logString);
        return 0;
    }
}
public static class Extensions
{
    public static unsafe String NewString(Byte* str, UInt32 length, char* stackBuffer)
    {
        HttpParser.AsciiToUtf16(str, stackBuffer, length);
        return new String(stackBuffer, 0, (int)length);
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
unsafe delegate void HeaderHandler(OpenHttpClient client, Byte* at, UInt32 length);
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
    public static unsafe readonly HttpHeader UnknownHeader = new HttpHeader(HttpHeaderID.Unknown, "?", HandleUnknownHeader);
    public static unsafe readonly HttpHeader[] Headers = new HttpHeader[] {
        new HttpHeader(HttpHeaderID.Host, "Host", HandleHostHeader),
    };
    static unsafe void HandleUnknownHeader(OpenHttpClient client, Byte* at, UInt32 length)
    {
        // ignore it for now
    }
    static unsafe void HandleHostHeader(OpenHttpClient client, Byte* at, UInt32 length)
    {
        if (client.host != null)
        {
            throw new FormatException("Host header appeared twice!");
        }
        char* stackBuffer = stackalloc char[(int)length];
        client.host = Extensions.NewString(at, length, stackBuffer);
        FastWebServer.Log("[{0}] Got Host Header '{1}'", client.logString, client.host);
    }
}
class OpenHttpClient
{
    public readonly String logString;
    public http_parser parser;
    public String url;
    public String host;
    public HttpHeader currentHeader;

    public unsafe OpenHttpClient(String logString)
    {
        this.logString = logString;
        this.parser = new http_parser();
        fixed(http_parser* parserPtr = &parser)
        {
            HttpParser.Init(parserPtr, http_parser_type.Request);
        }
        parser.data = (IntPtr)GCHandle.Alloc(this);
    }
    public unsafe void RecvHandler(ref SelectControl selectControl, Socket socket, Buf safeBuffer)
    {
        Int32 bytesReceived = socket.Receive(safeBuffer.array);
        if (bytesReceived <= 0)
        {
            FastWebServer.Log("[{0}] Closed", logString);
            selectControl.RemoveReceiveSocket(socket);
            return;
        }

        fixed (http_parser* parserPtr = &parser)
        {
            uint parsed = HttpParser.Execute(parserPtr, FastWebServer.globalParserSettings, safeBuffer.array, (uint)bytesReceived);
            if (parsed != bytesReceived)
            {
                throw new NotImplementedException(String.Format(
                    "Only parsed {0} bytes out of {1}, not implemented", parsed, bytesReceived));
            }
        }
    }
}