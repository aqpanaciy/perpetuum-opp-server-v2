using Newtonsoft.Json;
using System.Text;
using System.Threading.Channels;

namespace Perpetuum.Log.Loggers
{
    /// <summary>
    /// Logger to grafana loki
    /// </summary>
    public sealed class LokiLogger : ILogger<LogEvent>, IDisposable
    {
        private const string MediaType = "application/json";
        /// <summary>
        /// File system path for internal logger messages
        /// </summary>
        private readonly string _fallbackPath;
        /// <summary>
        /// URL for transmitting logger messages
        /// </summary>
        private readonly string _lokiURL;

        /// <summary>
        /// Channel for recording events. The logger stops when this channel is closed.
        /// </summary>
        private readonly Channel<LogEvent> _channel = Channel.CreateBounded<LogEvent>(1000);
        /// <summary>
        /// The cancellation token is used only for abnormal termination.
        /// </summary>
        private readonly CancellationTokenSource _source = new CancellationTokenSource();
        private readonly HttpClient _client = new HttpClient();
        private readonly Task _task;
        private readonly ILogEventFormatter<LogEvent, string> _formatter;

        public LokiLogger(ILogEventFormatter<LogEvent, string> formatter, string lokiURL, string fallbackPath)
        {
            _formatter = formatter;
            _task = Process(_source.Token);
            _fallbackPath = fallbackPath;
            _lokiURL = lokiURL;
            // Test log message to fallback path
            SafeFallbackWrite("logger start");
        }

        /// <summary>
        /// Processes the log events in the channel and sends them to Loki. If an error occurs, it writes the error message to the fallback file system path.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task Process(CancellationToken token)
        {
            try
            {
                while (await _channel.Reader.WaitToReadAsync(token))
                {
                    while (_channel.Reader.TryRead(out LogEvent logEvent))
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        long timeStampNs = new DateTimeOffset(logEvent.Timestamp).ToUnixTimeMilliseconds() * 1_000_000;

                        var message = _formatter.Format(logEvent);

                        var payload = new
                        {
                            streams = new[]
                            {
                                new
                                {
                                    stream = new
                                    {
                                        service_name = "Perpetuum.Server.V2",
                                        level = GetLelev(logEvent)
                                    },
                                    values = new[]
                                    {
                                        new[] { timeStampNs.ToString(), message }
                                    }
                                }
                            }
                        };

                        await Post(payload, token);
                    }
                }
            }
            catch (Exception ex)
            {
                SafeFallbackWrite(ex.Message);
            }
        }

        private async Task Post(object payload, CancellationToken token)
        {
            const int maxRetries = 3;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(payload);

                    using var content = new StringContent(json, Encoding.UTF8, MediaType);
                    using var response = await _client.PostAsync(_lokiURL, content, token);

                    response.EnsureSuccessStatusCode();
                    break;
                }
                catch (Exception ex) when (i < maxRetries - 1)
                {
                    SafeFallbackWrite(ex.Message);
                    await Task.Delay(200 * (i + 1), token);
                }
                catch (Exception ex)
                {
                    SafeFallbackWrite(ex.Message);
                }
            }
        }

        /// <summary>
        /// Gets the log level based on the log event type.
        /// </summary>
        /// <param name="logEvent">The log event.</param>
        /// <returns>The log level.</returns>
        private static int GetLelev(LogEvent logEvent)
        {
            int level = 0;
            switch (logEvent.LogType)
            {
                case LogType.Warning:
                    level = 4;
                    break;
                case LogType.Error:
                    if (logEvent.ThrownException is null)
                    {
                        level = 3;
                    }
                    else
                    {
                        level = 2;
                    }
                    break;
                default:
                    level = 5;
                    break;
            }

            return level;
        }

        public void Dispose()
        {
            _channel.Writer.Complete();
            _task?.Wait(5000, CancellationToken.None);
            _source.Cancel();
            _task?.Wait(CancellationToken.None);
            _source.Dispose();
            _client.Dispose();
        }

        /// <summary>
        /// Logs the specified log event.
        /// </summary>
        /// <param name="logEvent"></param>
        public void Log(LogEvent logEvent)
        {
            _channel.Writer.TryWrite(logEvent);
        }

        /// <summary>
        /// Writes a log message to the fallback file system path. This method is used when the logger cannot send messages to Loki.
        /// </summary>
        /// <param name="message">The log message.</param>
        private void SafeFallbackWrite(string message)
        {
            try
            {
                var line = $"{DateTime.UtcNow:O} | LOKI | {message}\n";

                Directory.CreateDirectory(Path.GetDirectoryName(_fallbackPath));
                File.AppendAllText(_fallbackPath, line);
            }
            catch
            {
                // This logger must not crash.
            }
        }
    }
}
