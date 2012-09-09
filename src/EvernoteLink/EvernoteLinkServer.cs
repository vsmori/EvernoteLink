using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Net;
using System.Web;
using Kayak;
using Kayak.Http;

namespace EvernoteLink {

    public class EvernoteLinkServer {
        public static string gENScriptPath = "";

    }

    public class SchedulerDelegate : ISchedulerDelegate {
        public void OnException(IScheduler scheduler, Exception e) {
            Debug.WriteLine("Error on scheduler.");
            e.DebugStackTrace();
        }

        public void OnStop(IScheduler scheduler) {

        }
    }

    /*
     * http://dev.evernote.com/documentation/local/chapters/Windows.php
     */
    public class RequestDelegate : IHttpRequestDelegate {
        public void OnRequest(HttpRequestHead request, IDataProducer requestBody,
            IHttpResponseDelegate response) {
            List<string> commands = new List<string> {"showNotes","SyncDatabase"};            
            NameValueCollection vars = HttpUtility.ParseQueryString(request.QueryString);
            string args = "";

            if (request.Method.ToUpperInvariant() == "GET" 
                && vars["a"] != null
                && commands.Contains(vars["a"])) {
                string res = "error";

                args += String.Format("{0} ", vars["a"]);
                vars.Remove("a");

                foreach (string arg in vars.AllKeys) {
                    args += String.Format("/{0} \"{1}\" ", arg, vars[arg]);
                }

                    try {
                        //create another instance of this process
                        ProcessStartInfo info = new ProcessStartInfo();
                        info.FileName = String.Format("\"{0}\"",EvernoteLinkServer.gENScriptPath);
                        info.Arguments = args;

                        Process.Start(info);
                        res = String.Format("{0} {1}", info.FileName, info.Arguments);
                    } catch (Exception e) {
                        res = e.Message;
                    }

                    var headers = new HttpResponseHead() {
                        Status = "200 OK",
                        Headers = new Dictionary<string, string>()
                    {
                        { "Content-Type", "text/plain" },
                        { "Content-Length", res.Length.ToString() },
                    }
                    };
                    response.OnResponse(headers, new BufferedProducer(res));
            } else {
                var responseBody = "The resource you requested ('" + request.Uri + "') could not be found.";
                var headers = new HttpResponseHead() {
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

    public class BufferedProducer : IDataProducer {
        ArraySegment<byte> data;

        public BufferedProducer(string data) : this(data, Encoding.UTF8) { }
        public BufferedProducer(string data, Encoding encoding) : this(encoding.GetBytes(data)) { }
        public BufferedProducer(byte[] data) : this(new ArraySegment<byte>(data)) { }
        public BufferedProducer(ArraySegment<byte> data) {
            this.data = data;
        }

        public IDisposable Connect(IDataConsumer channel) {
            // null continuation, consumer must swallow the data immediately.
            channel.OnData(data, null);
            channel.OnEnd();
            return null;
        }
    }

    public class BufferedConsumer : IDataConsumer {
        List<ArraySegment<byte>> buffer = new List<ArraySegment<byte>>();
        Action<string> resultCallback;
        Action<Exception> errorCallback;

        public BufferedConsumer(Action<string> resultCallback, Action<Exception> errorCallback) {
            this.resultCallback = resultCallback;
            this.errorCallback = errorCallback;
        }

        public bool OnData(ArraySegment<byte> data, Action continuation) {
            // since we're just buffering, ignore the continuation.
            // TODO: place an upper limit on the size of the buffer.
            // don't want a client to take up all the RAM on our server!
            buffer.Add(data);
            return false;
        }

        public void OnError(Exception error) {
            errorCallback(error);
        }

        public void OnEnd() {
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
