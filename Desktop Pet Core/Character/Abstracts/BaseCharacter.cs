using UnityEngine;
using UnityEngine.Events;

namespace UnityDesktopCharacter.Abstract {
	public abstract class BaseCharacter : MonoBehaviour {

		private const float DRAG_FACTOR = 1.2f;
		public bool IsPickedUp { get; private set; }
		private Vector3 _pickedUpOffset = Vector2.zero;

		[Header("Colliders")]
		[SerializeField]
		protected Rigidbody2D _rigidbody = null;
		[SerializeField]
		protected Collider2D _collider = null;

		[SerializeField]
		private Transform _head = null;
		/// <summary>
		/// The transform representing this character's head. Useful for positioning other elements.
		/// </summary>
		/// <value></value>
		public Transform HeadTransform { get { return this._head; } }

		/// <summary>
		/// The approximate size of the character, based on their collision model.
		/// </summary>
		/// <value></value>
		public Vector2 Size { get { return this._collider.bounds.size; } }

		/// <summary>
		/// The current boundary collider that the characrer is standing on. Returns null if they are in the air.
		/// </summary>
		/// <value></value>
		public BoundaryCollider CurrentBoundaryCollider {
			get {
				foreach (BoundaryCollider collider in PlayspaceManager.Instance.Boundaries) {
					if (collider.IsTouching(this._collider)) {
						return collider;
					}
				}
				return null;
			}
		}

		[Header("Events")]
		/// <summary>
		/// An event fired when the character is picked up.
		/// </summary>
		/// <returns></returns>
		public UnityEvent OnPickUp = new UnityEvent();
		/// <summary>
		/// An event fired when the character is dropped.
		/// </summary>
		/// <returns></returns>
		public UnityEvent OnDrop = new UnityEvent();

		// FixedUpdate is called once per physics frame.
		private void FixedUpdate() {
			if (this.IsPickedUp) {
				Vector3 worldPos = GetMousePosAsWorldPos();
				// Debug.Log(worldPos);
				PushTowardsPosition(worldPos);
			}
		}

		/// <summary>
		/// Picks up the character and applies force to move them towards the pointer position until dropped.
		/// </summary>
		public void PickUp() {
			this.IsPickedUp = true;
			this._rigidbody.gravityScale = 0;
			this._pickedUpOffset = GetMousePosAsWorldPos() - this.transform.position;
			this.OnPickUp.Invoke();
		}

		/// <summary>
		/// Releases the character from the pointer position.
		/// </summary>
		public void Drop() {
			this.IsPickedUp = false;
			this._rigidbody.gravityScale = this._rigidbody.drag;
			this._pickedUpOffset = Vector3.zero;
			this.OnDrop.Invoke();
		}

		/// <summary>
		/// Applies force to push the character towards the designated world position.
		/// </summary>
		/// <param name="position"></param>
		public void PushTowardsPosition(Vector3 position) {
			Vector3 scaleFactor = Vector3.one;
			float dragFactor = DRAG_FACTOR * this._rigidbody.drag;
			Vector3 force = Vector3.Scale(scaleFactor, Mathf.Max(1, dragFactor)
				* (position - this.transform.position - this._pickedUpOffset));
			this._rigidbody.AddForce(force);
		}

		#region Position Helpers

		public Vector3 GetDepthFromCamera() {
			return new Vector3(
				0,
				0,
				this.transform.position.z - PlayspaceManager.Instance.CameraDepth);
		}

		protected Vector3 GetMousePosAsWorldPos() {
			Vector3 mousePos = PlayspaceManager.Instance.ScaledScreenMousePosition;
			return PlayspaceManager.Instance.ScaledScreenToWorld(mousePos + GetDepthFromCamera(), true);
		}

		#endregion
	}
}