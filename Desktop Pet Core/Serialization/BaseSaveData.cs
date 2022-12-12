using UnityEngine;

namespace UnityDesktopCharacter.IO {
	[System.Serializable]
	public abstract class BaseSaveData {
		public string version = Application.version;
	}
}