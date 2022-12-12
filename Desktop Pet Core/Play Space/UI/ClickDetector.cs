using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityDesktopCharacter.UI {
	public class ClickDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {

		private const float DOUBLE_CLICK_THRESHOLD = 0.5f;
		private float _timeSinceLastClick = 0f;

		[System.Serializable]
		public class PointerEvent : UnityEvent<PointerEventData> {
			public PointerEvent() { }
		}
		public PointerEvent OnClickBegin = new PointerEvent();
		public PointerEvent OnClickRelease = new PointerEvent();
		public PointerEvent OnDoubleClick = new PointerEvent();

		public void OnPointerDown(PointerEventData eventData) {
			this.OnClickBegin.Invoke(eventData);
		}

		public void OnPointerUp(PointerEventData eventData) {
			this.OnClickRelease.Invoke(eventData);
			if (this._timeSinceLastClick > 0) {
				this.OnDoubleClick.Invoke(eventData);
				this._timeSinceLastClick = 0f;
			}
			else {
				this._timeSinceLastClick = DOUBLE_CLICK_THRESHOLD;
			}
		}

		private void Update() {
			if (this._timeSinceLastClick > 0) {
				this._timeSinceLastClick = this._timeSinceLastClick - Time.deltaTime;
			}
		}
	}
}