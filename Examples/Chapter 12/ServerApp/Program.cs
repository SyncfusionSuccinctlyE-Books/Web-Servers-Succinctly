// #define USE_SEPARATE_APP_DOMAIN

/*
Copyright (c) 2015 Marc Clifton
All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using RazorEngine;
using RazorEngine.Templating;

using Clifton.Extensions;
using Clifton.WebServer;

namespace ServerApp
{
	public class Person
	{
		public string Name {get;set;}

		public Person(string name)
		{
			Name=name;
		}
	}

	public class Program
	{
		public static List<Person> codeProject2015Mvp = new List<Person>()
		{
 			new Person("Christian Graus"),
 			new Person("BillWoodruff"),
 			new Person("Richard Deeming"),
 			new Person("Marc Clifton"),
 			new Person("George Mamaladze"),
 			new Person("Pete O'Hanlon"),
 			new Person("Dave Kreskowiak"),
 			new Person("Raul Iloc"),
 			new Person("Sacha Barber"),
 			new Person("Maciej Los"),
 			new Person("Richard MacCutchan"),
 			new Person("Shivprasad koirala"),
 			new Person("CPallini"),
 			new Person("Sergey Alexandrovich Kryukov"),
 			new Person("syed shanu"),
 			new Person("RyanDev"),
 			new Person("Kornfeld Eliyahu Peter"),
 			new Person("Shemeer NS"),
 			new Person("Florian Rappl"),
 			new Person("Ranjan.D"),
 			new Person("Rahul Rajat Singh"),
 			new Person("Dave Kerr"),
 			new Person("Mahsa Hassankashi"),
 			new Person("Abhinav S"),
 			new Person("Peter Leow"),
 			new Person("CHill60"),
 			new Person("Dr. Song Li"),
 			new Person("Marla Sukesh"),
 			new Person("Paulo Zemek"),
 			new Person("OriginalGriff"),
 			new Person("KARTHIK Bangalore"),
 			new Person("thatraja"),
 			new Person("adriancs"),
 			new Person("Akhil Mittal"),
 			new Person("Azim Zahir"),
 			new Person("Tadit Dash (ତଡିତ୍ କୁମାର ଦାଶ)"),
 			new Person("Dnyaneshwar@Pune"),
 			new Person("DamithSL"),
 			new Person("Snesh Prajapati"),
 			new Person("Debopam Pal"),
};
		public static IRequestHandler requestHandler;
		public static Workflow<ContextWrapper> workflow;
		public static RouteHandler routeHandler;
		public static RouteTable routeTable;
		public static SessionManager sessionManager;
		public static string websitePath;

		static void Main(string[] args)
		{
#if USE_SEPARATE_APP_DOMAIN
			if (AppDomain.CurrentDomain.IsDefaultAppDomain())
			{
				Console.WriteLine("Switching to secound AppDomain, for RazorEngine...");
				AppDomainSetup adSetup = new AppDomainSetup();
				adSetup.ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
				var current = AppDomain.CurrentDomain;
				var domain = AppDomain.CreateDomain("MyMainDomain", null, current.SetupInformation, new PermissionSet(PermissionState.Unrestricted), null);
				var exitCode = domain.ExecuteAssembly(Assembly.GetExecutingAssembly().Location);

				// RazorEngine will cleanup. 
				AppDomain.Unload(domain);

				return;
			}
#endif
			// Continue with our sever initialization...

			//string externalIP = GetExternalIP();
			//Console.WriteLine("External IP: " + externalIP);
			requestHandler = new SingleThreadedQueueingHandler();
			websitePath = GetWebsitePath();
			routeTable = InitializeRouteTable();
			sessionManager = new SessionManager(routeTable);
			routeHandler = new RouteHandler(routeTable, sessionManager);
			InitializeWorkflow(websitePath);
			Server server = new Server();
			server.Start(requestHandler, workflow);

			Console.WriteLine("Press a key to exit the server.");
			Console.ReadLine();
		}

		/// <summary>
		/// A workflow item, implementing a simple instrumentation of the client IP address, port, and URL.
		/// </summary>
		public static WorkflowState LogIPAddress(WorkflowContinuation<ContextWrapper> workflowContinuation, ContextWrapper wrapper)
		{
			Console.WriteLine(wrapper.Context.Request.RemoteEndPoint.ToString() + " : " + wrapper.Context.Request.RawUrl);

			return WorkflowState.Continue;
		}

		public static WorkflowState LogHit(WorkflowContinuation<ContextWrapper> workflowContinuation, ContextWrapper wrapper)
		{
			Console.Write(".");

			return WorkflowState.Continue;
		}

		/// <summary>
		/// Only intranet IP addresses are allowed.
		/// </summary>
		public static WorkflowState WhiteList(WorkflowContinuation<ContextWrapper> workflowContinuation, ContextWrapper wrapper)
		{
			string url = wrapper.Context.Request.RemoteEndPoint.ToString();
			bool valid = url.StartsWith("192.168") || url.StartsWith("127.0.0.1") || url.StartsWith("[::1]");
			WorkflowState ret = valid ? WorkflowState.Continue : WorkflowState.Abort;

			return ret;
		}

		/// <summary>
		/// Final step is to actually issue the response.
		/// </summary>
		public static WorkflowState Responder(WorkflowContinuation<ContextWrapper> workflowContinuation, ContextWrapper wrapper)
		{
			wrapper.Stopwatch.Stop();
			Server.CumulativeTime += wrapper.Stopwatch.ElapsedTicks;
			++Server.Samples;

			wrapper.Context.Response.ContentEncoding = wrapper.PendingResponse.Encoding;
			wrapper.Context.Response.ContentType = wrapper.PendingResponse.MimeType;
			wrapper.Context.Response.ContentLength64 = wrapper.PendingResponse.Data.Length;
			wrapper.Context.Response.OutputStream.Write(wrapper.PendingResponse.Data, 0, wrapper.PendingResponse.Data.Length);
			wrapper.Context.Response.StatusCode = 200;			// OK
			wrapper.Context.Response.Close();

			return WorkflowState.Continue;
		}

		/// <summary>
		/// Apply the Razor view engine to a page response.
		/// </summary>
		public static WorkflowState ViewEngine(WorkflowContinuation<ContextWrapper> workflowContinuation, ContextWrapper wrapper)
		{
			PendingPageResponse pageResponse = wrapper.PendingResponse as PendingPageResponse;
			
			// Only send page responses to the templating engine.
			if (pageResponse != null)
			{
				string html = pageResponse.Html;
				string templateKey = html.GetHashCode().ToString();
				// pageResponse.Html = Engine.Razor.RunCompile(html, templateKey, null, new { /* your dynamic model */ });
				try
				{
					pageResponse.Html = Engine.Razor.RunCompile(html, templateKey, null, new { People = codeProject2015Mvp });
				}
				catch (Exception ex)
				{
					// Helps with debugging runtime compilation errors!
					Console.WriteLine(ex.Message);
				}
			}

			return WorkflowState.Continue;
		}

		public static WorkflowState CsrfInjector(WorkflowContinuation<ContextWrapper> workflowContinuation, ContextWrapper wrapper)
		{
			PendingPageResponse pageResponse = wrapper.PendingResponse as PendingPageResponse;
			if (pageResponse != null)
			{
				// For form postbacks.
				pageResponse.Html = pageResponse.Html.Replace("%AntiForgeryToken%", "<input name=" + "csrf".SingleQuote() +
				" type='hidden' value=" + wrapper.Session["_CSRF_"].ToString().SingleQuote() +
				" id='__csrf__'/>");

				// For AJAX calls where the CSRF is in the RequestVerificationToken header:
				pageResponse.Html = pageResponse.Html.Replace("%CsrfValue%", wrapper.Session["_CSRF_"].ToString().SingleQuote());
			}

			return WorkflowState.Continue;
		}

		public static RouteTable InitializeRouteTable()
		{
			RouteTable routeTable = new RouteTable(websitePath);

			// Test parameterized URL
			routeTable.AddRoute("get", "param/{p1}/subpage/{p2}", new RouteEntry()
			{
				RouteHandler = (continuation, wrapper, session, parms) =>
				{
					wrapper.SetPendingResponse("<p>p1 = " + parms["p1"] + "</p><p>p2 = " + parms["p2"] + "</p>");
					
					return WorkflowState.Continue;
				}
			});

			// Example usage where we re-use the expirable session and authorization checks.
			// routeTable.AddExpirableRoute("get", "somepath", myRouteHandler);
			// routeTable.AddExpirableAuthorizedRoute("get", "someotherpath", myRouteHandler);

			// Test session expired and authorization flags			
			routeTable.AddRoute("get", "testsession", new RouteEntry()
			{
				SessionExpirationHandler = (continuation, wrapper, session, parms) =>
					{
						if (session.Expired)
						{
							// Redirect instead of throwing an exception.
							wrapper.Context.Redirect(@"ErrorPages\expiredSession");
							return WorkflowState.Abort;
						}
						else
						{
							return WorkflowState.Continue;
						}
					},
				AuthorizationHandler = (continuation, wrapper, session, parms) =>
					{
						if (!session.Authorized)
						{
							// Redirect instead of throwing an exception.
							wrapper.Context.Redirect(@"ErrorPages\notAuthorized");
							return WorkflowState.Abort;
						}
						else
						{
							return WorkflowState.Continue;
						}
					},
				RouteHandler = (continuation, wrapper, session, parms) =>
					{
						wrapper.SetPendingResponse("<p>Looking good!</p>");

						return WorkflowState.Continue;
					}
			});

			// Set the session expired and authorization flags for testing purposes.
			routeTable.AddRoute("get", "SetState", new RouteEntry()
			{
				RouteHandler = (continuation, wrapper, session, pathParams) =>
					{
						Dictionary<string, string> parms = wrapper.Context.GetUrlParameters();
						session.Expired = GetBooleanState(parms, "Expired", false);
						session.Authorized = GetBooleanState(parms, "Authorized", false);
						wrapper.SetPendingResponse(
							"<p>Expired has been set to " + session.Expired + "</p>"+
							"<p>Authorized has been set to "+session.Authorized + "</p>");

						return WorkflowState.Continue;
					}
			});

			// Test a form post
			routeTable.AddRoute("post", "login", "application/x-www-form-urlencoded", new RouteEntry()
			{
				RouteHandler = (continuation, wrapper, session, pathParams) =>
					{
						string data = new StreamReader(wrapper.Context.Request.InputStream, wrapper.Context.Request.ContentEncoding).ReadToEnd();
						wrapper.Context.Redirect("Welcome");

						// As a redirect, we don't want any downstream processing.
						return WorkflowState.Done;
					}
			});

			// Test a form post with JSON content
			routeTable.AddRoute("post", "login", "application/json; charset=UTF-8", new RouteEntry()
			{
				RouteHandler = (continuation, wrapper, session, pathParams) =>
					{
						string data = new StreamReader(wrapper.Context.Request.InputStream, wrapper.Context.Request.ContentEncoding).ReadToEnd();
						wrapper.Context.RespondWith("Welcome!");

						// As an AJAX response, we don't want any downstream processing.
						return WorkflowState.Done;
					}
			});

			routeTable.AddRoute("get", "loadtests", new RouteEntry()
			{
				RouteHandler = (continuation, wrapper, session, pathParams) =>
					{
						long nanosecondsPerTick = (1000L * 1000L * 1000L) / System.Diagnostics.Stopwatch.Frequency;

						if (Server.Samples == 0)
						{
							wrapper.SetPendingResponse("<p>No samples!</p>");
						}
						else
						{
							long avgTime = Server.CumulativeTime * nanosecondsPerTick / Server.Samples;
							string info = String.Format("<p>{0} responses, avg. response time = {1}ns</p><p>Resetting sample info.</p>", Server.Samples, avgTime.ToString("N0"));
							Server.CumulativeTime = 0;
							Server.Samples = 0;
							wrapper.SetPendingResponse(info);
						}

						return WorkflowState.Continue;
					}
			});

			routeTable.AddRoute("get", "sayhi", new RouteEntry()
			{
				RouteHandler = (continuation, wrapper, session, pathParams) =>
				{
					wrapper.SetPendingResponse("<p>hello</p>");

					return WorkflowState.Continue;
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
			workflow = new Workflow<ContextWrapper>(AbortHandler, OnException);
			// workflow.AddItem(new WorkflowItem<ContextWrapper>(LogIPAddress));
			// workflow.AddItem(new WorkflowItem<ContextWrapper>(LogHit));
			// workflow.AddItem(new WorkflowItem<ContextWrapper>(WhiteList));
			workflow.AddItem(new WorkflowItem<ContextWrapper>(sessionManager.Provider));
			workflow.AddItem(new WorkflowItem<ContextWrapper>(requestHandler.Process));
			workflow.AddItem(new WorkflowItem<ContextWrapper>(routeHandler.Route));
			workflow.AddItem(new WorkflowItem<ContextWrapper>(sph.GetContent));
			// workflow.AddItem(new WorkflowItem<ContextWrapper>(ViewEngine));
			// workflow.AddItem(new WorkflowItem<ContextWrapper>(CsrfInjector));
			workflow.AddItem(new WorkflowItem<ContextWrapper>(Responder));
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
			else if (websitePath.Contains("\\bin\\Release"))
			{
				// TODO: Fixup for running out of bin\Debug.  This is rather kludgy!
				websitePath = websitePath.LeftOfRightmostOf("\\bin\\Release\\" + websitePath.RightOfRightmostOf("\\")) + "\\Website";
			}
			else
			{
				// Remove app name and replace with Webiste
				websitePath = websitePath.LeftOfRightmostOf("\\") + "\\Website";
			}

			Console.WriteLine("Website path = '" + websitePath + "'");

			return websitePath;
		}

		static void AbortHandler(ContextWrapper wrapper)
		{
			HttpListenerResponse response = wrapper.Context.Response;
			response.OutputStream.Close();
		}

		static void OnException(ContextWrapper wrapper, Exception ex)
		{
			Console.WriteLine(ex.Message);

			if (ex is FileNotFoundException)
			{
				// Redirect to page not found
				wrapper.Context.Redirect(@"ErrorPages\pageNotFound");
			}
			else
			{
				// Redirect to server error
				wrapper.Context.Redirect(@"ErrorPages\serverError");
			}
		}

		public static string GetExternalIP()
		{
			string externalIP;
			externalIP = (new WebClient()).DownloadString("http://checkip.dyndns.org/");
			externalIP = (new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")).Matches(externalIP)[0].ToString();

			return externalIP;
		}
	}
}
