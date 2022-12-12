using UnityEngine;
using UnityDesktopCharacter.UI;

namespace UnityDesktopCharacter.Abstract {
	[RequireComponent(typeof(ClickDetector))]
	public abstract class BaseMenu : MonoBehaviour {
		[SerializeField]
		private MenuHandle _handle = null;
		public MenuHandle Handle { get { return this._handle; } }
		public bool IsVisible { get { return this.Handle.IsVisible; } }
		public bool IsOpen { get { return this.Handle.IsOpen; } }

		[SerializeField]
		private ClickDetector _clickDetector = null;
		public ClickDetector ClickDetector { get { return this._clickDetector; } }

		public abstract void Initialize();
	}
}