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
			Dictionary<string, string> kvParams = Server.ProcessUrlDelimitedParams(parms);

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
			response.StatusCode = 200;
			context.Response.OutputStream.Write(data, 0, data.Length);
			response.Close();
		}

		/// <summary>
		/// Redirect to the designated page.
		/// </summary>
		public static void Redirect(this HttpListenerContext context, string url)
		{
			url = url.Replace('\\', '/');
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;
			response.StatusCode = (int)HttpStatusCode.Redirect;
			string redirectUrl = request.Url.Scheme + "://" + request.Url.Host + "/" + url;
			response.Redirect(redirectUrl);
			response.Close();
		}
	}
}
