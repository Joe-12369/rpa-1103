using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Screenshot.IniFile;
using II.RPA.Screenshot.Plugin;
using II.RPA.Screenshot.Plugin.UnmanagedHelpers;

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace II.RPA.Screenshot.Plugin.Core {
	/// <summary>
	/// Description of PluginUtils.
	/// </summary>
	public static class PluginUtils {
		private static ILogger logger = Logger.OfContext();
		private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
		private const string PathKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\";
		private static readonly IDictionary<string, Image> ExeIconCache = new Dictionary<string, Image>();

		static PluginUtils() {
			CoreConfig.PropertyChanged += OnIconSizeChanged;
		}

		/// <summary>
		/// Simple global property to get the Screenshot host
		/// </summary>
		public static IScreenshotHost Host {
			get;
			set;
		}

		/// <summary>
		/// Clear icon cache
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void OnIconSizeChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "IconSize") {
				List<Image> cachedImages = new List<Image>();
				lock (ExeIconCache) {
					foreach (string key in ExeIconCache.Keys) {
						cachedImages.Add(ExeIconCache[key]);
					}
					ExeIconCache.Clear();
				}
				foreach (Image cachedImage in cachedImages)
				{
					cachedImage?.Dispose();
				}

			}
		}

		/// <summary>
		/// Get the path of an executable
		/// </summary>
		/// <param name="exeName">e.g. cmd.exe</param>
		/// <returns>Path to file</returns>
		public static string GetExePath(string exeName) {
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey(PathKey + exeName, false)) {
				if (key != null) {
					// "" is the default key, which should point to the requested location
					return (string)key.GetValue("");
				}
			}
			foreach (string pathEntry in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';')) {
				try {
					string path = pathEntry.Trim();
					if (!string.IsNullOrEmpty(path) && File.Exists(path = Path.Combine(path, exeName))) {
						return Path.GetFullPath(path);
					}
				} catch (Exception) {
					logger.LogWarning("Problem with path entry '{0}'.", pathEntry);
				}
			}
			return null;
		}

		/// <summary>
		/// Get icon from resource files, from the cache.
		/// Examples can be found here: https://diymediahome.org/windows-icons-reference-list-with-details-locations-images/
		/// </summary>
		/// <param name="path">path to the exe or dll</param>
		/// <param name="index">index of the icon</param>
		/// <returns>Bitmap with the icon or null if something happended</returns>
		public static Image GetCachedExeIcon(string path, int index) {
			string cacheKey = $"{path}:{index}";
			Image returnValue;
			lock (ExeIconCache)
			{
				if (ExeIconCache.TryGetValue(cacheKey, out returnValue))
				{
					return returnValue;
				}
				lock (ExeIconCache) {
					if (ExeIconCache.TryGetValue(cacheKey, out returnValue))
					{
						return returnValue;
					}
					returnValue = GetExeIcon(path, index);
					if (returnValue != null) {
						ExeIconCache.Add(cacheKey, returnValue);
					}
				}
			}
			return returnValue;
		}

		/// <summary>
		/// Get icon for executable
		/// </summary>
		/// <param name="path">path to the exe or dll</param>
		/// <param name="index">index of the icon</param>
		/// <returns>Bitmap with the icon or null if something happended</returns>
		private static Bitmap GetExeIcon(string path, int index) {
			if (!File.Exists(path)) {
				return null;
			}
			try {
				using (Icon appIcon = ImageHelper.ExtractAssociatedIcon(path, index, CoreConfig.UseLargeIcons)) {
					if (appIcon != null) {
						return appIcon.ToBitmap();
					}
				}
				using (Icon appIcon = Shell32.GetFileIcon(path, CoreConfig.UseLargeIcons ? Shell32.IconSize.Large : Shell32.IconSize.Small, false)) {
					if (appIcon != null) {
						return appIcon.ToBitmap();
					}
				}
			} catch (Exception exIcon) {
				 logger.LogError("error retrieving icon: ", exIcon);
			}
			return null;
		}

		/// <summary>
		/// Helper method to add a MenuItem to the File MenuItem of an ImageEditor
		/// </summary>
		/// <param name="imageEditor"></param>
		/// <param name="image">Image to display in the menu</param>
		/// <param name="text">Text to display in the menu</param>
		/// <param name="tag">The TAG value</param>
		/// <param name="shortcutKeys">Keys which can be used as shortcut</param>
		/// <param name="handler">The onclick handler</param>
		public static void AddToFileMenu(IImageEditor imageEditor, Image image, string text, object tag, Keys? shortcutKeys, EventHandler handler) {
			var item = new ToolStripMenuItem
			{
				Image = image,
				Text = text,
				Tag = tag
			};
			if (shortcutKeys.HasValue) {
				item.ShortcutKeys = shortcutKeys.Value;
			}
			item.Click += handler;
			AddToFileMenu(imageEditor, item);
		}

		/// <summary>
		/// Helper method to add a MenuItem to the File MenuItem of an ImageEditor
		/// </summary>
		/// <param name="imageEditor"></param>
		/// <param name="item"></param>
		public static void AddToFileMenu(IImageEditor imageEditor, ToolStripMenuItem item) {
			ToolStripMenuItem toolStripMenuItem = imageEditor.GetFileMenuItem();
			bool added = false;
			for(int i = 0; i< toolStripMenuItem.DropDownItems.Count; i++) {
				if (toolStripMenuItem.DropDownItems[i].GetType() == typeof(ToolStripSeparator)) {
					toolStripMenuItem.DropDownItems.Insert(i, item);
					added = true;
					break;
				}
			}
			if (!added) {
				toolStripMenuItem.DropDownItems.Add(item);
			}
		}
		
		/// <summary>
		/// Helper method to add a MenuItem to the Plugin MenuItem of an ImageEditor
		/// </summary>
		/// <param name="imageEditor"></param>
		/// <param name="item"></param>
		public static void AddToPluginMenu(IImageEditor imageEditor, ToolStripMenuItem item) {
			ToolStripMenuItem toolStripMenuItem = imageEditor.GetPluginMenuItem();
			bool added = false;
			for(int i = 0; i< toolStripMenuItem.DropDownItems.Count; i++) {
				if (toolStripMenuItem.DropDownItems[i].GetType() == typeof(ToolStripSeparator)) {
					toolStripMenuItem.DropDownItems.Insert(i, item);
					added = true;
					break;
				}
			}
			if (!added) {
				toolStripMenuItem.DropDownItems.Add(item);
			}
		}
		/// <summary>
		/// Helper method to add a plugin MenuItem to the Screenshot context menu
		/// </summary>
		/// <param name="host">IScreenshotHost</param>
		/// <param name="item">ToolStripMenuItem</param>
		public static void AddToContextMenu(IScreenshotHost host, ToolStripMenuItem item) {
			// Here we can hang ourselves to the main context menu!
			ContextMenuStrip contextMenu = host.MainMenu;
			bool addedItem = false;

			// Try to find a separator, so we insert ourselves after it 
			for(int i=0; i < contextMenu.Items.Count; i++) {
				if (contextMenu.Items[i].GetType() == typeof(ToolStripSeparator)) {
					// Check if we need to add a new separator, which is done if the first found has a Tag with the value "PluginsAreAddedBefore"
					if ("PluginsAreAddedBefore".Equals(contextMenu.Items[i].Tag)) {
						var separator = new ToolStripSeparator
						{
							Tag = "PluginsAreAddedAfter",
							Size = new Size(305, 6)
						};
						contextMenu.Items.Insert(i, separator);
					} else if (!"PluginsAreAddedAfter".Equals(contextMenu.Items[i].Tag)) {
						continue;
					}
					contextMenu.Items.Insert(i + 1, item);
					addedItem = true;
					break;
				}
			}
			// If we didn't insert the item, we just add it...
			if (!addedItem) {
				contextMenu.Items.Add(item);
			}
		}
	}
}
