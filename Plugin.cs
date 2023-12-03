using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityModManagerNet;

namespace dvNoCableMU;

#if DEBUG
[EnableReloading]
#endif
public static class Plugin
{
    private static UnityModManager.ModEntry.ModLogger _logger = null!;
    private static LocoManagerWindow _locoManagerWindow = null!;
    private static GameObject _gameObject = null!;

    public static Settings Settings = null!;

    [UsedImplicitly]
    public static bool Load(UnityModManager.ModEntry modEntry)
    {
        _logger = modEntry.Logger;

#if DEBUG
        modEntry.OnUnload = OnModUnload;
        _logger.Log("Enabled mod reloading.");
#endif

        var harmony = new Harmony(modEntry.Info.Id);
        harmony.PatchAll();
        _logger.Log("Patched absolutely nothing.");

        Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
        modEntry.OnGUI += OnGUI;
        modEntry.OnSaveGUI += OnSaveGUI;
        _logger.Log("Loaded settings and registered settings GUI event handlers.");

        WorldStreamingInit.LoadingFinished += OnLoadingFinished;
        UnloadWatcher.UnloadRequested += OnUnloadRequested;
        _logger.Log("Added game loaded event handlers.");

        // Create the GameObject for the UI. We want it to be maintained through
        // scene changes and we want the LocoManagerWindow component to start 
        // disabled because it should only be enabled once loaded into a game session.
        _gameObject = new GameObject("NoCableMUWindowDummy");
        Object.DontDestroyOnLoad(_gameObject);
        _gameObject.SetActive(false);
        _locoManagerWindow = _gameObject.AddComponent<LocoManagerWindow>();
        _locoManagerWindow.enabled = false;
        _gameObject.SetActive(true);
        _logger.Log("Added locomotive manager window.");

        _logger.Log($"Plugin {modEntry.Info.Id} is loaded!");

        return true;
    }

    private static void OnGUI(UnityModManager.ModEntry modEntry)
    {
        Settings.Draw(modEntry);
    }

    private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
    {
        Settings.Save(modEntry);
    }

#if DEBUG
    private static bool OnModUnload(UnityModManager.ModEntry modEntry)
    {
        var harmony = new Harmony(modEntry.Info.Id);
        harmony.UnpatchAll();

        Object.Destroy(_gameObject);
        WorldStreamingInit.LoadingFinished -= OnLoadingFinished;
        UnloadWatcher.UnloadRequested -= OnUnloadRequested;
        return true;
    }
#endif

    private static void OnLoadingFinished()
    {
        _logger.Log("Now gaming.");
        _locoManagerWindow.enabled = true;
    }

    private static void OnUnloadRequested()
    {
        _logger.Log("Unloading!");
        _locoManagerWindow.enabled = false;
    }
}