/*
Copyright (c) 2015, Marc Clifton
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list
  of conditions and the following disclaimer. 

* Redistributions in binary form must reproduce the above copyright notice, this 
  list of conditions and the following disclaimer in the documentation and/or other
  materials provided with the distribution. 
 
* Neither the name of MyXaml nor the names of its contributors may be
  used to endorse or promote products derived from this software without specific
  prior written permission. 

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Clifton.WebServer
{
	public class ThreadSemaphore
	{
		public int ThreadNumber { get; set; }
		public int QueueCount { get { return requests.Count; } }
		protected Semaphore sem;

		// Requests on this thread.
		protected ConcurrentQueue<WorkflowContext> requests;

		public ThreadSemaphore()
		{
			sem = new Semaphore(0, Int32.MaxValue);
			requests = new ConcurrentQueue<WorkflowContext>();
		}

		public void Enqueue(WorkflowContext context)
		{
			requests.Enqueue(context);
			sem.Release();
		}

		public void WaitOne()
		{
			sem.WaitOne();
		}

		public bool TryDequeue(out WorkflowContext context)
		{
			return requests.TryDequeue(out context);
		}
	}

	public class WorkflowContext
	{
		protected WorkflowContinuation<ContextWrapper> workflowContinuation;
		protected ContextWrapper context;

		public WorkflowContinuation<ContextWrapper> WorkflowContinuation { get { return workflowContinuation; } }
		public ContextWrapper Context { get { return context; } }

		public WorkflowContext(WorkflowContinuation<ContextWrapper> workflowContinuation, ContextWrapper context)
		{
			this.workflowContinuation = workflowContinuation;
			this.context = context;
		}
	}

	public class SingleThreadedQueueingHandler : IRequestHandler
	{
		protected ConcurrentQueue<WorkflowContext> requests;
		protected Semaphore semQueue;
		protected List<ThreadSemaphore> threadPool;
		protected const int MAX_WORKER_THREADS = 20;

		public SingleThreadedQueueingHandler()
		{
			threadPool = new List<ThreadSemaphore>();
			requests = new ConcurrentQueue<WorkflowContext>();
			semQueue = new Semaphore(0, Int32.MaxValue);
			StartThreads();

			// We'll use task to check on our queue in a separate thread.
			Task.Run(() =>
				{
					int threadIdx = 0;

					while (true)
					{
						semQueue.WaitOne();
						WorkflowContext context;

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
		/// A workflow item implementing the ContextWrapper handler.
		/// </summary>
		public WorkflowState Process(WorkflowContinuation<ContextWrapper> workflowContinuation, ContextWrapper context)
		{
			// Create a workflow context and queue it.
			requests.Enqueue(new WorkflowContext(workflowContinuation, context));
			semQueue.Release();

			return WorkflowState.Defer;
		}

		/// <summary>
		/// Initialize our worker threads.
		/// </summary>
		protected void StartThreads()
		{
			for (int i = 0; i < MAX_WORKER_THREADS; i++)
			{
				Thread thread = new Thread(new ParameterizedThreadStart(ProcessRequestThread));
				thread.IsBackground = true;
				ThreadSemaphore ts = new ThreadSemaphore() { ThreadNumber = i };
				threadPool.Add(ts);
				thread.Start(ts);
			}
		}

		/// <summary>
		/// A thread method that waits for work on its queue and then processes that work via
		/// the context's workflow engine.
		/// </summary>
		protected void ProcessRequestThread(object state)
		{
			ThreadSemaphore ts = (ThreadSemaphore)state;
			
			while (true)
			{
				ts.WaitOne();
				WorkflowContext context;

				if (ts.TryDequeue(out context))
				{
					// Wait until we exit the workflow internal continue from a defering state.
					while (!context.WorkflowContinuation.Deferred) Thread.Sleep(0);
					// Continue with where we left off for this context's workflow.
					context.WorkflowContinuation.Workflow.Continue(context.WorkflowContinuation, context.Context);
				}
			}
		}
	}
}
