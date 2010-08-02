using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace bot
{
	static class Git
	{
		public static string BaseUrl = "git://github.com/";
		public static string GitRoot = "state.git";

		public static void Init()
		{
			if (!Directory.Exists(GitRoot))
				External.Run("git", "init --bare {0}".F(GitRoot));
		}

		public static string[] GetRemotes()
		{
			return External.Run("git", "--git-dir={0} remote".F(GitRoot))
				.StandardOutput.Lines();
		}

		static Regex refRegex = new Regex("^(?'sha'[0-9a-f]{40}) refs/remotes/(?'alias'[^/]+)/(?'name'.+)$");
		public static Ref[] GetRefs()
		{
			return External.Run("git", "--git-dir={0} show-ref".F(GitRoot)).StandardOutput.Lines()
				.Select(a => refRegex.Match(a))
				.Where(m => m.Success)
				.Select(m => new Ref(m.Groups["alias"].Value, m.Groups["name"].Value, m.Groups["sha"].Value)).ToArray();
		}

		public static bool AddRepo(string alias, string githubName)
		{
			return !External.Run("git", "--git-dir={0} remote add {1} {2}{3}".F(
								GitRoot, alias, BaseUrl, githubName)).Failed;
		}

		public static bool Fetch()
		{
			External.Run("rm", "-rf {0}/refs/remotes/".F(GitRoot));
			return !External.Run("git", "--git-dir={0} fetch --all".F(GitRoot)).Failed;
		}

		public static Ref GetMergeBase( Ref a, Ref b )
		{
			var sha = External.Run("git", "--git-dir={0} merge-base {1} {2}".F(GitRoot, a.Sha, b.Sha)).StandardOutput;
			return new Ref("", "(mergebase)",
				sha.Length >= 40 ? sha.Substring(0, 40) : "");
		}

		public static string[] GetCommitsBetween(Ref a, Ref b)
		{
			return External.Run("git", "--git-dir={0} log {1}..{2} --graph --oneline --no-color".F(
				GitRoot, a.Sha, b.Sha)).StandardOutput.Lines();
		}
	}
}
