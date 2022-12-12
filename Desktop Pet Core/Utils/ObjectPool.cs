using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace UnityDesktopCharacter.Utils {
	public class ObjectPool<T> where T : UnityEngine.Component {

		private List<T> _active = new List<T>();
		public IEnumerable<T> ActiveObjects { get { return new ReadOnlyCollection<T>(this._active); } }
		private Queue<T> _inactive = new Queue<T>();
		private T _prefab = null;
		private Transform _parent = null;
		private int _initialSize = 0;

		public ObjectPool(T prefab, int initialSize, Transform parent) {
			this._initialSize = initialSize;
			this._prefab = prefab;
			this._parent = parent;
			for (int i = 0; i < this._initialSize; i++) {
				RetireInternal(Instantiate());
			}
		}

		private T Instantiate() {
			return MonoBehaviour.Instantiate<T>(this._prefab, Vector3.zero, Quaternion.identity);
		}

		private void RetireInternal(T instance) {
			instance.transform.SetParent(this._parent);
			instance.transform.localPosition = Vector3.zero;
			instance.gameObject.SetActive(false);

			this._inactive.Enqueue(instance);
		}

		public T GetNext() {
			if (this._inactive.Count <= 0) {
				RetireInternal(Instantiate());
			}
			T instance = this._inactive.Dequeue();
			instance.gameObject.transform.SetParent(null);
			instance.gameObject.SetActive(true);
			this._active.Add(instance);
			return instance;
		}

		public void Retire(T instance) {
			if (this._active.Contains(instance)) {
				this._active.Remove(instance);
				RetireInternal(instance);
			}
		}

		public void CullExcess() {
			if (this._active.Count + this._inactive.Count > this._initialSize
			&& this._inactive.Count > 0) {
				do {
					MonoBehaviour.Destroy(this._inactive.Dequeue().gameObject);
				} while (this._active.Count + this._inactive.Count > this._initialSize);
			}
		}
	}
}