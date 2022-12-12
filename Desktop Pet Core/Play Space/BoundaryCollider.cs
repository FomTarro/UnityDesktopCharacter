using System;
using UnityEngine;

namespace UnityDesktopCharacter {
	public class BoundaryCollider : MonoBehaviour {
		private const float OFFSET = 0.1f;
		[SerializeField]
		private BoxCollider2D _colliderMesh = null;
		[SerializeField]
		private PlatformEffector2D _effector = null;
		[SerializeField]
		private LineRenderer _renderer = null;

		/// <summary>
		/// The size of this boundary.
		/// </summary>
		/// <returns></returns>
		public Vector2 Size { get { return new Vector2(this._colliderMesh.size.x, this._colliderMesh.size.y); } }
		/// <summary>
		/// The world coordinates of the start and end of this boundary.
		/// </summary>
		/// <value></value>
		public BoundCoordinates WorldBounds { get; private set; }
		/// <summary>
		/// The world coordinates of the start and end of this boundary.
		/// </summary>
		/// <value></value>
		public BoundCoordinates ScreenBounds { get; private set; }

		[System.Serializable]
		public struct BoundCoordinates {
			public readonly Vector2 start;
			public readonly Vector2 end;
			public readonly Vector2 center;
			public BoundCoordinates(Vector2 start, Vector2 end, Vector2 center) {
				this.start = start;
				this.end = end;
				this.center = center;
			}

			public override string ToString() {
				return JsonUtility.ToJson(start) + " - " + JsonUtility.ToJson(end) + " - " + JsonUtility.ToJson(center);
			}
		}

		[SerializeField]
		private string _id;
		public string ID { get { return this._id; } }

		public void Configure(WindowManager.ScreenSpaceLine line, bool draw) {
			this._id = line.ID;
			this.ScreenBounds = new BoundCoordinates(line.Start, line.End, line.Start + line.End / 2f);
			Vector3 start = PlayspaceManager.Instance.ScaledScreenToWorld(PlayspaceManager.Instance.ScreenToScaledScreen(line.Start), false);
			Vector3 end = PlayspaceManager.Instance.ScaledScreenToWorld(PlayspaceManager.Instance.ScreenToScaledScreen(line.End), false);
			Vector3 center = ((start + end) - new Vector3(0, OFFSET, 0)) / 2f;
			this.WorldBounds = new BoundCoordinates(start, end, center);
			float width = Math.Max(OFFSET, Math.Abs(end.x - start.x));
			float height = Math.Max(OFFSET, Math.Abs(end.y - start.y));

			if (!center.Equals(this.transform.position)) {
				this.transform.position = center;
			}
			if (!Mathf.Approximately(this._colliderMesh.size.x, width) || !Mathf.Approximately(this._colliderMesh.size.y, height)) {
				this._colliderMesh.size = new Vector2(width, height);
				this._effector.useOneWay = height < width;
			}

			if (draw) {
				this._renderer.positionCount = 2;
				Vector3[] positions = new Vector3[2];
				positions[0] = start + new Vector3(0, 0, 10);
				positions[1] = end + new Vector3(0, 0, 10);
				this._renderer.SetPositions(positions);
			}
			else {
				this._renderer.positionCount = 0;
			}
		}

		public override string ToString() {
			return string.Format("{0}_{1}", this._id, this._colliderMesh.size.ToString());
		}

		public bool IsTouching(Collider2D collider) {
			return this._colliderMesh.IsTouching(collider);
		}
	}
}
