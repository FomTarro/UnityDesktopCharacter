using UnityDesktopCharacter.Abstract;
using UnityEngine;
using UnityEngine.Events;

namespace UnityDesktopCharacter.UI {
	public class MenuHandle : ClickDetector {

		[SerializeField]
		private RectTransform _rootElement = null;
		/// <summary>
		/// The local scale of the menu. Multiply with RootSize to get the "true size".
		/// </summary>
		/// <value></value>
		public Vector3 RootScale { get { return this._rootElement.transform.localScale; } }

		/// <summary>
		/// The screenspace position of the menu.
		/// </summary>
		/// <value></value>
		public Vector3 RootPosition { get { return this._rootElement.anchoredPosition; } }

		/// <summary>
		/// The screenspace size of the menu. Multiply with RootScale to get the "true size".
		/// </summary>
		/// <value></value>
		public Vector2 RootSize {
			get { return new Vector2(this._rootElement.rect.width, this._rootElement.rect.height); }
		}

		[SerializeField]
		private CanvasGroup _group = null;
		/// <summary>
		/// The opacity of the menu.
		/// </summary>
		/// <value></value>
		public float WindowAlpha { get { return this._group.alpha; } }

		private bool _isPickedUp = false;
		private Vector3 _pickedUpOffset = Vector3.zero;

		public bool IsVisible { get { return this._group.alpha > 0f; } }

		public bool IsOpen { get; private set; }

		[Header("Events")]
		/// <summary>
		/// An event which is fired when the menu is opened.
		/// </summary>
		/// <returns></returns>
		public UnityEvent OnOpen = new UnityEvent();
		/// <summary>
		/// An event which is fired when the menu is closed.
		/// </summary>
		/// <returns></returns>
		public UnityEvent OnClose = new UnityEvent();
		/// <summary>
		/// An event which is fired when the menu is brought to the foreground.
		/// </summary>
		/// <returns></returns>
		public UnityEvent OnFocus = new UnityEvent();
		/// <summary>
		/// An event which is fired when the picked up.
		/// </summary>
		/// <returns></returns>
		public UnityEvent OnPickUp = new UnityEvent();
		/// <summary>
		/// An event which is fired when the menu is dropped.
		/// </summary>
		/// <returns></returns>
		public UnityEvent OnDrop = new UnityEvent();

		// Start is called before the first frame update
		void Start() {
			this._rootElement.anchorMin = Vector2.zero;
			this._rootElement.anchorMax = Vector2.zero;
			SetPosition((PlayspaceManager.Instance.ScaledScreenSize/2f));

			this.OnClickBegin.AddListener((e) => { PickUp(); });
			this.OnClickRelease.AddListener((e) => { Drop(); });
			
			WindowManager.Instance.OnWindowResize.AddListener((o, n) => { SetPosition(this.RootPosition); });
			PlayspaceManager.Instance.OnScaleChange.AddListener((o, n) => { SetPosition(this.RootPosition); });
		}

		private void Update() {
			if (this._isPickedUp) {
				SetPosition(PlayspaceManager.Instance.ScaledScreenMousePosition);
			}
		}

		/// <summary>
		/// Brings the menu to the foreground, above other menus.
		/// </summary>
		public void BringToForeground() {
			Debug.Log(string.Format("Focusing menu: {0}", this._rootElement.name));
			this._rootElement.SetAsLastSibling();
			this.OnFocus.Invoke();
		}

		public void SendToBackground() {
			int index = this._rootElement.GetSiblingIndex();
			this._rootElement.SetAsFirstSibling();
			MenuHandle nextInLine = this._rootElement.parent.GetChild(index).GetComponentInChildren<MenuHandle>();
			if(nextInLine != null && nextInLine.IsOpen){
				nextInLine.BringToForeground();
			}

		}

		private void PickUp() {
			this._pickedUpOffset = (PlayspaceManager.Instance.ScaledScreenMousePosition - this.RootPosition);
			this._isPickedUp = true;
			BringToForeground();
			this.OnPickUp.Invoke();
		}

		private void Drop() {
			this._pickedUpOffset = Vector3.zero;
			this._isPickedUp = false;
			this.OnDrop.Invoke();
		}

		public void SetPosition(Vector3 position) {
			Vector3 adjustedPos = Vector3Int.FloorToInt(position - this._pickedUpOffset);
			Vector2 size = this.RootSize;
			adjustedPos = PlayspaceManager.Instance.ClampScaledScreenPosition(adjustedPos, size);
			this._rootElement.anchoredPosition = adjustedPos;
		}

		public void SetScale(Vector3 scale) {
			this._rootElement.localScale = scale;
		}

		public void SetOpacity(float opacity) {
			this._group.alpha = opacity;
		}

		/// <summary>
		/// Opens the menu. Does not fire the OnOpen event if the menu is already open.
		/// </summary>
		public void Open() {
			BringToForeground();
			this.IsOpen = true;
			if (!this._group.blocksRaycasts && !this._group.interactable) {
				Debug.Log(string.Format("Opening menu: {0}", this._rootElement.name));
				this.OnOpen.Invoke();
				this._group.interactable = true;
				this._group.blocksRaycasts = true;
			}
		}

		/// <summary>
		/// Closes the menu. Does not fire the OnClose event if the menu is already closed.
		/// </summary>
		public void Close() {
			this.IsOpen = false;
			if (this._group.interactable && this._group.blocksRaycasts) {
				Debug.Log(string.Format("Closing menu: {0}", this._rootElement.name));
				this.OnClose.Invoke();
				SendToBackground();
				this._group.interactable = false;
				this._group.blocksRaycasts = false;
			}
		}

	}
}