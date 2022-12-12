using System;
using System.Collections.Generic;
using System.IO;
using UnityDesktopCharacter.Utils;
using UnityDesktopCharacter.IO;
using UnityEngine;

namespace UnityDesktopCharacter {
	public class SaveDataManager : Singleton<SaveDataManager> {

		private static string ROOT_PATH { get { return Application.persistentDataPath; } }

		public override void Initialize() { }

		/// <summary>
		/// Opens the root data folder in the file browser.
		/// </summary>
		public void OpenDataFolder() {
			Application.OpenURL(ROOT_PATH);
		}

		/// <summary>
		/// Writes data to disk based on the provided Saveable instance.
		/// </summary>
		/// <param name="source">The Saveable instance.</param>
		/// <typeparam name="T">The type of save data.</typeparam>
		public void WriteSaveData<T>(ISaveable<T> source) where T : BaseSaveData {
			try {
				string folder = string.Empty;
				bool directoryExists = CreateFolder(source.FileFolder, out folder);
				if (directoryExists) {
					T content = source.ToSaveData();
					content.version = Application.version;
					File.WriteAllText(Path.Combine(folder, source.FileName), source.TransformBeforeWrite(JsonUtility.ToJson(content, true)));
					Debug.Log(string.Format("Writing file: {0}", source.FileName));
				}
				else {
					throw new DirectoryNotFoundException("Could not write data to path: " + source.FileFolder);
				}
			}
			catch (Exception e) {
				Debug.LogError(e);
			}
		}

		/// <summary>
		/// Reads data from disk based on the provided Saveable instance
		/// </summary>
		/// <param name="source">The Saveable instance.</param>
		/// <typeparam name="T">The type of save data.</typeparam>
		/// <returns>The data which was read.</returns>
		public T ReadSaveData<T>(ISaveable<T> source) where T : BaseSaveData, new() {
			try {
				string path = Path.Combine(ROOT_PATH, source.FileFolder, source.FileName);
				if (File.Exists(path)) {
					string content = File.ReadAllText(path);
					Debug.Log(string.Format("Reading file: {0}", source.FileName));
					T data = JsonUtility.FromJson<T>(source.TransformAfterRead(content));
					return data;
				}
			}
			catch (Exception e) {
				Debug.LogError(e);
			}
			return new T();
		}

		// TODO: do these actually belong in the WindowManager? They don't use the Windows API BUT they have nothing to do with save data, inherently...
		public struct FileData {
			public readonly string path;
			public readonly string name;
			public readonly string extension;
			public FileData(string path, string name, string extension) {
				this.path = path;
				this.name = name;
				this.extension = extension;
			}
		}

		/// <summary>
		/// Creates a directory relative to the root Data Folder, if the directory does not already exist.
		/// </summary>
		/// <param name="directory">The name of the dicrectory to create.</param>
		/// <param name="path">The path name of the created or existing directory. Empty if unsuccessful.</param>
		/// <returns>True if the directory is created or already exists, false otherwise.</returns>
		public static bool CreateFolder(string directory, out string path) {
			try {
				string combined = Path.Combine(ROOT_PATH, directory);
				if (!Directory.Exists(combined)) {
					Directory.CreateDirectory(combined);
				}
				path = combined;
				return true;
			}
			catch (Exception e) {
				Debug.LogError(e);
			}
			path = string.Empty;
			return false;
		}

		/// <summary>
		/// Iterates over every file in a given directory, and performs the given callback on each one.
		/// </summary>
		/// <param name="directory">The directroy to iterate over.</param>
		/// <param name="onFile">The callback to execute on each file.</param>
		/// <returns>List of all iterated files.</returns>
		public List<FileData> IterateFilesInDirectory(string directory, Action<FileData> onFile) {
			string[] files = new string[0];
			try {
				files = Directory.GetFiles(directory);
			}
			catch (Exception e) {
				Debug.LogError(string.Format("Error iterating on files in directory: {0} - {1}", directory, e));
			}
			List<FileData> dataFiles = new List<FileData>();
			foreach (string filePath in files) {
				try {
					FileData data = new FileData(
						filePath,
						Path.GetFileName(filePath),
						Path.GetExtension(filePath)
					);
					dataFiles.Add(data);
					onFile.Invoke(data);
				}
				catch (Exception e) {
					Debug.LogError(string.Format("Error invoking callback on file: {0} - {1}", filePath, e));
				}
			}
			return dataFiles;
		}

		/// <summary>
		/// Copies files that pass the provided filter, from a provided list of file paths to a provided directory location.
		/// </summary>
		/// <param name="directory">The directory to copy files to.</param>
		/// <param name="filePaths">The list of file paths to copy.</param>
		/// <param name="filter">The filter to apply to the list of files.</param>
		/// <returns>List of all copied files</returns>
		public List<FileData> CopyFilesToDirectory(string directory, List<string> filePaths, Func<FileData, bool> filter) {
			List<FileData> newFiles = new List<FileData>();
			if (Directory.Exists(directory)) {
				foreach (string file in filePaths) {
					try {
						string name = Path.GetFileName(file);
						string newPath = Path.Combine(directory, name);
						FileData data = new FileData(
							newPath,
							name,
							Path.GetExtension(newPath)
						);
						if (filter == null || (filter != null && filter.Invoke(data))) {
							File.Copy(file, newPath, true);
							newFiles.Add(data);
						}
					}
					catch (Exception e) {
						Debug.LogError(string.Format("Error copying file to directory: {0} {1} - {2}", file, directory, e));
					}
				}
			}
			return newFiles;
		}

		/// <summary>
		/// Makes a file path into a File System URI, allowing browsers to treat the path like an address.
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		public string MakeFileSystemURI(string filePath) {
			return Path.Combine("file://", filePath);
		}

		public bool WriteImage(string directory, string filename, Texture2D image){
			try{
				string path = Path.Combine(directory, filename);
				byte[] bytes = image.EncodeToPNG();
				File.WriteAllBytes(path, bytes);
				return true;
			}catch(Exception e){
				Debug.LogError(string.Format("Error writing image file to directory: {0} - {1}", directory, e));
			}
			return false;
		}
	}
}