namespace Scenebox.Tools;

[Tool( "Remover", "Remove GameObjects", "Construction" )]
[ToolControl( "Attack1", "Remove selected object" )]
public class RemoverTool : BaseTool
{
    public override void PrimaryUseStart()
    {
        var tr = Game.ActiveScene.Trace.Ray( new Ray( Toolgun.Player.Head.Transform.Position, Toolgun.Player.Direction.Forward ), 2000 )
            .WithoutTags( "trigger" )
            .Run();

        if ( !tr.Hit ) return;
        if ( tr.GameObject.Tags.HasAny( "player", "grabbed", "map" ) ) return;

        if ( tr.Body.IsValid() )
        {
            var position = tr.Body.GetBounds().Center;
            var rotation = tr.Body.Transform.Rotation;
            var size = tr.Body.GetBounds().Size * tr.Body.Transform.Scale;
            GameManager.Instance.BroadcastDestroyObjectEffect( position, rotation, size );
        }

        Toolgun.BroadcastUseEffects( tr.HitPosition, tr.Normal );
        GameManager.Instance.BroadcastDestroyObject( tr.GameObject.Id );
    }
}