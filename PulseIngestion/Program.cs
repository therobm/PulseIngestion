using System;
using System.Threading;

namespace PulseIngestion
{
	class Program
	{
		private static ManualResetEvent s_shutdownEvent = new ManualResetEvent(false);
		private static Main s_main;

		static void Main(string[] args)
		{
			s_main = new Main();
			if (!s_main.Initialize())
			{
				return;
			}
			s_main.Start();

			Console.CancelKeyPress += OnCancelKeyPress;

			s_shutdownEvent.WaitOne();
		}

		private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
		{
			e.Cancel = true;
			Log.Info("Shutdown requested.");
			s_main.Stop();
			s_shutdownEvent.Set();
		}
	}
}
