using UnityEngine;

namespace UnityDesktopCharacter {
	public class CanvasScaleChangeEventBroadcaster : MonoBehaviour {
        
		public PlayspaceManager.ScaleChangeEvent OnScaleChange = new PlayspaceManager.ScaleChangeEvent();
		private float _oldScale = 1f;

		private void OnRectTransformDimensionsChange() {
			try{
				if(this._oldScale != PlayspaceManager.Instance.ScreenScaleFactor){
					OnScaleChange.Invoke(this._oldScale, PlayspaceManager.Instance.ScreenScaleFactor);
				}
				this._oldScale = PlayspaceManager.Instance.ScreenScaleFactor;
			}catch(System.Exception e){
				Debug.LogWarning(string.Format("Error invoking OnScaleChange callback: {0}", e));
			}
		}
	}
}