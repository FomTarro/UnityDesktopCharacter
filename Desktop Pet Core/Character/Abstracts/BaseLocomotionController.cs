using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace UnityDesktopCharacter.Abstract {
	public abstract class BaseLocomotionController : MonoBehaviour {

		private Coroutine _locomotionRoutine = null;
		/// <summary>
		/// Is the character currently pathing to location?
		/// </summary>
		/// <value></value>
		public bool IsCurrentlyMoving { get { return this._locomotionRoutine != null; } }

		[System.Serializable]
		public class LocomotionEvent : UnityEvent<Vector3> { }
		/// <summary>
		/// An event fired when the character starts pathfinding.
		/// </summary>
		/// <returns></returns>
		public LocomotionEvent OnLocomotionStart = new LocomotionEvent();
		/// <summary>
		/// An event fired when the character reaches its pathfinding destination.
		/// </summary>
		/// <returns></returns>
		public LocomotionEvent OnLocomotionEnd = new LocomotionEvent();
		/// <summary>
		/// An event fired when the character has its pathfinding interrupted.
		/// </summary>
		/// <returns></returns>
		public LocomotionEvent OnLocomotionInterrupt = new LocomotionEvent();

		public void StartLocomotion(Vector3 worldPoint) {
			StopLocomotion();
			this._locomotionRoutine = StartCoroutine(PathingWrapper(worldPoint));
		}

		public void StopLocomotion() {
			if (this._locomotionRoutine != null) {
				StopCoroutine(this._locomotionRoutine);
				this._locomotionRoutine = null;
				this.OnLocomotionInterrupt.Invoke(this.transform.position);
			}
		}

		private IEnumerator PathingWrapper(Vector3 targetWorldPoint) {
			this.OnLocomotionStart.Invoke(this.transform.position);
			yield return LocomotionRoutine(this.transform.position, targetWorldPoint);
			this.OnLocomotionEnd.Invoke(this.transform.position);
			this._locomotionRoutine = null;
		}

		protected abstract IEnumerator LocomotionRoutine(Vector3 startWorldPoint, Vector3 targetWorldPoint);
	}
}