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

        Toolgun.BroadcastUseEffects();

        if ( !tr.Hit ) return;
        if ( tr.GameObject.Tags.HasAny( "player", "grabbed", "map" ) ) return;

        GameManager.Instance.BroadcastDestroyObject( tr.GameObject.Id );
    }
}