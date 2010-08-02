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
					if (existingRef != null)
					{
						if (existingRef.Sha == r.Sha) continue;	/* not interesting, this one hasnt changed. */
						/* todo: this is a branch *change*. figure out what happened. */
						yield return "\t{0}/{1}: {2} -> {3}".F(r.Alias, r.Name,
							existingRef.ShortSha, r.ShortSha);
					}
					else
					{
						/* todo: this is a *new* branch. figure out if anyone else has it. */
						yield return "\t{0}/{1}: {2} (new branch)".F(r.Alias, r.Name, r.ShortSha);
					}
				}

				foreach (var q in repo.Refs)
					if (!newRefs.Any(r => r.Name == q.Name))
						yield return "\t{0}/{1}: {2} -> (deleted)".F(q.Alias, q.Name, q.ShortSha);
			}

			foreach (var repo in repos)
				repo.Refs = updates[repo];
		}
	}
}
