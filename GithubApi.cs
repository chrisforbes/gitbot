using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace bot
{
	class Repo
	{
		public readonly string Alias, Name;
		public Ref[] Refs = { };		// cached

		public Repo(string alias, string name)
		{
			Alias = alias;
			Name = name;
		}

		public static IEnumerable<string> Update(Repo[] repos)
		{
			Dictionary<Repo, Ref[]> updates = new Dictionary<Repo, Ref[]>();

			foreach (var repo in repos)
			{
				var newRefs = GithubApi.GetRefs(repo.Name, repo.Alias);
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

	class Ref
	{
		public readonly string Alias, Name, Sha;

		public Ref(string alias, string src)
		{
			Alias = alias;
			var parts = src.Split(':').Select(a => a.Trim()).ToArray();
			Name = parts[0];
			Sha = parts[1];
		}

		public string ShortSha { get { return Sha.Substring(0, 8); } }
	}

	static class GithubApi
	{
		public static Ref[] GetRefs(string repo, string alias)
		{
			var url = "http://github.com/api/v2/yaml/repos/show/{0}/branches".F(repo);
			using (var wc = new WebClient())
			{
				try 
				{
					var result = wc.DownloadString(url);
					return result.Split('\n').Where(a => a.StartsWith(" ")).Select(x => new Ref(alias, x)).ToArray();
				}
				catch (WebException) 
				{
					return new Ref[0];
				}
			}
		}

		public static string F(this string fmt, params object[] args) { return string.Format(fmt, args); }
	}
}
