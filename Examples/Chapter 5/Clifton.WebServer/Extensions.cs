using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Clifton.Extensions;

namespace Clifton.WebServer
{
	public static class Extensions
	{
		/// <summary>
		/// Return the URL path.
		/// </summary>
		public static string Path(this HttpListenerContext context)
		{
			return context.Request.RawUrl.LeftOf("?").RightOf("/").ToLower();
		}

		/// <summary>
		/// Return the extension for the URL path's page.
		/// </summary>
		public static string Extension(this HttpListenerContext context)
		{
			return context.Path().RightOfRightmostOf('.').ToLower();
		}

		/// <summary>
		/// Returns the verb of the request: GET, POST, PUT, DELETE, and so forth.
		/// </summary>
		public static string Verb(this HttpListenerContext context)
		{
			return context.Request.HttpMethod.ToUpper();
		}

		/// <summary>
		/// Return the remote endpoint IP address.
		/// </summary>
		public static IPAddress EndpointAddress(this HttpListenerContext context)
		{
			return context.Request.RemoteEndPoint.Address;
		}

		/// <summary>
		/// Returns a dictionary of the parameters on the URL.
		/// </summary>
		public static Dictionary<string, string> GetUrlParameters(this HttpListenerContext context)
		{
			HttpListenerRequest request = context.Request;
			string parms = request.RawUrl.RightOf("?");
			Dictionary<string, string> kvParams = new Dictionary<string, string>();
			parms.If(d => d.Length > 0, (d) => d.Split('&').ForEach(keyValue => kvParams[keyValue.LeftOf('=').ToLower()] = Uri.UnescapeDataString(keyValue.RightOf('='))));

			return kvParams;
		}

		/// <summary>
		/// Respond with an HTML string.
		/// </summary>
		public static void RespondWith(this HttpListenerContext context, string html)
		{
			byte[] data = Encoding.UTF8.GetBytes(html);
			HttpListenerResponse response = context.Response;
			response.ContentEncoding = Encoding.UTF8;
			context.Response.ContentType = "text/html";
			context.Response.ContentLength64 = data.Length;
			context.Response.OutputStream.Write(data, 0, data.Length);
			response.StatusCode = 200;
			response.OutputStream.Close();
		}
	}
}
