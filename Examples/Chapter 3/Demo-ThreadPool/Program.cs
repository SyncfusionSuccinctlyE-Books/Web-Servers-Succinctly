using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Demo
{
	class Program
	{
		protected static DateTime timestampStart;

		static public void TimeStampStart()
		{
			timestampStart = DateTime.Now;
		}

		static public void TimeStamp(string msg)
		{
			long elapsed = (long)(DateTime.Now - timestampStart).TotalMilliseconds;
			Console.WriteLine("{0} : {1}", elapsed, msg);
		}

		static ListenerThreadHandler handler;

		static void Main(string[] args)
		{
			// Supports 20 simultaneous connections.
			handler = new ListenerThreadHandler();
			HttpListener listener = new HttpListener();
			string url = "http://localhost/";
			listener.Prefixes.Add(url);
			listener.Start();

			for (int i = 0; i < 20; i++)
			{
				ThreadPool.QueueUserWorkItem(WaitForConnection, listener);
			}

			TimeStampStart();

			for (int i = 0; i < 10; i++)
			{
				Console.WriteLine("Request #" + i);
				MakeRequest(i);
			}

			Console.WriteLine("Press a key to exit the server.");
			Console.ReadLine();
		}

		/// <summary>
		/// Issue GET request to localhost/index.html
		/// </summary>
		static async void MakeRequest(int i)
		{
			TimeStamp("MakeRequest " + i + " start, Thread ID: " + Thread.CurrentThread.ManagedThreadId);
			string ret = await RequestIssuer.HttpGet("http://localhost/index.html");
			TimeStamp("MakeRequest " + i + " end, Thread ID: " + Thread.CurrentThread.ManagedThreadId);
		}

		/// <summary>
		/// Block until a connection is received.
		/// </summary>
		static void WaitForConnection(object objListener)
		{
			HttpListener listener = (HttpListener)objListener;

			while (true)
			{
				TimeStamp("StartConnectionListener Thread ID: " + Thread.CurrentThread.ManagedThreadId);
				HttpListenerContext context = listener.GetContext();
				handler.Process(context);
			}
		}
	}
}
