using System;
using UnityModManagerNet;

namespace dvNoCableMU;

public class Settings : UnityModManager.ModSettings, IDrawable
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable FieldCanBeMadeReadOnly.Global
    [Draw("Steam locomotive MU")] public bool EnableSteamMU = false;
    [Draw("Sync cylinder cocks")] public bool EnableCylCocks = false;
    // ReSharper disable once InconsistentNaming
    [Draw("Mechanical locomotive MU")] public bool EnableMechanicalMU = false;
    // ReSharper restore FieldCanBeMadeReadOnly.Global

    public override void Save(UnityModManager.ModEntry modEntry)
    {
        Save(this, modEntry);
    }

    public void OnChange()
    {
        SettingsChanged?.Invoke(new SettingsValues(EnableSteamMU, EnableCylCocks, EnableMechanicalMU));
    }

    public event Action<SettingsValues>? SettingsChanged;
}