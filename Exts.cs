using System;
using System.Collections.Generic;
using System.Linq;

namespace bot
{
	static class Exts
	{
		public static string F(this string fmt, params object[] args)
		{
			return string.Format(fmt, args);
		}

		public static string[] Lines(this string s)
		{
			return s.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
		}
		
		public static IEnumerable<T> SymmetricDifference<T>(this IEnumerable<T> xs, IEnumerable<T> ys)
		{
			// this is probably a shockingly-slow way to do this, but it's concise.
			return xs.Except(ys).Concat(ys.Except(xs));
		}
	}
}
