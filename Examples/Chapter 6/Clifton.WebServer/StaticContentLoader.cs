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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Clifton.Extensions;

namespace Clifton.WebServer
{
	public class StaticContentLoader
	{
		protected string websitePath;
		protected Dictionary<string, FileExtensionHandler> extensionLoaderMap;

		public StaticContentLoader(string websitePath)
		{
			this.websitePath = websitePath;

			extensionLoaderMap = new Dictionary<string, FileExtensionHandler>() 
			{
				{"ico", new FileExtensionHandler() {Loader=ImageLoader, ContentType="image/ico"}},
				{"png", new FileExtensionHandler() {Loader=ImageLoader, ContentType="image/png"}},
				{"jpg", new FileExtensionHandler() {Loader=ImageLoader, ContentType="image/jpg"}},
				{"gif", new FileExtensionHandler() {Loader=ImageLoader, ContentType="image/gif"}},
				{"bmp", new FileExtensionHandler() {Loader=ImageLoader, ContentType="image/bmp"}},
				{"html", new FileExtensionHandler() {Loader=PageLoader, ContentType="text/html"}},
				{"css", new FileExtensionHandler() {Loader=FileLoader, ContentType="text/css"}},
				{"js", new FileExtensionHandler() {Loader=FileLoader, ContentType="text/javascript"}},
				{"json", new FileExtensionHandler() {Loader=FileLoader, ContentType="text/json"}},
				{"", new FileExtensionHandler() {Loader=PageLoader, ContentType="text/html"}},	  // no extension is assumed to be .html
			};
		}

		public WorkflowState GetContent(WorkflowContinuation<HttpListenerContext> workflowContinuation, HttpListenerContext context)
		{
			// Get the request.
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;

			// Get the path, everything up to the first ? and excluding the leading "/"
			string path = context.Path();
			string ext = context.Extension();

			// Default to index.html if only the URL is provided with no additional page information.
			if (String.IsNullOrEmpty(path))
			{
				path = "index.html";
				ext = "html";
			}

			if (String.IsNullOrEmpty(ext))
			{
				path = path + ".html";
			}

			path = websitePath + "\\" + path;
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

			return WorkflowState.Done;
		}

		public byte[] ImageLoader(HttpListenerContext context, string path, string ext)
		{
			FileStream fStream = new FileStream(path, FileMode.Open, FileAccess.Read);
			BinaryReader br = new BinaryReader(fStream);
			byte[] data = br.ReadBytes((int)fStream.Length);
			br.Close();
			fStream.Close();

			return data;
		}

		public byte[] FileLoader(HttpListenerContext context, string path, string ext)
		{
			string text = File.ReadAllText(path);
			byte[] data = Encoding.UTF8.GetBytes(text);

			return data;
		}

		public byte[] PageLoader(HttpListenerContext context, string path, string ext)
		{
			string text = File.ReadAllText(path);
			byte[] data = Encoding.UTF8.GetBytes(text);

			return data;
		}
	}
}
