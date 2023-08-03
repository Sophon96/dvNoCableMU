using System.Collections.Generic;
using System.Linq;
using DV.ThingTypes;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace dvNoCableMU;

public static class Plugin
{
    private static bool _loaded;
    private static UnityModManager.ModEntry.ModLogger _logger = null!;

    private static LocoWrapper? _currentLoco;
    private static readonly List<LocoWrapper> Locos = new();

    public static bool Load(UnityModManager.ModEntry modEntry)
    {
        // Plugin startup logic
        _logger = modEntry.Logger;

        var harmony = new Harmony(modEntry.Info.Id);
        harmony.PatchAll();

        WorldStreamingInit.LoadingFinished += OnLoadingFinished;
        PlayerManager.CarChanged += OnCarChanged;
        UnloadWatcher.UnloadRequested += OnUnloadRequested;

        // wtf do these actually do???
        modEntry.OnFixedGUI = OnGUI;
        modEntry.OnUpdate = Update;

        _logger.Log($"Plugin {modEntry.Info.Id} is loaded!");

        return true;
    }

    private static void OnGUI(UnityModManager.ModEntry modEntry)
    {
        if (!_loaded) return;

        // // Height of an item
        // const int itemHeight = 20;
        //
        // // Width of an item
        // const int itemWidth = 300;
        //
        // // Padding around the window and for the title bar
        // const int padding = 20;
        // const int titleBarHeight = 20;

        //var height = Locos.Count * itemHeight + titleBarHeight + 2 * padding;
        //var width = itemWidth + 2 * padding;

        GUILayout.Window(9600001, new Rect(20, 20, 300, 0), DrawWindow, "No Cable MU");
        return;

        void DrawWindow(int id)
        {
            //GUILayout.Space(titleBarHeight);
            GUILayout.BeginVertical();

            if (Locos.Count == 0)
            {
                GUILayout.Label("No Locomotives Paired!");
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Loco ID", GUILayout.Width(150));
                GUILayout.Label("Temp", GUILayout.Width(100));
                //GUILayout.Label("Status", GUILayout.Width(250));
                GUILayout.EndHorizontal();

                foreach (var loco in Locos)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(loco.ID, GUILayout.Width(150));
                    GUILayout.Label(((int)loco.Temp).ToString(), GUILayout.Width(100));
                    if (GUILayout.Button("X"))
                    {
                        Locos.Remove(loco);
                    }

                    GUILayout.EndHorizontal();
                }
            }

            if (Locos.Count >= 8) return;
            GUI.enabled = _currentLoco != null;
            if (GUILayout.Button("Pair Locomotive") && _currentLoco is not null && !Locos.Contains(_currentLoco))
            {
                modEntry.Logger.Log("\"Pair Locomotive\" button pressed. Adding locomotive...");
                Locos.Add(_currentLoco);
            }

            GUI.enabled = true;
            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        /*var richTextStyle = new GUIStyle
        {
            richText = true
        };

        GUILayout.BeginArea(new Rect(0, 0, 1000, 800), "Distributed Power", "box");

        GUILayout.BeginHorizontal("Locomotives", GUIStyle.none);

        if (Locos.Count == 0)
            GUILayout.Label("None registered");
        else
        {
            foreach (var loco in Locos)
            {
                GUILayout.BeginVertical();

                GUILayout.Label("Properties");

                GUILayout.Label("ID: " + loco.ID);
                GUILayout.Label("Type: " + loco.Type);
                GUILayout.Label("Derailed: " + loco.Derailed);
                GUILayout.Label("Exploded: " + loco.Exploded);
                GUILayout.Label("Speed: " + loco.Speed);

                GUILayout.Label("Brake: " + loco.Brake);
                GUILayout.Label("Engine On: " + loco.EngineOn);
                GUILayout.Label("Fuel Consumption: " + loco.FuelConsumption);
                GUILayout.Label("Max RPM: " + loco.MaxRpm);
                GUILayout.Label("RPM: " + loco.Rpm);
                GUILayout.Label("Normalized RPM: " + loco.RpmNorm);
                GUILayout.Label("Normalized Fuel: " + loco.FuelNorm);
                GUILayout.Label("Fuel Capacity: " + loco.FuelCap);
                GUILayout.Label("Individual Brake: " + loco.IndBrake);
                GUILayout.Label("Oil Level Normalized: " + loco.OilNorm);
                GUILayout.Label("Oil Capacity: " + loco.OilCap);
                GUILayout.Label("Reverser: " + loco.Reverser);
                GUILayout.Label("Sand Level Normalized: " + loco.SandNorm);
                GUILayout.Label("Sand Capacity: " + loco.SandCap);
                GUILayout.Label("Sander: " + loco.Sander);
                GUILayout.Label("Throttle: " + loco.Throttle);
                GUILayout.Label("Temperature: " + loco.Temp);

                if (loco.Type is TrainCarType.LocoShunter or TrainCarType.LocoDiesel)
                {
                    GUILayout.Label("Traction Motor Amps: " + loco.TmAmps);
                    GUILayout.Label("Normalized Traction Motor Amps: " + loco.TmAmpsNorm);
                    GUILayout.Label("Max Traction Motor Amps: " + loco.TmMaxAmps);
                    GUILayout.Label("Traction Motor RPM: " + loco.TmRpm);
                    GUILayout.Label("Normalized Traction Motor RPM: " + loco.TmRpmNorm);
                    GUILayout.Label("Traction Motor Fuse: " + loco.TmFuse);
                    GUILayout.Label("Traction Motor State: " + loco.TmState);
                }

                if (loco.Type is TrainCarType.LocoDiesel or TrainCarType.LocoDH4)
                    GUILayout.Label("Dynamic Brake: " + loco.DynBrake);

                if (loco.Type is TrainCarType.LocoDH4)
                {
                    GUILayout.Label("Fluid Coupler RPM: " + loco.FcRpm);
                    GUILayout.Label("Normalized Fluid Coupler RPM: " + loco.FcRpmNorm);
                    GUILayout.Label("Fluid Coupler Broken: " + loco.FcBroken);
                    GUILayout.Label("Fluid Coupler Active Config: " + loco.FcActiveConfig);
                    GUILayout.Label("Fluid Coupler Efficiency: " + loco.FcEfficiency);
                }

                if (GUILayout.Button("Unregister"))
                {
                    Locos.Remove(loco);
                }

                GUILayout.EndVertical();
            }
        }

        if (Locos.Count < 6)
        {
            if (GUILayout.Button("Register"))
            {
                if (_currentLoco is not null && !Locos.Contains(_currentLoco))
                {
                    modEntry.Logger.Log("\"Register\" button pressed. Registering new locomotive...");
                    Locos.Add(_currentLoco);
                }
            }
        }

        GUILayout.EndHorizontal();

        GUILayout.EndArea();*/
    }

    private static void OnLoadingFinished()
    {
        if (PlayerManager.Car is not null &&
            PlayerManager.Car.carType is TrainCarType.LocoShunter or TrainCarType.LocoDiesel or TrainCarType.LocoDH4)
        {
            _currentLoco = new LocoWrapper(PlayerManager.Car);
        }

        _loaded = true;
    }

    private static void OnUnloadRequested()
    {
        _currentLoco = null;
        _loaded = false;
    }

    private static void OnCarChanged(TrainCar? car)
    {
        if (car is null)
        {
            _currentLoco = null;
            _logger.Log("Loco changed to null.");
        }
        else if (car.carType is TrainCarType.LocoShunter or TrainCarType.LocoDiesel or TrainCarType.LocoDH4)
        {
            _currentLoco = new LocoWrapper(car);
            _logger.Log("Loco changed. ID: " + _currentLoco.ID + "; Type: " + _currentLoco.Type);
        }
    }

    private static void Update(UnityModManager.ModEntry modEntry, float value)
    {
        if (!_loaded || _currentLoco is null || !Locos.Contains(_currentLoco)) return;

        // hopefully _currentLoco doesn't change in a frame (it shouldn't) (source: my ass)
        foreach (var loco in Locos.Where(loco => loco != _currentLoco))
        {
            loco.Throttle = _currentLoco.Throttle;
            loco.Brake = _currentLoco.Brake;
            loco.IndBrake = _currentLoco.IndBrake;
            loco.Sander = _currentLoco.Sander;

            if (loco.DynBrake > -1 && _currentLoco.DynBrake > -1)
            {
                loco.DynBrake = _currentLoco.DynBrake;
            }

            bool reversed = (loco.Speed * _currentLoco.Speed) < 0;
            if (reversed)
            {
                loco.Reverser = 1 - _currentLoco.Reverser;
            }
            else
            {
                loco.Reverser = _currentLoco.Reverser;
            }
        }
    }
}