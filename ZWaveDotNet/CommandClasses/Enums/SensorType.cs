﻿// ZWaveDotNet Copyright (C) 2024 
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace ZWaveDotNet.CommandClasses.Enums
{
    public enum SensorType : byte
    {
        Undefined = 0x00,
        Temperature = 0x01,
        General = 0x02,
        Luminance = 0x03,
        Power = 0x04,
        RelativeHumidity = 0x05,
        Velocity = 0x06,
        Direction = 0x07,
        AtmosphericPressure = 0x08,
        BarometricPressure = 0x09,
        SolarRadiation = 0x0A,
        DewPoint = 0x0B,
        RainRate = 0x0C,
        TideLevel = 0x0D,
        Weight = 0x0E,
        Voltage = 0x0F,
        Current = 0x10,
        CO2 = 0x11,
        AirFlow = 0x12,
        TankCapacity = 0x13,
        Distance = 0x14,
        AnglePosition = 0x15,
        Rotation = 0x16,
        WaterTemperature = 0x17,
        SoilTemperature = 0x18,
        SeismicIntensity = 0x19,
        SeismicMagnitude = 0x1A,
        Ultraviolet = 0x1B,
        ElectricalResistivity = 0x1C,
        ElectricalConductivity = 0x1D,
        Loudness = 0x1E,
        Moisture = 0x1F,
        Frequency = 0x20,
        Time = 0x21,
        TargetTemperature = 0x22,
        ParticulateMatter25 = 0x23,
        FormaldehydeLevel = 0x24,
        RadonConcentration = 0x25,
        MethaneDensity = 0x26,
        VolatileOrganicCompoundLevel = 0x27,
        CarbonMonoxideLevel = 0x28,
        SoilHumidity = 0x29,
        SoilReactivity = 0x2A,
        SoilSalinity = 0x2B,
        HeartRate = 0x2C,
        BloodPressure = 0x2D,
        MuscleMass = 0x2E,
        FatMass = 0x2F,
        BoneMass = 0x30,
        TotalBodyWater = 0x31,
        BasalMetabolicRate = 0x32,
        BodyMassIndex = 0x33,
        AccelerationXAxis = 0x34,
        AccelerationYAxis = 0x35,
        AccelerationZAxis = 0x36,
        SmokeDensity = 0x37,
        WaterFlow = 0x38,
        WaterPressure = 0x39,
        RFSignalStrength = 0x3A,
        ParticulateMatter10 = 0x3B,
        RespiratoryRate = 0x3C,
        RelativeModulationLevel = 0x3D,
        BoilerWaterTemperature = 0x3E,
        DomesticHotWaterTemperature = 0x3F,
        OutsideTemperature = 0x40,
        ExhaustTemperature = 0x41,
        WaterChlorineLevel = 0x42,
        WaterAcidity = 0x43,
        WaterOxidationReductionPotential = 0x44,
        HeartRateLF_HFRatio = 0x45,
        MotionDirection = 0x46,
        AppliedForceOnTheSensor = 0x47,
        ReturnAirTemperature = 0x48,
        SupplyAirTemperature = 0x49,
        CondenserCoilTemperature = 0x4A,
        EvaporatorCoilTemperature = 0x4B,
        LiquidLineTemperature = 0x4C,
        DischargeLineTemperature = 0x4D,
        SuctionPressure = 0x4E,
        DischargePressure = 0x4F,
        DefrostTemperature = 0x50,
        Ozone = 0x51,
        SulfurDioxide = 0x52,
        NitrogenDioxide = 0x53,
        Ammonia = 0x54,
        Lead = 0x55,
        ParticulateMatter1 = 0x56,
        PersonCounterEntering = 0x57,
        PersonCounterExiting = 0x58
    };
}
