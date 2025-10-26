using Microsoft.Win32;
using System.Diagnostics;
using System;
using System.Collections.Generic;

public class RegistryHelper
{
    public string regKeyName;
    public RegistryHelper(string regkeyname)
    {
        this.regKeyName = regkeyname;
    }
    public void WriteToRegistry(string keyName, object value, Instance instance)
    {
        try
        {
            if (instance == null || string.IsNullOrEmpty(instance.Name))
            {
                Debug.WriteLine("Instance is null reg not writen");
                return;
            }
            using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(instance.GetKeyLocation()))
            {
                key?.SetValue(keyName, value);
            }
            //  Debug.WriteLine($"wrote key: {keyName}\t value: {value}\nto: {instance.GetKeyLocation()}");
        }
        catch { }
    }
    public void WriteMultiLineRegistry(string keyName, List<string>? list, Instance instance)
    {
        try
        {
            if (instance == null || string.IsNullOrEmpty(instance.Name))
            {
                Debug.WriteLine("Instance is null reg not written");
                return;
            }
            using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(instance.GetKeyLocation()))
            {
                if (key != null)
                {
                    if (list == null || list.Count == 0)
                    {
                        key.SetValue(keyName, Array.Empty<string>(), RegistryValueKind.MultiString);
                    }
                    else
                    {
                        key.SetValue(keyName, list.ToArray(), RegistryValueKind.MultiString);
                    }
                }
            }
        }
        catch
        {
        }
    }

    public void WriteIntArrayToRegistry(string keyName, int[]? values, Instance instance)
    {
        try
        {
            if (instance == null || string.IsNullOrEmpty(instance.Name))
            {
                Debug.WriteLine("Instance is null reg not writen");
                return;
            }
            if (values == null)
            {
                using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(instance.GetKeyLocation()))
                {
                    key?.SetValue(keyName, "");
                }
                return;
            }
            using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(instance.GetKeyLocation()))
            {
                key?.SetValue(keyName, string.Join(",", values));
            }
        }
        catch { }
    }
    public void WriteToRegistryRoot(string keyName, object value)
    {
        try
        {
            using (RegistryKey? key = Registry.CurrentUser.CreateSubKey($"SOFTWARE\\{regKeyName}"))
            {
                key?.SetValue(keyName, value);
            }
        }
        catch { }
    }
    public bool KeyExists(string keyName, Instance instance)
    {
        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(instance.GetKeyLocation()))
            {
                if (key?.GetValue(keyName) != null)
                {
                    Debug.WriteLine($"exists: {keyName},{key.GetValue(keyName)}");
                    return true;
                }
            }
            Debug.WriteLine($"doesnt exist: {keyName}");
            return false;
        }
        catch
        {
            Debug.WriteLine($"error opening HKCU\\SOFTWARE\\{regKeyName}, {keyName}");
            return false;
        }
    }

    public bool KeyExistsRoot(string keyName)
    {
        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{regKeyName}"))
            {
                if (key?.GetValue(keyName) != null)
                {
                    Debug.WriteLine($"exists: {keyName},{key.GetValue(keyName)}");
                    return true;
                }
            }
            Debug.WriteLine($"doesnt exist: {keyName}");
            return false;
        }
        catch
        {
            Debug.WriteLine($"error opening HKCU\\SOFTWARE\\{regKeyName}, {keyName}");
            return false;
        }
    }
    public object? ReadKeyValueRoot(string keyName)
    {
        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{regKeyName}"))
            {
                var raw = key?.GetValue(keyName);
                if (raw == null)
                {
                    Debug.WriteLine($"couldn't return for {keyName} (no value)");
                    return null;
                }
                if (raw is bool b)
                {
                    Debug.WriteLine($"returned bool for {keyName}");
                    return b;
                }
                if (bool.TryParse(raw.ToString(), out bool boolValue))
                {
                    Debug.WriteLine($"returned parsed bool for {keyName}");
                    return boolValue;
                }
                Debug.WriteLine($"returned string for {keyName}");
                return raw.ToString();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error ReadKeyValueRoot: " + ex.Message);
            return null;
        }
    }
    public int? ReadKeyValueRootInt(string keyName)
    {
        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{regKeyName}"))
            {
                var raw = key?.GetValue(keyName);
                if (raw == null)
                {
                    Debug.WriteLine($"couldn't return value for {keyName} (no value)");
                    return null;
                }
                if (int.TryParse(raw.ToString(), out int intValue))
                {
                    return intValue;
                }
                Debug.WriteLine($"value for {keyName} not an int: {raw}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error ReadKeyValueRootInt: " + ex.Message);
            return null;
        }
    }

    public void AddToAutoRun(string appName, string appPath)
    {
        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                key?.SetValue(appName, appPath);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error adding to autorun: " + ex.Message);
        }
    }

    public void RemoveFromAutoRun(string appName)
    {
        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                key?.DeleteValue(appName, false);
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error removing from autorun: " + ex.Message);
        }
    }



}

