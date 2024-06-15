
using System;

namespace Scenebox;

public class MeleeWeapon : Weapon
{
    [Property] public float Cooldown { get; set; } = 1f;
    [Property] public float Damage { get; set; } = 20f;
    [Property] public float Range { get; set; } = 100f;

    TimeSince timeSinceLastAttack = 10f;

    public override void Update()
    {
        if ( !IsEquipped ) return;

        if ( Input.Down( "Attack1" ) )
        {
            Attack();
        }
    }

    void Attack()
    {
        if ( timeSinceLastAttack < Cooldown ) return;

        var tr = Scene.Trace.Ray( new Ray( Player.Head.Transform.Position, Player.Direction.Forward ), Range )
            .IgnoreGameObjectHierarchy( GameObject.Root )
            .WithoutTags( "trigger" )
            .Radius( 1f )
            .Run();

        if ( tr.Hit )
        {
            Sound.Play( tr.Surface.Sounds.ImpactHard, tr.HitPosition );
            string decal = "";
            var decals = tr.Surface.ImpactEffects.BulletDecal;
            if ( (decals?.Count() ?? 0) > 0 )
                decal = decals.OrderBy( x => Random.Shared.Float() ).FirstOrDefault();

            if ( tr.GameObject?.Components?.TryGet<PropHelper>( out var propHelper ) ?? false )
            {
                propHelper.Damage( Damage );
            }

            if ( tr.GameObject?.Root?.Components?.TryGet<Player>( out var player ) ?? false )
            {
                player.Damage( Damage );
            }

            GameManager.Instance.SpawnDecal( decal, tr.HitPosition, tr.Normal, tr.GameObject?.Id ?? Guid.Empty );
        }

        BroadcastAttackAnimation();

        timeSinceLastAttack = 0f;
    }

    [Broadcast]
    void BroadcastAttackAnimation()
    {
        var playerRenderer = Player?.Body?.Components?.Get<SkinnedModelRenderer>();
        playerRenderer?.Set( "b_attack", true );
        Player?.ViewModel?.ModelRenderer?.Set( "b_attack", true );
    }
}