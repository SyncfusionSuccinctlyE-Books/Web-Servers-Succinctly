using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo
{
	/// <summary>
	/// Some useful string extensions.
	/// </summary>
	public static class ExtensionMethods
	{
		/// <summary>
		/// Return everything to the left of the first occurrence of the specified string,
		/// or the entire source string.
		/// </summary>
		public static string LeftOf(this String src, string s)
		{
			string ret = src;
			int idx = src.IndexOf(s);

			if (idx != -1)
			{
				ret = src.Substring(0, idx);
			}

			return ret;
		}

		/// <summary>
		/// Return everything to the right of the first occurrence of the specified string,
		/// or an empty string.
		/// </summary>
		public static string RightOf(this String src, string s)
		{
			string ret = String.Empty;
			int idx = src.IndexOf(s);

			if (idx != -1)
			{
				ret = src.Substring(idx + s.Length);
			}

			return ret;
		}
	}
}
