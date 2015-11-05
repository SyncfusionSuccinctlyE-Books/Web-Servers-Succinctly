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
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Clifton.Extensions;

namespace Clifton.WebServer
{
	public class RouteTable
	{
		protected string websitePath;		// used for dumping static page routes.
		protected ConcurrentDictionary<RouteKey, RouteEntry> routes;

		public RouteTable(string websitePath)
		{
			this.websitePath = websitePath;
			routes = new ConcurrentDictionary<RouteKey, RouteEntry>();
		}

		/// <summary>
		/// True if the routing table contains the verb-path key.
		/// </summary>
		public bool ContainsKey(RouteKey key)
		{
			RouteEntry entry = Parse(key, new PathParams());

			return entry != null;
		}

		/// <summary>
		/// True if the routing table contains the verb-path key.
		/// </summary>
		public bool Contains(string verb, string path, string contentType = "*")
		{
			RouteEntry entry = Parse(NewKey(verb, path, contentType), new PathParams());

			return entry != null;
		}

		/// <summary>
		/// Add a unique route.
		/// </summary>
		public void AddRoute(RouteKey key, RouteEntry route)
		{
			routes.ThrowIfKeyExists(key, "The route key " + key.ToString() + " already exists.")[key] = route;
		}

		/// <summary>
		/// Adds a unique route.
		/// </summary>
		public void AddRoute(string verb, string path, RouteEntry route)
		{
			AddRoute(NewKey(verb, path), route);
		}

		/// <summary>
		/// Adds a unique route.
		/// </summary>
		public void AddRoute(string verb, string path, string contentType, RouteEntry route)
		{
			AddRoute(NewKey(verb, path, contentType), route);
		}

		/// <summary>
		/// Get the route entry for the verb and path including any path parameters.
		/// Throws an exception if the route isn't found.
		/// </summary>
		public RouteEntry GetRouteEntry(RouteKey key, out PathParams parms)
		{
			parms = new PathParams();
			RouteEntry entry = Parse(key, parms);

			if (entry == null)
			{
				throw new ApplicationException("The route key " + key.ToString() + " does not exist.");
			}

			return entry;
		}

		/// <summary>
		/// Get the route entry for the verb and path including any path parameters.
		/// Throws an exception if the route isn't found.
		/// </summary>
		public RouteEntry GetRouteEntry(string verb, string path, out PathParams parms)
		{
			return GetRouteEntry(NewKey(verb, path), out parms);
		}

		/// <summary>
		/// Get the route entry for the verb and path including any path parameters.
		/// Throws an exception if the route isn't found.
		/// </summary>
		public RouteEntry GetRouteEntry(string verb, string path, string contentType, out PathParams parms)
		{
			return GetRouteEntry(NewKey(verb, path, contentType), out parms);
		}

		/// <summary>
		/// Returns true and populates the route entry and path parameters if the key exists.
		/// </summary>
		public bool TryGetRouteEntry(RouteKey key, out RouteEntry entry, out PathParams parms)
		{
			parms = new PathParams();
			entry = Parse(key, parms);

			return entry != null;
		}

		/// <summary>
		/// Returns true and populates the route entry and path parameters if the key exists.
		/// </summary>
		public bool TryGetRouteEntry(string verb, string path, out RouteEntry entry, out PathParams parms)
		{
			parms = new PathParams();
			entry = Parse(NewKey(verb, path), parms);

			return entry != null;
		}

		/// <summary>
		/// Returns true and populates the route entry and path parameters if the key exists.
		/// </summary>
		public bool TryGetRouteEntry(string verb, string path, string contentType, out RouteEntry entry, out PathParams parms)
		{
			parms = new PathParams();
			entry = Parse(NewKey(verb, path, contentType), parms);

			return entry != null;
		}

		/// <summary>
		/// Create a RouteKey given the verb and path.
		/// </summary>
		public RouteKey NewKey(string verb, string path, string contentType = "*")
		{
			return new RouteKey() { Verb = verb, Path = path, ContentType = contentType };
		}

		public void DumpRoutes()
		{
			Console.WriteLine("-- Custom Routes --");
			string s = String.Format("{0, -7} {1, -60} {2, -10} {3, -10} {4, -10}", "Verb", "Path", "Exp?", "Auth?", "Handler?");
			Console.WriteLine(s);
			s = String.Format("{0, -7} {1, -60} {2, -10} {3, -10} {4, -10}", new string('-', 7), new string('-', 60), new string('-', 10), new string('-', 10), new string('-', 10));
			Console.WriteLine(s);

			foreach (KeyValuePair<RouteKey, RouteEntry> kvp in routes)
			{
				s = String.Format("{0, -7} {1, -60} {2, -10} {3, -10} {4, -10}", kvp.Key.Verb, kvp.Key.Path, kvp.Value.SessionExpirationHandler != null, kvp.Value.AuthorizationHandler != null, kvp.Value.RouteHandler != null);
				Console.WriteLine(s);
			}

			Console.WriteLine("\r\n-- Static pages --");
			s = String.Format("{0, -7} {1, -60}", "Verb", "Path");
			Console.WriteLine(s);
			s = String.Format("{0, -7} {1, -60}", new string('-', 7), new string('-', 60));
			Console.WriteLine(s);

			string[] pages = Directory.GetFiles(websitePath, "*.html", SearchOption.AllDirectories);

			foreach (string pagePath in pages)
			{
				string localPath = pagePath.RightOf(websitePath).RightOf("\\").Replace("\\", "/");

				if (!routes.ContainsKey(NewKey("GET", localPath)))
				{
					s = String.Format("{0, -7} {1, -60}", "get", localPath);
					Console.WriteLine(s);
				}
			}
		}

		/// <summary>
		/// Parse the browser's path request and match it against the routes.
		/// If found, return the route entry (otherwise null). 
		/// Also if found, the parms will be populated with any segment parameters.
		/// </summary>
		protected RouteEntry Parse(RouteKey key, PathParams parms)
		{
			RouteEntry entry = null;

			// First, check non-parameterized routes.  
			// The most performant way to handle parameters is after the path,
			// rather than embedding parameters in the URL itself.
			if (!routes.TryGetValue(key, out entry))
			{
				string[] pathSegments = key.Path.Split('/');

				foreach (KeyValuePair<RouteKey, RouteEntry> route in routes)
				{
					// Above all else, verbs and, if not wildcarded, content type must match.
					if ( (route.Key.Verb == key.Verb) && ( (route.Key.ContentType=="*") || (route.Key.ContentType==key.ContentType)) )
					{
						string[] routeSegments = route.Key.Path.Split('/');

						// Then, segments must match
						if (Match(pathSegments, routeSegments, parms))
						{
							entry = route.Value;
							break;
						}
					}
				}
			}

			return entry;
		}

		/// <summary>
		/// Return true if the path and the route segments match.  Any parameters in the path
		/// get put into parms.  The first route that matches will win.
		/// </summary>
		protected bool Match(string[] pathSegments, string[] routeSegments, PathParams parms)
		{
			// Basic check: # of segments must be the same.
			bool ret = pathSegments.Length == routeSegments.Length;

			if (ret)
			{
				int n = 0;

				// Check each segment
				while (n < pathSegments.Length && ret)
				{
					string pathSegment = pathSegments[n];
					string routeSegment = routeSegments[n];
					++n;

					// Is it a parameterized segment (aka "capture segment") ?
					if (routeSegment.BeginsWith("{"))
					{
						string parmName = routeSegment.Between('{', '}');
						string value = pathSegment;
						parms[parmName] = value;
					}
					else // We could perform other checks, such as regex
					{
						ret = pathSegment == routeSegment;
					}
				}
			}

			return ret;
		}
	}
}
