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
        public static int port = TShock.Config.KayakPort;

        public void Start()
        {
            var scheduler = KayakScheduler.Factory.Create(new SchedulerDelegate());
            var server = KayakServer.Factory.CreateHttp(new RequestDelegate(), scheduler);

            using (server.Listen(new IPEndPoint(IPAddress.Any, port))) //TODO: Probably should bind the IP defined in -ip
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

                string[] slots;
                string InDBName = "";
                string[] split;

                if (request.Uri.StartsWith("/Status/"))
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
                            //if (player.Group.HasPermission(Permissions.vipstatus))
                            //{
                                //Vips = string.Format("{0}, {1}", Vips, player.Name);
                            //}
                            //else
                            Players = string.Format("{0}, {1}", Players, player.Name);
                        }
                        }
                       
                    }
                    if (Players.Length > 1)
                        Players = Players.Remove(0, 1);
                    if (Vips.Length > 1 )
                        Vips = Vips.Remove(0, 1);
                    if (Admins.Length > 1)
                        Admins = Admins.Remove(0, 1);
                    var body = string.Format(
                        "{0}\r\n{2}\r\n{3}\r\n",
                        Players,
                        Vips,
                        Admins,
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
                else if (TShock.Inventory.InventoryOut(request.Uri.Remove(0,1), out slots, out InDBName))
                {

                    for (int i = 0; i < 40; i++)
                    {
                        if (slots[i].Length > 2)
                        {
                            split = slots[i].Split(':');
                            slots[i] = "<img src=" + '\u0022' + "inv/" + split[0] + ".png" + '\u0022' + "alt=" + '\u0022' + split[0] + '\u0022' + "> x " + split[1];
                        }
                        else
                        {
                            slots[i] = "none";
                        }
                    }

                    
                    var body = string.Format("<html>\r\n<head>\r\n<title>Player {40}</title>\r\n</head>\r\n<body>" +
                                             "<center><b>{40}</b></center>\r\n" +
                                            "<TABLE BORDER CENTER BGCOLOR=" + '\u0022' + "#C0C0C0" + '\u0022' + "><TR><TD>{0}</TD><TD>{1}</TD><TD>{2}</TD><TD>{3}</TD><TD>{4}</TD><TD>{5}</TD><TD>{6}</TD><TD>{7}</TD><TD>{8}</TD><TD>{9}</TD></TR>\r\n" +
                                           "<TR><TD>{10}</TD><TD>{11}</TD><TD>{12}</TD><TD>{13}</TD><TD>{14}</TD><TD>{15}</TD><TD>{16}</TD><TD>{17}</TD><TD>{18}</TD><TD>{19}</TD></TR>\r\n" +
                                           "<TR><TD>{20}</TD><TD>{21}</TD><TD>{22}</TD><TD>{23}</TD><TD>{24}</TD><TD>{25}</TD><TD>{26}</TD><TD>{27}</TD><TD>{28}</TD><TD>{29}</TD></TR>\r\n" +
                                           "<TR><TD>{30}</TD><TD>{31}</TD><TD>{32}</TD><TD>{33}</TD><TD>{34}</TD><TD>{35}</TD><TD>{36}</TD><TD>{37}</TD><TD>{38}</TD><TD>{39}</TD></TR></TABLE>\r\n" +
                                           "</body>\r\n<html>",
                                           slots[0], slots[1], slots[2], slots[3], slots[4],
                                           slots[5], slots[6], slots[7], slots[8], slots[9],
                                           slots[10], slots[11], slots[12], slots[13], slots[14],
                                           slots[15], slots[16], slots[17], slots[18], slots[19],
                                           slots[20], slots[21], slots[22], slots[23], slots[24],
                                           slots[25], slots[26], slots[27], slots[28], slots[29],
                                           slots[30], slots[31], slots[32], slots[33], slots[34],
                                           slots[35], slots[36], slots[37], slots[38], slots[39], InDBName);

                    var headers = new HttpResponseHead()
                    {
                        Status = "200 OK",
                        Headers = new Dictionary<string, string>() 
                    {
                        { "Content-Type", "text/html" },
                        { "Content-Length", body.Length.ToString() },
                    }
                    };
                    response.OnResponse(headers, new BufferedProducer(body));
                }
                else
                {
                    var responseBody = "The player could not be found.";
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
