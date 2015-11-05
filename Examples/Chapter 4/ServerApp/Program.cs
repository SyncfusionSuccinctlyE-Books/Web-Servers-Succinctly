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
		public static IRequestHandler handler;
		public static Workflow<HttpListenerContext> workflow;

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
			handler = new SingleThreadedQueueingHandler();
			string websitePath = GetWebsitePath();
			InitializeWorkflow(websitePath);
			Server server = new Server();
			server.Start(handler, workflow);

			Console.WriteLine("Press a key to exit the server.");
			Console.ReadLine();
		}

		public static void InitializeWorkflow(string websitePath)
		{
			workflow = new Workflow<HttpListenerContext>(AbortHandler, OnException); 
			workflow.AddItem(new WorkflowItem<HttpListenerContext>(LogIPAddress));
			workflow.AddItem(new WorkflowItem<HttpListenerContext>(WhiteList));
			workflow.AddItem(new WorkflowItem<HttpListenerContext>(handler.Process));
			workflow.AddItem(new WorkflowItem<HttpListenerContext>(StaticResponse));
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

		public static WorkflowState StaticResponse(
			WorkflowContinuation<HttpListenerContext> workflowContinuation,
			HttpListenerContext context)
		{
			// Get the request.
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;

			// Get the path, everything up to the first ? and excluding the leading "/"
			string path = request.RawUrl.LeftOf("?").RightOf("/");
			string ext = path.RightOfRightmostOf('.');
			FileExtensionHandler extHandler;

			if (extensionLoaderMap.TryGetValue(ext, out extHandler))
			{
				byte[] data = extHandler.Loader(context, path, ext);
				response.ContentEncoding = Encoding.UTF8;
				context.Response.ContentType = extHandler.ContentType;
				context.Response.ContentLength64 = data.Length;
				context.Response.OutputStream.Write(data, 0, data.Length);
				response.StatusCode = 200;			// OK
				response.OutputStream.Close();
			}

			return WorkflowState.Continue;
		}

		public static Dictionary<string, FileExtensionHandler> extensionLoaderMap =
		  new Dictionary<string, FileExtensionHandler>() 
		{
			{"ico", new FileExtensionHandler() {Loader=ImageLoader, ContentType="image/ico"}},
			{"png", new FileExtensionHandler() {Loader=ImageLoader, ContentType="image/png"}},
			{"jpg", new FileExtensionHandler() {Loader=ImageLoader, ContentType="image/jpg"}},
			{"gif", new FileExtensionHandler() {Loader=ImageLoader, ContentType="image/gif"}},
			{"bmp", new FileExtensionHandler() {Loader=ImageLoader, ContentType="image/bmp"}},
			{"html", new FileExtensionHandler(){Loader=PageLoader, ContentType="text/html"}},
			{"css", new FileExtensionHandler() {Loader=FileLoader, ContentType="text/css"}},
			{"js", new FileExtensionHandler() {Loader=FileLoader, ContentType="text/javascript"}},
			{"json", new FileExtensionHandler() {Loader=FileLoader, ContentType="text/json"}},
			{"", new FileExtensionHandler() {Loader=PageLoader, ContentType="text/html"}}
		};

		public static byte[] ImageLoader(
			HttpListenerContext context,
			string path,
			string ext)
		{
			FileStream fStream = new FileStream(path, FileMode.Open, FileAccess.Read);
			BinaryReader br = new BinaryReader(fStream);
			byte[] data = br.ReadBytes((int)fStream.Length);
			br.Close();
			fStream.Close();

			return data;
		}

		public static byte[] FileLoader(
			HttpListenerContext context,
			string path,
			string ext)
		{
			string text = File.ReadAllText(path);
			byte[] data = Encoding.UTF8.GetBytes(text);

			return data;
		}

		public static byte[] PageLoader(
			HttpListenerContext context,
			string path,
			string ext)
		{
			if (String.IsNullOrEmpty(ext))
			{
				path = path + ".html";
			}

			string text = File.ReadAllText(path);
			byte[] data = Encoding.UTF8.GetBytes(text);

			return data;
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
	}
}
