
using System;

namespace Scenebox;

public class TraceWeapon : Weapon
{
    [Property] public float Cooldown { get; set; } = 0.3f;
    [Property] public float Range { get; set; } = 2000f;

    [Property, Group( "Sounds" )] SoundEvent ShootSound { get; set; }

    TimeSince timeSinceLastAttack = 10f;

    public override void Update()
    {
        if ( !IsEquipped ) return;

        if ( Input.Down( "Attack1" ) )
        {
            TryAttack();
        }
    }

    void TryAttack()
    {
        if ( timeSinceLastAttack < Cooldown ) return;

        var tr = Scene.Trace.Ray( new Ray( Player.Head.Transform.Position, Player.Direction.Forward ), Range )
            .IgnoreGameObjectHierarchy( GameObject.Root )
            .WithoutTags( "trigger" )
            .Run();

        Attack( tr );
        BroadcastBulletTrail( tr.StartPosition, tr.EndPosition, tr.Distance, 0 );
        BroadcastAttackAnimation();

        timeSinceLastAttack = 0f;
    }

    [Broadcast]
    void BroadcastAttackAnimation()
    {
        var playerRenderer = Player?.Body?.Components?.Get<SkinnedModelRenderer>();
        playerRenderer?.Set( "b_attack", true );
        Player?.ViewModel?.ModelRenderer?.Set( "b_attack", true );
        Sound.Play( ShootSound, Transform.Position );
    }
}