using System;
using System.Net;
using System.IO;
using System.Web;
namespace bot
{
    static class Shortener
	{
		static string RequestUrl = "http://api.bit.ly/v3/shorten?longUrl={0}&login={1}&apiKey={2}&format=txt";
		static String CompareLongUrl = "http://github.com/{0}/{1}/compare/{2}...{3}";
		static String CommitLongUrl = "http://github.com/{0}/{1}/commit/{2}";
		static string username, apiKey;
		
		public static string Shorten(Ref a, Ref b)
		{
			//NOTE: init username and apiKey here as appropriate. Not pushed to repo for (hopefully) obvious reasons
			
			//TODO: urlencode this result
			var LongUrl = (a == null) ? CommitLongUrl.F(b.UserName, b.RepoName, b.ShortSha)
				                	  : CompareLongUrl.F(b.UserName, b.RepoName, a.ShortSha, b.ShortSha);
			
			try 
			{
				var req = WebRequest.Create(RequestUrl.F(LongUrl, username, apiKey));
				var res = new StreamReader(req.GetResponse().GetResponseStream());
				var url = res.ReadToEnd().ToString();
				res.Close();
				return url;
			}
			catch (Exception) { return LongUrl; }
		}
	}
}

