using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Sparrow.NET.Framework.Network
{
    public sealed class OurHttpListener
    {
        private readonly IList<string> _prefixes = new List<string>();
        private TcpListener? _tcp;
        private CancellationTokenSource? _cts;
        private Task? _acceptLoop;
        private readonly Channel<OurHttpListenerContext> _poolRequest =
            Channel.CreateUnbounded<OurHttpListenerContext>();

        public bool IsListening => _tcp != null;

        public void AddPrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Prefix can't be null or empty", nameof(prefix));

            _prefixes.Add(prefix);
        }

        public void Start()
        {
            if (_tcp != null)
                throw new InvalidOperationException("The listener has already started");

            if (_prefixes.Count == 0)
                throw new InvalidOperationException("No prefixes added!");

            var first = _prefixes[0];
            var uri = new Uri(first, UriKind.Absolute);

            var ip = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                ? IPAddress.Loopback
                : IPAddress.Parse(uri.Host);

            _tcp = new TcpListener(ip, uri.Port);
            _tcp.Start();

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
        }

        public async Task StopAsync()
        {
            if (_tcp == null) return;

            _cts?.Cancel();
            try { _tcp.Stop(); } catch { }

            if (_acceptLoop != null)
            {
                try { await _acceptLoop.ConfigureAwait(false); } catch { }
            }

            _tcp = null;
        }

        public ValueTask<OurHttpListenerContext> GetNewRequestAsync()
            => _poolRequest.Reader.ReadAsync();

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            if (_tcp == null) return;

            while (!ct.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _tcp.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                _ = Task.Run(() => HandleClientAsync(client, ct), ct);
            }
        }

        private static async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8, false, 8192, leaveOpen: true))
            {
                stream.ReadTimeout = 5000;
                stream.WriteTimeout = 5000;
                var requestLine = await reader.ReadLineAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(requestLine))
                    return;

                var parts = requestLine.Split(' ');
                var method = parts.Length > 0 ? parts[0] : "GET";
                var path = parts.Length > 1 ? parts[1] : "/";
                var protocol = parts.Length > 2 ? parts[2] : "HTTP/1.1";
                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string? line;
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync().ConfigureAwait(false)))
                {
                    int idx = line.IndexOf(':');
                    if (idx <= 0) continue;
                    var name = line[..idx].Trim();
                    var value = line[(idx + 1)..].Trim();
                    headers[name] = value;
                }
                byte[]? bodyBytes = null;
                if (headers.TryGetValue("Content-Length", out var lenStr) &&
                    int.TryParse(lenStr, out var len) && len > 0)
                {
                    bodyBytes = new byte[len];
                    int read = 0;
                    while (read < len)
                    {
                        int r = await stream.ReadAsync(bodyBytes, read, len - read, ct).ConfigureAwait(false);
                        if (r <= 0) break;
                        read += r;
                    }
                }

                var request = new OurHttpListenerRequest(
                    method,
                    path,
                    protocol,
                    headers,
                    bodyBytes is null ? ReadOnlyMemory<byte>.Empty : new ReadOnlyMemory<byte>(bodyBytes));

                var response = new OurHttpListenerResponse(stream);
                var ctx = new OurHttpListenerContext(request, response);
            }
        }
    }
    public sealed class OurHttpListenerContext
    {
        public OurHttpListenerRequest Request { get; }
        public OurHttpListenerResponse Response { get; }

        public OurHttpListenerContext(OurHttpListenerRequest request, OurHttpListenerResponse response)
        {
            Request = request;
            Response = response;
        }
    }

    public sealed class OurHttpListenerRequest
    {
        public string HttpMethod { get; }
        public string Path { get; }
        public string Protocol { get; }
        public IReadOnlyDictionary<string, string> Headers { get; }
        public ReadOnlyMemory<byte> Body { get; }

        public OurHttpListenerRequest(
            string httpMethod,
            string path,
            string protocol,
            IReadOnlyDictionary<string, string> headers,
            ReadOnlyMemory<byte> body)
        {
            HttpMethod = httpMethod;
            Path = path;
            Protocol = protocol;
            Headers = headers;
            Body = body;
        }
    }

    public sealed class OurHttpListenerResponse
    {
        private readonly Stream _network;
        private readonly MemoryStream _buffer = new MemoryStream();
        private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);
        private bool _closed;

        public int StatusCode { get; internal set; } = 200;

        public OurHttpListenerResponse(Stream network)
        {
            _network = network;
            _headers["Content-Type"] = "text/plain; charset=utf-8";
        }

        internal async Task CloseAsync()
        {
            if (_closed) return;
            _closed = true;

            _headers["Content-Length"] = _buffer.Length.ToString();

            var responseLine = $"HTTP/1.1 {StatusCode} OK\r\n";
            var sb = new StringBuilder(responseLine);
            foreach (var h in _headers)
                sb.AppendLine($"{h.Key}: {h.Value}");
            sb.AppendLine();

            var headerBytes = Encoding.UTF8.GetBytes(sb.ToString());
            await _network.WriteAsync(headerBytes, 0, headerBytes.Length).ConfigureAwait(false);

            _buffer.Position = 0;
            await _buffer.CopyToAsync(_network).ConfigureAwait(false);
            await _network.FlushAsync().ConfigureAwait(false);
        }

        internal void Write(string response)
        {
            if (_closed) throw new InvalidOperationException("Response already closed");
            var bytes = Encoding.UTF8.GetBytes(response);
            _buffer.Write(bytes, 0, bytes.Length);
        }

        internal void WriteJson<TModel>(TModel obj)
        {
            if (_closed) throw new InvalidOperationException("Response already closed");

            _headers["Content-Type"] = "application/json; charset=utf-8";
            var json = JsonSerializer.Serialize(obj);
            var bytes = Encoding.UTF8.GetBytes(json);
            _buffer.Write(bytes, 0, bytes.Length);
        }
    }
}
