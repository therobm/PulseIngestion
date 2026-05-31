using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PulseIngestion
{
	using System.ComponentModel.DataAnnotations;
	using System.Diagnostics;
	using System.IO;
	using System.Linq.Expressions;
	using System.Threading.Tasks;

	public class FfmpegLauncher
	{



		public static async Task ToMP3(string fileSource)
		{
			Console.WriteLine("Processing: " + Path.GetFileName(fileSource));
			string exePath = DConfig.ActiveConfig.FFMpegLocation;
			string mp3 = Path.ChangeExtension(fileSource, ".mp3");
			string fileName = Path.GetFileName(mp3);

			string args = $"-y -threads 1 -i \"{fileSource}\" -vn -codec:a libmp3lame -b:a 320k -ar 44100 -map_metadata 0 -id3v2_version 3 -write_id3v1 1 \"{mp3}\"";

			try
			{
				await RunFfmpegCommand(exePath, args, fileName);
				if (DConfig.ActiveConfig.DeleteAfterConversion)
				{
					File.Delete(fileSource);
				}
				else
				{
					string backupFile = Path.ChangeExtension(fileSource, ".flac.bak");
					File.Move(fileSource, backupFile);
				}
			}
			catch(Exception ex)
			{
				//nobody gives a shit
				Console.WriteLine(ex);
			}

		}
		public static async Task RunFfmpegCommand(string ffmpegPath, string arguments, string consoleOutputPath)
		{
			Process process = new Process();
			process.StartInfo.FileName = ffmpegPath; // Path to ffmpeg.exe
			process.StartInfo.Arguments = arguments; // FFmpeg command-line arguments
			process.StartInfo.UseShellExecute = false; // Do not use the shell to start the process
			process.StartInfo.RedirectStandardInput = true; // Allow writing to stdin (e.g., to terminate)
			process.StartInfo.RedirectStandardOutput = true; // Redirect stdout for logging
			process.StartInfo.RedirectStandardError = true; // Redirect stderr for error messages

			process.OutputDataReceived += (sender, e) =>
			{
				if (!string.IsNullOrEmpty(e.Data) && DConfig.ActiveConfig.EnableFFMpegDebug)
					Console.WriteLine(e.Data);
			};

			process.ErrorDataReceived += (sender, e) =>
			{
				if (!string.IsNullOrEmpty(e.Data) && DConfig.ActiveConfig.EnableFFMpegDebug)
					Console.WriteLine(e.Data);
			};

			process.Start();

			// Begin async reading
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			await process.WaitForExitAsync(); // Wait for the process to complete
			if (process.ExitCode != 0)
			{
				throw new Exception($"FFmpeg exited with code {process.ExitCode}");
			}

		}

	}
}
