
using System;
using Scenebox.Tools;

namespace Scenebox;

public class Toolgun : Weapon
{

    [Property, Group( "Sounds" )] SoundEvent UseSound { get; set; }

    internal BaseTool CurrentTool = null;

    protected override void OnStart()
    {
        SetTool( TypeLibrary.GetType<BaseTool>( "Scenebox.Tools.RemoverTool" ) );
    }

    public override void Update()
    {
        if ( !IsEquipped ) return;

        if ( Input.Pressed( "attack1" ) ) CurrentTool?.PrimaryUseStart();
        if ( Input.Down( "attack1" ) ) CurrentTool?.PrimaryUseUpdate();
        if ( Input.Released( "attack1" ) ) CurrentTool?.PrimaryUseEnd();

        if ( Input.Pressed( "attack2" ) ) CurrentTool?.SecondaryUseStart();
        if ( Input.Down( "attack2" ) ) CurrentTool?.SecondaryUseUpdate();
        if ( Input.Released( "attack2" ) ) CurrentTool?.SecondaryUseEnd();
    }

    void PrimaryUse()
    {
        BroadcastUseEffects();
    }

    void SecondaryUse()
    {
        BroadcastUseEffects();
    }

    public void SetTool( TypeDescription toolDescription )
    {
        if ( CurrentTool != null )
        {
            CurrentTool?.OnUnequip();
            CurrentTool = null;
        }

        if ( toolDescription == null ) return;

        CurrentTool = TypeLibrary.Create<BaseTool>( toolDescription.TargetType );
        CurrentTool.Toolgun = this;
        CurrentTool?.OnEquip();
    }

    [Broadcast]
    public void BroadcastUseEffects()
    {
        var playerRenderer = Player?.Body?.Components?.Get<SkinnedModelRenderer>();
        playerRenderer?.Set( "b_attack", true );
        Player?.ViewModel?.ModelRenderer?.Set( "b_attack", true );
        var sound = Sound.Play( UseSound, Transform.Position );
        if ( Connection.Local.Id == Rpc.CallerId ) sound.ListenLocal = true;
    }

}