using System.Collections.Generic;
using System.Linq;

namespace bot
{
	class Repo
	{
		public readonly string Alias;
		public Ref[] Refs = { };		// cached

		public Repo(string alias)
		{
			Alias = alias;
		}
	}
}
