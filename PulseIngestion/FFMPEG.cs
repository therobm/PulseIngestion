using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PulseIngestion
{
	public class FfmpegLauncher
	{
		public static async Task ToMP3(string fileSource)
		{
			Console.WriteLine("Processing: " + Path.GetFileName(fileSource));
			string exePath = DConfig.s_activeConfig.FFMpegLocation;
			string mp3 = Path.ChangeExtension(fileSource, ".mp3");
			string fileName = Path.GetFileName(mp3);

			string args = $"-y -threads 1 -i \"{fileSource}\" -vn -codec:a libmp3lame -b:a 320k -ar 44100 -map_metadata 0 -id3v2_version 3 -write_id3v1 1 \"{mp3}\"";

			try
			{
				await RunFfmpegCommand(exePath, args, fileName);
				if (DConfig.s_activeConfig.DeleteAfterConversion)
				{
					File.Delete(fileSource);
				}
				else
				{
					string backupFile = Path.ChangeExtension(fileSource, ".flac.bak");
					File.Move(fileSource, backupFile);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		public static async Task RunFfmpegCommand(string ffmpegPath, string arguments, string consoleOutputPath)
		{
			Process process = new Process();
			process.StartInfo.FileName = ffmpegPath;
			process.StartInfo.Arguments = arguments;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;

			process.OutputDataReceived += OnFfmpegOutputData;
			process.ErrorDataReceived += OnFfmpegErrorData;

			process.Start();

			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			await process.WaitForExitAsync();
			if (process.ExitCode != 0)
			{
				throw new Exception($"FFmpeg exited with code {process.ExitCode}");
			}
		}

		private static void OnFfmpegOutputData(object sender, DataReceivedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.Data) && DConfig.s_activeConfig.EnableFFMpegDebug)
			{
				Console.WriteLine(e.Data);
			}
		}

		private static void OnFfmpegErrorData(object sender, DataReceivedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.Data) && DConfig.s_activeConfig.EnableFFMpegDebug)
			{
				Console.WriteLine(e.Data);
			}
		}
	}
}
