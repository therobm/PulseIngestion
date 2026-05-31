using System;
using System.Diagnostics;
using System.IO;

namespace PulseIngestion
{
	class Program
	{
		static async Task Main(string[] args)
		{
			// Your app entry point

			DConfig.Load();
			System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
			stopwatch.Start();
			Console.WriteLine("FFMpeg Location is: " + DConfig.ActiveConfig.FFMpegLocation);
			Console.WriteLine("MusicSource is: " + DConfig.ActiveConfig.MusicSource);
			Console.WriteLine("MusicDestination is: " + DConfig.ActiveConfig.MusicDestination);

			if (DConfig.ActiveConfig.DeleteEmptyFolders)
			{
				Console.WriteLine("Cleaning up empty folders...");
				foreach (string folder in Directory.EnumerateDirectories(DConfig.ActiveConfig.MusicSource, "*.*", SearchOption.AllDirectories))
				{
					Console.Write(".");
					if (Directory.EnumerateFileSystemEntries(folder).Any())
						continue;
					Directory.Delete(folder);
				}
			}
			Console.WriteLine(".");

			Console.WriteLine("Scanning source directory.");
			List<string> filesToConvert = new List<string>();
			foreach (string f in Directory.EnumerateFiles(DConfig.ActiveConfig.MusicSource, "*.*", SearchOption.AllDirectories))
			{
				Console.Write(".");

				if (DConfig.ActiveConfig.CleanupDuplicatesByWildcard && f.Contains(DConfig.ActiveConfig.DuplicateFileWildcardToken))
				{
					//verify there is a copy without the wildcard before deleting
					string goodName = f.Replace(DConfig.ActiveConfig.DuplicateFileWildcardToken, "");
					if (File.Exists(goodName))
					{
						File.Delete(f);
						Console.WriteLine("Deleted by Wildcard: " + f);
					}
					else
					{
						//move this file to the good name so we don't think it's a duplicate
						File.Move(f, goodName);
						Console.WriteLine("Renamed by Wildcard: " + f);
					}
						continue;
				}


				bool found = false;
				foreach (string ext in DConfig.ActiveConfig.SourceExtensions)
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

			int total = filesToConvert.Count();
			Console.WriteLine("Found " + filesToConvert.Count() + " to convert.");

			SemaphoreSlim throttler = new SemaphoreSlim(DConfig.ActiveConfig.ThreadCount);
			List<Task> runningTasks = new List<Task>();

			//short test for debug

			int limit = 0;
			int queueCount = 0;
			int completedCount = 0;

			Stopwatch conversionStopwatch = new Stopwatch();
			conversionStopwatch.Start();
			foreach (string file in filesToConvert)
			{
				if (limit > 0 && queueCount >= limit)
					break;
				await throttler.WaitAsync();
				queueCount++;
				string pinnedFile = file;
				Task t = Task.Run(async () =>
				{
					try
					{
						await FfmpegLauncher.ToMP3(pinnedFile);
						int completed = Interlocked.Increment(ref completedCount);
						int remaining = total - completed;
						double secondsPerFile = conversionStopwatch.Elapsed.TotalSeconds / completed;
						TimeSpan eta = TimeSpan.FromSeconds(secondsPerFile * remaining);
						Console.WriteLine("[" + completed + "/" + total + "] ETA: " + eta.ToString(@"hh\:mm\:ss") + " - " + Path.GetFileName(pinnedFile));
					}
					catch (Exception ex)
					{
						Console.WriteLine("Error converting " + pinnedFile + ": " + ex.Message);
					}
					finally
					{
						throttler.Release();
					}
				});
				runningTasks.Add(t);
			}
			await Task.WhenAll(runningTasks);

			stopwatch.Stop();
			Console.WriteLine("Task completed in " + stopwatch.Elapsed.ToString(@"hh\:mm\:ss"));


			for (int i = 0; i < 10; i++)
				Console.WriteLine(".");
		}


	}
}
