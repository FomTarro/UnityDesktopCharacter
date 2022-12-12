using System.Collections;
using UnityDesktopCharacter.Abstract;

using UnityEngine;

namespace UnityDesktopCharacter {
	public class WalkingController : BaseLocomotionController {

		[SerializeField]
		private BaseCharacter _character = null;
		[SerializeField]
		private Rigidbody2D _rigidbody = null;
		[SerializeField]
		private float _walkSpeed = 1f;

		public Vector3 GetRandomDestinationOnBoundary() {
			BoundaryCollider boundary = this._character.CurrentBoundaryCollider;
			if (boundary != null) {
				Vector3 scaledStart = PlayspaceManager.Instance.ScreenToScaledScreen(boundary.ScreenBounds.start);
				Vector3 boundedStart = PlayspaceManager.Instance.ScaledScreenToWorld(scaledStart, true);
				Vector3 scaledEnd = PlayspaceManager.Instance.ScreenToScaledScreen(boundary.ScreenBounds.end);
				Vector3 boundedEnd = PlayspaceManager.Instance.ScaledScreenToWorld(scaledEnd, true);

				// Debug.Log(scaledEnd + " - " + boundedEnd);
				return new Vector3(UnityEngine.Random.Range(boundedStart.x, boundedEnd.x), 0, 0);
			}
			return this._character.transform.position;
		}

		protected override IEnumerator LocomotionRoutine(Vector3 startWorldPoint, Vector3 targetWorldPoint) {
			Vector3 targetHorizontal = new Vector3(targetWorldPoint.x, 0, 0);
			do {
				Vector3 currentHorizontal = new Vector3(this.transform.position.x, 0, 0);
				Vector3 direction = (targetHorizontal - currentHorizontal).normalized;
				if(this._character.CurrentBoundaryCollider != null){
					this._rigidbody.velocity = direction * this._walkSpeed;
				}
				yield return null;
			} while (Mathf.Abs(this.transform.position.x - targetWorldPoint.x) > 0.1f);
			yield return null;
		}
	}
}