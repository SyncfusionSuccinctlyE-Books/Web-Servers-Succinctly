using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Demo
{
	public class SingleThreadedQueueingHandler
	{
		protected ConcurrentQueue<HttpListenerContext> requests;
		protected Semaphore semQueue;
		protected List<ThreadSemaphore> threadPool;
		protected const int MAX_WORKER_THREADS = 20;

		public SingleThreadedQueueingHandler()
		{
			threadPool = new List<ThreadSemaphore>();
			requests = new ConcurrentQueue<HttpListenerContext>();
			semQueue = new Semaphore(0, Int32.MaxValue);
			StartThreads();
			MonitorQueue();
		}

		protected void MonitorQueue()
		{
			Task.Run(() =>
			{
				int threadIdx = 0;

				// Forever...
				while (true)
				{
					// Wait until we have received a context.
					semQueue.WaitOne();
					HttpListenerContext context;

					if (requests.TryDequeue(out context))
					{
						// In a round-robin manner, queue up the request on the current
						// thread index then increment the index.
						threadPool[threadIdx].Enqueue(context);
						threadIdx = (threadIdx + 1) % MAX_WORKER_THREADS;
					}
				}
			});
		}

		/// <summary>
		/// Enqueue the received context rather than processing it.
		/// </summary>
		public void Process(HttpListenerContext context)
		{
			requests.Enqueue(context);
			semQueue.Release();
		}

		/// <summary>
		/// Start our worker threads.
		/// </summary>
		protected void StartThreads()
		{
			for (int i = 0; i < MAX_WORKER_THREADS; i++)
			{
				Thread thread = new Thread(new ParameterizedThreadStart(ProcessThread));
				thread.IsBackground = true;
				ThreadSemaphore ts = new ThreadSemaphore();
				threadPool.Add(ts);
				thread.Start(ts);
			}
		}

		/// <summary>
		/// As a thread, we wait until there's something to do.
		/// </summary>
		protected void ProcessThread(object state)
		{
			ThreadSemaphore ts = (ThreadSemaphore)state;

			while (true)
			{
				ts.WaitOne();
				HttpListenerContext context;

				if (ts.TryDequeue(out context))
				{
					Program.TimeStamp("Processing on thread " + Thread.CurrentThread.ManagedThreadId);
					CommonResponse(context);
				}
			}
		}

		protected void CommonResponse(HttpListenerContext context)
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
