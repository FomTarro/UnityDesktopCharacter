using UnityDesktopCharacter.Utils;
using UnityEngine;

namespace UnityDesktopCharacter.Abstract {

	public abstract class BaseCharacterManager<T1, T2> : Singleton<T2> where T1 : BaseCharacter where T2 : BaseCharacterManager<T1, T2> {

		[SerializeField]
		private T1 _character = null;
		/// <summary>
		/// The desktop character.
		/// </summary>
		/// <value></value>
		public T1 Character { get { return this._character; } }

	}
}