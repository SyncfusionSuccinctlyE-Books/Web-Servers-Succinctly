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
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Clifton.Extensions;

namespace Clifton.WebServer
{
	public class SessionManager
	{
		public string CsrfTokenName { get; set; }
		public int ExpireInSeconds { get; set; }
		protected RouteTable routeTable;

		/// <summary>
		/// Track all sessions.
		/// </summary>
		protected ConcurrentDictionary<IPAddress, Session> sessionMap;

		public Session this[HttpListenerContext context]
		{
			get
			{
				return sessionMap[context.EndpointAddress()];
			}
		}

		public SessionManager(RouteTable routeTable)
		{
			this.routeTable = routeTable;
			sessionMap = new ConcurrentDictionary<IPAddress, Session>();
			CsrfTokenName = "_CSRF_";
			ExpireInSeconds = 10 * 60;
		}

		public WorkflowState Provider(WorkflowContinuation<HttpListenerContext> workflowContinuation, HttpListenerContext context)
		{
			Session session;
			IPAddress endpointAddress = context.EndpointAddress();

			if (!sessionMap.TryGetValue(endpointAddress, out session))
			{
				session = new Session(endpointAddress);
				session[CsrfTokenName] = Guid.NewGuid().ToString();
				sessionMap[endpointAddress] = session;
			}
			else
			{
				// If the session exists, set the expired flag before we update the last connection date/time.
				// Once set, stays set until explicitly cleared.
				session.Expired |= session.IsExpired(ExpireInSeconds);
			}

			session.UpdateLastConnectionTime();
			WorkflowState ret = CheckExpirationAndAuthorization(workflowContinuation, context, session);

			return ret;
		}

		public void CleanupDeadSessions(int deadAfterSeconds)
		{
			sessionMap.Values.Where(s => s.IsExpired(deadAfterSeconds)).ForEach(s => sessionMap.Remove(s.EndpointAddress));
		}

		protected WorkflowState CheckExpirationAndAuthorization(WorkflowContinuation<HttpListenerContext> workflowContinuation, HttpListenerContext context, Session session)
		{
			// Inspect the route to see if we should do session expiration and/or session authorization checks.
			WorkflowState ret = WorkflowState.Continue;
			RouteEntry entry = null;

			if (routeTable.TryGetRouteEntry(context.Verb(), context.Path(), out entry))
			{
				if (entry.SessionExpirationProvider != null)
				{
					ret = entry.SessionExpirationProvider(workflowContinuation, context, session);
				}

				if (ret == WorkflowState.Continue)
				{
					if (entry.AuthorizationProvider != null)
					{
						ret = entry.AuthorizationProvider(workflowContinuation, context, session);
					}
				}
			}

			return ret;
		}
	}
}
