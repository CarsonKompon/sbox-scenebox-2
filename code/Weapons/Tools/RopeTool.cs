using System;

namespace Scenebox.Tools;

[Tool( "Rope", "Rope things together", "Constraints" )]
[Description( "A rope created a fixed constraint between objects." )]
public class RopeTool : BaseTool
{
    [Property, Range( 0, 50000, 1 )]
    [Title( "Force Limit" ), Description( "The constraint will break if the amount of force is greater than this. Set to 0 to not break. Unfortunately due to the way the physgun works, moving objects with a value greater than 0 will instantly break the joint." )]
    public float ForceLimit { get; set; } = 0;

    [Property, Title( "No Collide" )]
    public bool NoCollide { get; set; } = false;

    public override string Attack1Control => SelectedObject.IsValid() ? "Attach the object with a Rope constraint" : "Select an object to begin a Rope constraint";

    GameObject SelectedObject = null;
    int SelectedBodyIndex = 0;

    public override void OnEquip()
    {
        base.OnEquip();

        SelectedObject = null;
    }

    public override void PrimaryUseStart()
    {
        var tr = Game.ActiveScene.Trace.Ray( new Ray( Toolgun.Player.Head.Transform.Position, Toolgun.Player.Direction.Forward ), 2000 )
            .WithoutTags( "trigger" )
            .Run();

        if ( !tr.Hit ) return;
        if ( tr.GameObject.Tags.HasAny( "player", "grabbed", "map" ) ) return;
        if ( !tr.Body.IsValid() ) return;

        Toolgun.BroadcastUseEffects( tr.HitPosition, tr.Normal );

        if ( SelectedObject.IsValid() )
        {
            CompleteWeld( tr.Body, tr.GameObject );
            SelectedObject = null;
            return;
        }

        SelectedObject = tr.GameObject;
        SelectedBodyIndex = tr.Body.GroupIndex;
    }

    void CompleteWeld( PhysicsBody body, GameObject obj )
    {
        if ( SelectedObject.Tags.Has( "grabbed" ) || obj.Tags.Has( "grabbed" ) ) return;

        SelectedObject.Network.TakeOwnership();

        if ( SelectedBodyIndex >= 0 )
        {
            var renderer = SelectedObject.Root.Components.Get<SkinnedModelRenderer>();
            if ( renderer.IsValid() )
            {
                renderer.CreateBoneObjects = true;
                SelectedObject = renderer.GetBoneObject( SelectedBodyIndex );
            }
        }

        if ( body.GroupIndex >= 0 )
        {
            obj.Network.TakeOwnership();
            var renderer = obj.Root.Components.Get<SkinnedModelRenderer>();
            if ( renderer.IsValid() )
            {
                renderer.CreateBoneObjects = true;
                obj.Network.Refresh();
                obj = renderer.GetBoneObject( body.GroupIndex );
            }
        }

        var weld = SelectedObject.Components.Create<SpringJoint>();
        var length = (SelectedObject.Transform.Position - obj.Transform.Position).Length;
        weld.Body = obj;
        weld.MinLength = length;
        weld.MaxLength = length;
        weld.BreakForce = ForceLimit;
        weld.EnableCollision = !NoCollide;

        LegacyParticleSystem part = SelectedObject.Components.Create<LegacyParticleSystem>();
        part.Particles = ParticleSystem.Load( "particles/entity/rope.vpcf" );
        part.ControlPoints = new()
        {
            new ParticleControlPoint() { StringCP = "0", GameObjectValue = SelectedObject },
            new ParticleControlPoint() { StringCP = "1", GameObjectValue = obj }
        };

        var rootObject = SelectedObject.Root;
        rootObject.Network.Refresh();

        UndoManager.Instance.Add( "Undone Rope", new List<Guid>() { SelectedObject.Id, obj.Id }, () =>
        {
            if ( weld.IsValid() )
            {
                weld.Destroy();
                rootObject.Network.Refresh();
            }
        } );
    }
}
