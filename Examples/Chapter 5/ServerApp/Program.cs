/*
Copyright (c) 2015, BMBFA
All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Clifton.Extensions;
using Clifton.WebServer;

namespace WorkflowHandler
{
	public class Program
	{
		public static IRequestHandler requestHandler;
		public static Workflow<HttpListenerContext> workflow;
		public static RouteHandler routeHandler;
		public static RouteTable routeTable;

		/// <summary>
		/// A workflow item, implementing a simple instrumentation of the client IP address, port, and URL.
		/// </summary>
		public static WorkflowState LogIPAddress(WorkflowContinuation<HttpListenerContext> workflowContinuation, HttpListenerContext context)
		{
			Console.WriteLine(context.Request.RemoteEndPoint.ToString() + " : " + context.Request.RawUrl);

			return WorkflowState.Continue;
		}

		/// <summary>
		/// Only intranet IP addresses are allowed.
		/// </summary>
		public static WorkflowState WhiteList(WorkflowContinuation<HttpListenerContext> workflowContinuation, HttpListenerContext context)
		{
			string url = context.Request.RemoteEndPoint.ToString();
			bool valid = url.StartsWith("192.168") || url.StartsWith("127.0.0.1") || url.StartsWith("[::1]");
			WorkflowState ret = valid ? WorkflowState.Continue : WorkflowState.Abort;

			return ret;
		}

		public static void Main(string[] args)
		{
			requestHandler = new SingleThreadedQueueingHandler();
			string websitePath = GetWebsitePath();
			routeTable = InitializeRouteTable();
			routeHandler = new RouteHandler(routeTable);
			InitializeWorkflow(websitePath);
			Server server = new Server();
			server.Start(requestHandler, workflow);

			Console.WriteLine("Press a key to exit the server.");
			Console.ReadLine();
		}

		public static RouteTable InitializeRouteTable()
		{
			RouteTable routeTable = new RouteTable();

			routeTable.AddRoute("get", "restricted", new RouteEntry() 
				{ 
					RouteProvider = (continuation, context) => 
					{
						throw new ApplicationException("You can't do that."); 
					} 
				});

			return routeTable;
		}

		public static bool GetBooleanState(Dictionary<string, string> parms, string key, bool defaultValue)
		{
			bool ret = defaultValue;
			string val;

			if (parms.TryGetValue(key.ToLower(), out val))
			{
				switch(val.ToLower())
				{
					case "false":
					case "no":
					case "off":
						ret = false;
						break;

					case "true":
					case "yes":
					case "on":
						ret = true;
						break;
				}
			}

			return ret;
		}

		public static void InitializeWorkflow(string websitePath)
		{
			StaticContentLoader sph = new StaticContentLoader(websitePath);
			workflow = new Workflow<HttpListenerContext>(AbortHandler, OnException);
			workflow.AddItem(new WorkflowItem<HttpListenerContext>(LogIPAddress));
			workflow.AddItem(new WorkflowItem<HttpListenerContext>(WhiteList));
			workflow.AddItem(new WorkflowItem<HttpListenerContext>(requestHandler.Process));
			workflow.AddItem(new WorkflowItem<HttpListenerContext>(routeHandler.Route));
			workflow.AddItem(new WorkflowItem<HttpListenerContext>(sph.GetContent));
		}

		public static string GetWebsitePath()
		{
			// Path of our exe.
			string websitePath = Assembly.GetExecutingAssembly().Location;

			if (websitePath.Contains("\\bin\\Debug"))
			{
				// TODO: Fixup for running out of bin\Debug.  This is rather kludgy!
				websitePath = websitePath.LeftOfRightmostOf("\\bin\\Debug\\" + websitePath.RightOfRightmostOf("\\")) + "\\Website";
			}
			else
			{
				// Remove app name and replace with Webiste
				websitePath = websitePath.LeftOfRightmostOf("\\") + "\\Website";
			}

			Console.WriteLine("Website path = '" + websitePath + "'");

			return websitePath;
		}

		static void AbortHandler(HttpListenerContext context)
		{
			HttpListenerResponse response = context.Response;
			response.OutputStream.Close();
		}

		static void OnException(HttpListenerContext context, Exception ex)
		{
			HttpListenerResponse response = context.Response;
			response.ContentEncoding = Encoding.UTF8;
			context.Response.ContentType = "text/html";
			byte[] data = Encoding.UTF8.GetBytes(ex.Message);
			context.Response.ContentLength64 = data.Length;
			context.Response.OutputStream.Write(data, 0, data.Length);
			response.StatusCode = 200;			// OK
			response.OutputStream.Close();
		}
	}
}
