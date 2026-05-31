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
		static string s_configFile = "piConfig.json";
		static bool s_bFirstRun = false;
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
				// First run: write defaults and signal the caller to stop so the user
				// can edit the new file before any scanning happens.
				s_activeConfig = new PIConfig();
				Save();
				s_bFirstRun = true;
				return s_activeConfig;
			}
			else
			{
				LoadFromFile();
			}
#endif
			s_activeConfig.ValidateConfig();
			return s_activeConfig;
		}

		/// <summary>
		/// True when Load() just created the config file for the first time. The app
		/// should report this and exit rather than scan with untouched defaults.
		/// </summary>
		public static bool WasFirstRun()
		{
			return s_bFirstRun;
		}

		public static string GetConfigFileName()
		{
			return s_configFile;
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
