using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.WebServer
{
	public abstract class Response
	{
		private byte[] data;

		public virtual byte[] Data
		{
			get { return data; }
			set { data = value; }
		}

		public virtual Encoding Encoding { get { return Encoding.UTF8; } }
		public virtual string MimeType { get; set; }
		public virtual int StatusCode { get { return 200; } }
	}

	/// <summary>
	/// A pending byte response packet for non-text (as in binary) data like images.
	/// </summary>
	public class PendingByteResponse : Response
	{
	}

	/// <summary>
	/// A pending HTML page response packet.
	/// </summary>
	public class PendingPageResponse : Response
	{
		public string Html { get; set; }
		public override string MimeType { get { return "text/html"; } }
		public override byte[] Data { get { return Encoding.UTF8.GetBytes(Html); } }
	}

	/// <summary>
	/// A pending file response (like a Javascript or CSS file, which is still text.)
	/// </summary>
	public class PendingFileResponse : Response
	{
	}

	public class PendingJsonResponse : Response
	{
		public string Json { get; set; }
		public override string MimeType { get { return "text/html"; } }
		public override byte[] Data { get { return Encoding.UTF8.GetBytes(Json); } }
	}

	public class PendingTextResponse : Response
	{
		public string Text { get; set; }
		public override string MimeType { get { return "text/text"; } }
		public override byte[] Data { get { return Encoding.UTF8.GetBytes(Text); } }
	}

	public class PendingOKResponse : Response
	{
		public override byte[] Data { get { return Encoding.UTF8.GetBytes("OK"); } }
		public override int StatusCode { get { return 200; } }
	}

	public class PendingErrorResponse : Response
	{
		public string ErrorMessage { get; set; }
		public override string MimeType { get { return "text/text"; } }
		public override byte[] Data { get { return Encoding.UTF8.GetBytes(ErrorMessage); } }
		public override int StatusCode { get { return 400; } }

		public PendingErrorResponse()
		{
			ErrorMessage = "Error";
		}
	}

	/// <summary>
	/// A wrapper for HttpListenerContext so we can put pending
	/// byte[] and HTML responses into a workflow for downstream final
	/// processing (basically only for HTML) by a view engine.
	/// </summary>
	public class ContextWrapper
	{
		public HttpListenerContext Context { get; protected set; }
		public Response PendingResponse { get; set; }
		public Session Session { get; set; }
		public System.Diagnostics.Stopwatch Stopwatch { get; set; }

		public ContextWrapper(HttpListenerContext context)
		{
			Context = context;
			Stopwatch = new System.Diagnostics.Stopwatch();
			Stopwatch.Start();
		}

		/// <summary>
		/// Text or HTML response, suitable for input to a view engine.
		/// </summary>
		public void SetPendingResponse(string text)
		{
			PendingResponse = new PendingPageResponse() { Html = text };
		}

		public void SetPendingJsonResponse(string json)
		{
			PendingResponse = new PendingJsonResponse() { Json = json };
		}

		public void OK()
		{
			PendingResponse = new PendingOKResponse();
		}

		public void Error(string msg)
		{
			PendingResponse = new PendingErrorResponse() { ErrorMessage = msg };
		}
	}
}
