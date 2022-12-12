using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityDesktopCharacter.Utils {
	/// <summary>
	/// A Least-Recently-Used dictionary that holds a finite number of items
	/// and disposes of the least recently accessed item when the limit is exceeded
	/// </summary>
	/// <typeparam name="K">Key type</typeparam>
	/// <typeparam name="V">Value type</typeparam>
	public class LRUDictionary<K, V> : IDictionary<K, V> {

		private struct LRUItem {
			public K key;
			public V value;
			public LRUItem(K key, V value) {
				this.key = key;
				this.value = value;
			}
		}

		private Dictionary<K, LinkedListNode<LRUItem>> _dict;
		private LinkedList<LRUItem> _list;
		private int _size = -1;

		private Action<V> _callback;

		/// <summary>
		/// Creates a new Least Recently Used Dictionary
		/// </summary>
		/// <param name="maxSize">The maximum number of items to store</param>
		/// <param name="callback">The disposal callback to invoke on values as they are removed</param>
		public LRUDictionary(int maxSize, Action<V> callback) {
			this._size = maxSize;
			this._list = new LinkedList<LRUItem>();
			this._dict = new Dictionary<K, LinkedListNode<LRUItem>>();
			this._callback = callback;
		}

		public V this[K key] {
			get { return Get(key); }
			set {
				if (this._dict.ContainsKey(key)) {
					Remove(key);
					Add(key, value);
				}
				else {
					throw new KeyNotFoundException();
				}
			}
		}

		public IEnumerable<K> Keys { get { return this._dict.Keys; } }

		ICollection<K> IDictionary<K, V>.Keys => this._dict.Keys;

		public ICollection<V> Values => GetValues();

		public int Count => this._dict.Count;
		public bool IsReadOnly => false;

		public bool ContainsKey(K key) { return this._dict.ContainsKey(key); }

		public void Clear() {
			foreach (LRUItem node in this._list) {
				this._callback.Invoke(node.value);
			}
			this._list.Clear();
			this._dict.Clear();
		}

		public V Get(K key) {
			LinkedListNode<LRUItem> node;
			if (this._dict.TryGetValue(key, out node)) {
				V value = node.Value.value;
				this._list.Remove(node);
				this._list.AddLast(node);
				return value;
			}
			throw new KeyNotFoundException();
		}

		public void Add(K key, V val) {
			if (this._dict.Count >= _size) {
				RemoveFirst();
			}

			LRUItem cacheItem = new LRUItem(key, val);
			LinkedListNode<LRUItem> node = new LinkedListNode<LRUItem>(cacheItem);
			this._list.AddLast(node);
			this._dict.Add(key, node);
		}

		public bool Remove(K key) {
			LinkedListNode<LRUItem> node;
			if (this._dict.TryGetValue(key, out node)) {
				V value = node.Value.value;
				this._list.Remove(node);
				this._dict.Remove(node.Value.key);
				if (this._callback != null) {
					this._callback.Invoke(value);
				}
				return true;
			}
			return false;
		}

		private void RemoveFirst() {
			// Remove from LRUPriority
			LinkedListNode<LRUItem> node = this._list.First;
			this._list.RemoveFirst();

			// Remove from cache
			this._dict.Remove(node.Value.key);
			if (this._callback != null) {
				this._callback.Invoke(node.Value.value);
			}
		}

		public bool TryGetValue(K key, out V value) {
			try {
				value = Get(key);
				return true;
			}
			catch {
				value = default(V);
				return false;
			}
		}

		public void Add(KeyValuePair<K, V> item) {
			Add(item.Key, item.Value);
		}

		public bool Contains(KeyValuePair<K, V> item) {
			return this._dict.ContainsKey(item.Key) && this._dict[item.Key].Equals(item.Value);
		}

		public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex) {
			if (array == null) {
				throw new ArgumentNullException();
			}
			if (arrayIndex < 0) {
				throw new ArgumentOutOfRangeException();
			}
			if (arrayIndex + this._dict.Count > array.Length) {
				throw new ArgumentException();
			}
			int i = 0;
			foreach (K key in this._dict.Keys) {
				array[arrayIndex + i] = new KeyValuePair<K, V>(key, this._dict[key].Value.value);
				i = i + 1;
			}
		}

		public bool Remove(KeyValuePair<K, V> item) {
			return Remove(item.Key);
		}

		public IEnumerator<KeyValuePair<K, V>> GetEnumerator() {
			List<KeyValuePair<K, V>> list = new List<KeyValuePair<K, V>>();
			foreach (K key in this._dict.Keys) {
				list.Add(new KeyValuePair<K, V>(key, this._dict[key].Value.value));
			}
			return list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}

		private ICollection<V> GetValues() {
			ICollection<V> list = new List<V>();
			foreach (K key in this._dict.Keys) {
				list.Add(this._dict[key].Value.value);
			}
			return list;
		}
	}
}