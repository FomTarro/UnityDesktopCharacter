using System.Collections.Generic;
using UnityDesktopCharacter.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnityDesktopCharacter {
	public class PlayspaceManager : Singleton<PlayspaceManager> {

		[SerializeField]
		private Camera _camera = null;
		[SerializeField]
		private BoundaryCollider _borderColliderPrefab = null;
		private ObjectPool<BoundaryCollider> _pool;
		private Dictionary<string, BoundaryCollider> _colliders = new Dictionary<string, BoundaryCollider>();
		public List<BoundaryCollider> Boundaries { get { return new List<BoundaryCollider>(this._colliders.Values); } }

		public const float SCREEN_EDGE_BUFFER_X = 20f;
		public const float SCREEN_EDGE_BUFFER_Y = 20.5f;

		/// <summary>
		/// The Z-depth of the selected camera.
		/// </summary>
		/// <value></value>
		public float CameraDepth { get { return this._camera.transform.position.z; } }

		[Header("Misc")]
		[SerializeField]
		private Canvas _canvas = null;

		/// <summary>
		/// The size of the screen after factoring in scaling. The formula is 
		/// </summary>
		/// <value></value>
		public Vector2 ScaledScreenSize {
			get {
				return new Vector2(
				((RectTransform)this._canvas.transform).rect.width,
				((RectTransform)this._canvas.transform).rect.height);
			}
		}
		/// <summary>
		/// The current scale factor of the screen.
		/// </summary>
		/// <value></value>
		public float ScreenScaleFactor {
			get { return this._canvas.GetComponent<CanvasScaler>().scaleFactor; }
			set { this._canvas.GetComponent<CanvasScaler>().scaleFactor = value; }
		}

		/// <summary>
		/// The pointer position, clamped within valid playspace coordinates. 
		/// </summary>
		/// <value></value>
		public Vector3 ScaledScreenMousePosition {
			get {
				Vector2 localPoint = Vector2.zero;
				bool inRect = RectTransformUtility.ScreenPointToLocalPointInRectangle(
					(RectTransform)this._canvas.transform,
					Input.mousePosition,
					this._canvas.worldCamera,
					out localPoint);
				localPoint = localPoint + new Vector2(this.ScaledScreenSize.x / 2f, this.ScaledScreenSize.y / 2f);
				return ClampScaledScreenPosition(new Vector3(localPoint.x, localPoint.y, 0), Vector2.zero);
			}
		}

		/// <summary>
		/// The current framerate of the application.
		/// </summary>
		/// <value></value>
		public float FPS { get; private set; }

		/// <summary>
		/// An event that fires when the screen scale of the play space changes.
		/// 
		/// Argument represent old scale and the new scale.
		/// </summary>
		[System.Serializable]
		public class ScaleChangeEvent : UnityEvent<float, float> {
			public ScaleChangeEvent() { }
		}

		[Header("Events")]
		/// <summary>
		/// An event that fires when the resolution of the window changes in any way.
		/// </summary>
		/// <returns></returns>
		public ScaleChangeEvent OnScaleChange = new ScaleChangeEvent();

		public override void Initialize() {
			this._pool = new ObjectPool<BoundaryCollider>(this._borderColliderPrefab, 16, this.transform);
			CanvasScaleChangeEventBroadcaster broadcaster = this._canvas.gameObject.AddComponent<CanvasScaleChangeEventBroadcaster>();
			broadcaster.OnScaleChange.AddListener(OnScaleChange.Invoke);
			Application.targetFrameRate = 60;
		}

		// Update is called once per frame
		private float _deltaTime;
		private void Update() {
			CreateWindowColliders();
			this._deltaTime += (Time.deltaTime - this._deltaTime) * 0.1f;
			this.FPS = 1.0f / this._deltaTime;
		}

		public void Quit() {
			Debug.Log("Quitting application...");
			Application.Quit();
		}

		private void CreateWindowColliders() {
			List<WindowManager.ScreenSpaceLine> segments = WindowManager.Instance.GetWindowTopBorders();
			// segments.AddRange(WindowManager.Instance.GetDisplayBorder());
			List<string> keys = new List<string>(this._colliders.Keys);
			// retire unused colliders
			foreach (string id in keys) {
				WindowManager.ScreenSpaceLine match = segments.Find(s => s.ID.Equals(id));
				// no match for this ID
				if (match.Equals(default(WindowManager.ScreenSpaceLine))) {
					this._pool.Retire(this._colliders[id]);
					this._colliders.Remove(id);
				}
			}
			string debugText = "";
			// distribute new colliders
			foreach (WindowManager.ScreenSpaceLine segment in segments) {
				BoundaryCollider collider = this._colliders.ContainsKey(segment.ID)
					? this._colliders[segment.ID]
					: this._pool.GetNext();
				collider.Configure(segment, false);
				if (!this._colliders.ContainsKey(segment.ID)) {
					this._colliders.Add(segment.ID, collider);
				}
				debugText += collider.ToString() + "\n";
			}
		}

		/// <summary>
		/// Converts a raw pixel coordinate to a scaled screen (playspace) coordinate.
		/// </summary>
		/// <param name="pixelPosition">The raw pixel screen coordinate.</param>
		/// <returns></returns>
		public Vector3 ScreenToScaledScreen(Vector3 pixelPosition) {
			return pixelPosition / this.ScreenScaleFactor;
		}

		/// <summary>
		/// Converts a worldspace coordinate to a scaled screen coordinate.
		/// </summary>
		/// <param name="worldPosition">The worldpsace coordinate.</param>
		/// <returns></returns>
		public Vector3 WorldToScaledScreen(Vector3 worldPosition, bool bounded) {
			Vector3 screenPosition = this._camera.WorldToScreenPoint(worldPosition);
			Vector3 scaledPosition = new Vector3(screenPosition.x, screenPosition.y, 0f);
			if (bounded) {
				return ClampScaledScreenPosition(scaledPosition, Vector2.zero);
			}
			return scaledPosition;
		}

		/// <summary>
		/// Converts a scaled screen coordinate to a worldspace coordinate.
		/// </summary>
		/// <param name="scaledScreenPosition">The scaled screen coordinate.</param>
		/// <param name="bounded">Clamp the value to within the margins of the playspace?</param>
		/// <returns></returns>
		public Vector3 ScaledScreenToWorld(Vector3 scaledScreenPosition, bool bounded) {
			Vector2 screenExtents = this.ScaledScreenSize / 2f;
			Vector3 scaledPosition = new Vector3(scaledScreenPosition.x, scaledScreenPosition.y, 0);
			if (bounded) {
				scaledPosition = ClampScaledScreenPosition(scaledPosition, Vector2.zero);
			}
			return this._camera.ScreenToWorldPoint((scaledPosition) * this.ScreenScaleFactor);
		}

		/// <summary>
		/// Clamps a scaled screen coordinate to within the margins of the playspace on the screen.
		/// </summary>
		/// <param name="scaledScreenPosition">The scaled screen coordinate to clamp.</param>
		/// <param name="size">Size of the object at the given position.</param>
		/// <returns></returns>
		public Vector3 ClampScaledScreenPosition(Vector3 scaledScreenPosition, Vector2 size) {
			Vector2 extents = size / 2f;
			Vector2 screenExtents = this.ScaledScreenSize;
			return new Vector3(
				Mathf.Clamp(
					scaledScreenPosition.x,
					(PlayspaceManager.SCREEN_EDGE_BUFFER_X) + extents.x,
					(screenExtents.x - PlayspaceManager.SCREEN_EDGE_BUFFER_X) - extents.x),
				Mathf.Clamp(
					scaledScreenPosition.y,
					(PlayspaceManager.SCREEN_EDGE_BUFFER_Y) + extents.y,
					(screenExtents.y - PlayspaceManager.SCREEN_EDGE_BUFFER_Y) - extents.y),
				0);
			// return new Vector3(
			// 	Mathf.Clamp(
			// 		scaledScreenPosition.x,
			// 		(PlayspaceManager.SCREEN_EDGE_BUFFER_X - screenExtents.x) + extents.x,
			// 		(screenExtents.x - PlayspaceManager.SCREEN_EDGE_BUFFER_X) - extents.x),
			// 	Mathf.Clamp(
			// 		scaledScreenPosition.y,
			// 		(PlayspaceManager.SCREEN_EDGE_BUFFER_Y - screenExtents.y) + extents.y,
			// 		(screenExtents.y - PlayspaceManager.SCREEN_EDGE_BUFFER_Y) - extents.y),
			// 	0);
		}
	}
}