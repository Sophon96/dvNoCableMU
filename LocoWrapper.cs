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

    /*// Common to all locomotives
    private readonly Port _brakePort,
        _engineOnPort,
        _fuelConsumptionPort,
        _maxRpmPort, // TODO: remove?
        _rpmPort,
        _rpmNormPort, // TODO: remove?
        _fuelNormPort,
        _fuelCapPort,
        _indBrakePort,
        _oilNormPort,
        _oilCapPort,
        _reverserPort,
        _sandNormPort,
        _sandCapPort,
        _sanderPort,
        _throttlePort;*/

    // technically common to all locomotives, but hydraulic and electric have different impl
    private readonly Port _tempPort;

    /*// Electric stuff
    private readonly Port? _tmAmpsPort,
        _tmAmpsNormPort,
        _tmMaxAmpsPort,
        _tmRpmPort,
        _tmRpmNormPort,
        _tmFusePort,
        _tmStatePort;

    // DE6 and DH4 only
    private readonly Port? _dynBrakePort;

    // Hydro stuff
    private readonly Port? _fcRpmPort,
        _fcRpmNormPort,
        _fcBrokenPort,
        _fcActiveConfigPort, // TODO: remove?
        _fcEfficiencyPort; // TODO: remove?*/


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

        /*_simFlow.TryGetPort("brake.EXT_IN", out _brakePort);
        _simFlow.TryGetPort("de.ENGINE_ON", out _engineOnPort);
        _simFlow.TryGetPort("de.FUEL_CONSUMPTION_NORMALIZED", out _fuelConsumptionPort);
        _simFlow.TryGetPort("de.MAX_RPM", out _maxRpmPort);
        _simFlow.TryGetPort("de.RPM", out _rpmPort);
        _simFlow.TryGetPort("de.RPM_NORMALIZED", out _rpmNormPort);
        _simFlow.TryGetPort("fuel.NORMALIZED", out _fuelNormPort);
        _simFlow.TryGetPort("fuel.CAPACITY", out _fuelCapPort);
        _simFlow.TryGetPort("indBrake.EXT_IN", out _indBrakePort);
        _simFlow.TryGetPort("oil.NORMALIZED", out _oilNormPort);
        _simFlow.TryGetPort("oil.CAPACITY", out _oilCapPort);
        _simFlow.TryGetPort("reverser.CONTROL_EXT_IN", out _reverserPort);
        _simFlow.TryGetPort("sand.NORMALIZED", out _sandNormPort);
        _simFlow.TryGetPort("sand.CAPACITY", out _sandCapPort);
        _simFlow.TryGetPort("sander.CONTROL_EXT_IN", out _sanderPort);
        _simFlow.TryGetPort("throttle.EXT_IN", out _throttlePort);*/

        switch (Type)
        {
            case TrainCarType.LocoDiesel:
                /*_simFlow.TryGetPort("dynamicBrake.EXT_IN", out _dynBrakePort);*/
                goto case TrainCarType.LocoShunter;

            case TrainCarType.LocoShunter:
                _simFlow.TryGetPort("tmHeat.TEMPERATURE", out _tempPort);

                /*_simFlow.TryGetPort("tm.AMPS", out _tmAmpsPort);
                _simFlow.TryGetPort("tm.AMPS_NORMALIZED", out _tmAmpsNormPort);
                _simFlow.TryGetPort("tm.MAX_AMPS", out _tmMaxAmpsPort);
                _simFlow.TryGetPort("tm.RPM", out _tmRpmPort);
                _simFlow.TryGetPort("tm.RPM_NORMALIZED", out _tmRpmNormPort);
                _simFlow.TryGetPort("tm.OVERHEAT_POWER_FUSE_OFF", out _tmFusePort);
                _simFlow.TryGetPort("tm.TMS_STATE", out _tmStatePort);*/
                break;

            case TrainCarType.LocoDH4:
                _simFlow.TryGetPort("coolant.TEMPERATURE", out _tempPort);
                /*_simFlow.TryGetPort("hydroDynamicBrake.EXT_IN", out _dynBrakePort);

                _simFlow.TryGetPort("fluidCoupler.TURBINE_RPM", out _fcRpmPort);
                _simFlow.TryGetPort("fluidCoupler.TURBINE_RPM_NORMALIZED", out _fcRpmNormPort);
                _simFlow.TryGetPort("fluidCoupler.IS_BROKEN", out _fcBrokenPort);
                _simFlow.TryGetPort("fluidCoupler.ACTIVE_CONFIGURATION", out _fcActiveConfigPort);
                _simFlow.TryGetPort("fluidCoupler.EFFICIENCY", out _fcEfficiencyPort);*/
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

    /*public float EngineOn
    {
        get => _engineOnPort.Value;
        set => _engineOnPort.Value = value;
    }

    public float FuelConsumption => _fuelConsumptionPort.Value;

    public float MaxRpm => _maxRpmPort.Value;

    public float Rpm => _rpmPort.Value;

    public float RpmNorm => _rpmNormPort.Value;

    public float FuelNorm => _fuelNormPort.Value;

    public float FuelCap => _fuelCapPort.Value;*/

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

    /*public float OilNorm => _oilNormPort.Value;

    public float OilCap => _oilCapPort.Value;*/

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

    /*public float SandNorm => _sandNormPort.Value;

    public float SandCap => _sandCapPort.Value;*/

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

    /*public float TmAmps
    {
        get
        {
            if (_tmAmpsPort != null) return _tmAmpsPort.Value;
            return -1;
        }
    }

    public float TmAmpsNorm
    {
        get
        {
            if (_tmAmpsNormPort != null) return _tmAmpsNormPort.Value;
            return -1;
        }
    }

    public float TmMaxAmps
    {
        get
        {
            if (_tmMaxAmpsPort != null) return _tmMaxAmpsPort.Value;
            return -1;
        }
    }

    public float TmRpm
    {
        get
        {
            if (_tmRpmPort != null) return _tmRpmPort.Value;
            return -1;
        }
    }

    public float TmRpmNorm
    {
        get
        {
            if (_tmRpmNormPort != null) return _tmRpmNormPort.Value;
            return -1;
        }
    }

    public float TmFuse
    {
        get
        {
            if (_tmFusePort != null) return _tmFusePort.Value;
            return -1;
        }
    }

    public float TmState
    {
        get
        {
            if (_tmStatePort != null) return _tmStatePort.Value;
            return -1;
        }
    }*/

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

    /*public float FcRpm
    {
        get
        {
            if (_fcRpmPort != null) return _fcRpmPort.Value;
            return -1;
        }
    }

    public float FcRpmNorm
    {
        get
        {
            if (_fcRpmNormPort != null) return _fcRpmNormPort.Value;
            return -1;
        }
    }

    public float FcBroken
    {
        get
        {
            if (_fcBrokenPort != null) return _fcBrokenPort.Value;
            return -1;
        }
    }

    public float FcActiveConfig
    {
        get
        {
            if (_fcActiveConfigPort != null) return _fcActiveConfigPort.Value;
            return -1;
        }
    }

    public float FcEfficiency
    {
        get
        {
            if (_fcEfficiencyPort != null) return _fcEfficiencyPort.Value;
            return -1;
        }
    }*/
}