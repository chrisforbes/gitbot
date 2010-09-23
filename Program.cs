using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Text;
using System.IO;

namespace bot
{
	class Program
	{
		const int CheckInterval = 30000;
		const int PingInterval = 15000;

		static Connection conn;
		static List<Repo> repos = new List<Repo>();
		static string[] tags;
		static int NextCheckTime = Environment.TickCount;
		static bool UseSocks = false;
		
		//behaviour
		static bool RespondAsWhisper = false;
		static string[] AuthorizedNicks = new string[]{ };
		static string TagScript = "";
		
		//server info
		static string Server = null;
		static string Channel = null;
		static int Port = 6667;
		
		//bot info
		static string UserName = "pizzabot";
		static string Nick = "pizzabot";
		static string IRCName = "PizzaBot";
		
		static void Main(string[] args)
		{
			Git.GitRoot = "state.git";
			Git.BaseUrl = "git://github.com/";

			Git.Init();

			var initialRepoList = Git.GetRemotes();
			Console.WriteLine("Initial repo list:");

			foreach (var a in initialRepoList)
			{
				Console.WriteLine("\t{0}", a);
				repos.Add(new Repo(a));
			}

			if (!initialRepoList.Any())
				Console.WriteLine("\t(none)");


			var initialRefs = Git.GetRefs();
			foreach (var repo in repos)
				repo.Refs = initialRefs.Where(r => r.Alias == repo.Alias).ToArray();
			
			tags = Git.GetTags();

			try
			{
				for(int i = 0; i < args.Length; i++)
				{
					switch (args[i])
					{
						case "--socks" : UseSocks = true; break;
						case "--whisper" : RespondAsWhisper = true; break;
						case "--server" : Server = args[++i]; break;
						case "--channel" : Channel = args[++i]; break;
						case "--port" : Port = Int32.Parse(args[++i]); break;
						case "--user-name" : UserName = args[++i]; break;
						case "--nick" : Nick = args[++i]; break;
						case "--irc-name" : IRCName = args[++i]; break;
						case "--bitly-username" : Shortener.Username = args[++i]; break;
						case "--bitly-key" : Shortener.ApiKey = args[++i]; break;
						case "--authorized-nicks" : AuthorizedNicks = args[++i].Split(','); break;
						case "--tag-script" : TagScript = args[++i]; break;
					}
				}
			}
			catch (Exception) 
			{
				Console.WriteLine("Invalid type or number of arguments");
				return;
			}
			
			if (string.IsNullOrEmpty(Server) || string.IsNullOrEmpty(Channel))
			{
				Console.WriteLine("Must set a Server and Channel");
				return;
			}
			
			EstablishConnection();
			conn.OnCommand += OnCommand;
			Connect();

			while( conn.Connected )
			{
				conn.Write("PING {0}".F(Server));
				Thread.Sleep(PingInterval);

				if (NextCheckTime - Environment.TickCount < 0)
					Update();
			}

			Console.WriteLine("Connection dropped. Cause:");
			Console.WriteLine(conn.Exception);
		}
		
		static void EstablishConnection()
		{
			Socket s;
			if (UseSocks)
				s = SocksProxy.ConnectToSocks5Proxy("127.0.0.1", 1080, Server, (ushort) Port, "", "");
			else
			{
				s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				s.Connect(Server, Port);
			}
			conn = new Connection(s);
			
			conn.Run();
		}
		
		static void Connect()
		{
			//TODO: handle nickname in use / registered
			conn.Write("USER {0} a a :{1}".F(UserName, IRCName));
			conn.Write("NICK {0}".F(Nick));
			conn.Write("JOIN {0}".F(Channel));	
		}

		static void OnCommand(string c)
		{
			if (string.IsNullOrEmpty(c)) return;
			var agent = c.Split(':', '!', ' ').ElementAt(1);
			var commands = new Dictionary<string, Action<string[]>>();
			Action<string, Action<string[]>> Add = commands.Add;

			Add("@add <alias> <username/repo>", args =>
			{
				if (AuthorizedNicks.Count() != 0 && !AuthorizedNicks.Contains(agent))
				{	
					SendTo(agent, "You are not authorised to make changes.");
					return;	
				}
				
				lock ( repos )
				{
					if (!repos.Any(a => a.Alias == args[1]))
					{
						if (!Git.AddRepo(args[1], args[2]))
							SendTo(agent, "Failed adding alias");
						else
						{
							repos.Add(new Repo(args[1]));
							SendTo(agent, "Done.");
						}
					}
					else
						SendTo(agent, "Alias already exists");
				}

				NextCheckTime = Environment.TickCount;
			});

			Add("@rm <alias>", args =>
			{
				if (AuthorizedNicks.Count() != 0 && !AuthorizedNicks.Contains(agent))
				{	
					SendTo(agent, "You are not authorised to make changes.");
					return;	
				}
				
				lock ( repos )
				{
					if (repos.Any( a => a.Alias == args[1]))
					{
						repos.RemoveAll(r => r.Alias == args[1]);
							Git.RemoveRepo(args[1]);
						SendTo(agent, "Done.");
					}
					else
						SendTo(agent, "Alias doesn't exist");
				}
			});
			
			Add("@status <alias>", args =>
			{
				if (args[1] == "*")
				{
					lock ( repos )
					{
						foreach( var r in repos )
							SendTo(agent, "{0}: {1}".F(r.Alias, string.Join(", ", 
							       r.Refs.Select( rf => rf.Name ).ToArray()))); 
					}
					return;
				}
				
				lock ( repos )
				{
					if (repos.Any( a => a.Alias == args[1]))
					{
						var repo = repos.Where( r => r.Alias == args[1] ).First();
						foreach (var r in repo.Refs)
							SendTo(agent, "{0}: ({1}) {2}".F(r.Name, r.ShortSha, Git.GetMessage(r)));
					}
					else
						SendTo(agent, "Alias doesn't exist");
				}
			});
			

			Add("@repolist", args =>
			{
				if ( repos.Count() != 0)
				{
					var names = "";
					lock( repos )
						names = string.Join(", ", repos.Select(r => r.Alias).ToArray());

					 SendTo(agent, "I'm currently tracking: {0}".F(names));
				}
				else
					SendTo(agent, "I'm not tracking any repos yet");
			});

			Add("@help", args =>
			{
				SendTo(agent, "Commands:");
				foreach (var cmd in commands.Keys)
					SendTo(agent, "\t{0}".F(cmd));
			});

			Add("@conv <amt> <fromUnit> <toUnit>", args =>
			{
				if (args.Length == 4)
				{
					var amount = double.Parse(args[1]);
					var fromCur = args[2];
					var toCur = args[3];

					var sb = new StringBuilder();
					sb.AppendFormat("http://google.com/ig/calculator?hl=en&q={0}{1}%3D%3F{2}",
						amount, fromCur, toCur);

					WebRequest req = WebRequest.Create(sb.ToString());
					StreamReader reader = null;
					try
					{
						reader = new StreamReader(req.GetResponse().GetResponseStream());
					}
					catch (WebException e)
					{
						SendTo(agent, e.ToString());
						reader.Close();
						return;
					}

					var result = reader.ReadToEnd();

					reader.Close();

					// {lhs: "100 Euros",rhs: "139.983271 Australian dollars",error: "",icc: true}
					// don't know what icc is
					var components = result.Substring(1, result.Length - 2).Split(',');
					Dictionary<string, string> results = new Dictionary<string, string>();
					foreach (var component in components)
					{
						string[] pieces = component.Split(':');
						string key = pieces[0].Trim();
						string value = pieces[1].Trim();
						value = value.Substring(1, value.Length - 2);
						results.Add(key, value);
					}

					var lhs = results["lhs"];
					var rhs = results["rhs"];

					if (lhs == "" || rhs == "")
					{
						SendTo(agent, "Possible invalid query. Raw return: " + result);
					}
					else
					{
						SendTo(agent, lhs + " ~ " + rhs);
					}
				}
			});

			foreach( var cmd in commands )
				if (c.Contains(":" + cmd.Key.Split(' ')[0]))
				{
					var args = GetArgs(c, ":" + cmd.Key.Split(' ')[0]);
					if (args.Length != cmd.Key.Split(' ').Length)
						SendTo(agent, "What?");
					else
						cmd.Value(args);
				}
		}

		static string[] GetArgs(string command, string directive)
		{
			return command.Split(' ').SkipWhile(a => a != directive).ToArray();
		}
		
		static void SendTo(string user, string res)
		{
			if (RespondAsWhisper)
				conn.Write("PRIVMSG {0} :{1}".F(user, res)); 
			else
				conn.Write("PRIVMSG {0} :{1}: {2}".F(Channel, user, res));
		}
		
		static void Send(string res)
		{
			conn.Write("PRIVMSG {0} :{1}".F(Channel, res));
		}
		
		static void Update()
		{
			Repo[] snapshot;
			lock (repos)
				snapshot = repos.ToArray();

			Git.Fetch();

			foreach (var info in Analyzer.Update(snapshot, Git.GetRefs()))
				Send("{0}".F(info));

			var t = Git.GetTags();
			var newTag = t.Except(tags).LastOrDefault();
			if (newTag != null)
				try
				{
					Send("Running external command for tag `{0}`".F(newTag));
					External.Run(TagScript, newTag);
					Send("Done.");
				}
				catch( Exception e )
				{
					Console.WriteLine(e);
					Send("Fail! Check the console log for why.");
				}
			tags = t;

			NextCheckTime = Environment.TickCount + CheckInterval;
		}
	}
}
