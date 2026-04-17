using System.Text.Json;
using FluentAssertions;
using Nexo.GameDomain.Assets;

namespace Nexo.Tests.GameDomain;

public class SoundBankTests
{
    [Fact]
    public void SoundBankDescriptor_DefaultValues()
    {
        var bank = new SoundBankDescriptor();

        bank.Id.Should().BeEmpty();
        bank.Name.Should().BeEmpty();
        bank.Category.Should().Be("weapon");
        bank.Events.Should().BeEmpty();
        bank.Tags.Should().BeEmpty();
    }

    [Fact]
    public void SoundEvent_WithMultipleAudioIds()
    {
        var evt = new SoundEvent
        {
            EventName = "fire",
            AudioDescriptorIds = new[] { "gun_fire_01", "gun_fire_02", "gun_fire_03" },
            SelectionMode = "random_no_repeat",
            CooldownSeconds = 0.05,
            MaxConcurrent = 2,
            VolumeMultiplier = 0.9,
            PitchMultiplier = 1.1
        };

        evt.EventName.Should().Be("fire");
        evt.AudioDescriptorIds.Should().HaveCount(3);
        evt.AudioDescriptorIds[0].Should().Be("gun_fire_01");
        evt.SelectionMode.Should().Be("random_no_repeat");
        evt.CooldownSeconds.Should().Be(0.05);
        evt.MaxConcurrent.Should().Be(2);
        evt.VolumeMultiplier.Should().Be(0.9);
        evt.PitchMultiplier.Should().Be(1.1);
    }

    [Fact]
    public void SoundEvent_DefaultValues()
    {
        var evt = new SoundEvent();

        evt.EventName.Should().BeEmpty();
        evt.AudioDescriptorIds.Should().BeEmpty();
        evt.SelectionMode.Should().Be("random");
        evt.CooldownSeconds.Should().Be(0.0);
        evt.MaxConcurrent.Should().Be(3);
        evt.VolumeMultiplier.Should().Be(1.0);
        evt.PitchMultiplier.Should().Be(1.0);
    }

    [Fact]
    public void SoundEvent_SelectionModes()
    {
        var random = new SoundEvent { SelectionMode = "random" };
        var sequential = new SoundEvent { SelectionMode = "sequential" };
        var noRepeat = new SoundEvent { SelectionMode = "random_no_repeat" };

        random.SelectionMode.Should().Be("random");
        sequential.SelectionMode.Should().Be("sequential");
        noRepeat.SelectionMode.Should().Be("random_no_repeat");
    }

    [Fact]
    public void SoundBankDescriptor_JsonRoundTrip()
    {
        var original = new SoundBankDescriptor
        {
            Id = "assault_rifle_bank",
            Name = "Assault Rifle Sound Bank",
            Category = "weapon",
            Events = new[]
            {
                new SoundEvent
                {
                    EventName = "fire",
                    AudioDescriptorIds = new[] { "ar_fire_01", "ar_fire_02" },
                    SelectionMode = "random_no_repeat",
                    CooldownSeconds = 0.05,
                    MaxConcurrent = 2
                },
                new SoundEvent
                {
                    EventName = "reload",
                    AudioDescriptorIds = new[] { "ar_reload" },
                    SelectionMode = "sequential",
                    MaxConcurrent = 1
                }
            },
            Tags = new[] { "weapon", "assault_rifle" }
        };

        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(original, opts);
        var restored = JsonSerializer.Deserialize<SoundBankDescriptor>(json, opts);

        restored.Should().NotBeNull();
        restored!.Id.Should().Be("assault_rifle_bank");
        restored.Events.Should().HaveCount(2);
        restored.Events[0].EventName.Should().Be("fire");
        restored.Events[0].AudioDescriptorIds.Should().HaveCount(2);
        restored.Events[1].SelectionMode.Should().Be("sequential");
        restored.Tags.Should().HaveCount(2);
    }

    [Fact]
    public void AnimationSetDescriptor_DefaultValues()
    {
        var set = new AnimationSetDescriptor();

        set.Id.Should().BeEmpty();
        set.Name.Should().BeEmpty();
        set.Category.Should().Be("character");
        set.Mappings.Should().BeEmpty();
        set.BlendTrees.Should().BeEmpty();
        set.Tags.Should().BeEmpty();
    }

    [Fact]
    public void AnimationMapping_WithCrossfade()
    {
        var mapping = new AnimationMapping
        {
            GameplayState = "run",
            AnimationDescriptorId = "anim_run_forward",
            CrossfadeDuration = 0.2
        };

        mapping.GameplayState.Should().Be("run");
        mapping.AnimationDescriptorId.Should().Be("anim_run_forward");
        mapping.CrossfadeDuration.Should().Be(0.2);
    }

    [Fact]
    public void AnimationMapping_DefaultValues()
    {
        var mapping = new AnimationMapping();

        mapping.GameplayState.Should().BeEmpty();
        mapping.AnimationDescriptorId.Should().BeEmpty();
        mapping.CrossfadeDuration.Should().Be(0.15);
    }

    [Fact]
    public void BlendTree_1D_Configuration()
    {
        var tree = new BlendTree
        {
            Name = "Locomotion",
            BlendParameter = "Speed",
            BlendType = "1D",
            Children = new[]
            {
                new BlendTreeChild { AnimationDescriptorId = "anim_idle", Threshold = 0.0 },
                new BlendTreeChild { AnimationDescriptorId = "anim_walk", Threshold = 0.5 },
                new BlendTreeChild { AnimationDescriptorId = "anim_run", Threshold = 1.0, TimeScale = 1.2 }
            }
        };

        tree.Name.Should().Be("Locomotion");
        tree.BlendParameter.Should().Be("Speed");
        tree.BlendParameterY.Should().BeNull();
        tree.BlendType.Should().Be("1D");
        tree.Children.Should().HaveCount(3);
        tree.Children[0].Threshold.Should().Be(0.0);
        tree.Children[2].Threshold.Should().Be(1.0);
        tree.Children[2].TimeScale.Should().Be(1.2);
    }

    [Fact]
    public void BlendTree_2D_Configuration()
    {
        var tree = new BlendTree
        {
            Name = "DirectionalMovement",
            BlendParameter = "SpeedX",
            BlendParameterY = "SpeedY",
            BlendType = "2DFreeformDirectional",
            Children = new[]
            {
                new BlendTreeChild { AnimationDescriptorId = "anim_idle", Threshold = 0.0, ThresholdY = 0.0 },
                new BlendTreeChild { AnimationDescriptorId = "anim_forward", Threshold = 0.0, ThresholdY = 1.0 },
                new BlendTreeChild { AnimationDescriptorId = "anim_strafe_left", Threshold = -1.0, ThresholdY = 0.0 },
                new BlendTreeChild { AnimationDescriptorId = "anim_strafe_right", Threshold = 1.0, ThresholdY = 0.0 }
            }
        };

        tree.BlendParameterY.Should().Be("SpeedY");
        tree.BlendType.Should().Be("2DFreeformDirectional");
        tree.Children.Should().HaveCount(4);
        tree.Children[2].Threshold.Should().Be(-1.0);
        tree.Children[2].ThresholdY.Should().Be(0.0);
    }

    [Fact]
    public void BlendTree_DefaultValues()
    {
        var tree = new BlendTree();

        tree.Name.Should().BeEmpty();
        tree.BlendParameter.Should().BeEmpty();
        tree.BlendParameterY.Should().BeNull();
        tree.BlendType.Should().Be("1D");
        tree.Children.Should().BeEmpty();
    }

    [Fact]
    public void BlendTreeChild_DefaultValues()
    {
        var child = new BlendTreeChild();

        child.AnimationDescriptorId.Should().BeEmpty();
        child.Threshold.Should().Be(0);
        child.ThresholdY.Should().Be(0);
        child.TimeScale.Should().Be(1.0);
    }

    [Fact]
    public void AnimationSetDescriptor_JsonRoundTrip()
    {
        var original = new AnimationSetDescriptor
        {
            Id = "fps-char-set",
            Name = "FPS Character Animation Set",
            Category = "character",
            Mappings = new[]
            {
                new AnimationMapping { GameplayState = "idle", AnimationDescriptorId = "anim_idle", CrossfadeDuration = 0.1 },
                new AnimationMapping { GameplayState = "walk", AnimationDescriptorId = "anim_walk" },
                new AnimationMapping { GameplayState = "run", AnimationDescriptorId = "anim_run", CrossfadeDuration = 0.2 },
                new AnimationMapping { GameplayState = "jump", AnimationDescriptorId = "anim_jump", CrossfadeDuration = 0.05 }
            },
            BlendTrees = new[]
            {
                new BlendTree
                {
                    Name = "Locomotion",
                    BlendParameter = "Speed",
                    BlendType = "1D",
                    Children = new[]
                    {
                        new BlendTreeChild { AnimationDescriptorId = "anim_idle", Threshold = 0.0 },
                        new BlendTreeChild { AnimationDescriptorId = "anim_walk", Threshold = 0.5 },
                        new BlendTreeChild { AnimationDescriptorId = "anim_run", Threshold = 1.0 }
                    }
                }
            },
            Tags = new[] { "fps", "locomotion" }
        };

        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(original, opts);
        var restored = JsonSerializer.Deserialize<AnimationSetDescriptor>(json, opts);

        restored.Should().NotBeNull();
        restored!.Id.Should().Be("fps-char-set");
        restored.Mappings.Should().HaveCount(4);
        restored.Mappings[0].GameplayState.Should().Be("idle");
        restored.Mappings[2].CrossfadeDuration.Should().Be(0.2);
        restored.BlendTrees.Should().HaveCount(1);
        restored.BlendTrees[0].Children.Should().HaveCount(3);
        restored.Tags.Should().HaveCount(2);
    }
}
