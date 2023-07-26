using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using DV.ThingTypes;
using UnityEngine;

namespace MyFirstPlugin;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private bool _loaded;
    private LocoWrapper? _currentLoco;

    private readonly List<LocoWrapper> _locos = new();

    private void Awake()
    {
        // Plugin startup logic
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        WorldStreamingInit.LoadingFinished += OnLoadingFinished;
        PlayerManager.CarChanged += OnCarChanged;
        UnloadWatcher.UnloadRequested += OnUnloadRequested;
    }

    private void OnGUI()
    {
        if (!_loaded) return;

        var richTextStyle = new GUIStyle
        {
            richText = true
        };

        GUILayout.BeginArea(new Rect(0, 0, 1000, 800), "Distributed Power", "box");

        GUILayout.BeginHorizontal("Locomotives", GUIStyle.none);

        if (_locos.Count == 0)
            GUILayout.Label("None registered");
        else
        {
            foreach (var loco in _locos)
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
                    _locos.Remove(loco);
                }

                GUILayout.EndVertical();
            }
        }

        if (_locos.Count < 6)
        {
            if (GUILayout.Button("Register"))
            {
                if (_currentLoco is not null && !_locos.Contains(_currentLoco))
                {
                    Logger.LogInfo("\"Register\" button pressed. Registering new locomotive...");
                    _locos.Add(_currentLoco);
                }
            }
        }

        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void OnLoadingFinished()
    {
        if (PlayerManager.Car is not null &&
            PlayerManager.Car.carType is TrainCarType.LocoShunter or TrainCarType.LocoDiesel or TrainCarType.LocoDH4)
        {
            _currentLoco = new LocoWrapper(PlayerManager.Car);
        }

        _loaded = true;
    }

    private void OnUnloadRequested()
    {
        _currentLoco = null;
        _loaded = false;
    }

    private void OnCarChanged(TrainCar? car)
    {
        if (car is null)
        {
            _currentLoco = null;
            Logger.LogInfo("Loco changed to null.");
        }
        else if (car.carType is TrainCarType.LocoShunter or TrainCarType.LocoDiesel or TrainCarType.LocoDH4)
        {
            _currentLoco = new LocoWrapper(car);
            Logger.LogInfo("Loco changed. ID: " + _currentLoco.ID + "; Type: " + _currentLoco.Type);
        }
    }

    private void Update()
    {
        if (!_loaded || _currentLoco is null || !_locos.Contains(_currentLoco)) return;
        
        // hopefully _currentLoco doesn't change in a frame (it shouldn't) (source: my ass)
        foreach (var loco in _locos.Where(loco => loco != _currentLoco))
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