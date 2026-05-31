using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PulseIngestion
{
	public enum eMusicFormat
	{
		MP3,
		FLAC,
		M4A,
		AAC,
		OGG,
		OPUS,
		WAV,
		WMA,
		UNKNOWN
	}

	public class PIConfig
	{
		static string s_configFile = "config.txt";
		protected static PIConfig s_activeConfig = new PIConfig();
		// JsonStringEnumConverter writes/reads enums by name, so config.txt carries
		// "MP3" rather than an ordinal. Read matching is case-insensitive; an unknown
		// name throws JsonException (handled in LoadFromFile).
		static JsonSerializerOptions s_jsonOptions = BuildJsonOptions();

		public string FFMpegLocation = "C:\\ffmpeg\\bin\\ffmpeg.exe";
		public string MusicSource = "Z:\\MusicTest";
		public string MusicDestination = "Z:\\MusicTest";
		public string ReportDirectory = ""; // empty => <MusicSource>\PulseIngestion
		public int ThreadCount = 32;
		public int ScanningIntervalMinutes = 1440; //24h default
		public eMusicFormat DestinationMusicFormat = eMusicFormat.MP3;
		public eMusicFormat[] SourceFormats = { eMusicFormat.FLAC, eMusicFormat.AAC };
		public bool EnableFFMpegDebug = false;
		public bool EnableMediaConversion = true;
		public bool DeleteAfterConversion = false;
		public bool EnableOrganization = true;
		public bool RenameFilesToTrackTitle = true;
		public bool EnableLibraryStats = true;
		public bool RemoveEmptyDirectories = true;
		public bool CleanupDuplicatesByWildcard = true;
		public string DuplicateFileWildcardToken = " (1)";

		public bool ValidateConfig()
		{
			//todo add validation steps
			return true;
		}

		static JsonSerializerOptions BuildJsonOptions()
		{
			JsonSerializerOptions options = new JsonSerializerOptions();
			options.WriteIndented = true;
			options.IncludeFields = true;
			options.Converters.Add(new JsonStringEnumConverter());
			return options;
		}

		public static PIConfig Load()
		{
#if DEBUG
			//always rewrite our prefs in debug in case we forget and run a local test on a non-test env
			s_activeConfig = new PIConfig();
			Save();
#else
			if (!File.Exists(s_configFile))
			{
				s_activeConfig = new PIConfig();
				Save();
			}
			else
			{
				LoadFromFile();
			}
#endif
			s_activeConfig.ValidateConfig();
			return s_activeConfig;
		}

		private static void LoadFromFile()
		{
			string jsonString = File.ReadAllText(s_configFile);

			PIConfig newConfig;
			try
			{
				newConfig = JsonSerializer.Deserialize<PIConfig>(jsonString, s_jsonOptions);
			}
			catch (JsonException ex)
			{
				// Bad value (e.g. a mistyped DestinationMusicFormat) - keep defaults and
				// leave the file untouched so the user can find and fix their typo.
				Log.Error("Could not parse " + s_configFile + " - using defaults, file left untouched: " + ex.Message);
				s_activeConfig = new PIConfig();
				return;
			}

			if (newConfig != null)
			{
				s_activeConfig = newConfig;
			}
			else
			{
				s_activeConfig = new PIConfig();
			}
		}

		protected static void Save()
		{
			string jsonString = JsonSerializer.Serialize(s_activeConfig, s_jsonOptions);
			File.WriteAllText(s_configFile, jsonString);
		}
	}
}
