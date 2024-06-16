
using System;

namespace Scenebox;

public class Toolgun : Weapon
{

    [Property, Group( "Sounds" )] SoundEvent UseSound { get; set; }

    public override void Update()
    {
        if ( !IsEquipped ) return;

        if ( Input.Pressed( "attack1" ) )
        {
            PrimaryUse();
        }
        else if ( Input.Pressed( "attack2" ) )
        {
            SecondaryUse();
        }
    }

    void PrimaryUse()
    {
        BroadcastUseEffects();
    }

    void SecondaryUse()
    {
        BroadcastUseEffects();
    }

    [Broadcast]
    void BroadcastUseEffects()
    {
        var playerRenderer = Player?.Body?.Components?.Get<SkinnedModelRenderer>();
        playerRenderer?.Set( "b_attack", true );
        Player?.ViewModel?.ModelRenderer?.Set( "b_attack", true );
        var sound = Sound.Play( UseSound, Transform.Position );
        if ( Connection.Local.Id == Rpc.CallerId ) sound.ListenLocal = true;
    }

}