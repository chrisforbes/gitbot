using System;

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
	}
}
