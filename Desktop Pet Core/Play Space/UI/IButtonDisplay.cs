using System;

namespace UnityDesktopCharacter.UI {
	public interface IButtonDisplay {
		void DisplayButtons(params ButtonOption[] buttons);
	}

	[System.Serializable]
	public struct ButtonOption {
		public readonly string text;
		public readonly Action onClick;
		public ButtonOption(string text, Action onClick) {
			this.text = text;
			this.onClick = onClick;
		}
	}
}