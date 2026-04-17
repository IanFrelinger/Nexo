using System.Text.Json;
using FluentAssertions;
using Nexo.GameDomain.Assets;

namespace Nexo.Tests.GameDomain;

public class AnimationDescriptorTests
{
    [Fact]
    public void AnimationDescriptor_DefaultValues()
    {
        var anim = new AnimationDescriptor();

        anim.Id.Should().BeEmpty();
        anim.Name.Should().BeEmpty();
        anim.Category.Should().Be("character");
        anim.DefaultState.Should().Be("Idle");
        anim.States.Should().BeEmpty();
        anim.Transitions.Should().BeEmpty();
        anim.Parameters.Should().BeEmpty();
        anim.Layers.Should().BeEmpty();
        anim.Tags.Should().BeEmpty();
    }

    [Fact]
    public void AnimationState_WithEvents()
    {
        var state = new AnimationState
        {
            Name = "Attack",
            ClipName = "attack_01",
            Speed = 1.2,
            Loop = false,
            Mirror = false,
            SpeedParameterName = "AttackSpeed",
            Events = new[]
            {
                new AnimationEvent
                {
                    Time = 0.3,
                    FunctionName = "OnHitFrame",
                    StringParameter = "slash",
                    FloatParameter = 25.0,
                    IntParameter = 1
                },
                new AnimationEvent
                {
                    Time = 0.8,
                    FunctionName = "OnAttackEnd"
                }
            },
            Position = new Vector2Descriptor(100, 200)
        };

        state.Name.Should().Be("Attack");
        state.ClipName.Should().Be("attack_01");
        state.Speed.Should().Be(1.2);
        state.SpeedParameterName.Should().Be("AttackSpeed");
        state.Events.Should().HaveCount(2);
        state.Events[0].Time.Should().Be(0.3);
        state.Events[0].FunctionName.Should().Be("OnHitFrame");
        state.Events[0].StringParameter.Should().Be("slash");
        state.Events[0].FloatParameter.Should().Be(25.0);
        state.Events[0].IntParameter.Should().Be(1);
        state.Events[1].Time.Should().Be(0.8);
        state.Events[1].StringParameter.Should().BeNull();
        state.Position.X.Should().Be(100);
        state.Position.Y.Should().Be(200);
    }

    [Fact]
    public void AnimationTransition_WithConditions()
    {
        var transition = new AnimationTransition
        {
            FromState = "Idle",
            ToState = "Run",
            TransitionDuration = 0.15,
            ExitTime = 0.9,
            HasExitTime = true,
            HasFixedDuration = true,
            Conditions = new[]
            {
                new TransitionCondition
                {
                    ParameterName = "Speed",
                    Mode = "greater",
                    Threshold = 0.5
                },
                new TransitionCondition
                {
                    ParameterName = "IsGrounded",
                    Mode = "if_true",
                    Threshold = 0
                }
            }
        };

        transition.FromState.Should().Be("Idle");
        transition.ToState.Should().Be("Run");
        transition.TransitionDuration.Should().Be(0.15);
        transition.ExitTime.Should().Be(0.9);
        transition.HasExitTime.Should().BeTrue();
        transition.Conditions.Should().HaveCount(2);
        transition.Conditions[0].ParameterName.Should().Be("Speed");
        transition.Conditions[0].Mode.Should().Be("greater");
        transition.Conditions[0].Threshold.Should().Be(0.5);
        transition.Conditions[1].Mode.Should().Be("if_true");
    }

    [Fact]
    public void AnimationTransition_DefaultValues()
    {
        var transition = new AnimationTransition();

        transition.FromState.Should().BeEmpty();
        transition.ToState.Should().BeEmpty();
        transition.TransitionDuration.Should().Be(0.25);
        transition.ExitTime.Should().Be(1.0);
        transition.HasExitTime.Should().BeTrue();
        transition.HasFixedDuration.Should().BeTrue();
        transition.Conditions.Should().BeEmpty();
    }

    [Fact]
    public void AnimationParameter_Types()
    {
        var floatParam = new AnimationParameter { Name = "Speed", Type = "float", DefaultValue = 0 };
        var intParam = new AnimationParameter { Name = "WeaponIndex", Type = "int", DefaultValue = 0 };
        var boolParam = new AnimationParameter { Name = "IsGrounded", Type = "bool", DefaultValue = 1 };
        var triggerParam = new AnimationParameter { Name = "Jump", Type = "trigger", DefaultValue = 0 };

        floatParam.Type.Should().Be("float");
        intParam.Type.Should().Be("int");
        boolParam.Type.Should().Be("bool");
        triggerParam.Type.Should().Be("trigger");
    }

    [Fact]
    public void AnimationParameter_DefaultValues()
    {
        var param = new AnimationParameter();

        param.Name.Should().BeEmpty();
        param.Type.Should().Be("float");
        param.DefaultValue.Should().Be(0);
    }

    [Fact]
    public void AnimationLayer_WithAvatarMask()
    {
        var layer = new AnimationLayer
        {
            Name = "Upper Body",
            Weight = 1.0,
            BlendingMode = "override",
            AvatarMask = "UpperBody",
            IKPass = true
        };

        layer.Name.Should().Be("Upper Body");
        layer.Weight.Should().Be(1.0);
        layer.BlendingMode.Should().Be("override");
        layer.AvatarMask.Should().Be("UpperBody");
        layer.IKPass.Should().BeTrue();
    }

    [Fact]
    public void AnimationLayer_DefaultValues()
    {
        var layer = new AnimationLayer();

        layer.Name.Should().Be("Base Layer");
        layer.Weight.Should().Be(1.0);
        layer.BlendingMode.Should().Be("override");
        layer.AvatarMask.Should().BeNull();
        layer.IKPass.Should().BeFalse();
    }

    [Fact]
    public void AnimationEvent_DefaultValues()
    {
        var evt = new AnimationEvent();

        evt.Time.Should().Be(0);
        evt.FunctionName.Should().BeEmpty();
        evt.StringParameter.Should().BeNull();
        evt.FloatParameter.Should().Be(0);
        evt.IntParameter.Should().Be(0);
    }

    [Fact]
    public void TransitionCondition_DefaultValues()
    {
        var cond = new TransitionCondition();

        cond.ParameterName.Should().BeEmpty();
        cond.Mode.Should().Be("equals");
        cond.Threshold.Should().Be(0);
    }

    [Fact]
    public void AnimationState_DefaultValues()
    {
        var state = new AnimationState();

        state.Name.Should().BeEmpty();
        state.ClipName.Should().BeEmpty();
        state.Speed.Should().Be(1.0);
        state.Loop.Should().BeFalse();
        state.Mirror.Should().BeFalse();
        state.SpeedParameterName.Should().BeNull();
        state.Events.Should().BeEmpty();
        state.Position.Should().Be(new Vector2Descriptor());
    }

    [Fact]
    public void FullStateMachine_JsonRoundTrip()
    {
        var original = new AnimationDescriptor
        {
            Id = "fps-character",
            Name = "FPS Character Animator",
            Category = "character",
            DefaultState = "Idle",
            States = new[]
            {
                new AnimationState
                {
                    Name = "Idle",
                    ClipName = "idle_breathing",
                    Speed = 1.0,
                    Loop = true,
                    Position = new Vector2Descriptor(0, 0),
                    Events = new[]
                    {
                        new AnimationEvent { Time = 0.5, FunctionName = "OnBreathCycle" }
                    }
                },
                new AnimationState
                {
                    Name = "Walk",
                    ClipName = "walk_forward",
                    Speed = 1.0,
                    Loop = true,
                    SpeedParameterName = "MoveSpeed",
                    Position = new Vector2Descriptor(200, 0),
                    Events = new[]
                    {
                        new AnimationEvent { Time = 0.25, FunctionName = "OnFootstep", StringParameter = "left" },
                        new AnimationEvent { Time = 0.75, FunctionName = "OnFootstep", StringParameter = "right" }
                    }
                },
                new AnimationState
                {
                    Name = "Run",
                    ClipName = "run_forward",
                    Speed = 1.0,
                    Loop = true,
                    SpeedParameterName = "MoveSpeed",
                    Position = new Vector2Descriptor(400, 0)
                },
                new AnimationState
                {
                    Name = "Jump",
                    ClipName = "jump_up",
                    Speed = 1.0,
                    Loop = false,
                    Position = new Vector2Descriptor(200, -200)
                }
            },
            Transitions = new[]
            {
                new AnimationTransition
                {
                    FromState = "Idle",
                    ToState = "Walk",
                    TransitionDuration = 0.2,
                    HasExitTime = false,
                    Conditions = new[]
                    {
                        new TransitionCondition { ParameterName = "Speed", Mode = "greater", Threshold = 0.1 }
                    }
                },
                new AnimationTransition
                {
                    FromState = "Walk",
                    ToState = "Run",
                    TransitionDuration = 0.15,
                    HasExitTime = false,
                    Conditions = new[]
                    {
                        new TransitionCondition { ParameterName = "Speed", Mode = "greater", Threshold = 0.6 }
                    }
                },
                new AnimationTransition
                {
                    FromState = "Any",
                    ToState = "Jump",
                    TransitionDuration = 0.1,
                    HasExitTime = false,
                    Conditions = new[]
                    {
                        new TransitionCondition { ParameterName = "Jump", Mode = "if_true", Threshold = 0 }
                    }
                }
            },
            Parameters = new[]
            {
                new AnimationParameter { Name = "Speed", Type = "float", DefaultValue = 0 },
                new AnimationParameter { Name = "IsGrounded", Type = "bool", DefaultValue = 1 },
                new AnimationParameter { Name = "Jump", Type = "trigger", DefaultValue = 0 },
                new AnimationParameter { Name = "MoveSpeed", Type = "float", DefaultValue = 1.0 }
            },
            Layers = new[]
            {
                new AnimationLayer
                {
                    Name = "Base Layer",
                    Weight = 1.0,
                    BlendingMode = "override"
                },
                new AnimationLayer
                {
                    Name = "Upper Body",
                    Weight = 0.8,
                    BlendingMode = "override",
                    AvatarMask = "UpperBody",
                    IKPass = true
                }
            },
            Tags = new[] { "fps", "character", "humanoid" }
        };

        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(original, opts);
        var restored = JsonSerializer.Deserialize<AnimationDescriptor>(json, opts);

        restored.Should().NotBeNull();
        restored!.Id.Should().Be("fps-character");
        restored.Name.Should().Be("FPS Character Animator");
        restored.Category.Should().Be("character");
        restored.DefaultState.Should().Be("Idle");

        restored.States.Should().HaveCount(4);
        restored.States[0].Name.Should().Be("Idle");
        restored.States[0].Loop.Should().BeTrue();
        restored.States[0].Events.Should().HaveCount(1);
        restored.States[1].Name.Should().Be("Walk");
        restored.States[1].SpeedParameterName.Should().Be("MoveSpeed");
        restored.States[1].Events.Should().HaveCount(2);
        restored.States[1].Events[0].StringParameter.Should().Be("left");
        restored.States[3].Name.Should().Be("Jump");
        restored.States[3].Loop.Should().BeFalse();

        restored.Transitions.Should().HaveCount(3);
        restored.Transitions[0].FromState.Should().Be("Idle");
        restored.Transitions[0].ToState.Should().Be("Walk");
        restored.Transitions[0].Conditions.Should().HaveCount(1);
        restored.Transitions[2].FromState.Should().Be("Any");

        restored.Parameters.Should().HaveCount(4);
        restored.Parameters[0].Type.Should().Be("float");
        restored.Parameters[1].Type.Should().Be("bool");
        restored.Parameters[2].Type.Should().Be("trigger");

        restored.Layers.Should().HaveCount(2);
        restored.Layers[0].AvatarMask.Should().BeNull();
        restored.Layers[1].AvatarMask.Should().Be("UpperBody");
        restored.Layers[1].IKPass.Should().BeTrue();

        restored.Tags.Should().HaveCount(3);
        restored.Tags.Should().Contain("fps");
    }
}
