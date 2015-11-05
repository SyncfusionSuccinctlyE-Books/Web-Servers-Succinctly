using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Demo
{
	public class ListenerThreadHandler
	{
		public void Process(HttpListenerContext context)
		{
			Program.TimeStamp("Process Thread ID: " + Thread.CurrentThread.ManagedThreadId);
			CommonResponse(context);
		}

		public void CommonResponse(HttpListenerContext context)
		{
			// Artificial delay.
			Thread.Sleep(1000);

			// Get the request.
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;

			// Get the path, everything up to the first ? and excluding the leading "/"
			string path = request.RawUrl.LeftOf("?").RightOf("/");

			// Load the file and respond with a UTF8 encoded version of it.
			string text = File.ReadAllText(path);
			byte[] data = Encoding.UTF8.GetBytes(text);
			response.ContentType = "text/html";
			response.ContentLength64 = data.Length;
			response.OutputStream.Write(data, 0, data.Length);
			response.ContentEncoding = Encoding.UTF8;
			response.StatusCode = 200; // OK
			response.OutputStream.Close();
		}
	}
}
