namespace Scenebox;

public sealed class PropHelper : Component, Component.ICollisionListener
{
    [RequireComponent] Prop Prop { get; set; }
    Vector3 _lastPosition = Vector3.Zero;
    Vector3 Velocity;

    protected override void OnStart()
    {
        _lastPosition = Prop.Transform.Position;
        Velocity = 0;
    }

    protected override void OnFixedUpdate()
    {
        Velocity = (Prop.Transform.Position - _lastPosition) / Time.Delta;
        _lastPosition = Prop.Transform.Position;
    }

    public void OnCollisionStart( Collision other )
    {
        var speed = Velocity.Length;
        var otherSpeed = other.Other.Body.Velocity.Length;
        if ( otherSpeed > speed ) speed = otherSpeed;
        if ( speed >= 1200 )
        {
            var dmg = speed / 8f;
            if ( Prop.Health <= dmg )
            {

            }
            Prop.OnDamage( new DamageInfo( dmg, null, null ) );
        }
    }
}