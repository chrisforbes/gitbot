using System.Collections.Generic;
using System.Linq;

namespace bot
{
	static class Analyzer
	{
		static Pair<Ref,int> GetBestMergeBase(Ref a, Ref[] refs)
		{
			return refs
				.Select(r => Git.GetMergeBase(a, r))
				.Select(r => Pair.New(r, Git.GetCommitsBetween(r, a).Length))
				.OrderBy(r => r.u)
				.FirstOrDefault(r => r.t.Sha != "");
		}

		static string CharacterizeChange(Ref a, Ref b, Ref[] oldRefs)
		{
			if (a == null)
			{
				var m = GetBestMergeBase(b, oldRefs);
				if (m.t == null)
					return "{0}/{1}: {2} (new branch) {3}".F(b.Alias, b.Name, b.ShortSha, Shortener.Shorten(a, b));

				if (m.u == 1)
					return "{0}/{1}: {2} -> {3} (new branch; 1 new commit) {4} {5}".F(
						b.Alias, b.Name, m.t.ShortSha, b.ShortSha, Shortener.Shorten(m.t, b), Git.GetMessage(b)); 
				
				return "{0}/{1}: {2} -> {3} (new branch; {4} new commits) {5}".F(
					b.Alias, b.Name, m.t.ShortSha, b.ShortSha, m.u, Shortener.Shorten(m.t, b));
			}

			if (b == null)
				return "{0}/{1}: {2} -> (deleted)".F(a.Alias, a.Name, a.ShortSha);

			// the meaty case: this branch existed, 
			{
				var basicReport = "{0}/{1}: {2} -> {3}".F(a.Alias, a.Name, a.ShortSha, b.ShortSha);

				var m = Git.GetMergeBase(a, b);
				if (m.Sha == "")
					return basicReport;

				if (m.Sha == a.Sha)
				{
					// fast-forward. todo: find out if anyone else had these commits first.
					var newCommits = Git.GetCommitsBetween(m, b).Length;

					//if we have a prevOwner
					var prevOwner = oldRefs.FirstOrDefault(q => q.Sha == b.Sha);
					if (prevOwner != null)
					{
						if(newCommits == 1)
							return "{0} (1 new commit from {1}/{2}) {3} {4}".F(basicReport,
								prevOwner.Alias, prevOwner.Name, Shortener.Shorten(null, b), Git.GetMessage(b));
						else
							return "{0} ({1} new commits from {2}/{3}) {4}".F(basicReport, newCommits,
								prevOwner.Alias, prevOwner.Name, Shortener.Shorten(a, b));
					}
					
					//otherwise
					if (newCommits == 1)
						return "{0} (1 new commit) {1} {2}".F(basicReport , Shortener.Shorten(null, b), Git.GetMessage(b));
					
					return "{0} ({1} new commits) {2}".F(basicReport, newCommits, Shortener.Shorten(a, b));
				}

				if (m.Sha == b.Sha)
				{
					// rewind. note; existence of these commits in some other tree is irrelevant.
					var newCommits = Git.GetCommitsBetween(m, a);
					if (newCommits.Length == 1)
						return "{0} (1 commit removed)".F(basicReport);
					else
						return "{0} ({1} commits removed)".F(basicReport, newCommits.Length);
				}

				if (Git.GetCommitsBetween(m, a).Length == 1 &&
					Git.GetCommitsBetween(m, b).Length == 1)
				{
					return "{0} (amended) {1} {2}".F(basicReport, Shortener.Shorten(m, b), Git.GetMessage(b));
				}

				return "{0} {1}".F(basicReport, Shortener.Shorten(a, b));
			}
		}

		public static IEnumerable<string> Update(Repo[] repos, Ref[] allNewRefs)
		{
			Dictionary<Repo, Ref[]> updates = new Dictionary<Repo, Ref[]>();
			var oldRefs = repos.SelectMany(r => r.Refs).ToArray();

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

					yield return CharacterizeChange(existingRef, r, oldRefs);
				}

				foreach (var q in repo.Refs)
					if (!newRefs.Any(r => r.Name == q.Name))
						yield return CharacterizeChange(q, null, oldRefs);
			}

			foreach (var repo in repos)
				repo.Refs = updates[repo];
		}
	}
}
