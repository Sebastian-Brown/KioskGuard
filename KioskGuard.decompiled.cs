using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: AssemblyVersion("0.0.0.0")]
internal class Entry
{
	public int Id;

	public string Name;

	public string Exe;

	public string Art;

	public string Genre;

	public string Source;
}
internal class Host
{
	private static string BaseDir = AppDomain.CurrentDomain.BaseDirectory;

	private static Config cfg;

	private static System.Windows.Application app;

	private static List<Entry> entries = new List<Entry>();

	private static ShellWindow shell;

	private static Process target;

	private static string targetExe;

	private static DateTime launchGraceUntil;

	private static bool targetWindowSeen;

	private static DispatcherTimer guard;

	private static List<Blocker> blockers = new List<Blocker>();

	private static Notice notice;

	private static bool failureShown;

	[STAThread]
	private static void Main()
	{
		Dpi.Enable();
		cfg = Config.Load(Path.Combine(BaseDir, "settings.txt"));
		System.Windows.Application application = new System.Windows.Application();
		application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
		app = application;
		app.DispatcherUnhandledException += delegate(object s, DispatcherUnhandledExceptionEventArgs e)
		{
			File.WriteAllText(Path.Combine(BaseDir, "KioskGuard-crash.log"), e.Exception.ToString());
			e.Handled = true;
		};
		string[] array = new string[6] { "Cat.mp4", "Church.mp4", "Coast.mp4", "Cow.mp4", "Gems.mp4", "Trees.mp4" };
		string text = null;
		if (cfg.StartupMode == "custom")
		{
			text = cfg.StartupMedia;
		}
		else if (cfg.StartupMode.StartsWith("fixed:"))
		{
			text = Path.Combine(BaseDir, "media", cfg.StartupMode.Substring(6) + ".mp4");
		}
		else if (cfg.StartupMode != "none")
		{
			text = Path.Combine(BaseDir, "media", array[new Random().Next(array.Length)]);
		}
		if (string.IsNullOrEmpty(text) || !File.Exists(text))
		{
			StartInterface(null);
		}
		else
		{
			Splash splash = new Splash(Screen.PrimaryScreen.Bounds, BaseDir, text, cfg.StartupFit);
			splash.Done += delegate
			{
				StartInterface(delegate
				{
					splash.Close();
				});
			};
			splash.Show();
		}
		app.Run();
	}

	private static void StartInterface(Action ready)
	{
		entries = Catalog();
		CacheArt();
		shell = new ShellWindow(Path.Combine(BaseDir, "ui"), () => Json(), () => SettingsJson(), delegate(int id)
		{
			Launch(id);
		}, delegate
		{
			Cleanup();
			app.Shutdown();
		}, delegate
		{
			entries = Catalog();
			CacheArt();
			return Json();
		}, (string m) => ApplySetting(m), (string kind) => ImportMedia(kind), ready);
		shell.Show();
		DispatcherTimer dispatcherTimer = new DispatcherTimer();
		dispatcherTimer.Interval = TimeSpan.FromMilliseconds(300.0);
		guard = dispatcherTimer;
		guard.Tick += delegate
		{
			Tick();
		};
		guard.Start();
	}

	private static string E(string s)
	{
		return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "")
			.Replace("\n", " ");
	}

	private static string Json()
	{
		return "[" + string.Join(",", entries.Select((Entry x) => "{\"id\":" + x.Id + ",\"name\":\"" + E(x.Name) + "\",\"genre\":\"" + E(x.Genre) + "\",\"source\":\"" + E(x.Source) + "\",\"path\":\"" + E(x.Exe) + "\",\"art\":" + (File.Exists(Path.Combine(BaseDir, "ui", "cache", x.Id + ".jpg")) ? ("\"cache/" + x.Id + ".jpg\"") : "null") + "}")) + "]";
	}

	private static string SettingsJson()
	{
		Screen[] allScreens = Screen.AllScreens;
		return "{\"startupMode\":\"" + E(cfg.StartupMode) + "\",\"startupMedia\":\"" + E(cfg.StartupMedia) + "\",\"startupFit\":\"" + E(cfg.StartupFit) + "\",\"blockerStyle\":\"" + E(cfg.BlockerStyle) + "\",\"blockerMedia\":\"" + E(cfg.BlockerMedia) + "\",\"blockerFit\":\"" + E(cfg.BlockerFit) + "\",\"showBlockerText\":" + (cfg.ShowBlockerText ? "true" : "false") + ",\"blockerMessage\":\"" + E(cfg.BlockerMessage) + "\",\"blockerTextColor\":\"" + E(cfg.BlockerTextColor) + "\",\"hotkey\":\"" + E(cfg.HotkeyDisplay) + "\",\"monitor\":" + cfg.Monitor + ",\"monitors\":[" + string.Join(",", allScreens.Select((Screen x) => "\"" + E(x.DeviceName + " — " + x.Bounds.Width + " × " + x.Bounds.Height) + "\"")) + "],\"theme\":{\"bg\":\"" + cfg.ThemeBg + "\",\"surface\":\"" + cfg.ThemeSurface + "\",\"accent\":\"" + cfg.ThemeAccent + "\",\"text\":\"" + cfg.ThemeText + "\"}}";
	}

	private static void CacheArt()
	{
		string text = Path.Combine(BaseDir, "ui", "cache");
		string text2 = Path.Combine(BaseDir, "ui", "overrides");
		Directory.CreateDirectory(text);
		Directory.CreateDirectory(text2);
		foreach (Entry entry in entries)
		{
			try
			{
				string text3 = Path.Combine(text2, entry.Id + ".jpg");
				string text4 = (File.Exists(text3) ? text3 : entry.Art);
				if (File.Exists(text4))
				{
					File.Copy(text4, Path.Combine(text, entry.Id + ".jpg"), overwrite: true);
				}
			}
			catch
			{
			}
		}
	}

	private static string ApplySetting(string m)
	{
		try
		{
			string[] array = m.Split(new char[1] { ':' }, 3);
			if (array.Length < 3)
			{
				return SettingsJson();
			}
			string text = array[1];
			string text2 = array[2];
			switch (text)
			{
			case "startupMode":
				cfg.StartupMode = text2;
				break;
			case "startupFit":
				cfg.StartupFit = text2;
				break;
			case "blockerStyle":
				cfg.BlockerStyle = text2;
				break;
			case "blockerFit":
				cfg.BlockerFit = text2;
				break;
			case "showBlockerText":
				cfg.ShowBlockerText = text2 == "1";
				break;
			case "blockerMessage":
				cfg.BlockerMessage = Uri.UnescapeDataString(text2);
				break;
			case "blockerTextColor":
				cfg.BlockerTextColor = text2;
				break;
			case "monitor":
				cfg.Monitor = int.Parse(text2);
				break;
			case "theme-bg":
				cfg.ThemeBg = text2;
				break;
			case "theme-surface":
				cfg.ThemeSurface = text2;
				break;
			case "theme-accent":
				cfg.ThemeAccent = text2;
				break;
			case "theme-text":
				cfg.ThemeText = text2;
				break;
			case "hotkey":
			{
				string[] array2 = text2.Split(',');
				cfg.Hotkey = (Keys)int.Parse(array2[0]);
				cfg.Ctrl = array2[1] == "1";
				cfg.Alt = array2[2] == "1";
				cfg.Shift = array2[3] == "1";
				cfg.HotkeyDisplay = Uri.UnescapeDataString(array2[4]);
				break;
			}
			}
			cfg.Save();
		}
		catch
		{
		}
		return SettingsJson();
	}

	private static string ImportMedia(string kind)
	{
		bool flag = kind.StartsWith("choose:art:");
		Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
		openFileDialog.Filter = (flag ? "Images|*.png;*.jpg;*.jpeg;*.bmp;*.webp|All files|*.*" : ((kind == "startup") ? "Videos|*.mp4;*.wmv;*.avi;*.mov|All files|*.*" : "Media|*.mp4;*.wmv;*.avi;*.mov;*.png;*.jpg;*.jpeg;*.gif|All files|*.*"));
		Microsoft.Win32.OpenFileDialog openFileDialog2 = openFileDialog;
		if (openFileDialog2.ShowDialog() == true)
		{
			if (flag)
			{
				int num = int.Parse(kind.Substring(11));
				string text = Path.Combine(BaseDir, "ui", "overrides", num + ".jpg");
				Directory.CreateDirectory(Path.GetDirectoryName(text));
				File.Copy(openFileDialog2.FileName, text, overwrite: true);
				CacheArt();
			}
			else
			{
				string extension = Path.GetExtension(openFileDialog2.FileName);
				string text2 = Path.Combine(BaseDir, "media", kind + "-custom" + extension);
				File.Copy(openFileDialog2.FileName, text2, overwrite: true);
				if (kind == "startup")
				{
					cfg.StartupMedia = text2;
					cfg.StartupMode = "custom";
				}
				else
				{
					cfg.BlockerMedia = text2;
					cfg.BlockerStyle = "custom";
				}
				cfg.Save();
			}
		}
		return SettingsJson();
	}

	private static List<Entry> Catalog()
	{
		List<Entry> list = new List<Entry>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Action<Entry> action = delegate(Entry x)
		{
			if (x != null && File.Exists(x.Exe) && Path.GetExtension(x.Exe).Equals(".exe", StringComparison.OrdinalIgnoreCase) && seen.Add(x.Name))
			{
				x.Id = list.Count;
				list.Add(x);
			}
		};
		Steam(action);
		string[] array = new string[2]
		{
			Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
			Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
		};
		foreach (string path in array)
		{
			try
			{
				string[] files = Directory.GetFiles(path, "*.lnk", SearchOption.AllDirectories);
				foreach (string text in files)
				{
					string text2 = Shortcut(text);
					action(new Entry
					{
						Name = Path.GetFileNameWithoutExtension(text),
						Exe = text2,
						Genre = Genre(text, text2),
						Source = "Windows"
					});
				}
			}
			catch
			{
			}
		}
		return list.OrderBy((Entry x) => x.Name).Select(delegate(Entry x, int i)
		{
			x.Id = i;
			return x;
		}).ToList();
	}

	private static void Steam(Action<Entry> add)
	{
		foreach (string item in SteamRoots())
		{
			string[] files = Directory.GetFiles(item, "appmanifest_*.acf");
			foreach (string path in files)
			{
				try
				{
					string t = File.ReadAllText(path);
					string path2 = V(t, "appid");
					string name = V(t, "name");
					string path3 = V(t, "installdir");
					string path4 = Path.Combine(item, "common", path3);
					string exe = (from x in Directory.GetFiles(path4, "*.exe", SearchOption.AllDirectories)
						where !Path.GetFileName(x).ToLower().Contains("crash") && !Path.GetFileName(x).ToLower().Contains("unins")
						orderby new FileInfo(x).Length descending
						select x).FirstOrDefault();
					string cache = Path.Combine(Path.GetDirectoryName(item), "appcache", "librarycache", path2);
					string art = new string[3] { "library_600x900.jpg", "library_hero.jpg", "logo.png" }.Select((string x) => Path.Combine(cache, x)).FirstOrDefault(File.Exists);
					add(new Entry
					{
						Name = name,
						Exe = exe,
						Art = art,
						Genre = "Games",
						Source = "Steam"
					});
				}
				catch
				{
				}
			}
		}
	}

	private static IEnumerable<string> SteamRoots()
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = null;
		try
		{
			using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Valve\\Steam"))
			{
				text = Convert.ToString(registryKey.GetValue("SteamPath"));
			}
		}
		catch
		{
		}
		if (string.IsNullOrEmpty(text))
		{
			text = "C:\\Program Files (x86)\\Steam";
		}
		string text2 = Path.Combine(text, "steamapps");
		if (Directory.Exists(text2))
		{
			hashSet.Add(text2);
		}
		try
		{
			string input = File.ReadAllText(Path.Combine(text2, "libraryfolders.vdf"));
			foreach (Match item in Regex.Matches(input, "\\\"path\\\"\\s+\\\"([^\\\"]+)\\\""))
			{
				string text3 = Path.Combine(item.Groups[1].Value.Replace("\\\\", "\\"), "steamapps");
				if (Directory.Exists(text3))
				{
					hashSet.Add(text3);
				}
			}
		}
		catch
		{
		}
		return hashSet;
	}

	private static string V(string t, string k)
	{
		Match match = Regex.Match(t, "\\\"" + k + "\\\"\\s+\\\"([^\\\"]+)\\\"");
		if (!match.Success)
		{
			return "";
		}
		return match.Groups[1].Value;
	}

	private static string Shortcut(string f)
	{
		try
		{
			Type typeFromProgID = Type.GetTypeFromProgID("WScript.Shell");
			object obj = Activator.CreateInstance(typeFromProgID);
			object obj2 = typeFromProgID.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, obj, new object[1] { f });
			return Convert.ToString(obj2.GetType().InvokeMember("TargetPath", BindingFlags.GetProperty, null, obj2, null));
		}
		catch
		{
			return null;
		}
	}

	private static string Genre(string n, string p)
	{
		string text = (n + " " + p).ToLower();
		if (text.Contains("chrome") || text.Contains("edge") || text.Contains("firefox"))
		{
			return "Browsers";
		}
		if (text.Contains("spotify") || text.Contains("media") || text.Contains("vlc"))
		{
			return "Media";
		}
		if (text.Contains("office") || text.Contains("word") || text.Contains("excel"))
		{
			return "Productivity";
		}
		return "Applications";
	}

	private static Screen ActiveScreen()
	{
		Screen[] allScreens = Screen.AllScreens;
		if (cfg.Monitor < 0 || cfg.Monitor >= allScreens.Length)
		{
			return Screen.PrimaryScreen;
		}
		return allScreens[cfg.Monitor];
	}

	private static void Launch(int id)
	{
		Entry entry = entries.FirstOrDefault((Entry a) => a.Id == id);
		if (entry == null)
		{
			return;
		}
		Process process = Existing(entry.Exe);
		if (process == null)
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo(entry.Exe);
			processStartInfo.WorkingDirectory = Path.GetDirectoryName(entry.Exe);
			processStartInfo.UseShellExecute = true;
			process = Process.Start(processStartInfo);
		}
		target = process;
		targetExe = entry.Exe;
		launchGraceUntil = DateTime.UtcNow.AddSeconds(45.0);
		targetWindowSeen = false;
		failureShown = false;
		Native.Start(cfg.Hotkey, cfg.Ctrl, cfg.Alt, cfg.Shift);
		foreach (Blocker blocker2 in blockers)
		{
			blocker2.Close();
		}
		blockers.Clear();
		Screen screen = ActiveScreen();
		Screen[] allScreens = Screen.AllScreens;
		foreach (Screen screen2 in allScreens)
		{
			if (screen2.DeviceName != screen.DeviceName)
			{
				Blocker blocker = new Blocker(screen2.Bounds, cfg.BlockerStyle, cfg.BlockerMedia, cfg.BlockerFit, cfg.BlockerMessage, cfg.ShowBlockerText, cfg.BlockerTextColor);
				blockers.Add(blocker);
				blocker.Show();
			}
		}
	}

	private static Process Existing(string f)
	{
		Process[] processesByName = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(f));
		foreach (Process process in processesByName)
		{
			try
			{
				if (process.MainModule.FileName.Equals(f, StringComparison.OrdinalIgnoreCase))
				{
					return process;
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static void Tick()
	{
		if (Native.Exit)
		{
			Native.Stop();
			foreach (Blocker blocker2 in blockers)
			{
				blocker2.Close();
			}
			blockers.Clear();
			failureShown = false;
			target = null;
			targetExe = null;
			targetWindowSeen = false;
			if (notice != null)
			{
				notice.Close();
			}
			if (shell != null)
			{
				shell.Show();
				shell.Activate();
			}
			return;
		}
		if (Native.TakeNotice())
		{
			if (notice != null)
			{
				notice.Close();
			}
			notice = new Notice(ActiveScreen().WorkingArea);
			notice.Show();
		}
		if (target != null && target.HasExited)
		{
			// ShellExecute can return a short-lived bootstrap process. During startup,
			// reacquire the configured executable and do not report a closure until a
			// real application window has appeared or the grace period has expired.
			Process replacement = string.IsNullOrEmpty(targetExe) ? null : Existing(targetExe);
			if (replacement != null && !replacement.HasExited)
			{
				target = replacement;
				return;
			}
			if (!targetWindowSeen && DateTime.UtcNow < launchGraceUntil)
			{
				return;
			}
			if (failureShown)
			{
				return;
			}
			failureShown = true;
			foreach (Blocker blocker3 in blockers)
			{
				blocker3.Close();
			}
			blockers.Clear();
			Screen[] allScreens = Screen.AllScreens;
			foreach (Screen screen in allScreens)
			{
				Blocker blocker = new Blocker(screen.Bounds, cfg.BlockerStyle, cfg.BlockerMedia, cfg.BlockerFit, "The protected application closed unexpectedly.\nUse the assigned hotkey to unlock this session.", true, cfg.BlockerTextColor);
				blockers.Add(blocker);
				blocker.Show();
			}
		}
		else if (target != null && !target.HasExited)
		{
			target.Refresh();
			if (target.MainWindowHandle != IntPtr.Zero)
			{
				targetWindowSeen = true;
				Native.Move(target.MainWindowHandle, ActiveScreen().WorkingArea);
				Native.Focus(target.MainWindowHandle);
			}
		}
	}

	private static void Cleanup()
	{
		Native.Stop();
		foreach (Blocker blocker in blockers)
		{
			blocker.Close();
		}
		if (notice != null)
		{
			notice.Close();
		}
		if (shell != null)
		{
			shell.Close();
		}
	}
}
internal class ShellWindow : Window
{
	private WebView2 web = new WebView2();

	private string folder;

	private Func<string> json;

	private Func<string> settings;

	private Func<string> refresh;

	private Func<string, string> setting;

	private Func<string, string> import;

	private Action<int> launch;

	private Action exit;

	private Action ready;

	public ShellWindow(string f, Func<string> j, Func<string> gs, Action<int> l, Action e, Func<string> r, Func<string, string> set, Func<string, string> imp, Action done)
	{
		folder = f;
		json = j;
		settings = gs;
		launch = l;
		exit = e;
		refresh = r;
		setting = set;
		import = imp;
		ready = done;
		base.WindowStyle = WindowStyle.None;
		base.ResizeMode = ResizeMode.NoResize;
		base.WindowState = WindowState.Maximized;
		base.Background = System.Windows.Media.Brushes.Black;
		base.Content = web;
		RoutedEventHandler value = async delegate
		{
			CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebView2Data"));
			await web.EnsureCoreWebView2Async(env);
			web.CoreWebView2.SetVirtualHostNameToFolderMapping("app.local", folder, CoreWebView2HostResourceAccessKind.DenyCors);
			web.CoreWebView2.WebMessageReceived += delegate(object a, CoreWebView2WebMessageReceivedEventArgs b)
			{
				Message(b.TryGetWebMessageAsString());
			};
			web.Source = new Uri("https://app.local/index.html");
		};
		base.Loaded += value;
	}

	private void Message(string m)
	{
		switch (m)
		{
		case "ready":
			SendApps(json());
			SendSettings(settings());
			if (ready != null)
			{
				ready();
				ready = null;
			}
			return;
		case "refresh":
			SendApps(refresh());
			return;
		case "exit":
			exit();
			return;
		case "choose:startup":
			SendSettings(import("startup"));
			return;
		case "choose:blocker":
			SendSettings(import("blocker"));
			return;
		}
		if (m.StartsWith("choose:art:"))
		{
			import(m);
			SendApps(refresh());
		}
		else if (m.StartsWith("setting:"))
		{
			SendSettings(setting(m));
		}
		else if (m.StartsWith("launch:"))
		{
			launch(int.Parse(m.Substring(7)));
			Hide();
		}
	}

	private async void SendApps(string data)
	{
		await web.ExecuteScriptAsync("window.receiveApps(" + data + ")");
	}

	private async void SendSettings(string data)
	{
		await web.ExecuteScriptAsync("window.receiveSettings(" + data + ")");
	}
}
internal class Config
{
	public string File;

	public string StartupMode = "bundled";

	public string StartupMedia = "";

	public string StartupFit = "fill";

	public string BlockerStyle = "ocean";

	public string BlockerMedia = "";

	public string BlockerFit = "fill";

	public string BlockerMessage = "This display is protected during the current session.";

	public string BlockerTextColor = "#ffffff";

	public string HotkeyDisplay = "Ctrl + Shift + F12";

	public string ThemeBg = "#07090d";

	public string ThemeSurface = "#121722";

	public string ThemeAccent = "#39d9c8";

	public string ThemeText = "#f7f9fc";

	public int Monitor;

	public bool ShowBlockerText = true;

	public Keys Hotkey = Keys.F12;

	public bool Ctrl = true;

	public bool Alt;

	public bool Shift = true;

	public static Config Load(string f)
	{
		Config config = new Config();
		config.File = f;
		Config config2 = config;
		if (System.IO.File.Exists(f))
		{
			string[] array = System.IO.File.ReadAllLines(f);
			foreach (string text in array)
			{
				int num = text.IndexOf('=');
				if (num < 1)
				{
					continue;
				}
				string text2 = text.Substring(0, num);
				string text3 = text.Substring(num + 1);
				try
				{
					switch (text2)
					{
					case "StartupMode":
						config2.StartupMode = text3;
						break;
					case "StartupMedia":
						config2.StartupMedia = text3;
						break;
					case "StartupFit":
						config2.StartupFit = text3;
						break;
					case "BlockerStyle":
						config2.BlockerStyle = text3;
						break;
					case "BlockerMedia":
						config2.BlockerMedia = text3;
						break;
					case "BlockerFit":
						config2.BlockerFit = text3;
						break;
					case "ShowBlockerText":
						config2.ShowBlockerText = text3 == "1";
						break;
					case "BlockerMessage":
						config2.BlockerMessage = Encoding.UTF8.GetString(Convert.FromBase64String(text3));
						break;
					case "BlockerTextColor":
						config2.BlockerTextColor = text3;
						break;
					case "Monitor":
						config2.Monitor = int.Parse(text3);
						break;
					case "Hotkey":
						config2.Hotkey = (Keys)int.Parse(text3);
						break;
					case "Ctrl":
						config2.Ctrl = text3 == "1";
						break;
					case "Alt":
						config2.Alt = text3 == "1";
						break;
					case "Shift":
						config2.Shift = text3 == "1";
						break;
					case "HotkeyDisplay":
						config2.HotkeyDisplay = text3;
						break;
					case "ThemeBg":
						config2.ThemeBg = text3;
						break;
					case "ThemeSurface":
						config2.ThemeSurface = text3;
						break;
					case "ThemeAccent":
						config2.ThemeAccent = text3;
						break;
					case "ThemeText":
						config2.ThemeText = text3;
						break;
					}
				}
				catch
				{
				}
			}
		}
		return config2;
	}

	public void Save()
	{
		System.IO.File.WriteAllLines(File, new string[19]
		{
			"StartupMode=" + StartupMode,
			"StartupMedia=" + StartupMedia,
			"StartupFit=" + StartupFit,
			"BlockerStyle=" + BlockerStyle,
			"BlockerMedia=" + BlockerMedia,
			"BlockerFit=" + BlockerFit,
			"ShowBlockerText=" + (ShowBlockerText ? "1" : "0"),
			"BlockerMessage=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(BlockerMessage)),
			"BlockerTextColor=" + BlockerTextColor,
			"Monitor=" + Monitor,
			"Hotkey=" + (int)Hotkey,
			"Ctrl=" + (Ctrl ? "1" : "0"),
			"Alt=" + (Alt ? "1" : "0"),
			"Shift=" + (Shift ? "1" : "0"),
			"HotkeyDisplay=" + HotkeyDisplay,
			"ThemeBg=" + ThemeBg,
			"ThemeSurface=" + ThemeSurface,
			"ThemeAccent=" + ThemeAccent,
			"ThemeText=" + ThemeText
		});
	}
}
internal class Splash : Window
{
	private WebView2 web = new WebView2();

	private DispatcherTimer timeout = new DispatcherTimer();

	private string root;

	private string file;

	private string fit;

	private bool finished;

	public event Action Done;

	public Splash(Rectangle r, string baseDir, string video, string mediaFit)
	{
		root = baseDir;
		file = video;
		fit = mediaFit;
		base.WindowStyle = WindowStyle.None;
		base.ResizeMode = ResizeMode.NoResize;
		base.Topmost = true;
		base.ShowInTaskbar = false;
		base.Background = System.Windows.Media.Brushes.Black;
		Place.Attach(this, r);
		base.Content = web;
		RoutedEventHandler value = async delegate
		{
			try
			{
				CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(options: new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required"), browserExecutableFolder: null, userDataFolder: Path.Combine(root, "WebView2SplashData"));
				await web.EnsureCoreWebView2Async(env);
				web.CoreWebView2.SetVirtualHostNameToFolderMapping("kiosk.local", root, CoreWebView2HostResourceAccessKind.DenyCors);
				web.CoreWebView2.WebMessageReceived += delegate(object a, CoreWebView2WebMessageReceivedEventArgs b)
				{
					Message(b.TryGetWebMessageAsString());
				};
				web.Source = new Uri("https://kiosk.local/ui/splash.html?file=/media/" + Uri.EscapeDataString(Path.GetFileName(file)) + "&fit=" + Uri.EscapeDataString(fit ?? "fill"));
				timeout.Start();
			}
			catch (Exception ex)
			{
				File.WriteAllText(Path.Combine(root, "KioskGuard-video-error.log"), ex.ToString());
				Complete();
			}
		};
		base.Loaded += value;
		timeout.Interval = TimeSpan.FromSeconds(35.0);
		timeout.Tick += delegate
		{
			File.WriteAllText(Path.Combine(root, "KioskGuard-video-error.log"), "Startup video timed out.");
			Complete();
		};
	}

	private void Message(string m)
	{
		if (m == "ended")
		{
			Complete();
		}
		else if (m.StartsWith("error:"))
		{
			File.WriteAllText(Path.Combine(root, "KioskGuard-video-error.log"), m);
			Complete();
		}
	}

	private void Complete()
	{
		if (!finished)
		{
			finished = true;
			timeout.Stop();
			if (this.Done != null)
			{
				this.Done();
			}
		}
	}
}
internal class Blocker : Window
{
	private WebView2 web = new WebView2();

	private string root;

	private string style;

	private string file;

	private string fit;

	private string message;

	private string color;

	private bool showText;

	public Blocker(Rectangle r, string mediaStyle, string mediaFile, string mediaFit, string text, bool show, string textColor)
	{
		root = AppDomain.CurrentDomain.BaseDirectory;
		style = mediaStyle;
		file = mediaFile;
		fit = mediaFit;
		message = text;
		showText = show;
		color = textColor;
		base.WindowStyle = WindowStyle.None;
		base.ResizeMode = ResizeMode.NoResize;
		base.Topmost = true;
		base.ShowInTaskbar = false;
		base.Background = System.Windows.Media.Brushes.Black;
		Place.Attach(this, r);
		base.Content = web;
		RoutedEventHandler value = async delegate
		{
			try
			{
				CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(root, "WebView2BlockerData"));
				await web.EnsureCoreWebView2Async(env);
				web.CoreWebView2.SetVirtualHostNameToFolderMapping("kiosk.local", root, CoreWebView2HostResourceAccessKind.DenyCors);
				string media = ((style == "custom" && File.Exists(file)) ? ("/media/" + Uri.EscapeDataString(Path.GetFileName(file))) : "");
				web.Source = new Uri("https://kiosk.local/ui/blocker.html?style=" + Uri.EscapeDataString(style) + "&file=" + Uri.EscapeDataString(media) + "&fit=" + Uri.EscapeDataString(fit ?? "fill") + "&show=" + (showText ? "1" : "0") + "&message=" + Uri.EscapeDataString(message ?? "") + "&color=" + Uri.EscapeDataString(color ?? "#ffffff"));
			}
			catch
			{
			}
		};
		base.Loaded += value;
	}
}
internal class Notice : Window
{
	private DispatcherTimer timer = new DispatcherTimer();

	public Notice(Rectangle r)
	{
		base.WindowStyle = WindowStyle.None;
		base.ResizeMode = ResizeMode.NoResize;
		base.Topmost = true;
		base.ShowInTaskbar = false;
		double num = Math.Max(1.0, Math.Min(2.5, Math.Max((double)r.Width / 1920.0, (double)r.Height / 1080.0)));
		int num2 = (int)(500.0 * num);
		int num3 = (int)(140.0 * num);
		int num4 = (int)(20.0 * num);
		Rectangle r2 = new Rectangle(r.Right - num2 - num4, r.Bottom - num3 - num4, num2, num3);
		Place.Attach(this, r2);
		base.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#151A26"));
		base.Content = new TextBlock
		{
			Text = "This session is locked.\nUse the assigned hotkey to change programs.",
			Foreground = System.Windows.Media.Brushes.White,
			FontSize = 18.0,
			FontWeight = FontWeights.SemiBold,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(25.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		timer.Interval = TimeSpan.FromSeconds(3.5);
		DispatcherTimer dispatcherTimer = timer;
		EventHandler value = delegate
		{
			timer.Stop();
			Close();
		};
		dispatcherTimer.Tick += value;
		base.Loaded += delegate
		{
			timer.Start();
		};
	}
}
internal static class Place
{
	public static void Attach(Window w, Rectangle r)
	{
		w.SourceInitialized += delegate
		{
			SetWindowPos(new WindowInteropHelper(w).Handle, new IntPtr(-1), r.X, r.Y, r.Width, r.Height, 64u);
		};
	}

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(IntPtr h, IntPtr z, int x, int y, int w, int hgt, uint f);
}
internal static class Dpi
{
	[DllImport("user32.dll")]
	private static extern bool SetProcessDpiAwarenessContext(IntPtr x);

	public static void Enable()
	{
		try
		{
			SetProcessDpiAwarenessContext(new IntPtr(-4));
		}
		catch
		{
		}
	}
}
internal static class Native
{
	private delegate IntPtr H(int n, IntPtr w, IntPtr l);

	private static H cb = Hook;

	private static IntPtr h;

	private static int key = 123;

	private static bool needCtrl = true;

	private static bool needAlt;

	private static bool needShift = true;

	private static volatile bool notice;

	public static volatile bool Exit;

	public static bool TakeNotice()
	{
		if (!notice)
		{
			return false;
		}
		notice = false;
		return true;
	}

	public static void Start(Keys k, bool c, bool a, bool s)
	{
		key = (int)k;
		needCtrl = c;
		needAlt = a;
		needShift = s;
		Exit = false;
		notice = false;
		using (Process process = Process.GetCurrentProcess())
		using (ProcessModule processModule = process.MainModule)
		{
			h = SetWindowsHookEx(13, cb, GetModuleHandle(processModule.ModuleName), 0u);
		}
	}

	public static void Stop()
	{
		if (h != IntPtr.Zero)
		{
			UnhookWindowsHookEx(h);
		}
		h = IntPtr.Zero;
		Exit = false;
		notice = false;
	}

	private static bool D(int k)
	{
		return (GetAsyncKeyState(k) & 0x8000) != 0;
	}

	private static IntPtr Hook(int n, IntPtr w, IntPtr l)
	{
		if (n >= 0 && (w == (IntPtr)256 || w == (IntPtr)260))
		{
			int num = Marshal.ReadInt32(l);
			bool flag = D(17);
			bool flag2 = D(18);
			bool flag3 = D(16);
			if (num == key && flag == needCtrl && flag2 == needAlt && flag3 == needShift)
			{
				Exit = true;
				return (IntPtr)1;
			}
			if (num == 91 || num == 92 || (num == 9 && flag2) || (num == 27 && (flag2 || flag)) || (num == 115 && flag2))
			{
				notice = true;
				return (IntPtr)1;
			}
		}
		return CallNextHookEx(h, n, w, l);
	}

	public static void Move(IntPtr w, Rectangle r)
	{
		SetWindowPos(w, IntPtr.Zero, r.X, r.Y, r.Width, r.Height, 4u);
	}

	public static void Focus(IntPtr w)
	{
		ShowWindow(w, 9);
		SetForegroundWindow(w);
	}

	[DllImport("user32.dll")]
	private static extern IntPtr SetWindowsHookEx(int i, H p, IntPtr m, uint t);

	[DllImport("user32.dll")]
	private static extern bool UnhookWindowsHookEx(IntPtr h);

	[DllImport("user32.dll")]
	private static extern IntPtr CallNextHookEx(IntPtr h, int n, IntPtr w, IntPtr l);

	[DllImport("kernel32.dll")]
	private static extern IntPtr GetModuleHandle(string n);

	[DllImport("user32.dll")]
	private static extern short GetAsyncKeyState(int k);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(IntPtr h, IntPtr z, int x, int y, int w, int hgt, uint f);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr h, int c);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(IntPtr h);
}
