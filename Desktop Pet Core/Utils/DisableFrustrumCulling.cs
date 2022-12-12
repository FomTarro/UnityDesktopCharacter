using UnityEngine;

namespace UnityDesktopCharacter.Utils {
	public class DisableFrustrumCulling : MonoBehaviour {

		private const int BOUND = 99999;
		private const float Z_NEAR = 0.001f;
		private const float Z_FAR = 99999;

		[SerializeField]
		private Camera _cam = null;

		private void OnPreCull() {
			this._cam.cullingMatrix =
				Matrix4x4.Ortho(-BOUND, BOUND, -BOUND, BOUND, Z_NEAR, Z_FAR) *
				Matrix4x4.Translate(Vector3.forward * -BOUND / 2f) *
				this._cam.worldToCameraMatrix;
		}

		private void OnDisable() {
			this._cam.ResetCullingMatrix();
		}
	}
}