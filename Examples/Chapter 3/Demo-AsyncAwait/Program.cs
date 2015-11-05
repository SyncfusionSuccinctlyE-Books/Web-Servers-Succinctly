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

		static Semaphore sem;
		static ListenerThreadHandler handler;

		static void Main(string[] args)
		{
			// Supports 20 simultaneous connections.
			sem = new Semaphore(20, 20);
			handler = new ListenerThreadHandler();
			HttpListener listener = new HttpListener();
			string url = "http://localhost/";
			listener.Prefixes.Add(url);
			listener.Start();

			Task.Run(() =>
			{
				while (true)
				{
					sem.WaitOne();
					StartConnectionListener(listener);
				}
			});

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

		static async void StartConnectionListener(HttpListener listener)
		{
			TimeStamp("StartConnectionListener Thread ID: " + Thread.CurrentThread.ManagedThreadId);

			// Wait for a connection. Return to caller while we wait.
			HttpListenerContext context = await listener.GetContextAsync();

			// Release the semaphore so that another listener can be immediately started up.
			sem.Release();

			handler.Process(context);
		}
	}
}
