using System.Collections.Generic;
using System.Linq;

namespace bot
{
	static class Analyzer
	{
		public static string CharacterizeChange(Ref a, Ref b)
		{
			if (a == null)
				return "\t{0}/{1}: {2} (new branch)".F(b.Alias, b.Name, b.ShortSha);

			if (b == null)
				return "\t{0}/{1}: {2} -> (deleted)".F(a.Alias, a.Name, a.ShortSha);

			return "\t{0}/{1}: {2} -> {3}".F(a.Alias, a.Name, a.ShortSha, b.ShortSha);
		}

		public static IEnumerable<string> Update(Repo[] repos, Ref[] allNewRefs)
		{
			Dictionary<Repo, Ref[]> updates = new Dictionary<Repo, Ref[]>();

			foreach (var repo in repos)
			{
				var newRefs = allNewRefs.Where(r => r.Alias == repo.Alias).ToArray();
				updates[repo] = newRefs;

				foreach (var r in newRefs)
				{
					/* determine what happened */
					var existingRef = repo.Refs.FirstOrDefault(q => q.Name == r.Name);
					if (existingRef != null && existingRef.Sha == r.Sha)
						continue;

					yield return CharacterizeChange(existingRef, r);
				}

				foreach (var q in repo.Refs)
					if (!newRefs.Any(r => r.Name == q.Name))
						yield return CharacterizeChange(q, null);
			}

			foreach (var repo in repos)
				repo.Refs = updates[repo];
		}
	}
}
