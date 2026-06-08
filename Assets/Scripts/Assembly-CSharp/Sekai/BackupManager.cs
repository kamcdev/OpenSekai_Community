using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Sekai.CustomMusicScoreManager;
using Sekai.MusicScoreMaker.Common;
using UnityEngine;

namespace Sekai
{
	public static class BackupManager
	{
		public const string BackupDirectoryName = "backup";
		public const string ScoresSubDirectoryName = "scores";
		public const string SettingsSubDirectoryName = "settings";

		public enum RestoreScope
		{
			None = 0,
			ScoresOnly = 1,
			All = 2
		}

		public static string BackupDirectory => Path.Combine(Application.persistentDataPath, BackupDirectoryName);

		public static string StartBackup()
		{
			string uuid = Guid.NewGuid().ToString("N");
			string backupRoot = Path.Combine(BackupDirectory, uuid);
			string scoresDir = Path.Combine(backupRoot, ScoresSubDirectoryName);
			string settingsDir = Path.Combine(backupRoot, SettingsSubDirectoryName);

			Directory.CreateDirectory(scoresDir);
			Directory.CreateDirectory(settingsDir);

			var items = CustomMusicScoreManagerService.LoadItems();
			foreach (var item in items)
			{
				if (item?.Entry == null || string.IsNullOrEmpty(item.Entry.RootDirectory))
				{
					continue;
				}

				string scoreZipPath = Path.Combine(scoresDir, item.Entry.Manifest.id + ".zip");
				CustomMusicScoreManagerService.ExportZip(item.Entry, scoreZipPath);
			}

			BackupSettingsFile(settingsDir, Path.Combine(Application.persistentDataPath, "ApplicationLocalSettings.json"), "ApplicationLocalSettings.json");
			BackupSettingsFile(settingsDir, Path.Combine(Application.persistentDataPath, "LiveSettingData.json"), "LiveSettingData.json");
			BackupSettingsFile(settingsDir, Path.Combine(Application.persistentDataPath, "inappcache", "MusicScoreMakerSettingData.json"), "MusicScoreMakerSettingData.json");

			string zipPath = Path.Combine(BackupDirectory, uuid + ".zip");
			if (File.Exists(zipPath))
			{
				File.Delete(zipPath);
			}
			ZipFile.CreateFromDirectory(backupRoot, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);

			try
			{
				Directory.Delete(backupRoot, true);
			}
			catch
			{
			}

			return zipPath;
		}

		public static async Task<string> StartBackupAsync(Action<int, int> progressCallback = null)
		{
			return await Task.Run(() =>
			{
				string uuid = Guid.NewGuid().ToString("N");
				string backupRoot = Path.Combine(BackupDirectory, uuid);
				string scoresDir = Path.Combine(backupRoot, ScoresSubDirectoryName);
				string settingsDir = Path.Combine(backupRoot, SettingsSubDirectoryName);

				Directory.CreateDirectory(scoresDir);
				Directory.CreateDirectory(settingsDir);

				var items = CustomMusicScoreManagerService.LoadItems();
				int total = items.Count;
				int current = 0;

				foreach (var item in items)
				{
					current++;
					progressCallback?.Invoke(current, total);

					if (item?.Entry == null || string.IsNullOrEmpty(item.Entry.RootDirectory))
					{
						continue;
					}

					string scoreZipPath = Path.Combine(scoresDir, item.Entry.Manifest.id + ".zip");
					CustomMusicScoreManagerService.ExportZip(item.Entry, scoreZipPath);
				}

				progressCallback?.Invoke(total, total);

				BackupSettingsFile(settingsDir, Path.Combine(Application.persistentDataPath, "ApplicationLocalSettings.json"), "ApplicationLocalSettings.json");
				BackupSettingsFile(settingsDir, Path.Combine(Application.persistentDataPath, "LiveSettingData.json"), "LiveSettingData.json");
				BackupSettingsFile(settingsDir, Path.Combine(Application.persistentDataPath, "inappcache", "MusicScoreMakerSettingData.json"), "MusicScoreMakerSettingData.json");

				string zipPath = Path.Combine(BackupDirectory, uuid + ".zip");
				if (File.Exists(zipPath))
				{
					File.Delete(zipPath);
				}
				ZipFile.CreateFromDirectory(backupRoot, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);

				try
				{
					Directory.Delete(backupRoot, true);
				}
				catch
				{
				}

				return zipPath;
			});
		}

		public static bool StartRestore(string zipPath, RestoreScope scope)
		{
			if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath))
			{
				Debug.LogWarning("Backup file not found: " + zipPath);
				return false;
			}

			string tempRoot = Path.Combine(Application.temporaryCachePath, "Restore_" + Guid.NewGuid().ToString("N"));
			try
			{
				Directory.CreateDirectory(tempRoot);
				ZipFile.ExtractToDirectory(zipPath, tempRoot);

				string scoresDir = Path.Combine(tempRoot, ScoresSubDirectoryName);
				string settingsDir = Path.Combine(tempRoot, SettingsSubDirectoryName);

				if (scope == RestoreScope.All)
				{
					RestoreSettings(settingsDir);
				}

				if (scope == RestoreScope.ScoresOnly || scope == RestoreScope.All)
				{
					RestoreScores(scoresDir);
				}

				return true;
			}
			catch (Exception ex)
			{
				Debug.LogError("Restore failed: " + ex.Message);
				return false;
			}
			finally
			{
				try
				{
					if (Directory.Exists(tempRoot))
					{
						Directory.Delete(tempRoot, true);
					}
				}
				catch
				{
				}
			}
		}

		public static async Task<bool> StartRestoreAsync(string zipPath, RestoreScope scope, Action<int, int> progressCallback = null)
		{
			return await Task.Run(() =>
			{
				if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath))
				{
					Debug.LogWarning("Backup file not found: " + zipPath);
					return false;
				}

				string tempRoot = Path.Combine(Application.temporaryCachePath, "Restore_" + Guid.NewGuid().ToString("N"));
				try
				{
					Directory.CreateDirectory(tempRoot);
					ZipFile.ExtractToDirectory(zipPath, tempRoot);

					string scoresDir = Path.Combine(tempRoot, ScoresSubDirectoryName);
					string settingsDir = Path.Combine(tempRoot, SettingsSubDirectoryName);

					if (scope == RestoreScope.All)
					{
						RestoreSettings(settingsDir);
					}

					if (scope == RestoreScope.ScoresOnly || scope == RestoreScope.All)
					{
						if (Directory.Exists(scoresDir))
						{
							var zipFiles = Directory.GetFiles(scoresDir, "*.zip");
							int total = zipFiles.Length;
							int current = 0;

							foreach (var zipFile in zipFiles)
							{
								current++;
								progressCallback?.Invoke(current, total);

								try
								{
									CustomMusicScoreManagerService.ImportZip(zipFile);
								}
								catch (Exception ex)
								{
									Debug.LogError("Failed to restore score from " + zipFile + ": " + ex.Message);
								}
							}

							progressCallback?.Invoke(total, total);
						}
					}

					return true;
				}
				catch (Exception ex)
				{
					Debug.LogError("Restore failed: " + ex.Message);
					return false;
				}
				finally
				{
					try
					{
						if (Directory.Exists(tempRoot))
						{
							Directory.Delete(tempRoot, true);
						}
					}
					catch
					{
					}
				}
			});
		}

		public static void ShareBackup(string zipPath)
		{
			if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath))
			{
				return;
			}

#if UNITY_ANDROID
			try
			{
				using (AndroidJavaClass helper = new AndroidJavaClass("com.opensekai.ShareExportHelper"))
				{
					helper.CallStatic("ShareFile", zipPath);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("Share failed: " + ex.Message);
			}
#elif UNITY_STANDALONE || UNITY_EDITOR
			try
			{
				string directory = Path.GetDirectoryName(zipPath);
				if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
				{
					System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + zipPath + "\"");
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("Open explorer failed: " + ex.Message);
			}
#endif
		}

		private static void BackupSettingsFile(string destinationDir, string sourcePath, string fileName)
		{
			if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
			{
				return;
			}

			try
			{
				string destPath = Path.Combine(destinationDir, fileName);
				File.Copy(sourcePath, destPath, true);
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Failed to backup settings file " + sourcePath + ": " + ex.Message);
			}
		}

		private static void RestoreSettings(string settingsDir)
		{
			if (string.IsNullOrEmpty(settingsDir) || !Directory.Exists(settingsDir))
			{
				return;
			}

			string appLocalSettings = Path.Combine(settingsDir, "ApplicationLocalSettings.json");
			if (File.Exists(appLocalSettings))
			{
				try
				{
					string destPath = Path.Combine(Application.persistentDataPath, "ApplicationLocalSettings.json");
					File.Copy(appLocalSettings, destPath, true);
				}
				catch (Exception ex)
				{
					Debug.LogWarning("Failed to restore ApplicationLocalSettings: " + ex.Message);
				}
			}

			string liveSettingData = Path.Combine(settingsDir, "LiveSettingData.json");
			if (File.Exists(liveSettingData))
			{
				try
				{
					string destPath = Path.Combine(Application.persistentDataPath, "LiveSettingData.json");
					File.Copy(liveSettingData, destPath, true);
				}
				catch (Exception ex)
				{
					Debug.LogWarning("Failed to restore LiveSettingData: " + ex.Message);
				}
			}

			string musicScoreMakerSettings = Path.Combine(settingsDir, "MusicScoreMakerSettingData.json");
			if (File.Exists(musicScoreMakerSettings))
			{
				try
				{
					string destDir = Path.Combine(Application.persistentDataPath, "inappcache");
					Directory.CreateDirectory(destDir);
					string destPath = Path.Combine(destDir, "MusicScoreMakerSettingData.json");
					File.Copy(musicScoreMakerSettings, destPath, true);
				}
				catch (Exception ex)
				{
					Debug.LogWarning("Failed to restore MusicScoreMakerSettingData: " + ex.Message);
				}
			}
		}

		private static void RestoreScores(string scoresDir)
		{
			if (string.IsNullOrEmpty(scoresDir) || !Directory.Exists(scoresDir))
			{
				return;
			}

			var zipFiles = Directory.GetFiles(scoresDir, "*.zip");
			foreach (var zipFile in zipFiles)
			{
				try
				{
					CustomMusicScoreManagerService.ImportZip(zipFile);
				}
				catch (Exception ex)
				{
					Debug.LogError("Failed to restore score from " + zipFile + ": " + ex.Message);
				}
			}
		}
	}
}