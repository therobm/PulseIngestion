using PulseIngestion.Metadata;
using System;
using System.Collections.Generic;
using System.IO;

namespace PulseIngestion.Scanners
{
	/// <summary>
	/// Read-only survey of the destination library. Reports counts (songs, albums,
	/// artists, size, per-format) so each report captures the library's state at
	/// that point in the cycle.
	/// </summary>
	public class LibraryStats : Scanner
	{
		// Joins artist+album into one key so two artists with a "Greatest Hits"
		// don't collapse into a single album. A tab won't appear in a real tag.
		private const string c_albumKeySeparator = "\t";

		private MetadataReader m_reader;

		public LibraryStats(PIConfig config) : base(config)
		{
			m_bIsActive = m_config.EnableLibraryStats;
			m_reader = new TagLibReader();
		}

		public override void Initialize()
		{
			m_workingDirectory = m_config.MusicDestination;
			base.Initialize();
		}

		protected override bool PinSectionToTop()
		{
			return true;
		}

		protected override void DoWork()
		{
			SurveyLibrary();
			base.DoWork();
		}

		private void SurveyLibrary()
		{
			if (!Directory.Exists(m_workingDirectory))
			{
				RecordWarning("Library directory does not exist: " + m_workingDirectory);
				OnComplete();
				return;
			}

			int songs = 0;
			int untagged = 0;
			long totalBytes = 0;
			HashSet<string> artists = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			HashSet<string> albums = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, int> byFormat = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

			foreach (string file in Directory.EnumerateFiles(m_workingDirectory, "*.*", SearchOption.AllDirectories))
			{
				ReportProgress();
				if (!MusicFormat.IsAudioFile(file))
				{
					continue;
				}

				songs++;
				totalBytes += GetFileLength(file);
				CountFormat(byFormat, file);

				bool tagged = AccumulateTags(file, artists, albums);
				if (!tagged)
				{
					untagged++;
				}
			}

			RecordInfo("Songs: " + songs.ToString("N0"));
			RecordInfo("Albums: " + albums.Count.ToString("N0"));
			RecordInfo("Artists: " + artists.Count.ToString("N0"));
			RecordInfo("Untagged files: " + untagged.ToString("N0"));
			RecordInfo("Total size: " + FormatSize(totalBytes));
			RecordFormatBreakdown(byFormat);

			OnComplete();
		}

		private bool AccumulateTags(string file, HashSet<string> artists, HashSet<string> albums)
		{
			TrackTags tags;
			try
			{
				tags = m_reader.Read(file);
			}
			catch (Exception)
			{
				return false;
			}

			bool hasArtist = !string.IsNullOrEmpty(tags.Artist);
			bool hasAlbum = !string.IsNullOrEmpty(tags.Album);
			if (!hasArtist || !hasAlbum)
			{
				return false;
			}

			artists.Add(tags.Artist);
			albums.Add(tags.Artist + c_albumKeySeparator + tags.Album);
			return true;
		}

		private static long GetFileLength(string file)
		{
			try
			{
				FileInfo info = new FileInfo(file);
				return info.Length;
			}
			catch (Exception)
			{
				return 0;
			}
		}

		private static void CountFormat(Dictionary<string, int> byFormat, string file)
		{
			string extension = Path.GetExtension(file).TrimStart('.').ToLowerInvariant();
			int current = 0;
			byFormat.TryGetValue(extension, out current);
			byFormat[extension] = current + 1;
		}

		private void RecordFormatBreakdown(Dictionary<string, int> byFormat)
		{
			if (byFormat.Count == 0)
			{
				return;
			}

			string[] extensions = new string[byFormat.Count];
			byFormat.Keys.CopyTo(extensions, 0);
			Array.Sort(extensions);

			for (int i = 0; i < extensions.Length; i++)
			{
				string extension = extensions[i];
				RecordInfo("Format " + extension + ": " + byFormat[extension].ToString("N0"));
			}
		}

		private static string FormatSize(long bytes)
		{
			double kb = 1024.0;
			double mb = kb * 1024.0;
			double gb = mb * 1024.0;

			if (bytes < mb)
			{
				return (bytes / kb).ToString("0.0") + " KB";
			}
			if (bytes < gb)
			{
				return (bytes / mb).ToString("0.0") + " MB";
			}
			return (bytes / gb).ToString("0.00") + " GB";
		}
	}
}
