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

using Clifton.Extensions;

namespace Clifton.WebServer
{
	public class Server
	{
		public static long CumulativeTime = 0;
		public static Dictionary<string, long> PageTimes = new Dictionary<string, long>();
		public static Dictionary<string, long> PageHits = new Dictionary<string, long>();
		public static long Samples = 0;

		protected IRequestHandler handler;
		protected Workflow<ContextWrapper> workflow;
		protected HttpListener listener;

		public static Dictionary<string, string> ProcessUrlDelimitedParams(string parms)
		{
			Dictionary<string, string> kvParams = new Dictionary<string, string>();
			parms.If(d => d.Length > 0, (d) => d.Split('&').ForEach(keyValue => kvParams[keyValue.LeftOf('=').ToLower()] = Uri.UnescapeDataString(keyValue.RightOf('='))));

			return kvParams;
		}

		public static Dictionary<string, object> ProcessUrlDelimitedParamsAsStringObjectPairs(string parms)
		{
			Dictionary<string, object> kvParams = new Dictionary<string, object>();
			parms.If(d => d.Length > 0, (d) => d.Split('&').ForEach(keyValue => kvParams[keyValue.LeftOf('=').ToLower()] = Uri.UnescapeDataString(keyValue.RightOf('='))));

			return kvParams;
		}

		public void Start(IRequestHandler handler, Workflow<ContextWrapper> workflow)
		{
			this.handler = handler;
			this.workflow = workflow;
			listener = new HttpListener();
			listener.Prefixes.Add("http://localhost/");
			// listener.Prefixes.Add("https://localhost:443/");
			listener.Start();

			Task.Run(() => WaitForConnection(listener));
			//for (int i = 0; i < 20; i++)
			//{
			//	IAsyncResult result = listener.BeginGetContext(new AsyncCallback(WebRequestCallback), listener);
			//}
		}

		// http://weblog.west-wind.com/posts/2005/Dec/04/Add-a-Web-Server-to-your-NET-20-app-with-a-few-lines-of-code
		protected void WebRequestCallback(IAsyncResult result)
		{
			// Get out the context object
			HttpListenerContext context = listener.EndGetContext(result);
			// *** Immediately set up the next context
			listener.BeginGetContext(new AsyncCallback(WebRequestCallback), listener);
			ContextWrapper contextWrapper = new ContextWrapper(context);

			// Create a local workflow instance associated with the workflow for this request.
			workflow.Execute(contextWrapper);
		}

		protected void WaitForConnection(object objListener)
		{
			HttpListener listener = (HttpListener)objListener;

			while (true)
			{
				// Wait for a connection.  Return to caller while we wait.
				HttpListenerContext context = listener.GetContext();
				ContextWrapper contextWrapper = new ContextWrapper(context);

				// Create a local workflow instance associated with the workflow for this request.
				workflow.Execute(contextWrapper);
			}
		}
	}
}
