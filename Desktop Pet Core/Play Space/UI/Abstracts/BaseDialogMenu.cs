using System;
using UnityDesktopCharacter.UI;

namespace UnityDesktopCharacter.Abstract {
	public abstract class BaseDialogMenu : BaseMenu, IButtonDisplay, IDialogDisplay {
		public abstract void DisplayButtons(params ButtonOption[] buttons);
		public abstract void DisplayText(string text, Action onTextComplete);
	}
}