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
					return "\t{0}/{1}: {2} (new branch, can't figure out where it came from)".F(b.Alias, b.Name, b.ShortSha);

				return "\t{0}/{1}: {2} -> {3} (new branch; +{4} new commits)".F(
					b.Alias, b.Name, m.t.ShortSha, b.ShortSha, m.u);
			}

			if (b == null)
				return "\t{0}/{1}: {2} -> (deleted)".F(a.Alias, a.Name, a.ShortSha);

			// the meaty case: this branch existed, 
			{
				var basicReport = "\t{0}/{1}: {2} -> {3}".F(a.Alias, a.Name, a.ShortSha, b.ShortSha);

				var m = Git.GetMergeBase(a, b);
				if (m.Sha == "")
					return basicReport;

				if (m.Sha == a.Sha)
				{
					// fast-forward. todo: find out if anyone else had these commits first.
					var newCommits = Git.GetCommitsBetween(m, b).Length;

					var prevOwner = oldRefs.FirstOrDefault(q => q.Sha == b.Sha);
					if (prevOwner != null)
						return "{0} (ff; +{1} new commits from {2}/{3})".F(basicReport, newCommits,
							prevOwner.Alias, prevOwner.Name);

					return "{0} (ff; +{1} new commits)".F(basicReport, newCommits);
				}

				if (m.Sha == b.Sha)
				{
					// rewind. note; existence of these commits in some other tree is irrelevant.
					var newCommits = Git.GetCommitsBetween(m, b);
					return "{0} (rw; -{1} commits removed)".F(basicReport, newCommits.Length);
				}

				return basicReport;
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
