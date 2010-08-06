using System.Linq;

namespace bot
{
	class Ref
	{
		public readonly string Alias, Name, Sha;

		public Ref(string alias, string name, string sha)
		{
			Alias = alias;
			Name = name;
			Sha = sha;
		}

		public string ShortSha { get { return Sha.Substring(0, 8); } }
		
		public string UserName { get { return Git.GetRemoteName(this); } }
		
		public string RepoName { get { return Git.GetRemoteRepoName(this); } }
	}
}
