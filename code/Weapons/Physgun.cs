
namespace Scenebox;

public class Physgun : Weapon
{
    [Property] LegacyParticleSystem BeamParticles { get; set; }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        if ( !IsEquipped ) return;

        if ( Input.Down( "Attack1" ) )
        {
            BeamParticles.Enabled = true;

            UpdateBeam();
        }
        else
        {
            BeamParticles.Enabled = false;
        }
    }

    void UpdateBeam()
    {
        var tr = Scene.Trace.Ray( new Ray( Player.Head.Transform.Position, Player.Direction.Forward ), 800 )
            .IgnoreGameObjectHierarchy( GameObject.Root )
            .WithoutTags( "player", "trigger" )
            .Run();

        var endPos = tr.Hit ? tr.HitPosition : tr.EndPosition;

        if ( tr.Hit )
        {

        }

        var viewModel = Player.ViewModel;
        var fcp = BeamParticles.ControlPoints[0];
        if ( Player.IsFirstPerson && viewModel.IsValid() )
        {
            fcp.VectorValue = viewModel.Muzzle.Transform.Position;
            Log.Info( "true!!" );
        }
        else
        {
            fcp.VectorValue = Muzzle.Transform.Position;
        }
        BeamParticles.ControlPoints[0] = fcp;

        var cp = BeamParticles.ControlPoints[1];
        cp.VectorValue = endPos;
        BeamParticles.ControlPoints[1] = cp;
    }
}