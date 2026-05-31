using System.IO;
using System.Text.Json;

namespace PulseIngestion
{
	public class DConfig
	{
		static string s_configFile = "config.txt";
		public static DConfig s_activeConfig = new DConfig();

		public static void Load()
		{
			if (!File.Exists(s_configFile))
			{
				Save();
			}
			else
			{
				JsonSerializerOptions options = new JsonSerializerOptions
				{
					IncludeFields = true
				};
				string jsonString = File.ReadAllText(s_configFile);
				DConfig newConfig = JsonSerializer.Deserialize<DConfig>(jsonString, options);
				if (newConfig != null)
				{
					s_activeConfig = newConfig;
				}
				else
				{
					Save();
				}
			}
		}

		public static void Save()
		{
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				WriteIndented = true,
				IncludeFields = true
			};

			string jsonString = JsonSerializer.Serialize(s_activeConfig, options);
			File.WriteAllText(s_configFile, jsonString);
		}

		public string FFMpegLocation = "C:\\ffmpeg\\bin\\ffmpeg.exe";
		public string MusicSource = "Z:\\Music";
		public string MusicDestination = "Z:\\Music";
		public int ThreadCount = 32;
		public string[] SourceExtensions = { "flac", "aac" };
		public bool EnableFFMpegDebug = false;
		public bool DeleteAfterConversion = true;
		public bool DeleteEmptyFolders = true;
		public bool CleanupDuplicatesByWildcard = true;
		public string DuplicateFileWildcardToken = " (1)";
	}
}
