using System;
using System.Collections.Generic;
using System.Linq;
using DV.ThingTypes;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityModManagerNet;

namespace dvNoCableMU;

public static class Plugin
{
    private static bool _loaded;
    private static UnityModManager.ModEntry.ModLogger _logger = null!;

    private static (LocoWrapper loco, bool reversed)? _currentLoco;
    private static readonly List<(LocoWrapper loco, bool reversed)> Locos = new();

    [UsedImplicitly]
    public static bool Load(UnityModManager.ModEntry modEntry)
    {
        _logger = modEntry.Logger;

        var harmony = new Harmony(modEntry.Info.Id);
        harmony.PatchAll();

        WorldStreamingInit.LoadingFinished += OnLoadingFinished;
        PlayerManager.CarChanged += OnCarChanged;
        UnloadWatcher.UnloadRequested += OnUnloadRequested;
        
        modEntry.OnFixedGUI = OnGUI;

        _logger.Log($"Plugin {modEntry.Info.Id} is loaded!");

        return true;
    }

    private static void OnGUI(UnityModManager.ModEntry modEntry)
    {
        if (!_loaded) return;

        GUILayout.Window(9600001, new Rect(20, 20, 300, 0), DrawWindow, "No Cable MU");
        return;

        void DrawWindow(int id)
        {
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
                // TODO: remove this code below
                GUILayout.Label("Reversed", GUILayout.Width(150));
                GUILayout.EndHorizontal();

                var locosToRemove = new List<(LocoWrapper loco, bool reversed)>();

                foreach (var loco in Locos)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(loco.loco.ID, GUILayout.Width(150));
                    GUILayout.Label(loco.loco.Temp.ToString("F1"), GUILayout.Width(100));
                    GUILayout.Label(loco.reversed.ToString(), GUILayout.Width(150));

                    if (GUILayout.Button("X"))
                    {
                        _logger.Log($"Queuing a locomotive to unregister. GUID: {loco.loco.GUID}; " +
                                    $"ID: {loco.loco.ID}; Type: {loco.loco.Type}");
                        locosToRemove.Add(loco);
                    }

                    GUILayout.EndHorizontal();
                }

                foreach (var loco in locosToRemove)
                {
                    Locos.Remove(loco);
                    _logger.Log($"Unregistered a locomotive. GUID: {loco.loco.GUID}; " +
                                $"ID: {loco.loco.ID}; Type: {loco.loco.Type}");

                    if (Locos.IndexOf(loco) != 0) continue;
                    loco.loco.TrainsetChanged -= OnFirstLocoTrainsetChanged;
                    Locos[0].loco.TrainsetChanged += OnFirstLocoTrainsetChanged;
                    _logger.Log(
                        "Unregistered first locomotive, so TrainsetChanged event handler " +
                        "was removed from it and added to the new first locomotive");
                }
            }
            
            GUI.enabled = _currentLoco != null;
            if (GUILayout.Button("Pair Locomotive") && _currentLoco is not null && !Locos.Contains(_currentLoco.Value))
            {
                modEntry.Logger.Log("\"Pair Locomotive\" button pressed. Adding locomotive...");
                Locos.Add(_currentLoco.Value);

                _currentLoco.Value.loco.ThrottleValueUpdated += f => Locos.ForEach(x => x.loco.Throttle = f);
                _currentLoco.Value.loco.BrakeValueUpdated += f => Locos.ForEach(x => x.loco.Brake = f);
                _currentLoco.Value.loco.IndBrakeValueUpdated += f => Locos.ForEach(x => x.loco.IndBrake = f);
                _currentLoco.Value.loco.ReverserValueUpdated += f =>
                    Locos.ForEach(x => x.loco.Reverser = !(_currentLoco.Value.reversed ^ x.reversed) ? f : 1 - f);
                _currentLoco.Value.loco.SanderValueUpdated += f => Locos.ForEach(x => x.loco.Sander = f);
                _currentLoco.Value.loco.DynBrakeValueUpdated += f => Locos.ForEach(x => x.loco.DynBrake = f);
                
                // Reset all controls
                float maxBrake = Locos.Select(x => x.loco.Brake).Max();
                float maxIndBrake = Locos.Select(x => x.loco.IndBrake).Max();
                Locos.ForEach(x =>
                {
                    x.loco.Throttle = 0;
                    x.loco.Reverser = 0;
                    x.loco.DynBrake = 0;
                    x.loco.Sander = 0;
                    x.loco.Brake = maxBrake;
                    x.loco.IndBrake = maxIndBrake;
                });

                if (Locos.Count == 1)
                {
                    _currentLoco.Value.loco.TrainsetChanged += OnFirstLocoTrainsetChanged;
                    _logger.Log("Registered first locomotive, added TrainsetChanged event handler.");
                }
            }

            GUI.enabled = true;
            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
    }

    private static void OnLoadingFinished()
    {
        _logger.Log("Now gaming.");
        if (PlayerManager.Car is not null &&
            PlayerManager.Car.carType is TrainCarType.LocoShunter or TrainCarType.LocoDiesel or TrainCarType.LocoDH4)
        {
            _currentLoco = (new LocoWrapper(PlayerManager.Car), false);
            _logger.Log($"Started gaming with a locomotive. GUID: {_currentLoco.Value.loco.GUID}; " +
                        $"ID: {_currentLoco.Value.loco.ID}; Type: {_currentLoco.Value.loco.Type}");
        }

        _loaded = true;
    }

    private static void OnUnloadRequested()
    {
        _currentLoco = null;
        Locos.Clear();
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
            // No locomotives registered.
            if (Locos.Count == 0)
            {
                _currentLoco = (new LocoWrapper(car), false);
                _logger.Log($"Loco changed to unregistered one. GUID: {_currentLoco.Value.loco.GUID}; " +
                            $"ID: {_currentLoco.Value.loco.ID}; Type: {_currentLoco.Value.loco.Type}");
                return;
            }

            // Not in our trainset
            if (!Locos[0].loco.Trainset.cars.Contains(car))
            {
                _currentLoco = null;
                _logger.Log("Loco changed to one that is not in our trainset.");
                return;
            }

            // Already registered locomotive.
            if (Locos.Exists(x => x.loco.GUID == car.CarGUID))
            {
                _currentLoco = Locos.Find(x => x.loco.GUID == car.CarGUID);
                _logger.Log($"Loco changed back to a registered one. GUID: {_currentLoco.Value.loco.GUID}; " +
                            $"ID: {_currentLoco.Value.loco.ID}; Type: {_currentLoco.Value.loco.Type}");
                return;
            }

            // Locomotive in train, but unregistered.
            _currentLoco = (new LocoWrapper(car),
                IsTrainCarRelativelyReversed(Locos[0].loco.Couplers, car) ^ Locos[0].reversed);
            _logger.Log($"Loco changed to unregistered one. GUID: {_currentLoco.Value.loco.GUID}; " +
                        $"ID: {_currentLoco.Value.loco.ID}; Type: {_currentLoco.Value.loco.Type}");
        }
    }

    private static void OnFirstLocoTrainsetChanged(Trainset trainset)
    {
        Locos.RemoveAll(loco => !trainset.cars.Exists(x => x.CarGUID == loco.loco.GUID));
        
        // Possible that player connected the locomotive they're standing in
        // without leaving the locomotive (e.g. using driving UI), so just check again
        OnCarChanged(PlayerManager.Car);
    }

    private static bool IsTrainCarRelativelyReversed(Coupler[] couplers, TrainCar target)
    {
        var currentCoupler = couplers[0];
        while (currentCoupler.IsCoupled())
        {
            currentCoupler = currentCoupler.coupledTo;
            if (currentCoupler.train.CarGUID == target.CarGUID) return currentCoupler.isFrontCoupler;

            currentCoupler = currentCoupler.GetOppositeCoupler();
        }

        currentCoupler = couplers[1];
        while (currentCoupler.IsCoupled())
        {
            currentCoupler = currentCoupler.coupledTo;
            if (currentCoupler.train.CarGUID == target.CarGUID) return !currentCoupler.isFrontCoupler;

            currentCoupler = currentCoupler.GetOppositeCoupler();
        }

        throw new ArgumentOutOfRangeException(nameof(target),
            "Target locomotive is not connected to source locomotive!");
    }
}