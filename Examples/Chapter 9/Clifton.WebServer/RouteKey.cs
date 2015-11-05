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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Clifton.Extensions;

namespace Clifton.WebServer
{
	/// <summary>
	/// A structure consisting of the verb and path, suitable as a key for the route table entry.
	/// Key verbs are always converted to uppercase, paths are always converted to lowercase.
	/// </summary>
	public class RouteKey
	{
		private string verb;
		private string path;
		private string contentType;

		public string Verb
		{
			get { return verb; }
			set { verb = value.ToUpper(); }
		}

		public string Path
		{
			get { return path; }
			set 
			{
				// Programmer should not need to worry about whether paths begin with a leading slash
				// or not in the route table.
				if (value.BeginsWith("/"))
				{
					path = value.Substring(1).ToLower();
				}
				else
				{
					path = value.ToLower();
				}
			}
		}

		/// <summary>
		/// Content type.  The setter will strip off the charset encoding.
		/// </summary>
		public string ContentType
		{
			get { return contentType; }
			set { contentType = value == null ? String.Empty : value.LeftOf(";"); }
		}

		public RouteKey()
		{
			verb = String.Empty;
			path = String.Empty;
			contentType = "*";
		}

		public override bool Equals(object obj)
		{
			bool ret=false;
			RouteKey key=(RouteKey)obj;

			if (contentType == "*")
			{
				ret = verb == key.verb && path == key.path;
			}
			else
			{
				ret = verb == key.verb && path == key.path && contentType == key.contentType;
			}

			return ret;
		}

		public override int GetHashCode()
		{
			int ret = 0;

			if (contentType == "*")
			{
				ret = verb.GetHashCode() ^ path.GetHashCode();
			}
			else
			{
				ret = verb.GetHashCode() ^ path.GetHashCode() ^ contentType.GetHashCode();
			}

			return ret;
		}

		public override string ToString()
		{
			return Verb + " : " + Path + contentType.Parens();
		}
	}
}
