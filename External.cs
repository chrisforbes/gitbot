using System.Diagnostics;
using System.Text;
using System;

namespace bot
{
	class Output
	{
		public string StandardOutput;
		public string StandardError;
		public int ExitCode;
	}

	static class External
	{
		public static Output Run(string filename, string args)
		{
			var psi = new ProcessStartInfo
			{
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8,

				UseShellExecute = false,
				FileName = filename,
				Arguments = args,
			};

			var process = Process.Start(psi);
			process.WaitForExit();

			var result = new Output
			{
				ExitCode = process.ExitCode,
				StandardOutput = process.StandardOutput.ReadToEnd(),
				StandardError = process.StandardError.ReadToEnd()
			};

			Console.WriteLine("{0} {1} [Exited with {2}]",
				filename, args, result.ExitCode);

			return result;
		}
	}
}
