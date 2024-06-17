namespace Scenebox;

public class Camera : Weapon, ICameraOverride
{
    public bool IsActive => IsEquipped;

    public float RollOffset { get; set; } = 0f;
    public float FieldOfView { get; set; } = 100f;

    public override void Update()
    {
        if ( !IsActive ) return;
        if ( !Player.IsFirstPerson ) Player.IsFirstPerson = true;

        if ( Input.Pressed( "reload" ) )
        {
            ResetView();
        }

        bool canMove = true;

        if ( Input.Down( "attack2" ) )
        {
            FieldOfView += Input.MouseDelta.y * 0.1f;
            FieldOfView = FieldOfView.Clamp( 1f, 140f );
            RollOffset += Input.MouseDelta.x * 0.01f;
            RollOffset = RollOffset.Clamp( -180f, 180f );
            canMove = false;
        }

        var eyeAngles = Player.Head.Transform.Rotation.Angles();
        var sens = Preferences.Sensitivity * FieldOfView / 200f;
        if ( canMove )
        {
            eyeAngles.pitch += Input.MouseDelta.y * sens / 100f;
            eyeAngles.yaw -= Input.MouseDelta.x * sens / 100f;
        }
        eyeAngles.roll = 0f;
        eyeAngles.pitch = eyeAngles.pitch.Clamp( -89.9f, 89.9f );
        Player.Head.Transform.Rotation = eyeAngles;

        Scene.Camera.Transform.Position = Player.Head.Transform.Position;
        Scene.Camera.Transform.Rotation = eyeAngles + new Angles( 0, 0, RollOffset );
        Scene.Camera.FieldOfView = FieldOfView;
        Player.Direction = eyeAngles;
    }

    void ResetView()
    {
        RollOffset = 0f;
        FieldOfView = 100f;
    }
}