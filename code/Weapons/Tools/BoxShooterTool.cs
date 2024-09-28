namespace Scenebox.Tools;

[Tool( "Box Shooter", "Shoots Boxes", "Fun" )]
public class BoxShooterTool : BaseTool
{
    public override string Attack1Control => "Shoot a single Box";
    public override string Attack2Control => "Shoot a ton of Boxes";
    public override string ReloadControl => "Change Model to Shoot";

    [Property] Model Model { get; set; } = Cloud.Model( "facepunch.wooden_crate" );

    TimeSince TimeSinceShoot = 0;

    public override void PrimaryUseStart()
    {
        SpawnBox();
    }

    public override void SecondaryUseUpdate()
    {
        if ( TimeSinceShoot > 1f / 15f )
        {
            SpawnBox();
            TimeSinceShoot = 0;
        }
    }

    public override void ReloadUseStart()
    {
        var tr = Game.ActiveScene.Trace.Ray( new Ray( Toolgun.Player.Head.Transform.Position, Toolgun.Player.Direction.Forward ), 2000 )
            .WithoutTags( "trigger" )
            .Run();

        if ( !tr.Hit ) return;
        if ( tr.GameObject.Tags.HasAny( "player", "grabbed", "map" ) ) return;

        if ( tr.GameObject.Components.TryGet<ModelRenderer>( out var mr ) )
        {
            Model = mr.Model;
        }

        Sound.Play( "ui.drag.stop" );
    }

    void SpawnBox()
    {
        var dir = Toolgun.Player.Direction;
        var pos = Toolgun.Player.Head.Transform.Position + dir.Forward * 50;

        var obj = GameManager.Instance.SpawnModel( Model, pos, dir );
        var Rigidbody = obj.Components.Get<Rigidbody>();
        Rigidbody.Velocity = dir.Forward * 1000;
    }

}