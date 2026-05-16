using PlanetaryFormation.SimulationCore.Behavior;

namespace PlanetaryFormation.Tests;

public class LocomotionControllerTests
{
    [Fact]
    public void ComputeTorque_ClampsToMaxMagnitude()
    {
        var controller = new LocomotionController { Kp = 100f, Kd = 10f, MaxTorque = 50f };

        float torque = controller.ComputeTorque(targetAngle: 90f, currentAngle: 0f, angularVelocity: 0f);

        Assert.Equal(50f, torque);
    }

    [Fact]
    public void FromMass_UsesFloorScaleOfPointFive()
    {
        var controller = LocomotionController.FromMass(0.2f);

        Assert.Equal(40f, controller.Kp);
        Assert.Equal(5f, controller.Kd);
        Assert.Equal(100f, controller.MaxTorque);
        Assert.Equal(25f, controller.UprightTorqueMagnitude);
    }

    [Fact]
    public void FromMass_ScalesLinearlyForLargerMass()
    {
        var controller = LocomotionController.FromMass(2f);

        Assert.Equal(160f, controller.Kp);
        Assert.Equal(20f, controller.Kd);
        Assert.Equal(400f, controller.MaxTorque);
        Assert.Equal(100f, controller.UprightTorqueMagnitude);
        Assert.Equal(0f, controller.UprightTargetDeg);
    }
}
