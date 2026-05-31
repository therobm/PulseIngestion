
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;


namespace PulseIngestion
{
	public class DConfig
	{
		static string ConfigFile = "config.txt";
		public static DConfig ActiveConfig = new DConfig();

		public static void Load()
		{
			if (!File.Exists(ConfigFile))
			{
				Save();
			}
			else
			{
				var options = new JsonSerializerOptions
				{
					IncludeFields = true    // THIS allows fields to be serialized
				};
				string jsonString = File.ReadAllText(ConfigFile);
				DConfig newConfig = JsonSerializer.Deserialize<DConfig>(jsonString, options);
				if (newConfig != null)
					ActiveConfig = newConfig;
				else
					Save();
			}
			//FFMpegLocation = File.ReadAllText(ConfigFile);

		}
		public static void Save()
		{
			var options = new JsonSerializerOptions
			{
				WriteIndented = true,   // optional, makes JSON readable
				IncludeFields = true    // THIS allows fields to be serialized
			};

			string jsonString = JsonSerializer.Serialize(ActiveConfig, options);
			File.WriteAllText(ConfigFile, jsonString);
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
