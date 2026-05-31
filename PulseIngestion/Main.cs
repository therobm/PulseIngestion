

using PulseIngestion.Reporting;
using PulseIngestion.Scanners;
using System;
using System.Collections.Generic;
using System.Threading;

namespace PulseIngestion
{
	public class Main
	{
		volatile bool m_bIsRunning = false;
		PIConfig m_config;
		List<Scanner> m_scanners = new List<Scanner>();
		Thread m_loopThread;
		AutoResetEvent m_wakeEvent = new AutoResetEvent(false);

		public Main()
		{
		}

		public bool Initialize()
		{
			m_config = PIConfig.Load();

			if (PIConfig.WasFirstRun())
			{
				string fileName = PIConfig.GetConfigFileName();
				Log.Info("No config file found - a default " + fileName + " has been created.");
				Log.Info("Edit it for your environment, then relaunch PulseIngestion.");
				Console.WriteLine();
				Console.WriteLine("Press Enter to exit...");
				Console.ReadLine();
				return false;
			}

			Log.Info("FFMpeg Location is: " + m_config.FFMpegLocation);
			Log.Info("MusicSource is: " + m_config.MusicSource);
			Log.Info("MusicDestination is: " + m_config.MusicDestination);

			m_scanners.Add(new EmptyFolders(m_config));
			m_scanners.Add(new Deduplicate(m_config));
			m_scanners.Add(new MediaConversion(m_config));
			m_scanners.Add(new OrganizeLibrary(m_config));
			m_scanners.Add(new LibraryStats(m_config));

			for (int i = 0; i < m_scanners.Count; i++)
			{
				m_scanners[i].Initialize();
			}

			m_loopThread = new Thread(Loop);
			m_loopThread.IsBackground = false;
			m_loopThread.Name = "ScannerLoop";
			return true;
		}

		public void Start()
		{
			if (m_bIsRunning)
			{
				return;
			}
			m_bIsRunning = true;
			m_loopThread.Start();
		}

		public void Stop()
		{
			if (!m_bIsRunning)
			{
				return;
			}
			m_bIsRunning = false;
			m_wakeEvent.Set();
			m_loopThread.Join();
		}

		void Loop()
		{
			while (m_bIsRunning)
			{
				Report report = new Report();
				report.MarkStarted();

				for (int i = 0; i < m_scanners.Count; i++)
				{
					m_scanners[i].Pump(report);
				}

				report.MarkFinished();
				report.Write(m_config);

				if (!m_bIsRunning)
				{
					break;
				}

				int intervalMs = m_config.ScanningIntervalMinutes * 60 * 1000;
				m_wakeEvent.WaitOne(intervalMs);
			}
		}
	}
}
