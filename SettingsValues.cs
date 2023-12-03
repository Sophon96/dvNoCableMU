namespace dvNoCableMU;

public class SettingsValues
{
    public bool EnableSteamMU { get; private set; }
    public bool EnableCylCocks { get; private set; }
    public bool EnableMechanicalMU { get; private set; }

    public SettingsValues(bool enableSteamMU, bool enableCylCocks, bool enableMechanicalMU)
    {
        EnableSteamMU = enableSteamMU;
        EnableCylCocks = enableCylCocks;
        EnableMechanicalMU = enableMechanicalMU;
    }
}