using System;
using UnityEngine;

namespace UnityDesktopCharacter.Utils {
	/// <summary>
	/// Class for objects that exist on launch and need to be loaded in a specific order
	/// </summary>
	public abstract class OrderedInitializer : MonoBehaviour, IComparable {

		[Tooltip("The order in which the Initialize method will be called. Larger values go later.")]
		[SerializeField]
		private int _initializationOrder;

		/// <summary>
		/// Method that is called upon
		/// 
		/// Will be called during the Awake phase of the MonoBheaviour lifecycle.
		/// </summary>
		public abstract void Initialize();

		private static bool LOADING_STARTED = false;
		protected static bool INITIALIZED = false;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void AfterSceneLoad() {
			// the first object to run Start will Initialize all of the currently extant 
			if (!LOADING_STARTED) {
				LOADING_STARTED = true;
				OrderedInitializer[] objects = FindObjectsOfType<OrderedInitializer>();
				Array.Sort(objects);
				foreach (OrderedInitializer o in objects) {
					Debug.LogFormat("Initializing {0}", o.name);
					try {
						o.Initialize();
						INITIALIZED = true;
					}
					catch (Exception e) {
						Debug.LogError(string.Format("Error initializing {0} - {1}", o.name, e));
					}
				}
			}
		}

		public int CompareTo(object obj) {
			try {
				OrderedInitializer o = (OrderedInitializer)obj;
				return _initializationOrder - o._initializationOrder;
			}
			catch {
				return 0;
			}
		}
	}
}