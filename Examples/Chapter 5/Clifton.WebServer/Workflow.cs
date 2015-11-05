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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.WebServer
{
	/// <summary>
	/// The Workflow class handles a list of workflow items that we can use to 
	/// determine the processing of a request.
	/// </summary>
	public class Workflow<T>
	{
		public Action<T> AbortHandler { get; protected set; }
		public Action<T, Exception> ExceptionHandler { get; protected set; }

		protected List<WorkflowItem<T>> items;

		public Workflow(Action<T> abortHandler, Action<T, Exception> exceptionHandler)
		{
			items = new List<WorkflowItem<T>>();
			AbortHandler = abortHandler;
			ExceptionHandler = exceptionHandler;
		}

		/// <summary>
		/// Add a workflow item.
		/// </summary>
		public void AddItem(WorkflowItem<T> item)
		{
			items.Add(item);
		}

		/// <summary>
		/// Execute the workflow from the beginning.
		/// </summary>
		public void Execute(T data)
		{
			WorkflowContinuation<T> continuation = new WorkflowContinuation<T>(this);
			InternalContinue(continuation, data);
		}

		/// <summary>
		/// Continue a deferred workflow, unless it is aborted.
		/// </summary>
		public void Continue(WorkflowContinuation<T> wc, T data)
		{
			// TODO: Throw exception instead?
			if ( (!wc.Abort) && (!wc.Done) )
			{
				wc.Defer = false;
				InternalContinue(wc, data);
			}
		}

		/// <summary>
		/// Internally, we execute workflow steps until:
		/// 1. we reach the end of the workflow chain
		/// 2. we are instructed to abort the workflow
		/// 3. we are instructed to defer execution until later.
		/// </summary>
		protected void InternalContinue(WorkflowContinuation<T> wc, T data)
		{
			while ((wc.WorkflowStep < items.Count) && !wc.Abort && !wc.Defer && !wc.Done)
			{
				try
				{
					WorkflowState state = items[wc.WorkflowStep++].Execute(wc, data);

					switch (state)
					{
						case WorkflowState.Abort:
							wc.Abort = true;
							wc.Workflow.AbortHandler(data);
							break;

						case WorkflowState.Defer:
							wc.Defer = true;
							break;

						case WorkflowState.Done:
							wc.Done = true;
							break;
					}
				}
				catch (Exception ex)
				{
					// Yes, the user's exception handler could itself through an exception
					// from which we need to protect ourselves.
					try
					{
						wc.Workflow.ExceptionHandler(data, ex);
					}
					catch { /* Now what? */ }
					// TODO: Should we use a different flag, like "Exception"?  Can't be Abort, as this invokes an app-specific handler.
					wc.Done = true;
				}
			}
		}
	}
}
