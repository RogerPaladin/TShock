using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Kayak;
using Kayak.Http;
namespace TShockAPI.Kayak
{
    class KayakBase
    {
        public static int port = 8080;

        public void Start()
        {
            var scheduler = KayakScheduler.Factory.Create(new SchedulerDelegate());
            var server = KayakServer.Factory.CreateHttp(new RequestDelegate(), scheduler);

            using (server.Listen(new IPEndPoint(IPAddress.Any, 8080))) //TODO: Probably should bind the IP defined in -ip
            {
                scheduler.Start();
            }
        
        }

        class SchedulerDelegate : ISchedulerDelegate
        {
            public void OnException(IScheduler scheduler, Exception e)
            {
                Console.WriteLine("Error on scheduler.");
                e.DebugStackTrace();
            }

            public void OnStop(IScheduler scheduler)
            {
                
            }
        }

        class RequestDelegate : IHttpRequestDelegate
        {
            public void OnRequest(HttpRequestHead request, IDataProducer requestBody,
                IHttpResponseDelegate response)
            {

                if (request.Uri.StartsWith("/"))
                {
                    int count = 0;  
                    string Admins = String.Empty;
                    string Players = String.Empty;
                    string Vips = String.Empty;
                    foreach (TSPlayer player in TShock.Players)
                      {
                        if (player != null && player.Active)
                        {
                      count++;
                      if (player.Group.HasPermission(Permissions.adminstatus))
                      {
                            Admins = string.Format("{0}, {1}", Admins, player.Name);
                      }
                        else
                        {
                            if (player.Group.HasPermission(Permissions.vipstatus))
                            {
                                Vips = string.Format("{0}, {1}", Vips, player.Name);
                            }
                            else
                            Players = string.Format("{0}, {1}", Players, player.Name);
                        }
                        }
                       
                    }
                    var body = string.Format(
                        "Players: {0}\r\nVips: {1}\r\nAdmins: {2}\r\nTotal online players: {3}\r\n",
                        Players.Remove(0, 1),
                        Vips.Remove(0, 1),
                        Admins.Remove(0, 1),
                        count);

                    var headers = new HttpResponseHead()
                    {
                        Status = "200 OK",
                        Headers = new Dictionary<string, string>() 
                    {
                        { "Content-Type", "text/plain" },
                        { "Content-Length", body.Length.ToString() },
                    }
                    };
                    response.OnResponse(headers, new BufferedProducer(body));
                }
                else if (request.Uri.StartsWith("/bufferedecho"))
                {
                    // when you subecribe to the request body before calling OnResponse,
                    // the server will automatically send 100-continue if the client is 
                    // expecting it.
                    requestBody.Connect(new BufferedConsumer(bufferedBody =>
                    {
                        var headers = new HttpResponseHead()
                        {
                            Status = "200 OK",
                            Headers = new Dictionary<string, string>() 
                                {
                                    { "Content-Type", "text/plain" },
                                    { "Content-Length", request.Headers["Content-Length"] },
                                    { "Connection", "close" }
                                }
                        };
                        response.OnResponse(headers, new BufferedProducer(bufferedBody));
                    }, error =>
                    {
                        // XXX
                        // uh oh, what happens?
                    }));
                }
                else if (request.Uri.StartsWith("/echo"))
                {
                    var headers = new HttpResponseHead()
                    {
                        Status = "200 OK",
                        Headers = new Dictionary<string, string>() 
                        {
                            { "Content-Type", "text/plain" },
                            { "Content-Length", request.Headers["Content-Length"] },
                            { "Connection", "close" }
                        }
                    };

                    // if you call OnResponse before subscribing to the request body,
                    // 100-continue will not be sent before the response is sent.
                    // per rfc2616 this response must have a 'final' status code,
                    // but the server does not enforce it.
                    response.OnResponse(headers, requestBody);
                }
                else
                {
                    var responseBody = "The resource you requested ('" + request.Uri + "') could not be found.";
                    var headers = new HttpResponseHead()
                    {
                        Status = "404 Not Found",
                        Headers = new Dictionary<string, string>()
                    {
                        { "Content-Type", "text/plain" },
                        { "Content-Length", responseBody.Length.ToString() }
                    }
                    };
                    var body = new BufferedProducer(responseBody);

                    response.OnResponse(headers, body);
                }
            }
        }

        class BufferedProducer : IDataProducer
        {
            ArraySegment<byte> data;

            public BufferedProducer(string data) : this(data, Encoding.UTF8) { }
            public BufferedProducer(string data, Encoding encoding) : this(encoding.GetBytes(data)) { }
            public BufferedProducer(byte[] data) : this(new ArraySegment<byte>(data)) { }
            public BufferedProducer(ArraySegment<byte> data)
            {
                this.data = data;
            }

            public IDisposable Connect(IDataConsumer channel)
            {
                // null continuation, consumer must swallow the data immediately.
                channel.OnData(data, null);
                channel.OnEnd();
                return null;
            }
        }

        class BufferedConsumer : IDataConsumer
        {
            List<ArraySegment<byte>> buffer = new List<ArraySegment<byte>>();
            Action<string> resultCallback;
            Action<Exception> errorCallback;

            public BufferedConsumer(Action<string> resultCallback,
        Action<Exception> errorCallback)
            {
                this.resultCallback = resultCallback;
                this.errorCallback = errorCallback;
            }
            public bool OnData(ArraySegment<byte> data, Action continuation)
            {
                // since we're just buffering, ignore the continuation. 
                // TODO: place an upper limit on the size of the buffer. 
                // don't want a client to take up all the RAM on our server! 
                buffer.Add(data);
                return false;
            }
            public void OnError(Exception error)
            {
                errorCallback(error);
            }

            public void OnEnd()
            {
                // turn the buffer into a string. 
                // 
                // (if this isn't what you want, you could skip 
                // this step and make the result callback accept 
                // List<ArraySegment<byte>> or whatever) 
                // 
                var str = buffer
                    .Select(b => Encoding.UTF8.GetString(b.Array, b.Offset, b.Count))
                    .Aggregate((result, next) => result + next);

                resultCallback(str);
            }
        } 
    }
}
