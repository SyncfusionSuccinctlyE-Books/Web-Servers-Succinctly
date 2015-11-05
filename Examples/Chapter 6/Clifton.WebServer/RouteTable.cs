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
using System.Text;
using System.Threading.Tasks;

using Clifton.Extensions;

namespace Clifton.WebServer
{
	public class RouteTable
	{
		protected ConcurrentDictionary<RouteKey, RouteEntry> routes;

		public RouteTable()
		{
			routes = new ConcurrentDictionary<RouteKey, RouteEntry>();
		}

		/// <summary>
		/// True if the routing table contains the verb-path key.
		/// </summary>
		public bool ContainsKey(RouteKey key)
		{
			return routes.ContainsKey(key);
		}

		/// <summary>
		/// True if the routing table contains the verb-path key.
		/// </summary>
		public bool Contains(string verb, string path)
		{
			return ContainsKey(NewKey(verb, path));
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
		/// Get the route entry for the verb and path.
		/// </summary>
		public RouteEntry GetRouteEntry(RouteKey key)
		{
			return routes.ThrowIfKeyDoesNotExist(key, "The route key " + key.ToString() + " does not exist.")[key];
		}

		/// <summary>
		/// Get the route entry for the verb and path.
		/// </summary>
		public RouteEntry GetRouteEntry(string verb, string path)
		{
			return GetRouteEntry(NewKey(verb, path));
		}

		/// <summary>
		/// Returns true and populates the out entry parameter if the key exists.
		/// </summary>
		public bool TryGetRouteEntry(RouteKey key, out RouteEntry entry)
		{
			return routes.TryGetValue(key, out entry);
		}

		/// <summary>
		/// Returns true and populates the out entry parameter if the key exists.
		/// </summary>
		public bool TryGetRouteEntry(string verb, string path, out RouteEntry entry)
		{
			return routes.TryGetValue(NewKey(verb, path), out entry);
		}

		/// <summary>
		/// Create a RouteKey given the verb and path.
		/// </summary>
		public RouteKey NewKey(string verb, string path)
		{
			return new RouteKey() { Verb = verb, Path = path };
		}
	}
}
