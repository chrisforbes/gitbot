using System.Net.Sockets;
using System.IO;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace bot
{
	class Program
	{
		const int CheckInterval = 30000;

		static Connection conn;
		static List<Repo> repos = new List<Repo>();
		static int NextCheckTime = Environment.TickCount;
		static bool UseSocks = false;

		static void Main(string[] args)
		{
			Socket s;
			if (UseSocks)
				s = SocksProxy.ConnectToSocks5Proxy("127.0.0.1", 1080,
					"irc.freenode.net", 6667, "", "");
			else
			{
				s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				s.Connect("irc.freenode.net", 6667);
			}
			conn = new Connection(s);
			
			conn.OnCommand += OnCommand;

			conn.Run();

			conn.Write("USER pizzabot a a :PizzaBot Returns!");
			conn.Write("NICK openra_pizzabot");
			conn.Write("JOIN #openra");

			while( conn.Connected )
			{
				// do stuff
				conn.Write("PING irc.freenode.net");
				Thread.Sleep(15000);

				if (NextCheckTime - Environment.TickCount < 0)
					Update();
			}

			Console.WriteLine("Connection dropped. Cause:");
			Console.WriteLine(conn.Exception);
		}

		static void OnCommand(string c)
		{
			Console.WriteLine(c);
			var agent = c.Split(':', '!', ' ').ElementAt(1);

			if (c.Contains(":@add"))
			{
				var args = c.Split(' ').SkipWhile(a => a != ":@add").ToArray();
				if (args.Length != 3)
				{
					conn.Write("PRIVMSG #openra :{0}: What? ({1})".F(agent, args.Length));
					return;
				}

				lock(repos)
					repos.Add(new Repo(args[1], args[2]));

				conn.Write("PRIVMSG #openra :{0}: Done.".F(agent));
				NextCheckTime = Environment.TickCount;
			}

			if (c.Contains(":@rm"))
			{
				var args = c.Split(' ').SkipWhile(a => a != ":@rm").ToArray();
				if (args.Length != 2)
				{
					conn.Write("PRIVMSG #openra :{0}: What?".F(agent));
					return;
				}

				lock (repos)
					repos.RemoveAll(r => r.Alias == args[1]);

				conn.Write("PRIVMSG #openra :{0}: Done.".F(agent));
			}

			if (c.Contains(":@repolist"))
			{
				var names = "";
				lock( repos )
					names = string.Join(",", repos.Select(r => r.Alias).ToArray());

				conn.Write("PRIVMSG #openra :{0}: I'm currently tracking: {1}".F(agent, names));
			}
		}

		static void Update()
		{
			Repo[] snapshot;
			lock (repos)
				snapshot = repos.ToArray();

			var lastRepo = "";
			foreach (var info in Repo.Update(snapshot))
			{
				var alias = info.Split('/')[0];
				if (alias != lastRepo)
				{
					lastRepo = alias;
					conn.Write("PRIVMSG #openra :In `{0}`:".F(alias.Trim()));
				}
				conn.Write("PRIVMSG #openra :{0}".F(info));
			}

			NextCheckTime = Environment.TickCount + CheckInterval;
		}
	}
}
