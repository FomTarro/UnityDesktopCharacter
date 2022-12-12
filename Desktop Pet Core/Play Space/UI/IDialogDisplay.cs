using System;

namespace UnityDesktopCharacter.UI {
	public interface IDialogDisplay {
		void DisplayText(string text, Action onTextComplete);
	}
}