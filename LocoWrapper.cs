using System;
using DV.Simulation.Cars;
using DV.ThingTypes;
using LocoSim.Definitions;
using LocoSim.Implementations;

namespace dvNoCableMU;

public class LocoWrapper: IEquatable<LocoWrapper>
{
    private readonly TrainCar _loco;

    private SimController _simController;

    private BaseControlsOverrider controlsOverrider;

    private readonly SimulationFlow _simFlow;
    
    private readonly Port _tempPort;

    private bool _hasDynBrake = false, _hasCylCocks = false, _hasGearbox = false;

    private readonly Port? _cylCocksPort;
    // I don't know if we *have* to use the out ports.
    private readonly Port? _gearAInPort, _gearBInPort;

    public TrainCarType Type { get; }
    public Trainset Trainset { get; private set; }

    public event Action<Trainset> TrainsetChanged
    {
        add => _loco.TrainsetChanged += value;
        remove => _loco.TrainsetChanged -= value;
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

            case TrainCarType.LocoDM3:
                _hasGearbox = true;
                _simFlow.TryGetPort("gearInputA.CONTROL_EXT_IN", out _gearAInPort);
                _simFlow.TryGetPort("gearInputB.CONTROL_EXT_IN", out _gearBInPort);
                goto case TrainCarType.LocoDH4;

            case TrainCarType.LocoDH4:
                _simFlow.TryGetPort("coolant.TEMPERATURE", out _tempPort);
                break;
            
            case TrainCarType.LocoS060:
            case TrainCarType.LocoSteamHeavy:
                _tempPort = new Port("",
                    new PortDefinition(PortType.READONLY_OUT, PortValueType.TEMPERATURE, "nocablemufake"));
                _simFlow.TryGetPort("cylinderCock.EXT_IN", out _cylCocksPort);
                _hasCylCocks = true;
                break;
            
            default:
                // TODO: LOG ERROR
                // very bad happened
                throw new ArgumentOutOfRangeException(nameof(loco.carType),
                    "Unrecognized locomotives type! Got type \"" + loco.carType + '"');
        }

        _hasDynBrake = controlsOverrider.DynamicBrake != null;
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
            if (_hasDynBrake) return controlsOverrider.DynamicBrake.Value;
            return -1;
        }
        set
        {
            if (_hasDynBrake) controlsOverrider.DynamicBrake.Set(value);
        }
    }
    
    public event Action<float> DynBrakeValueUpdated
    {
        add
        {
            if (_hasDynBrake) controlsOverrider.DynamicBrake.ControlUpdated += value;
        }
        remove
        {
            if (_hasDynBrake) controlsOverrider.DynamicBrake.ControlUpdated -= value;
        }
    }

    public float CylCocks
    {
        get
        {
            // The locomotive shouldn't lose or grow cylinder cocks so the value
            // from the constructor should be fine.
            if (_hasCylCocks) return _cylCocksPort!.Value;
            return -1;
        }
        set
        {
            if (_hasCylCocks) _cylCocksPort!.ExternalValueUpdate(value);
        }
    }
    
    public event Action<float> CylCocksUpdated
    {
        add
        {
            if (_hasCylCocks) _cylCocksPort!.ValueUpdatedInternally += value;
        }
        remove
        {
            if (_hasCylCocks) _cylCocksPort!.ValueUpdatedInternally -= value;
        }
    }

    public float GearA
    {
        get
        {
            if (_hasGearbox) return _gearAInPort!.Value;
            return -1;
        }
        set
        {
            if (_hasGearbox) _gearAInPort!.ExternalValueUpdate(value);
        }
    }
    
    public event Action<float> GearAUpdated
    {
        add
        {
            if (_hasGearbox) _gearAInPort!.ValueUpdatedInternally += value;
        }
        remove
        {
            if (_hasGearbox) _gearAInPort!.ValueUpdatedInternally -= value;
        }
    }
    
    public float GearB
    {
        get
        {
            if (_hasGearbox) return _gearBInPort!.Value;
            return -1;
        }
        set
        {
            if (_hasGearbox) _gearBInPort!.ExternalValueUpdate(value);
        }
    }
    
    public event Action<float> GearBUpdated
    {
        add
        {
            if (_hasGearbox) _gearBInPort!.ValueUpdatedInternally += value;
        }
        remove
        {
            if (_hasGearbox) _gearBInPort!.ValueUpdatedInternally -= value;
        }
    }
}