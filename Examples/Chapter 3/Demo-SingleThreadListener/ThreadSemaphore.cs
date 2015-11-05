using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Demo
{
	/// <summary>
	/// Track the semaphore and context queue associated with a worker thread.
	/// </summary>
	public class ThreadSemaphore
	{
		public int QueueCount { get { return requests.Count; } }

		protected Semaphore sem;
		protected ConcurrentQueue<HttpListenerContext> requests;

		public ThreadSemaphore()
		{
			sem = new Semaphore(0, Int32.MaxValue);
			requests = new ConcurrentQueue<HttpListenerContext>();
		}

		/// <summary>
		/// Enqueue a request context and release the semaphore that
		/// a thread is waiting on.
		/// </summary>
		public void Enqueue(HttpListenerContext context)
		{
			requests.Enqueue(context);
			sem.Release();
		}

		/// <summary>
		/// Wait for the semaphore to be released.
		/// </summary>
		public void WaitOne()
		{
			sem.WaitOne();
		}

		/// <summary>
		/// Dequeue a request.
		/// </summary>
		public bool TryDequeue(out HttpListenerContext context)
		{
			return requests.TryDequeue(out context);
		}
	}
}
