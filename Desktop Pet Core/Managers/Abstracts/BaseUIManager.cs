using System;
using System.Collections.Generic;
using UnityDesktopCharacter.UI;
using UnityDesktopCharacter.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace UnityDesktopCharacter.Abstract {
	public abstract class BaseUIManager<T1, T2, T3> : Singleton<T3> where T3 : BaseUIManager<T1, T2, T3> where T2 : Menu<T1> {

		[Header("Menu Settings")]
		[SerializeField]
		protected BaseDialogMenu _dialogMenu = null;
		[SerializeField]
		protected List<T2> _menus = new List<T2>();

		[Header("Events")]
		public UnityEvent OnAnyOpen = new UnityEvent();
		public UnityEvent OnAnyClose = new UnityEvent();

		/// <summary>
		/// Returns the number of currently open menus, including the dialog menu.
		/// </summary>
		/// <returns></returns>
		public int OpenMenuCount {
			get {
				return this._menus.FindAll((s) => { return s.instance.Handle.IsOpen; }).Count
					+ (this._dialogMenu.Handle.IsOpen ? 1 : 0);
			}
		}

		public override void Initialize() {
			Debug.Log("Initializing UI Setup...");
			this._dialogMenu.Initialize();
			this._dialogMenu.Handle.SetOpacity(0);
			this._dialogMenu.Handle.Close();
			this._dialogMenu.Handle.OnOpen.AddListener(OnAnyOpenWrapper);
			this._dialogMenu.Handle.OnClose.AddListener(OnAnyCloseWrapper);
			foreach (T2 menu in this._menus) {
				menu.instance.Initialize();
				menu.instance.Handle.SetOpacity(0);
				menu.instance.Handle.Close();
				menu.instance.Handle.OnOpen.AddListener(OnAnyOpenWrapper);
				menu.instance.Handle.OnClose.AddListener(OnAnyCloseWrapper);
			}
			Debug.Log("Completed UI Setup!");
		}

		/// <summary>
		/// Opens the menu of the given type, if one exists.
		/// </summary>
		/// <param name="menuType">The menu type to open.</param>
		public void OpenMenu(T1 menuType) {
			T2 menu = this._menus.Find((m) => m.type.Equals(menuType));
			if (menu != null) {
				menu.instance.Handle.Open();
			}
			else {
				Debug.LogWarning(string.Format("No menu of type {0} is registered to the UI Manager.", menuType));
			}
		}

		public BaseMenu GetMenu(T1 menuType) {
			T2 menu = this._menus.Find((m) => m.type.Equals(menuType));
			if (menu != null) {
				return menu.instance;
			}
			return null;
		}

		public void OpenDialogMenu(string text, Action onTextComplete, params ButtonOption[] options) {
			this._dialogMenu.Handle.Open();
			this._dialogMenu.DisplayText(text, onTextComplete);
			this._dialogMenu.DisplayButtons(options);
		}

		public void CloseDialogMenu() {
			this._dialogMenu.Handle.Close();
		}

		private void OnAnyOpenWrapper(){
			this.OnAnyOpen.Invoke();
		}

		private void OnAnyCloseWrapper(){
			this.OnAnyClose.Invoke();
		}
	}

	[System.Serializable]
	public abstract class Menu<K1> {
		public K1 type;
		public BaseMenu instance;
	}
}