using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DV.ThingTypes;
using DV.Utils;
using UnityEngine;
using UnityModManagerNet;

namespace dvNoCableMU;

public class LocoManagerWindow : MonoBehaviour
{
    private readonly List<(LocoWrapper loco, bool reversed)> _locos = new();
    private readonly UnityModManager.ModEntry.ModLogger _logger = new(typeof(LocoManagerWindow).ToString());

    private (LocoWrapper loco, bool reversed)? _currentLoco;

    private string _statusMessage = "Unset (this shouldn't ever be seen)";

    private readonly Rect _windowRect = new(20, 20, 300, 0);

    private void OnEnable()
    {
        PlayerManager.CarChanged += OnCarChanged;
        SingletonBehaviour<CarSpawner>.Instance.CarAboutToBeDeleted += OnLocoDestroyed;
        if (PlayerManager.Car is not null &&
            PlayerManager.Car.carType is TrainCarType.LocoShunter or TrainCarType.LocoDiesel or TrainCarType.LocoDH4)
        {
            _currentLoco = (new LocoWrapper(PlayerManager.Car), false);
            _logger.Log($"Started gaming with a locomotive. GUID: {_currentLoco.Value.loco.GUID}; " +
                        $"ID: {_currentLoco.Value.loco.ID}; Type: {_currentLoco.Value.loco.Type}");
        }

        _statusMessage = _currentLoco == null ? "Inactive" : "Ready to pair";
    }

    private void OnDisable()
    {
        PlayerManager.CarChanged -= OnCarChanged;
        SingletonBehaviour<CarSpawner>.Instance.CarAboutToBeDeleted -= OnLocoDestroyed;
        _currentLoco = null;
        _statusMessage = "Plugin is unloaded (this message should not be seen)";

        foreach (var (loco, reversed) in _locos)
        {
            loco.ThrottleValueUpdated -= OnLocoThrottleValueUpdated;
            loco.BrakeValueUpdated -= OnLocoBrakeValueUpdated;
            loco.IndBrakeValueUpdated -= OnLocoIndBrakeValueUpdated;
            loco.ReverserValueUpdated -=
                reversed ? OnLocoReverserValueUpdatedReversed : OnLocoReverserValueUpdatedNormal;
            loco.SanderValueUpdated -= OnLocoSanderValueUpdated;
            loco.DynBrakeValueUpdated -= OnLocoDynBrakeValueUpdated;
        }

        _locos.Clear();
    }

    private void OnGUI()
    {
        GUILayout.Window(9600001, _windowRect, DrawWindow, "No Cable MU");
    }

    private void DrawWindow(int id)
    {
        GUILayout.BeginVertical();

        if (_locos.Count == 0)
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

            foreach (var loco in _locos)
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

            foreach (var loco in locosToRemove) StartCoroutine(RemoveLoco(loco));
        }

        GUI.enabled = _currentLoco != null && !_locos.Contains(_currentLoco.Value);
        if (GUILayout.Button("Pair Locomotive") && _currentLoco is not null)
        {
            _logger.Log("\"Pair Locomotive\" button pressed. Adding locomotive...");
            StartCoroutine(AddLoco(_currentLoco.Value.loco, _currentLoco.Value.reversed));
        }

        GUI.enabled = true;
        GUILayout.Label($"Status: {_statusMessage}");
        GUILayout.EndVertical();

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }


    private IEnumerator RemoveLoco((LocoWrapper loco, bool reversed) loco)
    {
        _locos.Remove(loco);
        _logger.Log($"Unregistered a locomotive. GUID: {loco.loco.GUID}; " +
                    $"ID: {loco.loco.ID}; Type: {loco.loco.Type}");
        _statusMessage = _currentLoco != null ? "Ready to pair" : _locos.Count > 0 ? "Active" : "Inactive";
        yield return null;

        loco.loco.ThrottleValueUpdated -= OnLocoThrottleValueUpdated;
        loco.loco.BrakeValueUpdated -= OnLocoBrakeValueUpdated;
        loco.loco.IndBrakeValueUpdated -= OnLocoIndBrakeValueUpdated;
        loco.loco.ReverserValueUpdated -=
            loco.reversed ? OnLocoReverserValueUpdatedReversed : OnLocoReverserValueUpdatedNormal;
        loco.loco.SanderValueUpdated -= OnLocoSanderValueUpdated;
        loco.loco.DynBrakeValueUpdated -= OnLocoDynBrakeValueUpdated;
        yield return null;

        if (_locos.IndexOf(loco) != 0) yield break;
        loco.loco.TrainsetChanged -= OnFirstLocoTrainsetChanged;
        _locos[0].loco.TrainsetChanged += OnFirstLocoTrainsetChanged;
        _logger.Log(
            "Unregistered first locomotive, so TrainsetChanged event handler " +
            "was removed from it and added to the new first locomotive");
    }

    private IEnumerator AddLoco(LocoWrapper targetLoco, bool reversed)
    {
        if (targetLoco == null) throw new ArgumentNullException(nameof(targetLoco));

        _locos.Add((targetLoco, reversed));
        _statusMessage = "Active";
        yield return null;

        targetLoco.ThrottleValueUpdated += OnLocoThrottleValueUpdated;
        targetLoco.BrakeValueUpdated += OnLocoBrakeValueUpdated;
        targetLoco.IndBrakeValueUpdated += OnLocoIndBrakeValueUpdated;
        targetLoco.ReverserValueUpdated +=
            reversed ? OnLocoReverserValueUpdatedReversed : OnLocoReverserValueUpdatedNormal;
        targetLoco.SanderValueUpdated += OnLocoSanderValueUpdated;
        targetLoco.DynBrakeValueUpdated += OnLocoDynBrakeValueUpdated;
        yield return null;

        // Reset all controls
        var (maxBrake, maxIndBrake) = _locos.Aggregate((MaxBrake: 0f, MaxIndBrake: 0f),
            (max, loco) =>
            {
                max.MaxBrake = Math.Max(max.MaxBrake, loco.loco.Brake);
                max.MaxIndBrake = Math.Max(max.MaxIndBrake, loco.loco.IndBrake);
                return max;
            });
        _locos.ForEach(x =>
        {
            x.loco.Throttle = 0;
            x.loco.Reverser = 0.5f;
            x.loco.DynBrake = 0;
            x.loco.Sander = 0;
            x.loco.Brake = maxBrake;
            x.loco.IndBrake = maxIndBrake;
        });
        yield return null;

        if (_locos.Count == 1)
        {
            targetLoco.TrainsetChanged += OnFirstLocoTrainsetChanged;
            _logger.Log("Registered first locomotive, added TrainsetChanged event handler.");
        }
    }

    private void OnLocoDynBrakeValueUpdated(float f)
    {
        _locos.ForEach(x => x.loco.DynBrake = f);
    }

    private void OnLocoSanderValueUpdated(float f)
    {
        _locos.ForEach(x => x.loco.Sander = f);
    }

    private void OnLocoReverserValueUpdatedNormal(float f)
    {
        _locos.ForEach(x => x.loco.Reverser = x.reversed ? 1 - f : f);
    }

    private void OnLocoReverserValueUpdatedReversed(float f)
    {
        _locos.ForEach(x => x.loco.Reverser = x.reversed ? f : 1 - f);
    }

    private void OnLocoIndBrakeValueUpdated(float f)
    {
        _locos.ForEach(x => x.loco.IndBrake = f);
    }

    private void OnLocoBrakeValueUpdated(float f)
    {
        _locos.ForEach(x => x.loco.Brake = f);
    }

    private void OnLocoThrottleValueUpdated(float f)
    {
        _locos.ForEach(x => x.loco.Throttle = f);
    }

    private void OnCarChanged(TrainCar? car)
    {
        if (car is null)
        {
            _currentLoco = null;
            _statusMessage = _locos.Count > 0 ? "Active" : "Inactive";
            _logger.Log("Loco changed to null.");
        }
        else if (car.carType is TrainCarType.LocoShunter or TrainCarType.LocoDiesel or TrainCarType.LocoDH4)
        {
            // No locomotives registered.
            if (_locos.Count == 0)
            {
                _currentLoco = (new LocoWrapper(car), false);
                _statusMessage = "Ready to pair";
                _logger.Log($"Loco changed to unregistered one. GUID: {_currentLoco.Value.loco.GUID}; " +
                            $"ID: {_currentLoco.Value.loco.ID}; Type: {_currentLoco.Value.loco.Type}");
                return;
            }

            // Not in our trainset
            if (!_locos[0].loco.Trainset.cars.Contains(car))
            {
                _currentLoco = null;
                _statusMessage = _locos.Count > 0 ? "Active" : "Inactive";
                _logger.Log("Loco changed to one that is not in our trainset.");
                return;
            }

            // Already registered locomotive.
            if (_locos.Exists(x => x.loco.GUID == car.CarGUID))
            {
                _currentLoco = _locos.Find(x => x.loco.GUID == car.CarGUID);
                _statusMessage = "Active";
                _logger.Log($"Loco changed back to a registered one. GUID: {_currentLoco.Value.loco.GUID}; " +
                            $"ID: {_currentLoco.Value.loco.ID}; Type: {_currentLoco.Value.loco.Type}");
                return;
            }

            // Locomotive in train, but unregistered.
            _currentLoco = (new LocoWrapper(car),
                IsTrainCarRelativelyReversed(_locos[0].loco.Couplers, car) ^ _locos[0].reversed);
            _statusMessage = "Ready to pair";
            _logger.Log($"Loco changed to unregistered one. GUID: {_currentLoco.Value.loco.GUID}; " +
                        $"ID: {_currentLoco.Value.loco.ID}; Type: {_currentLoco.Value.loco.Type}");
        }
    }

    private void OnFirstLocoTrainsetChanged(Trainset trainset)
    {
        for (int i = _locos.Count - 1; i >= 0; i--)
        {
            if (!trainset.cars.Exists(x => x.CarGUID == _locos[i].loco.GUID))
            {
                StartCoroutine(RemoveLoco(_locos[i]));
            }
        }
        
        // Possible that player connected the locomotive they're standing in
        // without leaving the locomotive (e.g. using driving UI), so just check again
        OnCarChanged(PlayerManager.Car);
    }

    private void OnLocoDestroyed(TrainCar loco)
    {
        StartCoroutine(RemoveLoco(_locos.Find(x => x.loco.GUID == loco.CarGUID)));
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