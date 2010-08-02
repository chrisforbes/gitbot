using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace bot
{
	class Connection
	{
		Socket s;
		NetworkStream stream;
		StreamReader reader;
		StreamWriter writer;
		Thread t;

		public volatile bool Connected;
		public Exception Exception;

		public Connection(Socket s)
		{
			this.s = s;
			stream = new NetworkStream(s);
			reader = new StreamReader(stream);
			writer = new StreamWriter(stream);
		}

		public event Action<string> OnCommand = _ => { };

		public void Run()
		{
			Connected = true;
			t = new Thread(() =>
			{
				try
				{
					for (; ; )
						OnCommand(reader.ReadLine());
				}
				catch (Exception e)
				{
					Exception = e;
					Connected = false;
				}
			}) { IsBackground = true };
			t.Start();
		}

		public void Stop()
		{
			s.Close();	/* fairly brutal, the thread will give up and die pretty fast now. */
		}

		public void Write(string text)
		{
			lock (this)
			{
				try
				{
					writer.WriteLine(text);
					writer.Flush();
				}
				catch { }
			}
		}
	}
}
