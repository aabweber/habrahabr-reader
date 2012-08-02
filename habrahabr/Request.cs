using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Threading;

namespace habrahabr
{
    class RequestTimeout
    {
        private DispatcherTimer timer;
        private HttpWebRequest wr;
        public RequestTimeout(HttpWebRequest wr_)
        {
            wr = wr_;
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 10, 0);
            timer.Tick += delegate(object s, EventArgs args)
            {
                timer.Stop();
                if (wr.UserAgent != "dispatched")
                {
                    wr.UserAgent = "aborted_by_timeout";
                    wr.Abort();
                }
            };
            timer.Start();
        }
    }

    static public class Request
    {
        public static HttpWebRequest New(string url, RequestDelegate requestDelegate = null, string user_agent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.19 (KHTML, like Gecko) Chrome/18.0.1025.142 Safari/535.19", bool ignore_http_status = false)
        {
            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(new Uri(url, UriKind.Absolute));
            wr.Headers["User-Agent"] = user_agent;
            
            new RequestTimeout(wr);

            wr.BeginGetResponse(new AsyncCallback((IAsyncResult asynchronousResult) =>
            {
                HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;
                bool aborted_by_timeout = request.UserAgent == "aborted_by_timeout";
                request.UserAgent = "dispatched";
                try
                {
                    bool cancelled = false;
                    HttpWebResponse response = null;
                    try
                    {
                        response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
                    }
                    catch (WebException e)
                    {
                        if (e.Status == WebExceptionStatus.RequestCanceled)
                        {
                            cancelled = true;
                        }
                        else
                        {
                            requestDelegate(null, null);
                        }
                    }
                    if (aborted_by_timeout)
                    {
                        requestDelegate(null, null);
                    }else if (!cancelled)
                    {
                        if (ignore_http_status || response.StatusCode == HttpStatusCode.OK)
                        {
                            Stream dataStream = response.GetResponseStream();
                            StreamReader reader = new StreamReader(dataStream, true);//new Windows1251Encoding());
                            string responseFromServer = reader.ReadToEnd();
                            reader.Close();
                            dataStream.Close();
                            response.Close();
                            if (requestDelegate != null)
                            {
                                requestDelegate(responseFromServer, response.Headers);
                            }
                        }
                        else
                        {
                            requestDelegate(null, null);
                        }
                    }
                }
                catch(Exception e)
                {
                }
            }), wr);
            return wr;
        }
    }
    
    
    public delegate void RequestDelegate(string content, WebHeaderCollection header);
}
