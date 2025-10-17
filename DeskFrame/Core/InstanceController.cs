using DeskFrame;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows.Interop;
using System.Runtime.InteropServices;

public class InstanceController
{
    public static string appName = "DeskFrame";
    public bool isInitializingInstances = false;
    public List<Instance> Instances = new List<Instance>();
    public RegistryHelper reg = new RegistryHelper(appName);
    public List<DeskFrameWindow> _subWindows = new List<DeskFrameWindow>();
    public List<IntPtr> _subWindowsPtr = new List<IntPtr>();
    private bool Visible = true;

    // Liste der Dateien/Ordner deren Attribute wir geändert haben
    private List<(string Path, FileAttributes Original)> _desktopHiddenItems = new();
    // Explorer Desktop Icon Sichtbarkeit (HideIcons) – 0 = sichtbar, 1 = ausgeblendet
    private int? _originalHideIcons = null; // merken zum Wiederherstellen
    private bool _hideIconsModified = false;
    // Fallback: direkte Fenster-Hides (SHELLDLL_DefView / SysListView32) falls Registry Toggle keine Wirkung zeigt
    private List<IntPtr> _hiddenDesktopListViews = new();

    public void WriteOverInstanceToKey(Instance instance, string oldKey)
    {

        try
        {
            Debug.WriteLine($"old: {oldKey}\t{instance.Name}");

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@$"SOFTWARE\{appName}\Instances\{instance.Name}"))
            {
                key.SetValue("Name", instance.Name!);
                key.SetValue("PosX", instance.PosX!);
                key.SetValue("PosY", instance.PosY!);
                key.SetValue("Width", instance.Width!);
                key.SetValue("Height", instance.Height!);
                key.SetValue("IconSize", instance.IconSize!);
                key.SetValue("IdleOpacity", instance.IdleOpacity!);
                key.SetValue("AnimationSpeed", instance.AnimationSpeed!);
                key.SetValue("Minimized", instance.Minimized!);
                key.SetValue("Folder", instance.Folder!);
                key.SetValue("TitleFontFamily", instance.TitleFontFamily!);
                key.SetValue("ShowHiddenFiles", instance.ShowHiddenFiles!);
                key.SetValue("LastAccesedToFirstRow", instance.LastAccesedToFirstRow);
                key.SetValue("ShowFileExtension", instance.ShowFileExtension!);
                key.SetValue("ShowFileExtensionIcon", instance.ShowFileExtensionIcon!);
                key.SetValue("ShowHiddenFilesIcon", instance.ShowHiddenFilesIcon!);
                key.SetValue("ShowDisplayName", instance.ShowDisplayName!);
                key.SetValue("IsLocked", instance.IsLocked!);
                key.SetValue("ShowInGrid", instance.ShowInGrid!);
                key.SetValue("AutoExpandonCursor", instance.AutoExpandonCursor);
                key.SetValue("ShowShortcutArrow", instance.ShowShortcutArrow);
                key.SetValue("FolderOpenInsideFrame", instance.FolderOpenInsideFrame);
                key.SetValue("CheckFolderSize", instance.CheckFolderSize);
                key.SetValue("TitleBarColor", instance.TitleBarColor!);
                key.SetValue("TitleTextColor", instance.TitleTextColor!);
                key.SetValue("TitleTextAlignment", instance.TitleTextAlignment.ToString());
                key.SetValue("TitleText", instance.TitleText != null ? instance.TitleText : instance.Name);
                key.SetValue("BorderColor", instance.BorderColor!);
                key.SetValue("BorderEnabled", instance.BorderEnabled!);
                key.SetValue("FileFilterRegex", instance.FileFilterRegex!);
                key.SetValue("FileFilterHideRegex", instance.FileFilterHideRegex!);
                key.SetValue("ListViewBackgroundColor", instance.ListViewBackgroundColor!);
                key.SetValue("ListViewFontColor", instance.ListViewFontColor!);
                key.SetValue("ListViewFontShadowColor", instance.ListViewFontShadowColor!);
                key.SetValue("Opacity", instance.Opacity);
                key.SetValue("SortBy", instance.SortBy);
                key.SetValue("FolderOrder", instance.FolderOrder);
                if (instance.ShowOnVirtualDesktops != null && instance.ShowOnVirtualDesktops.Length > 0)
                {
                    key.SetValue("ShowOnVirtualDesktops", string.Join(",", instance.ShowOnVirtualDesktops));
                }
                if (instance.LastAccessedFiles != null && instance.LastAccessedFiles.Count > 0)
                {
                    key.SetValue("LastAccessedFiles", instance.LastAccessedFiles.ToArray(), RegistryValueKind.MultiString);
                }
                key.SetValue("TitleFontSize", instance.TitleFontSize);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WriteOverInstanceToKey failed: {ex.Message}");
        }
        Registry.CurrentUser.DeleteSubKey(@$"SOFTWARE\{appName}\Instances\{oldKey}", throwOnMissingSubKey: false);
    }
    private void InitDetails()
    {
        if (reg.KeyExistsRoot("blurBackground"))
        {
            ChangeBlur((bool)reg.ReadKeyValueRoot("blurBackground"));
        }
    }
    public void WriteInstanceToKey(Instance instance)
    {
        if (string.IsNullOrEmpty(instance.Name))
        {
            Debug.WriteLine("instance.Name is null, Instance is not written to key");
            return;
        }
        try
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(instance.GetKeyLocation()))
            {
                if (instance.Name != null) key.SetValue("Name", instance.Name);
                key.SetValue("PosX", instance.PosX);
                key.SetValue("PosY", instance.PosY);
                key.SetValue("Width", instance.Width);
                key.SetValue("Height", instance.Height);
                key.SetValue("IconSize", instance.IconSize);
                key.SetValue("IdleOpacity", instance.IdleOpacity);
                key.SetValue("AnimationSpeed", instance.AnimationSpeed);
                key.SetValue("Minimized", instance.Minimized);
                if (instance.Folder != null) key.SetValue("Folder", instance.Folder);
                if (instance.TitleFontFamily != null) key.SetValue("TitleFontFamily", instance.TitleFontFamily);
                key.SetValue("ShowHiddenFiles", instance.ShowHiddenFiles);
                key.SetValue("LastAccesedToFirstRow", instance.LastAccesedToFirstRow);
                key.SetValue("ShowFileExtension", instance.ShowFileExtension);
                key.SetValue("ShowFileExtensionIcon", instance.ShowFileExtensionIcon);
                key.SetValue("ShowHiddenFilesIcon", instance.ShowHiddenFilesIcon);
                key.SetValue("ShowDisplayName", instance.ShowDisplayName);
                key.SetValue("IsLocked", instance.IsLocked);
                key.SetValue("ShowInGrid", instance.ShowInGrid);
                key.SetValue("AutoExpandonCursor", instance.AutoExpandonCursor);
                key.SetValue("ShowShortcutArrow", instance.ShowShortcutArrow);
                key.SetValue("FolderOpenInsideFrame", instance.FolderOpenInsideFrame);
                key.SetValue("CheckFolderSize", instance.CheckFolderSize);
                if (instance.TitleBarColor != null) key.SetValue("TitleBarColor", instance.TitleBarColor);
                if (instance.TitleTextColor != null) key.SetValue("TitleTextColor", instance.TitleTextColor);
                key.SetValue("TitleTextAlignment", instance.TitleTextAlignment.ToString());
                if (instance.TitleText != null) key.SetValue("TitleText", instance.TitleText);
                if (instance.BorderColor != null) key.SetValue("BorderColor", instance.BorderColor);
                key.SetValue("BorderEnabled", instance.BorderEnabled);
                if (instance.FileFilterRegex != null) key.SetValue("FileFilterRegex", instance.FileFilterRegex);
                if (instance.FileFilterHideRegex != null) key.SetValue("FileFilterHideRegex", instance.FileFilterHideRegex);
                if (instance.ListViewBackgroundColor != null) key.SetValue("ListViewBackgroundColor", instance.ListViewBackgroundColor);
                if (instance.ListViewFontColor != null) key.SetValue("ListViewFontColor", instance.ListViewFontColor);
                if (instance.ListViewFontShadowColor != null) key.SetValue("ListViewFontShadowColor", instance.ListViewFontShadowColor);
                key.SetValue("Opacity", instance.Opacity);
                key.SetValue("SortBy", instance.SortBy);
                key.SetValue("FolderOrder", instance.FolderOrder);
                if (instance.ShowOnVirtualDesktops != null) key.SetValue("ShowOnVirtualDesktops", string.Join(",", instance.ShowOnVirtualDesktops));
                if (instance.LastAccessedFiles != null && instance.LastAccessedFiles.Count > 0)
                {
                    key.SetValue("LastAccessedFiles", instance.LastAccessedFiles.ToArray(), RegistryValueKind.MultiString);
                }
                key.SetValue("TitleFontSize", instance.TitleFontSize);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WriteInstanceToKey failed: {ex.Message}");
        }
    }

    public void AddInstance()
    {
        var existingEmptyInstance = Instances.FirstOrDefault(instance => instance.Name == "empty");

        if (existingEmptyInstance != null)
        {
            Instances.Remove(existingEmptyInstance);
        }

        Instances.Add(new Instance("empty", false));
        MainWindow._controller.WriteInstanceToKey(Instances.Last());
        var subWindow = new DeskFrameWindow(Instances.Last());
        subWindow.ChangeBackgroundOpacity(Instances.Last().Opacity);
        _subWindows.Add(subWindow);
        subWindow.Show();
        _subWindowsPtr.Add(new WindowInteropHelper(subWindow).Handle);
        InitDetails();
    }
    public void RemoveInstance(Instance instance, DeskFrameWindow window)
    {
        Instances.Remove(instance);
        _subWindows.Remove(window);
    }
    public void ChangeBlur(bool toBlur)
    {
        foreach (DeskFrameWindow window in _subWindows)
        {
            window.BackgroundType(toBlur);
        }
    }

    public void ChangeBackgroundOpacity(int num)
    {
        foreach (DeskFrameWindow window in _subWindows)
        {
            window.ChangeBackgroundOpacity(num);
        }
    }
    public void CheckFrameWindowsLive()
    {
        int closedCount = 0;
        foreach (var window in _subWindows)
        {
            if (new WindowInteropHelper(window).Handle == IntPtr.Zero) closedCount++;
        }
        if (_subWindows.Count != 0 && !isInitializingInstances)
        {
            if (closedCount == _subWindows.Count)
            {
                foreach (var window in _subWindows)
                {
                    window.Close();
                }
                _subWindows.Clear();
                _subWindowsPtr.Clear();
                Instances.Clear();
                InitInstances();
            }
            if (closedCount != _subWindows.Count)
            {
                foreach (var window in _subWindows)
                {
                    window.AdjustPosition();
                }
            }
        }
    }
    public void ChangeVisibility()
    {
        if (Visible)
        {
            foreach (var window in _subWindows)
            {
                window.Hide();
            }
            Visible = false;
        }
        else
        {
            foreach (var window in _subWindows)
            {
                window.Show();
            }
            Visible = true;
        }
    }
    public void InitInstances()
    {
        isInitializingInstances = true;
        Debug.WriteLine("Init...");
        try
        {
            using (RegistryKey instancesKey = Registry.CurrentUser.OpenSubKey(@$"SOFTWARE\{appName}\Instances")!)
            {
                if (instancesKey != null)
                {
                    string[] instanceNames = instancesKey.GetSubKeyNames();
                    Debug.WriteLine($"\n");

                    foreach (var item in instanceNames)
                    {
                        Debug.WriteLine($"subkeyname: {item}");
                    }

                    foreach (string instance in instanceNames)
                    {

                        Debug.WriteLine($"instanceNames: {instance}");


                        using (RegistryKey instanceKey = Registry.CurrentUser.OpenSubKey(@$"SOFTWARE\{appName}\Instances\{instance}")!)
                        {
                            Debug.WriteLine("valid");
                            if (instanceKey != null)
                            {
                                Instance temp = new Instance("", false);
                                Debug.WriteLine("valied 2");

                                foreach (var valueName in instanceKey.GetValueNames())   // Read all values under the current subkey
                                {
                                    object value = instanceKey.GetValue(valueName)!;

                                    if (value != null)
                                    {
                                        switch (valueName)
                                        {
                                            case "PosX":
                                                if (double.TryParse(value.ToString(), out double parsedPosX))
                                                {
                                                    temp.PosX = parsedPosX;
                                                    Debug.WriteLine($"PosX added: {temp.PosX}");
                                                }
                                                else
                                                {
                                                    Debug.WriteLine("Failed to parse PosX.");
                                                }
                                                break;
                                            case "PosY":
                                                if (double.TryParse(value.ToString(), out double parsedPosY))
                                                {
                                                    temp.PosY = parsedPosY;
                                                    Debug.WriteLine($"PosY added: {temp.PosY}");
                                                }
                                                else
                                                {
                                                    Debug.WriteLine("Failed to parse PosY.");
                                                }
                                                break;

                                            case "Width":
                                                if (double.TryParse(value.ToString(), out double parsedWidth))
                                                {
                                                    temp.Width = parsedWidth;
                                                    Debug.WriteLine($"Width added: {temp.Width}");
                                                }
                                                else
                                                {
                                                    Debug.WriteLine("Failed to parse Width.");
                                                }
                                                break;

                                            case "Height":
                                                if (double.TryParse(value.ToString(), out double parsedHeight))
                                                {
                                                    temp.Height = parsedHeight;
                                                }
                                                break;
                                            case "IconSize":
                                                if (int.TryParse(value.ToString(), out int parseIconSize))
                                                {
                                                    temp.IconSize = parseIconSize;
                                                }
                                                break;
                                            case "IdleOpacity":
                                                if (double.TryParse(value.ToString(), out double parsedIdleOpacity))
                                                {
                                                    temp.IdleOpacity = parsedIdleOpacity;
                                                }

                                                break;
                                            case "AnimationSpeed":
                                                if (double.TryParse(value.ToString(), out double parsedAnimationSpeed))
                                                {
                                                    temp.AnimationSpeed = parsedAnimationSpeed;
                                                }
                                                break;

                                            case "Name":
                                                temp.Name = value.ToString()!;
                                                Debug.WriteLine($"Name added\t{temp.Name}");
                                                break;
                                            case "Folder":
                                                Debug.WriteLine("+trest:  " + value.ToString());
                                                temp.Folder = value.ToString()!;
                                                Debug.WriteLine($"Folder added\t{temp.Folder}");
                                                break;
                                            case "TitleFontFamily":
                                                temp.TitleFontFamily = value.ToString()!;
                                                Debug.WriteLine($"TitleFontFamily added\t{temp.TitleFontFamily}");
                                                break;
                                            case "Minimized":
                                                temp.Minimized = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"Minimized added\t{temp.Minimized}");
                                                break;
                                            case "ShowHiddenFiles":
                                                temp.ShowHiddenFiles = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"ShowHiddenFiles added\t{temp.ShowHiddenFiles}");
                                                break;
                                            case "LastAccesedToFirstRow":
                                                temp.LastAccesedToFirstRow = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"LastAccesedToFirstRow added\t{temp.LastAccesedToFirstRow}");
                                                break;
                                            case "ShowFileExtension":
                                                temp.ShowFileExtension = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"ShowFileExtension added\t{temp.ShowFileExtension}");
                                                break;
                                            case "ShowFileExtensionIcon":
                                                temp.ShowFileExtensionIcon = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"ShowFileExtensionIcon added\t{temp.ShowFileExtensionIcon}");
                                                break;
                                            case "ShowHiddenFilesIcon":
                                                temp.ShowHiddenFilesIcon = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"ShowHiddenFilesIcon added\t{temp.ShowHiddenFilesIcon}");
                                                break;
                                            case "ShowDisplayName":
                                                temp.ShowDisplayName = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"ShowDisplayName added\t{temp.ShowDisplayName}");
                                                break;
                                            case "IsLocked":
                                                temp.IsLocked = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"IsLocked added\t{temp.IsLocked}");
                                                break;
                                            case "ShowInGrid":
                                                temp.ShowInGrid = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"ShowInGrid added\t{temp.ShowInGrid}");
                                                break;
                                            case "AutoExpandonCursor":
                                                temp.AutoExpandonCursor = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"AutoExpandonCursor added\t{temp.AutoExpandonCursor}");
                                                break;
                                            case "ShowShortcutArrow":
                                                temp.ShowShortcutArrow = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"ShowShortcutArrow added\t{temp.ShowShortcutArrow}");
                                                break; 
                                            case "FolderOpenInsideFrame":
                                                temp.FolderOpenInsideFrame = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"FolderOpenInsideFrame added\t{temp.FolderOpenInsideFrame}");
                                                break;
                                            case "CheckFolderSize":
                                                temp.CheckFolderSize = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"CheckFolderSize added\t{temp.CheckFolderSize}");
                                                break;
                                            case "TitleBarColor":
                                                temp.TitleBarColor = value.ToString()!;
                                                Debug.WriteLine($"TitleBarColor added\t{temp.TitleBarColor}");
                                                break;
                                            case "TitleTextColor":
                                                temp.TitleTextColor = value.ToString()!;
                                                Debug.WriteLine($"TitleTextColor added\t{temp.TitleTextColor}");
                                                break;
                                            case "TitleText":
                                                temp.TitleText = value.ToString();
                                                Debug.WriteLine($"TitleText added\t{temp.TitleText}");
                                                break;
                                            case "TitleTextAlignment":
                                                if (Enum.TryParse<System.Windows.HorizontalAlignment>(value.ToString()!, out var alignment))
                                                {
                                                    temp.TitleTextAlignment = alignment;
                                                }
                                                break;
                                            case "BorderColor":
                                                temp.BorderColor = value.ToString()!;
                                                Debug.WriteLine($"BorderColor added\t{temp.BorderColor}");
                                                break;
                                            case "BorderEnabled":
                                                temp.BorderEnabled = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"BorderEnabled added\t{temp.BorderEnabled}");
                                                break;
                                            case "FileFilterRegex":
                                                temp.FileFilterRegex = value.ToString()!;
                                                Debug.WriteLine($"FileFilterRegex added\t{temp.FileFilterRegex}");
                                                break;
                                            case "FileFilterHideRegex":
                                                temp.FileFilterHideRegex = value.ToString()!;
                                                Debug.WriteLine($"FileFilterHideRegex added\t{temp.FileFilterHideRegex}");
                                                break;
                                            case "ListViewBackgroundColor":
                                                temp.ListViewBackgroundColor = value.ToString()!;
                                                Debug.WriteLine($"ListViewBackgroundColor added\t{temp.ListViewBackgroundColor}");
                                                break;
                                            case "ListViewFontColor":
                                                temp.ListViewFontColor = value.ToString()!;
                                                Debug.WriteLine($"ListViewFontColor added\t{temp.ListViewFontColor}");
                                                break;
                                            case "ListViewFontShadowColor":
                                                temp.ListViewFontShadowColor = value.ToString()!;
                                                Debug.WriteLine($"ListViewFontShadowColor added\t{temp.ListViewFontShadowColor}");
                                                break;
                                            case "Opacity":
                                                if (int.TryParse(value.ToString(), out int parsedOpacity))
                                                {
                                                    temp.Opacity = parsedOpacity;
                                                    Debug.WriteLine($"Opacity added\t{temp.Opacity}");
                                                }
                                                break;
                                            case "SortBy":
                                                if (Int32.TryParse(value.ToString(), out int parsedSortBy))
                                                {
                                                    temp.SortBy = parsedSortBy;
                                                }
                                                else
                                                {
                                                    temp.SortBy = 1;
                                                }
                                                break;
                                            case "FolderOrder":
                                                if (Int32.TryParse(value.ToString(), out int parsedFolderOrder))
                                                {
                                                    temp.FolderOrder = parsedFolderOrder;
                                                }
                                                break;
                                            case "ShowOnVirtualDesktops":
                                                if (value is string stringShowOnVirtualDesktops)
                                                {
                                                    try
                                                    {
                                                        int[] parsedArray = stringShowOnVirtualDesktops
                                                            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                                            .Select(s => int.Parse(s))
                                                            .ToArray();

                                                        temp.ShowOnVirtualDesktops = parsedArray;
                                                        Debug.WriteLine($"ShowOnVirtualDesktops added\t{temp.ShowOnVirtualDesktops}");
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Debug.WriteLine($"ShowOnVirtualDesktops failed to parse:\t{ex.Message}");
                                                    }
                                                }
                                                break; ;
                                            case "LastAccessedFiles":
                                                if (value is string[] strArray)
                                                {
                                                    temp.LastAccessedFiles = new List<string>(strArray);
                                                }
                                                break;

                                            case "TitleFontSize":
                                                if (double.TryParse(value.ToString(), out double parsedFontSize))
                                                {
                                                    temp.TitleFontSize = parsedFontSize;
                                                    Debug.WriteLine($"TitleFontSize loaded: {temp.TitleFontSize}");
                                                }
                                                break;
                                            default:
                                                Debug.WriteLine($"Unknown value: {valueName}");
                                                break;
                                        }
                                    }

                                }
                                if (temp.Name != "empty")
                                {
                                    if (Path.Exists(temp.Folder))
                                    {
                                        Instances.Add(temp);
                                    }
                                    else
                                    {
                                        RegistryKey key = Registry.CurrentUser.OpenSubKey(temp.GetKeyLocation(), true)!;
                                        if (key != null)
                                        {
                                            Registry.CurrentUser.DeleteSubKeyTree(temp.GetKeyLocation());
                                        }
                                    }
                                }

                            }
                            else
                            {
                                Debug.WriteLine("instance not valid");
                            }
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("try add an empty");
                    Instances.Add(new Instance("empty", false));
                    MainWindow._controller.WriteInstanceToKey(Instances[0]);

                }
            }
            Debug.WriteLine("Showing windows...");
            foreach (var Instance in Instances)
            {
                var subWindow = new DeskFrameWindow(Instance);
                _subWindows.Add(subWindow);
                subWindow.ChangeBackgroundOpacity(Instance.Opacity);
                subWindow.Show();
                _subWindowsPtr.Add(new WindowInteropHelper(subWindow).Handle);
                InitDetails();
            }
            foreach (var window in _subWindows)
            {
                window.HandleWindowMove(true);
                if (window.WonRight != null)
                {
                    window.WonRight.HandleWindowMove(false);
                }
                if (window.WonLeft != null)
                {
                    window.WonLeft.HandleWindowMove(false);
                }
            }
            if (Instances.Count == 0)
            {
                AddInstance();
            }
            Debug.WriteLine("Showing windows DONE");
            // Ensure a desktop instance exists if user wants automatic desktop box
            TryAddDesktopInstance();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ERROR reading key: {ex.Message}");
        }
        isInitializingInstances = false;
    }

    private void TryAddDesktopInstance()
    {
        try
        {
            string desktopPathRaw = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            // Zusätzlich: Öffentlicher Desktop (enthält oft weitere sichtbare Verknüpfungen)
            string publicDesktopPathRaw = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory));
            string desktopPath = NormalizePath(desktopPathRaw);
            if (string.IsNullOrWhiteSpace(desktopPath)) return;
            bool alreadyHasDesktop = Instances.Any(i => NormalizePath(i.Folder) == desktopPath);
            if (alreadyHasDesktop) return;
            var desktopInstance = new Instance("Desktop", false)
            {
                Folder = desktopPathRaw, // Registry & UI können Original behalten
                Name = "Desktop",
                PosX = 20,
                PosY = 20,
                Width = 420,
                Height = 320,
                Minimized = false,
                ShowHiddenFiles = true, // wichtig damit versteckte Dateien sichtbar bleiben
                ShowDisplayName = true,
                AutoExpandonCursor = true,
                BorderEnabled = false,
            };
            Instances.Add(desktopInstance);
            WriteInstanceToKey(desktopInstance);
            var subWindow = new DeskFrameWindow(desktopInstance);
            _subWindows.Add(subWindow);
            subWindow.ChangeBackgroundOpacity(desktopInstance.Opacity);
            // Vor dem Laden der Dateien Desktop-Inhalte verstecken (User + Public) & Explorer Einstellungen anpassen
            EnsureExplorerHidesHiddenFiles();
            EnsureExplorerHidesDesktopIcons();
            HideDesktopItemsForFrame(desktopPathRaw);
            HideDesktopItemsForFrame(publicDesktopPathRaw);
            subWindow.Show();
            _subWindowsPtr.Add(new WindowInteropHelper(subWindow).Handle);
            subWindow.HandleWindowMove(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TryAddDesktopInstance failed: {ex.Message}");
        }
    }

    // Versteckt alle normalen Dateien/Ordner auf dem Desktop (setzt Hidden) und merkt Originalattribute
    private void HideDesktopItemsForFrame(string desktopPath)
    {
        try
        {
            if (!Directory.Exists(desktopPath)) return;

            var entries = Directory.EnumerateFileSystemEntries(desktopPath);
            foreach (var entry in entries)
            {
                try
                {
                    // Systemdateien, Recycle Bin Links, Desktop.ini überspringen
                    string name = Path.GetFileName(entry);
                    if (string.Equals(name, "desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                    if (name.StartsWith("$RECYCLE") || name.StartsWith("Recycle") ) continue;

                    FileAttributes attrs = File.GetAttributes(entry);
                    // Bereits versteckt -> nicht doppelt hinzufügen
                    if ((attrs & FileAttributes.Hidden) == FileAttributes.Hidden) continue;

                    // Speichern Original
                    _desktopHiddenItems.Add((entry, attrs));
                    File.SetAttributes(entry, attrs | FileAttributes.Hidden);
                }
                catch (Exception inner)
                {
                    Debug.WriteLine($"HideDesktopItemsForFrame item failed: {inner.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"HideDesktopItemsForFrame failed: {ex.Message}");
        }
    }

    // Stellt ursprüngliche Attribute wieder her
    public void RestoreDesktopItems()
    {
        foreach (var item in _desktopHiddenItems)
        {
            try
            {
                if (File.Exists(item.Path) || Directory.Exists(item.Path))
                {
                    File.SetAttributes(item.Path, item.Original);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RestoreDesktopItems failed for {item.Path}: {ex.Message}");
            }
        }
        _desktopHiddenItems.Clear();
        RestoreExplorerHiddenSetting();
        RestoreExplorerDesktopIcons();
    }

    // Normalisiert Pfade zur robusten Duplikat-Erkennung (volle Pfadauflösung + LongPath + Kleinbuchstaben)
    private string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        try
        {
            string full = Path.GetFullPath(path);
            // Long path Auflösung
            Span<char> buffer = stackalloc char[full.Length + 10];
            uint len = GetLongPathName(full, buffer, (uint)buffer.Length);
            string longPath = (len > 0 && len < buffer.Length) ? new string(buffer.Slice(0, (int)len)) : full;
            return longPath.TrimEnd(Path.DirectorySeparatorChar).ToLowerInvariant();
        }
        catch
        {
            return path.Trim().ToLowerInvariant();
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetLongPathName(string lpszShortPath, Span<char> lpszLongPath, uint cchBuffer);

    // --- Explorer Hidden Einstellung manipulieren (Registry) ---
    private bool _explorerHiddenModified = false;
    private int? _originalExplorerHiddenValue = null; // 1 = "Zeige versteckte Dateien", 2 = "Verstecke"

    private void EnsureExplorerHidesHiddenFiles()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true);
            if (key == null) return;
            object? val = key.GetValue("Hidden");
            if (val is int current && current == 1)
            {
                _originalExplorerHiddenValue = current;
                key.SetValue("Hidden", 2, Microsoft.Win32.RegistryValueKind.DWord);
                _explorerHiddenModified = true;
                BroadcastSettingsChange();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"EnsureExplorerHidesHiddenFiles failed: {ex.Message}");
        }
    }

    // Desktop-Icons komplett ausblenden (Shell/DesktopView) über HideIcons
    private void EnsureExplorerHidesDesktopIcons()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true);
            if (key == null) return;
            object? val = key.GetValue("HideIcons");
            if (val is int current)
            {
                // Semantik: HideIcons = 0 => Icons werden angezeigt, HideIcons = 1 => Icons ausgeblendet.
                // Falls derzeit sichtbar (0), setzen wir auf 1 zum Ausblenden und merken uns den Originalwert.
                if (current == 0)
                {
                    _originalHideIcons = current;
                    key.SetValue("HideIcons", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    _hideIconsModified = true;
                    BroadcastSettingsChange();
                }
            }
            else
            {
                // Wert existiert nicht -> wir nehmen an: Icons waren sichtbar (äquivalent zu 0) und setzen auf 1 (ausblenden)
                _originalHideIcons = 0;
                key.SetValue("HideIcons", 1, Microsoft.Win32.RegistryValueKind.DWord);
                _hideIconsModified = true;
                BroadcastSettingsChange();
            }
            // Direkt im Anschluss Fallback versuchen, damit Sichtbarkeit sofort verschwindet ohne Explorer-Neustart
            TryHideDesktopShellViews();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"EnsureExplorerHidesDesktopIcons failed: {ex.Message}");
        }
    }

    private void RestoreExplorerHiddenSetting()
    {
        if (!_explorerHiddenModified || _originalExplorerHiddenValue is null) return;
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true);
            if (key != null)
            {
                key.SetValue("Hidden", _originalExplorerHiddenValue, Microsoft.Win32.RegistryValueKind.DWord);
                BroadcastSettingsChange();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RestoreExplorerHiddenSetting failed: {ex.Message}");
        }
        finally
        {
            _explorerHiddenModified = false;
            _originalExplorerHiddenValue = null;
        }
    }

    private void RestoreExplorerDesktopIcons()
    {
        if (!_hideIconsModified || _originalHideIcons is null) return;
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true);
            if (key != null)
            {
                key.SetValue("HideIcons", _originalHideIcons, Microsoft.Win32.RegistryValueKind.DWord);
                BroadcastSettingsChange();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RestoreExplorerDesktopIcons failed: {ex.Message}");
        }
        finally
        {
            _hideIconsModified = false;
            _originalHideIcons = null;
        }
        // Fenster wieder herstellen
        RestoreDesktopShellViews();
    }

    [DllImport("shell32.dll")] private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
    private const uint SHCNE_ASSOCCHANGED = 0x8000000; // Trigger global refresh
    private const uint SHCNF_IDLIST = 0x0;
    private const uint HWND_BROADCAST = 0xFFFF;
    private const uint WM_SETTINGCHANGE = 0x1A;
    private const uint SMTO_ABORTIFHUNG = 0x0002;

    private void BroadcastSettingsChange()
    {
        try
        {
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            SendMessageTimeout(new IntPtr(HWND_BROADCAST), WM_SETTINGCHANGE, IntPtr.Zero, IntPtr.Zero, SMTO_ABORTIFHUNG, 2000, out _);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"BroadcastSettingsChange failed: {ex.Message}");
        }
    }

    // --------- Fallback: Desktop Icon Fenster direkt verstecken ---------
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    private void TryHideDesktopShellViews()
    {
        try
        {
            // Schon versteckt? nicht erneut
            if (_hiddenDesktopListViews.Count > 0) return;
            List<IntPtr> listViewHandles = new();
            EnumWindows((topHwnd, _) =>
            {
                // Kandidaten: Progman oder WorkerW Fenster enthalten SHELLDLL_DefView
                var className = GetWindowClass(topHwnd);
                if (className == "Progman" || className == "WorkerW")
                {
                    EnumChildWindows(topHwnd, (child, __) =>
                    {
                        var childClass = GetWindowClass(child);
                        if (childClass == "SHELLDLL_DefView")
                        {
                            // Direkt das DefView verstecken (enthält ListView)
                            listViewHandles.Add(child);
                        }
                        return true;
                    }, IntPtr.Zero);
                }
                return true;
            }, IntPtr.Zero);

            foreach (var h in listViewHandles.Distinct())
            {
                if (ShowWindow(h, SW_HIDE))
                {
                    _hiddenDesktopListViews.Add(h);
                }
            }
            Debug.WriteLine($"TryHideDesktopShellViews: versteckte Handles={_hiddenDesktopListViews.Count}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TryHideDesktopShellViews failed: {ex.Message}");
        }
    }

    private void RestoreDesktopShellViews()
    {
        try
        {
            foreach (var h in _hiddenDesktopListViews)
            {
                ShowWindow(h, SW_SHOW);
            }
            Debug.WriteLine($"RestoreDesktopShellViews: wiederhergestellt={_hiddenDesktopListViews.Count}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RestoreDesktopShellViews failed: {ex.Message}");
        }
        finally
        {
            _hiddenDesktopListViews.Clear();
        }
    }

    private string GetWindowClass(IntPtr hWnd)
    {
        var sb = new System.Text.StringBuilder(256);
        if (GetClassName(hWnd, sb, sb.Capacity) != 0)
        {
            return sb.ToString();
        }
        return string.Empty;
    }
}