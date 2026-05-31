using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PulseIngestion
{
	class Program
	{
		private static int s_completedCount = 0;

		static async Task Main(string[] args)
		{
			DConfig.Load();
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			Console.WriteLine("FFMpeg Location is: " + DConfig.s_activeConfig.FFMpegLocation);
			Console.WriteLine("MusicSource is: " + DConfig.s_activeConfig.MusicSource);
			Console.WriteLine("MusicDestination is: " + DConfig.s_activeConfig.MusicDestination);

			if (DConfig.s_activeConfig.DeleteEmptyFolders)
			{
				Console.WriteLine("Cleaning up empty folders...");
				foreach (string folder in Directory.EnumerateDirectories(DConfig.s_activeConfig.MusicSource, "*.*", SearchOption.AllDirectories))
				{
					Console.Write(".");
					if (Directory.EnumerateFileSystemEntries(folder).Any())
					{
						continue;
					}
					Directory.Delete(folder);
				}
			}
			Console.WriteLine(".");

			Console.WriteLine("Scanning source directory.");
			List<string> filesToConvert = new List<string>();
			foreach (string f in Directory.EnumerateFiles(DConfig.s_activeConfig.MusicSource, "*.*", SearchOption.AllDirectories))
			{
				Console.Write(".");

				if (DConfig.s_activeConfig.CleanupDuplicatesByWildcard && f.Contains(DConfig.s_activeConfig.DuplicateFileWildcardToken))
				{
					string goodName = f.Replace(DConfig.s_activeConfig.DuplicateFileWildcardToken, "");
					if (File.Exists(goodName))
					{
						File.Delete(f);
						Console.WriteLine("Deleted by Wildcard: " + f);
					}
					else
					{
						File.Move(f, goodName);
						Console.WriteLine("Renamed by Wildcard: " + f);
					}
					continue;
				}

				bool found = false;
				foreach (string ext in DConfig.s_activeConfig.SourceExtensions)
				{
					if (f.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
					{
						found = true;
						break;
					}
				}
				if (found)
				{
					filesToConvert.Add(f);
					continue;
				}
			}

			int total = filesToConvert.Count;
			Console.WriteLine("Found " + filesToConvert.Count + " to convert.");

			SemaphoreSlim throttler = new SemaphoreSlim(DConfig.s_activeConfig.ThreadCount);
			List<Task> runningTasks = new List<Task>();

			int limit = 0;
			int queueCount = 0;

			Stopwatch conversionStopwatch = new Stopwatch();
			conversionStopwatch.Start();
			foreach (string file in filesToConvert)
			{
				if (limit > 0 && queueCount >= limit)
				{
					break;
				}
				await throttler.WaitAsync();
				queueCount++;
				string pinnedFile = file;
				Task conversionTask = ConvertFileAsync(pinnedFile, total, conversionStopwatch, throttler);
				runningTasks.Add(conversionTask);
			}
			await Task.WhenAll(runningTasks);

			stopwatch.Stop();
			Console.WriteLine("Task completed in " + stopwatch.Elapsed.ToString(@"hh\:mm\:ss"));

			for (int i = 0; i < 10; i++)
			{
				Console.WriteLine(".");
			}
		}

		private static async Task ConvertFileAsync(string file, int total, Stopwatch conversionStopwatch, SemaphoreSlim throttler)
		{
			try
			{
				await FfmpegLauncher.ToMP3(file);
				int completed = Interlocked.Increment(ref s_completedCount);
				int remaining = total - completed;
				double secondsPerFile = conversionStopwatch.Elapsed.TotalSeconds / completed;
				TimeSpan eta = TimeSpan.FromSeconds(secondsPerFile * remaining);
				Console.WriteLine("[" + completed + "/" + total + "] ETA: " + eta.ToString(@"hh\:mm\:ss") + " - " + Path.GetFileName(file));
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error converting " + file + ": " + ex.Message);
			}
			finally
			{
				throttler.Release();
			}
		}
	}
}
