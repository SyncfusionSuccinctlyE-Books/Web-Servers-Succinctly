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
		static Semaphore sem;

		static void Main(string[] args)
		{
			// Supports 20 simultaneous connections.
			sem = new Semaphore(20, 20);
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

			Console.WriteLine("Press a key to exit the server.");
			Console.ReadLine();
		}

		/// <summary>
		/// Await connections.
		/// </summary>
		static async void StartConnectionListener(HttpListener listener)
		{
			// Wait for a connection. Return to caller while we wait.
			HttpListenerContext context = await listener.GetContextAsync();

			// Release the semaphore so that another listener can be immediately started up.
			sem.Release();

			// Get the request.
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;

			// Get the path, everything up to the first ? and excluding the leading "/"
			string path = request.RawUrl.LeftOf("?").RightOf("/");
			Console.WriteLine(path);	// Nice to see some feedback.

			try
			{
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
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}
	}
}
