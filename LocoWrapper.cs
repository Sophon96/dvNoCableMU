using System;
using DV.Simulation.Cars;
using DV.ThingTypes;
using LocoSim.Implementations;

namespace dvNoCableMU;

public class LocoWrapper: IEquatable<LocoWrapper>
{
    private readonly TrainCar _loco;

    private SimController _simController;

    private BaseControlsOverrider controlsOverrider;

    private readonly SimulationFlow _simFlow;
    
    private readonly Port _tempPort;

    public TrainCarType Type { get; }
    public Trainset Trainset { get; private set; }

    public event Action<Trainset> TrainsetChanged
    {
        add => _loco.TrainsetChanged += value;
        remove => _loco.TrainsetChanged += value;
    }


    public LocoWrapper(TrainCar loco)
    {
        _loco = loco;
        _simController = loco.GetComponent<SimController>();
        controlsOverrider = _simController.controlsOverrider;
        _simFlow = _simController.simFlow;

        Type = loco.carType;

        Trainset = loco.trainset;
        loco.TrainsetChanged += OnTrainsetChanged;

        switch (Type)
        {
            case TrainCarType.LocoDiesel:
                goto case TrainCarType.LocoShunter;

            case TrainCarType.LocoShunter:
                _simFlow.TryGetPort("tmHeat.TEMPERATURE", out _tempPort);
                break;

            case TrainCarType.LocoDH4:
                _simFlow.TryGetPort("coolant.TEMPERATURE", out _tempPort);
                break;

            default:
                // TODO: LOG ERROR
                // very bad happened
                throw new ArgumentOutOfRangeException(nameof(loco.carType),
                    "Unrecognized locomotives type! Got type \"" + loco.carType + '"');
        }
    }

    public bool Equals(LocoWrapper? other)
    {
        if (other is null) return false;
        return _loco.CarGUID == other._loco.CarGUID;
    }

    private void OnTrainsetChanged(Trainset a)
    {
        Trainset = a;
    }

    // TODO: consider removing all unused properties and respective ports (like half the code here lol)
    public bool Derailed => _loco.derailed;
    public bool Exploded => _loco.isExploded;
    public string ID => _loco.ID;
    public string GUID => _loco.CarGUID;
    public float Speed => _loco.GetForwardSpeed();
    public Coupler[] Couplers => _loco.couplers;

    // TODO: null checks on all controls
    public float Brake
    {
        get => controlsOverrider.Brake.Value;
        // This has less precedence than the physical MU cable
        // which is fine, I guess.
        set => controlsOverrider.Brake.Set(value);
    }

    public event Action<float> BrakeValueUpdated
    {
        add => controlsOverrider.Brake.ControlUpdated += value;
        remove => controlsOverrider.Brake.ControlUpdated -= value;
    }
    
    public float IndBrake
    {
        get => controlsOverrider.IndependentBrake.Value;
        set => controlsOverrider.IndependentBrake.Set(value);
    }
    
    public event Action<float> IndBrakeValueUpdated
    {
        add => controlsOverrider.IndependentBrake.ControlUpdated += value;
        remove => controlsOverrider.IndependentBrake.ControlUpdated -= value;
    }

    public float Reverser
    {
        get => controlsOverrider.Reverser.Value;
        set => controlsOverrider.Reverser.Set(value);
    }
    
    public event Action<float> ReverserValueUpdated
    {
        add => controlsOverrider.Reverser.ControlUpdated += value;
        remove => controlsOverrider.Reverser.ControlUpdated -= value;
    }

    public float Sander
    {
        get => controlsOverrider.Sander.Value;
        set => controlsOverrider.Sander.Set(value);
    }
    
    public event Action<float> SanderValueUpdated
    {
        add => controlsOverrider.Sander.ControlUpdated += value;
        remove => controlsOverrider.Sander.ControlUpdated -= value;
    }

    public float Throttle
    {
        get => controlsOverrider.Throttle.Value;
        set => controlsOverrider.Throttle.Set(value);
    }
    
    public event Action<float> ThrottleValueUpdated
    {
        add => controlsOverrider.Throttle.ControlUpdated += value;
        remove => controlsOverrider.Throttle.ControlUpdated -= value;
    }

    public float Temp => _tempPort.Value;

    public float DynBrake
    {
        get
        {
            if (controlsOverrider.DynamicBrake != null) return controlsOverrider.DynamicBrake.Value;
            return -1;
        }
        set
        {
            if (controlsOverrider.DynamicBrake != null) controlsOverrider.DynamicBrake.Set(value);
        }
    }
    
    public event Action<float> DynBrakeValueUpdated
    {
        add
        {
            if (controlsOverrider.DynamicBrake != null) controlsOverrider.DynamicBrake.ControlUpdated += value;
        }
        remove
        {
            if (controlsOverrider.DynamicBrake != null) controlsOverrider.DynamicBrake.ControlUpdated -= value;
        }
    }
}