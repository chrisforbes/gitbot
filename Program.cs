using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;

namespace bot
{
	class Program
	{
		const int CheckInterval = 30000;
		const int PingInterval = 15000;

		static Connection conn;
		static List<Repo> repos = new List<Repo>();
		static int NextCheckTime = Environment.TickCount;
		static bool UseSocks = false;
		
		//server info
		static string Server = "irc.freenode.net";
		static int Port = 6667;
		static string Channel = "#openra";
		
		//bot info
		static string UserName = "pizzabot2";
		static string Nick = "openra_pizzabot2";
		static string IRCName = "PizzaBot Returns!";

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

			UseSocks = args.Contains("--socks");

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
			var agent = c.Split(':', '!', ' ').ElementAt(1);
			var commands = new Dictionary<string, Action<string[]>>();
			Action<string, Action<string[]>> Add = commands.Add;

			Add("@add <alias> <username/repo>", args =>
					{
						if (!repos.Any(a => a.Alias == args[1]))
						{
							if (!Git.AddRepo(args[1], args[2]))
								SendTo(agent, "Failed adding alias");
							else
							{
								lock (repos)
									repos.Add(new Repo(args[1]));
								SendTo(agent, "Done.");
							}
						}
						else
							SendTo(agent, "Alias already exists");

						NextCheckTime = Environment.TickCount;
					});

			Add("@rm <alias>", args =>
					{
						if (repos.Any(a => a.Alias == args[1]))
						{
							lock (repos)
								repos.RemoveAll(r => r.Alias == args[1]);
							SendTo(agent, "Done.");
						}
						else
							SendTo(agent, "Alias doesn't exist");
					});

			Add("@repolist", args =>
					{
						var names = "";
						lock (repos)
							names = string.Join(", ", repos.Select(r => r.Alias).ToArray());

						SendTo(agent, "I'm currently tracking: {0}".F(names));
					});

			Add("@quit", args =>
					{
						SendTo(agent, "Ok, Bye!");
						conn.Write("QUIT");
						conn.Stop();
					});

			Add("@help", args =>
				{
					SendTo(agent, "Commands:");
					foreach (var cmd in commands.Keys)
						SendTo(agent, "\t{0}".F(cmd));
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

			var lastRepo = "";
			foreach (var info in Repo.Update(snapshot, Git.GetRefs() ))
			{
				var alias = info.Split('/')[0];
				if (alias != lastRepo)
				{
					lastRepo = alias;
					Send("In `{0}`:".F(alias.Trim()));
				}
				Send("{0}".F(info));
			}

			NextCheckTime = Environment.TickCount + CheckInterval;
		}
	}
}
