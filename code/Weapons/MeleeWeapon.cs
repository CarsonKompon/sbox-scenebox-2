
using System;

namespace Scenebox;

public class MeleeWeapon : Weapon
{
    [Property] public float Cooldown { get; set; } = 1f;

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

        var playerRenderer = Player?.Body?.Components?.Get<SkinnedModelRenderer>();
        playerRenderer?.Set( "b_attack", true );
        Player?.ViewModel?.ModelRenderer?.Set( "b_attack", true );

        timeSinceLastAttack = 0f;
    }
}