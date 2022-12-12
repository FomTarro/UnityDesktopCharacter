#if !UNITY_EDITOR
#define NOT_EDITOR
#endif

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityDesktopCharacter.Utils;
using UnityDesktopCharacter.WindowsAPI;
using UnityEngine;
using UnityEngine.Events;

namespace UnityDesktopCharacter {
	public class WindowManager : Singleton<WindowManager> {

		#region External Signatures

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private struct WindowMargin {
			public int cxLeftWidth;
			public int cxRightWifth;
			public int cyTopHeight;
			public int cyBottomHeight;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private struct WindowRect {
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct WindowPoint {
			public int x, y;
			public WindowPoint(int aX, int aY) {
				x = aX;
				y = aY;
			}
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private class MonitorInfo {

			public int cbSize = Marshal.SizeOf(typeof(MonitorInfo));
			public WindowRect rcMonitor = new WindowRect();
			public WindowRect rcWork = new WindowRect();
			public int dwFlags = 0;
			[MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = 32)]
			public char[] szDevice = new char[32];
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct SystemMessage {
			public IntPtr hwnd;
			public WindowsMessageType message;
			public IntPtr wParam;
			public IntPtr lParam;
			public ushort time;
			public WindowPoint pt;
		}

		private const uint MSG_DROP_FILES = 0x0233;
		private const int GWL_EXSTYLE = -20;
		private const uint WS_EX_LAYERED = 0x00080000;
		private const uint WS_EX_TRANSPARENT = 0x00000020;
		private const uint WS_EX_ACCEPTFILES = 0x00000010;
		private const uint WS_EX_COMPOSITED = 0x02000000;
		private const uint LWA_COLORKEY = 0x00000001;

		// Windows
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool EnumWindows(WindowEnumDelegate lpEnumFunc, IntPtr lParam);
		private delegate bool WindowEnumDelegate(IntPtr handleWindow, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern IntPtr GetActiveWindow();


		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool GetWindowRect(IntPtr hWnd, ref WindowRect rect);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern bool IsWindowVisible(IntPtr hWnd);


		[DllImport("user32.dll")]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWindInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		[DllImport("user32.dll")]
		private static extern int SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);


		// Monitors
		[DllImport("user32.dll")]
		static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr lParam);
		private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref WindowRect lprcMonitor, IntPtr lParam);
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern bool GetMonitorInfo(IntPtr hmonitor, [In, Out] MonitorInfo info);

		// File Drag and Drop
		[DllImport("shell32.dll")]
		private static extern void DragAcceptFiles(IntPtr hwnd, bool fAccept);
		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		private static extern uint DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, uint cch);
		[DllImport("shell32.dll")]
		private static extern void DragFinish(IntPtr hDrop);
		[DllImport("shell32.dll")]
		private static extern void DragQueryPoint(IntPtr hDrop, out WindowPoint pos);

		// Misc
		[DllImport("Dwmapi.dll")]
		private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref WindowMargin margins);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);
		[DllImport("kernel32.dll")]
		private static extern uint GetCurrentThreadId();

		[DllImport("user32.dll")]
		private static extern bool EnumThreadWindows(uint dwThreadId, ThreadEnumDelegate lpfn, IntPtr lParam);
		private delegate bool ThreadEnumDelegate(IntPtr Hwnd, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(WindowsHookType hookType, WindowsHookDelegate lpfn, IntPtr hMod, uint dwThreadId);
		private delegate IntPtr WindowsHookDelegate(int code, IntPtr wParam, IntPtr lParam);
		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);
		[DllImport("user32.dll")]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		#endregion

		#region Member Variables

		private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
		private static IntPtr THIS_WINDOW_HANDLE;
		private static float Z_DEPTH = 2f;
		private static readonly WaitForEndOfFrame WAIT_FOR_END_OF_FRAME = new WaitForEndOfFrame();

		[Tooltip("Should the window allow intermediate alpha transparency, or rasterize everything into all-or-nothing alpha transparency?")]
		[SerializeField]
		private bool _alphaMode = true;
		private static Texture2D PIXEL_UNDER_POINTER;

		private float _oldWidth = 0.0f;
		private float _oldHeight = 0.0f;
		private DisplayInfo _currentDisplay;
		/// <summary>
		/// Info about the display monitor that the Unity application is currently on.
		/// </summary>
		/// <value></value>
		public DisplayInfo CurrentDisplay {
			get {
				if (default(DisplayInfo).Equals(this._currentDisplay)) {
					this._currentDisplay = Displays[0];
				}
				return this._currentDisplay;
			}
		}
		/// <summary>
		/// A list containing info about all available display monitors.
		/// </summary>
		/// <returns></returns>
		public List<DisplayInfo> Displays { get { return this.GetDisplays(); } }

		/// <summary>
		/// A list containing information about all visible windows on the desktop.
		/// </summary>
		/// <returns></returns>
		public List<WindowInfo> Windows { get { return this.GetWindows(true); } }

		/// <summary>
		/// An event that fires when the Unity window is clicked. 
		/// 
		/// Argument represent whether or not the click should pass through, and the screenspace position of the click.
		/// </summary>
		[System.Serializable]
		public class WindowClickEvent : UnityEvent<bool, Vector2> {
			public WindowClickEvent() { }
		}

		/// <summary>
		/// An event that fires when the resolution of the window changes in any way.
		/// 
		/// Argument represent the old resolution and the new resolution.
		/// </summary>
		[System.Serializable]
		public class WindowResizeEvent : UnityEvent<Vector2, Vector2> {
			public WindowResizeEvent() { }
		}

		[Header("Events")]
		/// <summary>
		/// An event that fires when the Unity window is clicked. 
		/// </summary>
		/// <returns></returns>
		public WindowClickEvent OnWindowClick = new WindowClickEvent();
		/// <summary>
		/// An event that fires when the resolution of the window changes in any way.
		/// </summary>
		/// <returns></returns>
		public WindowResizeEvent OnWindowResize = new WindowResizeEvent();

		/// <summary>
		/// An event that fires when a file is dragged and dropped into the window. 
		/// 
		/// Argument represent a list of file paths, and the screenspace position of the drop.
		/// </summary>
		[System.Serializable]
		public class FileDropEvent : UnityEvent<List<string>, Vector2> {
			public FileDropEvent() { }
		}
		public FileDropEvent OnFileDrop = new FileDropEvent();

		/// <summary>
		/// An event that fires when a key is pressed. 
		/// 
		/// Argument represent the virtual keycode.
		/// </summary>
		[System.Serializable]
		public class KeyboardEvent : UnityEvent<VirtualKeys> {
			public KeyboardEvent() { }
		}
		public KeyboardEvent OnKeyDown = new KeyboardEvent();
		public KeyboardEvent OnKeyUp = new KeyboardEvent();

		private Dictionary<string, FileSystemWatcher> _fsWatchers = new Dictionary<string, FileSystemWatcher>();

		[Header("Debug Settings")]
		[SerializeField]
		private LineRenderer DEBUG_LINE_PREFAB = null;
		private List<LineRenderer> DEBUG_WINDOW_BORDERS = new List<LineRenderer>();
		public DEBUG_DRAW_MODE DEBUG_MODE { get; private set; }

		[Serializable]
		public enum DEBUG_DRAW_MODE {
			NONE,
			WINDOW_BORDERS,
			WINDOW_TOPS,
		}

		#endregion

		#region Lifecycle

		public override void Initialize() {
			PIXEL_UNDER_POINTER = new Texture2D(1, 1, TextureFormat.RGBA32, false);
			THIS_WINDOW_HANDLE = GetThisWindowHandle(); ;
			StandaloneWindowSetup();
			InstallDropFilesHook();
			InstallLowLevelKeyboardHook();
			Application.runInBackground = true;
		}

		private void Update() {
			CheckClick();
			CheckDisplaySize();
			ProcessDropFilesEvents();
			ProcessKeyEvents();
			ProcessFileSystemChangeEvents();
			DetermineDebugMode();
		}

		private void OnApplicationQuit() {
			UninstallDropFilesHook();
			UninstallLowLevelKeyboardHook();
		}

		[System.Diagnostics.Conditional("NOT_EDITOR")]
		private void StandaloneWindowSetup() {
			Debug.Log("Initializing Window Settings...");
			WindowMargin margins = new WindowMargin { cxLeftWidth = -1 };
			DwmExtendFrameIntoClientArea(THIS_WINDOW_HANDLE, ref margins);
			if (!this._alphaMode) {
				// This does not handle elements with opacity, alphas have to be 0 or 1, no in-between
				SetLayeredWindowAttributes(THIS_WINDOW_HANDLE, 0, 0, LWA_COLORKEY);
				SetWindowLong(THIS_WINDOW_HANDLE, GWL_EXSTYLE, WS_EX_LAYERED);
			}
			else {
				SetWindowLong(THIS_WINDOW_HANDLE, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
			}
			SetWindowPos(THIS_WINDOW_HANDLE, HWND_TOPMOST, 0, 0, 0, 0, 0);
			Debug.Log("Completed Window Settings!");
		}

		[System.Diagnostics.Conditional("NOT_EDITOR")]
		private void CheckClick() {
			if (this._alphaMode) {
				StartCoroutine(AllowClickthrough((allow) => {
					uint layer = allow ? WS_EX_LAYERED | WS_EX_TRANSPARENT : WS_EX_LAYERED | WS_EX_ACCEPTFILES;
					SetWindowLong(THIS_WINDOW_HANDLE, GWL_EXSTYLE, layer);
					this.OnWindowClick.Invoke(!allow, Input.mousePosition);
				}));
			}
		}

		/// <summary>
		/// Should the click pass through this window?
		/// </summary>
		/// <param name="onCheckComplete">Callback that recieved the decision at the end of the frame.</param>
		/// <returns>True if the click should pass through the window, false if the click should be intercepted by the window.</returns>
		private static IEnumerator AllowClickthrough(System.Action<bool> onCheckComplete) {
			Vector3 mousePos = Input.mousePosition;
			// if the cursor is out of bounds, it's not over the app, therefore allow clickthrough
			// TODO: [d3d11] attempting to ReadPixels outside of RenderTexture bounds! Reading (1920, 574, 1921, 575) from (1920, 1080) 
			if (mousePos.x > Screen.width - 1 || mousePos.x < 0 || mousePos.y > Screen.height - 1 || mousePos.y < 0) {
				onCheckComplete.Invoke(true);
			}
			else {
				Rect regionToReadFrom = new Rect((int)mousePos.x, (int)mousePos.y, 1, 1);
				bool updateMipMapsAutomatically = false;
				// this is a coroutine because we need to wait until the end of the frame to read pixel data
				yield return WAIT_FOR_END_OF_FRAME;
				PIXEL_UNDER_POINTER.ReadPixels(regionToReadFrom, 0, 0, updateMipMapsAutomatically);

				// if the cursor is over an area with full transparency, allow click through
				onCheckComplete.Invoke(PIXEL_UNDER_POINTER.GetPixel(0, 0).a == 0.0f);
			}
		}

		/// <summary>
		/// Adds a handle to a list that was passed in to unmanaged code as a handle itself.
		/// </summary>
		/// <param name="handle">The handle to add.</param>
		/// <param name="list">The list to add to.</param>
		private static void AddHandleToList(IntPtr handle, IntPtr list) {
			(GCHandle.FromIntPtr(list).Target as List<IntPtr>).Add(handle);
		}

		#endregion

		#region Display Selection

		[System.Serializable]
		public struct DisplayInfo {
			public string Availability { get; }
			public int ScreenHeight { get; }
			public int ScreenWidth { get; }
			public DisplayRect MonitorArea { get; }
			public DisplayRect WorkArea { get; }
			public string Name { get; }

			private IntPtr _handle;

			public DisplayInfo(IntPtr handle, int screenWidth, int screenHeight, DisplayRect monitorArea, DisplayRect workArea, string availability, string name) {
				this._handle = handle;
				this.ScreenWidth = screenWidth;
				this.ScreenHeight = screenHeight;
				this.MonitorArea = monitorArea;
				this.WorkArea = workArea;
				this.Availability = availability;
				this.Name = name;
			}

			[System.Serializable]
			public struct DisplayRect {
				public int left { get; }
				public int right { get; }
				public int top { get; }
				public int bottom { get; }

				public DisplayRect(int left, int right, int top, int bottom) {
					this.left = left;
					this.right = right;
					this.top = top;
					this.bottom = bottom;
				}
			}
		}

		private static bool AddDisplayHandle(IntPtr handle, IntPtr hdcMonitor, ref WindowRect lprcMonitor, IntPtr list) {
			AddHandleToList(handle, list);
			return true;
		}

		private List<DisplayInfo> GetDisplays() {
			List<IntPtr> handles = new List<IntPtr>();
			GCHandle handlesHandle = GCHandle.Alloc(handles);
			EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, AddDisplayHandle, GCHandle.ToIntPtr(handlesHandle));
			List<DisplayInfo> displays = new List<DisplayInfo>();
			foreach (IntPtr handle in handles) {
				MonitorInfo monitor = new MonitorInfo();
				monitor.cbSize = (int)Marshal.SizeOf(monitor);
				bool success = GetMonitorInfo(handle, monitor);
				if (success) {
					DisplayInfo display = new DisplayInfo(
						handle,
						(monitor.rcMonitor.right - monitor.rcMonitor.left),
						(monitor.rcMonitor.bottom - monitor.rcMonitor.top),
						new DisplayInfo.DisplayRect(monitor.rcMonitor.left, monitor.rcMonitor.right, monitor.rcMonitor.top, monitor.rcMonitor.bottom),
						new DisplayInfo.DisplayRect(monitor.rcWork.left, monitor.rcWork.right, monitor.rcWork.top, monitor.rcWork.bottom),
						monitor.dwFlags.ToString(),
						string.Format("Display {0}", displays.Count + 1)
					);
					// Debug.Log(display.Name + " - " + JsonUtility.ToJson(monitor.rcWork));
					displays.Add(display);
				}
			}
			handlesHandle.Free();
			return displays;
		}

		/// <summary>
		/// Move the Unity application to the designated monitor display.
		/// </summary>
		/// <param name="display">Display to move the application to.</param>
		/// <returns>Returns true if the operation is successful, false otherwise.</returns>
		public bool ChangeDisplay(DisplayInfo display) {
			Debug.Log(String.Format("Switching to {0} ({1}*{2})", display.Name, display.ScreenWidth, display.ScreenHeight));
			bool success = MoveWindow(THIS_WINDOW_HANDLE, display.WorkArea.left, display.WorkArea.top, display.ScreenWidth, display.ScreenHeight, true);
			if (success) {
				this._currentDisplay = display;
			}
			return success;
		}

		private void CheckDisplaySize() {
			if (this._oldWidth != Screen.width || this._oldHeight != Screen.height) {
				try {
					OnWindowResize.Invoke(new Vector3(this._oldWidth, this._oldHeight), new Vector2(Screen.width, Screen.height));
				}
				catch (Exception e) {
					Debug.LogError(string.Format("Error invoking OnWindowResize callback: {0}", e));
				}
				this._oldWidth = Screen.width;
				this._oldHeight = Screen.height;
			}
		}

		#endregion

		#region Window Selection

		[System.Serializable]
		public struct WindowInfo {

			public string Title { get; }
			public Vector2 Dimensions { get; }
			public WindowRect WindowArea { get; }
			public ScreenSpacePoints ScreenCoordinates { get; }
			public int Depth { get; }
			private IntPtr _handle;
			public string HandleID { get { return this._handle.ToString(); } }

			public WindowInfo(IntPtr handle, String title, WindowInfo.WindowRect area, int depth) {
				this._handle = handle;
				this.Title = title;
				this.WindowArea = area;
				this.Dimensions = new Vector2(
					WindowArea.right - WindowArea.left + 1,
					WindowArea.bottom - WindowArea.top + 1
				);
				this.Depth = depth;
				this.ScreenCoordinates = default(ScreenSpacePoints);
				this.ScreenCoordinates = GenerateScreenCoordinates();
			}

			private ScreenSpacePoints GenerateScreenCoordinates() {
				// TODO: this might need to accomodate for vertically stacked monitors
				Vector3 topLeft = new Vector3(
					(float)(WindowArea.left) - WindowManager.Instance.CurrentDisplay.MonitorArea.left,
					(float)(Screen.height - WindowArea.top),
					Z_DEPTH);
				Vector3 topRight = new Vector3(
					(float)(WindowArea.right) - WindowManager.Instance.CurrentDisplay.MonitorArea.left,
					(float)(Screen.height - WindowArea.top),
					Z_DEPTH);
				Vector3 bottomRight = new Vector3(
					(float)(WindowArea.right) - WindowManager.Instance.CurrentDisplay.MonitorArea.left,
					(float)(Screen.height - WindowArea.bottom),
					Z_DEPTH);
				Vector3 bottomLeft = new Vector3(
					(float)(WindowArea.left) - WindowManager.Instance.CurrentDisplay.MonitorArea.left,
					(float)(Screen.height - WindowArea.bottom),
					Z_DEPTH);
				return new ScreenSpacePoints(topLeft, topRight, bottomRight, bottomLeft);
			}

			[System.Serializable]
			public struct WindowRect {
				public int left;
				public int top;
				public int right;
				public int bottom;
				public WindowRect(int left, int right, int top, int bottom) {
					this.left = left;
					this.right = right;
					this.top = top;
					this.bottom = bottom;
				}
			}

			/// <summary>
			/// Returns true if the specified handle is for this window.
			/// </summary>
			/// <param name="handle"></param>
			/// <returns></returns>
			public bool IsSameHandle(IntPtr handle) {
				return this._handle.ToInt64() == handle.ToInt64();
			}

			public override bool Equals(object obj) {
				WindowInfo other = (WindowInfo)obj;
				return IsSameHandle(other._handle);
			}

			public override int GetHashCode() {
				return base.GetHashCode();
			}
		}

		public struct ScreenSpacePoints {
			public Vector3 TopLeft { get; }
			public Vector3 TopRight { get; }
			public Vector3 BottomRight { get; }
			public Vector3 BottomLeft { get; }

			public ScreenSpacePoints(Vector3 topLeft, Vector3 topRight, Vector3 bottomRight, Vector3 bottomLeft) {
				this.TopLeft = topLeft;
				this.TopRight = topRight;
				this.BottomRight = bottomRight;
				this.BottomLeft = bottomLeft;
			}
		}

		public struct ScreenSpaceLine {
			public Vector3 Start { get; }
			public Vector3 End { get; }
			public string ID { get; }
			public ScreenSpaceLine(Vector3 start, Vector3 end, string ID) {
				this.Start = start;
				this.End = end;
				this.ID = ID;
			}
		}

		private static bool AddWindowHandle(IntPtr handle, IntPtr list) {
			AddHandleToList(handle, list);
			return true;
		}

		private LRUDictionary<long, string> _windowTitles = new LRUDictionary<long, string>(64, (s) => { });
		private List<WindowInfo> GetWindows(bool onlyVisible) {
			List<IntPtr> handles = new List<IntPtr>();
			GCHandle handlesHandle = GCHandle.Alloc(handles);
			EnumWindows(AddWindowHandle, GCHandle.ToIntPtr(handlesHandle));
			List<WindowInfo> windows = new List<WindowInfo>();
			int z = 0;
			foreach (IntPtr handle in handles) {
				if (!onlyVisible || IsWindowVisible(handle)) {
					WindowRect rect = new WindowRect();
					GetWindowRect(handle, ref rect);
					if (!this._windowTitles.ContainsKey(handle.ToInt64())) {
						string title = string.Empty;
						int size = GetWindowTextLength(handle);
						if (size > 0) {
							StringBuilder builder = new StringBuilder(size + 1);
							GetWindowText(handle, builder, builder.Capacity);
							title = builder.ToString();
						}
						this._windowTitles.Add(handle.ToInt64(), title);
					}
					WindowInfo window = new WindowInfo(
						handle,
						this._windowTitles[handle.ToInt64()],
						new WindowInfo.WindowRect(rect.left, rect.right, rect.top, rect.bottom),
						z);
					windows.Add(window);
				}
				z = z + 1;
			}
			handlesHandle.Free();
			return windows;
		}

		private static IntPtr GetThisWindowHandle() {
			IntPtr returnHwnd = IntPtr.Zero;
			var threadId = GetCurrentThreadId();
			EnumThreadWindows(threadId,
				(hWnd, lParam) => {
					if (returnHwnd == IntPtr.Zero) returnHwnd = hWnd;
					return true;
				}, IntPtr.Zero);
			return returnHwnd;
		}

		/// <summary>
		/// Calculates the unobstructed lines running across the tops of all visible windows in the current display monitor.
		/// </summary>
		/// <returns>A list of line segments representing the top borders of all visible windows, as well as the "floor".</returns>
		public List<ScreenSpaceLine> GetWindowTopBorders() {
			string formattedID = "{0}_{1}_{2}";
			List<ScreenSpaceLine> allLines = new List<ScreenSpaceLine>();
			// remove the Unity App window and nameless windows from comparison
			List<WindowInfo> allWindows = Windows.Where((w) => w.Title.Length > 0 && !w.IsSameHandle(THIS_WINDOW_HANDLE)).ToList();
			foreach (WindowInfo window in allWindows) {
				// remove all windows lower in depth than the current window
				List<WindowInfo> higherWindows = allWindows.Where((w) => w.Depth < window.Depth).ToList();
				// sort so that lowest x value (leftmost) is first in list
				higherWindows.Sort((x, y) => x.WindowArea.left - y.WindowArea.left);
				List<ScreenSpaceLine> lines = new List<ScreenSpaceLine>();
				// establish complete, uninterrupted top line for this window
				ScreenSpaceLine initialLine = new ScreenSpaceLine(window.ScreenCoordinates.TopLeft, window.ScreenCoordinates.TopRight, string.Format(formattedID, window.Title, window.HandleID, 0));
				lines.Add(initialLine);
				for (int i = 0; i < lines.Count; i++) {
					// check if other window slices this line
					foreach (WindowInfo otherWindow in higherWindows) {
						ScreenSpaceLine line = lines[i];
						// in between left and right bound, split the line in two
						if (otherWindow.ScreenCoordinates.TopLeft.x > line.Start.x
						&& otherWindow.ScreenCoordinates.TopRight.x < line.End.x
						// check vertical position
						&& otherWindow.ScreenCoordinates.TopLeft.y >= window.ScreenCoordinates.TopLeft.y
						&& otherWindow.ScreenCoordinates.BottomLeft.y < window.ScreenCoordinates.TopLeft.y) {
							lines[i] = new ScreenSpaceLine(line.Start, new Vector3(otherWindow.ScreenCoordinates.TopLeft.x, line.End.y, line.End.z), line.ID);
							lines.Add(new ScreenSpaceLine(new Vector3(otherWindow.ScreenCoordinates.TopRight.x, line.Start.y, line.Start.z), line.End, string.Format(formattedID, window.Title, window.HandleID, i + 1)));
						}
						// overlaps and eclipses right bound
						else if (otherWindow.ScreenCoordinates.TopLeft.x > line.Start.x
						&& otherWindow.ScreenCoordinates.TopLeft.x < line.End.x
						&& otherWindow.ScreenCoordinates.TopRight.x >= line.End.x
						// check vertical position
						&& otherWindow.ScreenCoordinates.TopLeft.y >= window.ScreenCoordinates.TopLeft.y
						&& otherWindow.ScreenCoordinates.BottomLeft.y < window.ScreenCoordinates.TopLeft.y) {
							lines[i] = new ScreenSpaceLine(line.Start, new Vector3(otherWindow.ScreenCoordinates.TopLeft.x, line.End.y, line.End.z), line.ID);
						}
						// overlaps and eclipses left bound
						else if (otherWindow.ScreenCoordinates.TopLeft.x <= line.Start.x
						&& otherWindow.ScreenCoordinates.TopRight.x > line.Start.x
						&& otherWindow.ScreenCoordinates.TopRight.x < line.End.x
						// check vertical position
						&& otherWindow.ScreenCoordinates.TopLeft.y >= window.ScreenCoordinates.TopLeft.y
						&& otherWindow.ScreenCoordinates.BottomLeft.y < window.ScreenCoordinates.TopLeft.y) {
							lines[i] = new ScreenSpaceLine(new Vector3(otherWindow.ScreenCoordinates.TopRight.x, line.Start.y, line.Start.z), line.End, line.ID);
						}
						// totally eclipsed by another window, "discard" the line
						else if (otherWindow.ScreenCoordinates.TopLeft.x <= line.Start.x
						&& otherWindow.ScreenCoordinates.TopRight.x >= line.End.x
						// check vertical position
						&& otherWindow.ScreenCoordinates.TopLeft.y >= window.ScreenCoordinates.TopLeft.y
						&& otherWindow.ScreenCoordinates.BottomLeft.y < window.ScreenCoordinates.TopLeft.y) {
							lines[i] = new ScreenSpaceLine(-Vector3.one, -Vector3.one, line.ID);
						}
					}
				}
				// discard lines with zero distance or that are out of frame
				lines = lines.Where((l) => !l.Start.Equals(l.End) && l.Start.y < Screen.height).ToList();
				allLines.AddRange(lines);
			}
			// TODO: make the border line calculations their own function
			ScreenSpaceLine floor = new ScreenSpaceLine(new Vector3(-Screen.width, 0, Z_DEPTH), new Vector3(2 * Screen.width, 0, Z_DEPTH), string.Format(formattedID, "FLOOR", 0, 0));
			allLines.Add(floor);
			return allLines;
		}

		public List<ScreenSpaceLine> GetDisplayBorder() {
			ScreenSpaceLine floor = new ScreenSpaceLine(new Vector3(0, 0, Z_DEPTH), new Vector3(Screen.width, 0, Z_DEPTH), "FLOOR");
			ScreenSpaceLine celing = new ScreenSpaceLine(new Vector3(0, Screen.height, Z_DEPTH), new Vector3(Screen.width, Screen.height, Z_DEPTH), "CEILING");
			ScreenSpaceLine wallLeft = new ScreenSpaceLine(new Vector3(0, 0, Z_DEPTH), new Vector3(0, Screen.height, Z_DEPTH), "WALL_LEFT");
			ScreenSpaceLine wallRight = new ScreenSpaceLine(new Vector3(Screen.width, 0, Z_DEPTH), new Vector3(Screen.width, Screen.height, Z_DEPTH), "WALL_RIGHT");
			List<ScreenSpaceLine> frame = new List<ScreenSpaceLine>();
			frame.Add(floor);
			frame.Add(celing);
			frame.Add(wallLeft);
			frame.Add(wallRight);
			return frame;
		}

		#endregion

		#region Drag and Drop

		private struct DroppedFileInfo {

			public List<string> Files { get; }
			public WindowPoint Position { get; }

			public DroppedFileInfo(List<string> files, WindowPoint position) {
				this.Files = files;
				this.Position = position;
			}
		}

		private static IntPtr DROPPED_FILE_HOOK_HANDLE;
		private static readonly Queue<DroppedFileInfo> DROPPED_FILE_QUEUE = new Queue<DroppedFileInfo>();
		private static IntPtr EnqueueDroppedFiles(int code, IntPtr wParam, IntPtr lParam) {
			SystemMessage message = Marshal.PtrToStructure<SystemMessage>(lParam);
			if (code == 0 && message.message == WindowsMessageType.DROPFILES) {
				WindowPoint pos;
				DragQueryPoint(message.wParam, out pos);

				// 0xFFFFFFFF as index makes the method return the number of files
				uint fileCount = DragQueryFile(message.wParam, 0xFFFFFFFF, null, 0);
				StringBuilder builder = new StringBuilder(1024);

				List<string> result = new List<string>();
				for (uint i = 0; i < fileCount; i++) {
					int len = (int)DragQueryFile(message.wParam, i, builder, 1024);
					result.Add(builder.ToString(0, len));
					builder.Length = 0;
				}
				DragFinish(message.wParam);
				DROPPED_FILE_QUEUE.Enqueue(new DroppedFileInfo(result, pos));
			}
			return CallNextHookEx(DROPPED_FILE_HOOK_HANDLE, code, wParam, lParam);
		}

		[System.Diagnostics.Conditional("NOT_EDITOR")]
		private void InstallDropFilesHook() {
			Debug.Log("Installing drag and drop hook...");
			uint threadID = GetCurrentThreadId();
			IntPtr hModule = GetModuleHandle(null);
			DROPPED_FILE_HOOK_HANDLE = SetWindowsHookEx(WindowsHookType.WH_GETMESSAGE, EnqueueDroppedFiles, hModule, threadID);
			// Allow dragging of files onto the main window. generates the WM_DROPFILES message
			DragAcceptFiles(THIS_WINDOW_HANDLE, true);
			Debug.Log("Completed drag and drop hook installation!");
		}

		[System.Diagnostics.Conditional("NOT_EDITOR")]
		private void UninstallDropFilesHook() {
			Debug.Log("Uninstalling drag and drop hook...");
			UnhookWindowsHookEx(DROPPED_FILE_HOOK_HANDLE);
			DragAcceptFiles(THIS_WINDOW_HANDLE, false);
			DROPPED_FILE_HOOK_HANDLE = IntPtr.Zero;
			Debug.Log("Completed drag and drop hook uninstallation!");
		}

		private void ProcessDropFilesEvents() {
			while (DROPPED_FILE_QUEUE.Count > 0) {
				Debug.Log("Processing drop event...");
				DroppedFileInfo info = DROPPED_FILE_QUEUE.Dequeue();
				OnFileDrop.Invoke(info.Files, new Vector2(info.Position.x, info.Position.y));
			}
		}

		#endregion

		#region Keyboard

		private struct KeyboardInfo {
			public VirtualKeys Key { get; }
			public bool Up { get; }
			public KeyboardInfo(bool up, VirtualKeys key) {
				this.Up = up;
				this.Key = key;
			}
		}

		private static IntPtr KEYBOARD_HANDLE;
		private static readonly Queue<KeyboardInfo> KEYBOARD_QUEUE = new Queue<KeyboardInfo>();

		[System.Diagnostics.Conditional("NOT_EDITOR")]
		private void InstallLowLevelKeyboardHook() {
			Debug.Log("Installing keyboard hook...");
			uint threadID = GetCurrentThreadId();
			IntPtr hModule = GetModuleHandle(null);
			KEYBOARD_HANDLE = SetWindowsHookEx(WindowsHookType.WH_KEYBOARD_LL, RecieveKeyEvent, hModule, 0);
			// Allow dragging of files onto the main window. generates the WM_DROPFILES message
			Debug.Log("Completed keyboard hook installation!");
		}

		[System.Diagnostics.Conditional("NOT_EDITOR")]
		private void UninstallLowLevelKeyboardHook() {
			Debug.Log("Uninstalling keyboard hook...");
			UnhookWindowsHookEx(KEYBOARD_HANDLE);
			KEYBOARD_HANDLE = IntPtr.Zero;
			Debug.Log("Completed keyboard hook uninstallation!");
		}

		private static IntPtr RecieveKeyEvent(int code, IntPtr wParam, IntPtr lParam) {
			if (code >= 0) {
				int vkCode = Marshal.ReadInt32(lParam);
				int typeCode = wParam.ToInt32();
				if(typeCode == 256){
					KEYBOARD_QUEUE.Enqueue(new KeyboardInfo(true, (VirtualKeys)(vkCode)));
				}
				else if(typeCode == 255){
					KEYBOARD_QUEUE.Enqueue(new KeyboardInfo(false, (VirtualKeys)(vkCode)));
				}
			}
			return CallNextHookEx(KEYBOARD_HANDLE, code, wParam, lParam);
		}

		private void ProcessKeyEvents() {
			while (KEYBOARD_QUEUE.Count > 0) {
				KeyboardInfo info = KEYBOARD_QUEUE.Dequeue();
				if(info.Up){
					OnKeyUp.Invoke(info.Key);
				}else{
					OnKeyDown.Invoke(info.Key);
				}
			}
		}

		#endregion

		#region File System Hooks

		[System.Serializable]
		public struct FileSystemChangeEvent {
			public readonly string path;
			public readonly string name;
			public FileSystemChangeEvent(string path, string name) {
				this.path = path;
				this.name = name;
			}
		}

		private static readonly ConcurrentQueue<Action> FS_WATCHER_QUEUE = new ConcurrentQueue<Action>();
		/// <summary>
		/// Installs a hook to listen for file system changes in a specific directory.
		/// </summary>
		/// <param name="path">Path to the directory or file to watch.</param>
		/// <param name="onChange">Callback to execute when change occurs.</param>
		/// <returns>True if the hook is successfully installed, false otherwise.</returns>
		public bool InstallFileSystemHook(string path, Action<FileSystemChangeEvent> onChange) {
			if (Directory.Exists(path)) {
				// if we are not already listening to this path, make a new watcher
				if (!this._fsWatchers.ContainsKey(path)) {
					try {
						FileSystemWatcher watcher = new FileSystemWatcher(@path);
						watcher.NotifyFilter = NotifyFilters.Attributes
						| NotifyFilters.CreationTime
						| NotifyFilters.DirectoryName
						| NotifyFilters.FileName
						| NotifyFilters.LastAccess
						| NotifyFilters.LastWrite
						| NotifyFilters.Security
						| NotifyFilters.Size;
						watcher.Filter = string.Empty;
						watcher.IncludeSubdirectories = true;
						watcher.Error += new ErrorEventHandler((s, e) => {
							FS_WATCHER_QUEUE.Enqueue(() => { Debug.LogError(e); });
						});
						watcher.EnableRaisingEvents = true;
						this._fsWatchers.Add(path, watcher);
					}
					catch {
						return false;
					}
				}
				// apply designated callbacks to the listener
				// (They are wrapped in an Equeue so that they can be dequeued/called on the main thread)
				if (this._fsWatchers.ContainsKey(path)) {
					try {
						this._fsWatchers[path].Created += new FileSystemEventHandler((s, e) => {
							FS_WATCHER_QUEUE.Enqueue(() => { onChange.Invoke(new FileSystemChangeEvent(e.FullPath, e.Name)); });
						});
						this._fsWatchers[path].Deleted += new FileSystemEventHandler((s, e) => {
							FS_WATCHER_QUEUE.Enqueue(() => { onChange.Invoke(new FileSystemChangeEvent(e.FullPath, e.Name)); });
						});
						this._fsWatchers[path].EnableRaisingEvents = true;
					}
					catch {
						return false;
					}
					return true;
				}
			}
			return false;
		}

		private void ProcessFileSystemChangeEvents() {
			while (FS_WATCHER_QUEUE.Count > 0) {
				Action action = null;
				if (FS_WATCHER_QUEUE.TryDequeue(out action)) {
					if (action != null) {
						action.Invoke();
					}
				}
			}
		}

		#endregion

		#region Debug

		/// <summary>
		/// Sets the debug rendering mode.
		/// </summary>
		/// <param name="mode">Mode to set</param>
		public void SetDebugMode(DEBUG_DRAW_MODE mode) {
			this.DEBUG_MODE = mode;
		}

		private void DetermineDebugMode() {
			if (this.DEBUG_MODE == DEBUG_DRAW_MODE.WINDOW_BORDERS) {
				DebugDrawAllWindowBorders();
			}
			else if (this.DEBUG_MODE == DEBUG_DRAW_MODE.WINDOW_TOPS) {
				DebugDrawAllWindowTopLines();
			}
			else {
				ClearDebugDrawing();
			}
		}

		private void ClearDebugDrawing() {
			foreach (LineRenderer renderer in this.DEBUG_WINDOW_BORDERS) {
				Destroy(renderer.gameObject);
			}
			this.DEBUG_WINDOW_BORDERS.Clear();
		}

		private void DebugDrawAllWindowBorders() {
			ClearDebugDrawing();
			DEBUG_WINDOW_BORDERS = new List<LineRenderer>();
			foreach (WindowInfo window in this.Windows) {
				if (window.Title.Length > 0) {
					DrawDebugLine(
						window.ScreenCoordinates.TopLeft,
						window.ScreenCoordinates.TopRight,
						window.ScreenCoordinates.BottomRight,
						window.ScreenCoordinates.BottomLeft,
						window.ScreenCoordinates.TopLeft);
				}
			}
		}

		private void DebugDrawAllWindowTopLines() {
			ClearDebugDrawing();
			DEBUG_WINDOW_BORDERS = new List<LineRenderer>();
			foreach (ScreenSpaceLine segment in GetWindowTopBorders()) {
				DrawDebugLine(segment.Start, segment.End);
			}
		}

		private void DrawDebugLine(params Vector3[] points) {
			LineRenderer renderer = Instantiate<LineRenderer>(DEBUG_LINE_PREFAB, Vector3.zero, Quaternion.identity, this.transform);
			renderer.positionCount = points.Length;
			Vector3[] positions = new Vector3[points.Length];
			for (int i = 0; i < positions.Length; i++) {
				positions[i] = PlayspaceManager.Instance.ScaledScreenToWorld(PlayspaceManager.Instance.ScreenToScaledScreen(points[i]), false) + new Vector3(0, 0, 10);
			}
			renderer.SetPositions(positions);
			DEBUG_WINDOW_BORDERS.Add(renderer);
		}

		#endregion
	}
}