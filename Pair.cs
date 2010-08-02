using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bot
{
	struct Pair<T,U>
	{
		public T t;
		public U u;

		public Pair(T t, U u) { this.t = t; this.u = u; }
	}

	static class Pair
	{
		public static Pair<T, U> New<T, U>(T t, U u)
		{
			return new Pair<T, U>(t, u);
		}
	}
}
