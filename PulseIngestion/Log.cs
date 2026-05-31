using System;
using System.IO;
using System.Text;

namespace PulseIngestion
{
	public static class Log
	{
		enum eLogType
		{
			Info,
			Warning,
			Error,
			Exception
		}

		static readonly object s_consoleLock = new object();
		static StreamWriter s_fileWriter;
		static string s_logDirectory = "logs";
		static long s_maxFileSize = 1 * 1024 * 1024; // 1MB per file
		static long s_currentSize = 0;
		static int s_maxFiles = 3;

		static Log()
		{
			Directory.CreateDirectory(s_logDirectory);
			OpenNewLogFile();
		}

		public static void Info(string message)
		{
			Write(eLogType.Info, message);
		}

		public static void Warning(string message)
		{
			Write(eLogType.Warning, message);
		}

		public static void Error(string message)
		{
			Write(eLogType.Error, message);
		}

		public static void Exception(Exception ex)
		{
			Write(eLogType.Exception, BuildExceptionText(null, ex));
		}

		public static void Exception(string message, Exception ex)
		{
			Write(eLogType.Exception, BuildExceptionText(message, ex));
		}

		static string BuildExceptionText(string message, Exception ex)
		{
			StringBuilder builder = new StringBuilder();

			if (!string.IsNullOrEmpty(message))
			{
				builder.Append(message);
				builder.Append(" - ");
			}

			if (ex == null)
			{
				builder.Append("(null exception)");
				return builder.ToString();
			}

			builder.Append(ex.GetType().Name);
			builder.Append(": ");
			builder.Append(ex.Message);

			Exception inner = ex.InnerException;
			while (inner != null)
			{
				builder.Append(" <- ");
				builder.Append(inner.GetType().Name);
				builder.Append(": ");
				builder.Append(inner.Message);
				inner = inner.InnerException;
			}

			if (!string.IsNullOrEmpty(ex.StackTrace))
			{
				builder.Append(Environment.NewLine);
				builder.Append(ex.StackTrace);
			}

			return builder.ToString();
		}

		static void Write(eLogType logType, string message)
		{
			// Locked so the concurrent conversion tasks don't interleave console
			// output or race on the shared file writer.
			lock (s_consoleLock)
			{
				string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
				string line = "[" + logType.ToString() + "][" + timestamp + "] " + message;

				bool colorize = logType != eLogType.Info;
				if (colorize)
				{
					if (logType == eLogType.Warning)
					{
						Console.ForegroundColor = ConsoleColor.Yellow;
					}
					else
					{
						Console.ForegroundColor = ConsoleColor.Red;
					}
				}

				Console.WriteLine(line);

				if (colorize)
				{
					Console.ResetColor();
				}

				WriteToFile(line);
			}
		}

		static void OpenNewLogFile()
		{
			if (s_fileWriter != null)
			{
				s_fileWriter.Close();
			}

			string filename = Path.Combine(s_logDirectory, "log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
			s_fileWriter = new StreamWriter(filename, false);
			s_fileWriter.AutoFlush = true;
			s_currentSize = 0;

			PruneOldFiles();
		}

		static void PruneOldFiles()
		{
			string[] files = Directory.GetFiles(s_logDirectory, "log_*.txt");
			if (files.Length <= s_maxFiles)
			{
				return;
			}

			Array.Sort(files);
			int removeCount = files.Length - s_maxFiles;
			for (int i = 0; i < removeCount; i++)
			{
				File.Delete(files[i]);
			}
		}

		static void WriteToFile(string line)
		{
			s_fileWriter.WriteLine(line);
			s_currentSize += line.Length + 2;
			if (s_currentSize >= s_maxFileSize)
			{
				OpenNewLogFile();
			}
		}
	}
}
