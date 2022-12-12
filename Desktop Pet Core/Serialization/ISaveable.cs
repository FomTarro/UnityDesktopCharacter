namespace UnityDesktopCharacter.IO {
	public interface ISaveable<T> where T : BaseSaveData {

		/// <summary>
		/// The folder name to write data into, relative to this app's persistent data path.
		/// </summary>
		/// <value></value>
		string FileFolder { get; }
		/// <summary>
		/// The name of the file to read and write from, including extension.
		/// </summary>
		/// <value></value>
		string FileName { get; }

		/// <summary>
		/// Method for doing any data transformation after reading file content. For example, decryption. Should return valid JSON.
		/// </summary>
		/// <param name="content">String content from the file.</param>
		/// <returns>Valid JSON</returns>
		string TransformAfterRead(string content);
		/// <summary>
		/// Method for doing any data transformation before reading writing file JSON data. For example, encryption. Input will always be valid JSON.
		/// </summary>
		/// <param name="content">String content to write to the file.</param>
		/// <returns>String content</returns>
		string TransformBeforeWrite(string content);

		/// <summary>
		/// Method for acting on the data thatw as loaded from file.
		/// </summary>
		/// <param name="data">Object representing the data that has been loaded from file.</param>
		void FromSaveData(T data);
		/// <summary>
		/// Method for converting this object into a serializable data representation.
		/// </summary>
		/// <returns></returns>
		T ToSaveData();
	}
}