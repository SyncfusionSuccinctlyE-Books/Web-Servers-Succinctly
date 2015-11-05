using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Clifton.WebServer;

namespace ServerApp
{
	public static class RouteTableExtensions
	{
		/// <summary>
		/// Add a route with session expiration checking.
		/// </summary>
		public static void AddExpirableRoute(this RouteTable routeTable, 
			string verb, 
			string path,
			Func<WorkflowContinuation<ContextWrapper>, ContextWrapper, Session, PathParams, WorkflowState> routeHandler)
		{
			routeTable.AddRoute(verb, path, new RouteEntry()
			{
				SessionExpirationHandler = (continuation, context, session, parms) => 
				{ 
					/* Your expiration check */ 
					return WorkflowState.Continue; 
				},
				RouteHandler = routeHandler,
			});
		}

		/// <summary>
		/// Add a route with session expiration and authorization checking.
		/// </summary>
		public static void AddExpirableAuthorizedRoute(this RouteTable routeTable,
			string verb,
			string path,
			Func<WorkflowContinuation<ContextWrapper>, ContextWrapper, Session, PathParams, WorkflowState> routeHandler)
		{
			routeTable.AddRoute(verb, path, new RouteEntry()
			{
				SessionExpirationHandler = (continuation, context, session, parms) =>
				{
					/* Your expiration check */
					return WorkflowState.Continue;
				},

				AuthorizationHandler = (continuation, context, session, parms) =>
				{
					/* Your authentication check */
					return WorkflowState.Continue;
				},

				RouteHandler = routeHandler,
			});
		}
	}
}
