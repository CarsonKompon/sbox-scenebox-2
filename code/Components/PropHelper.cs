namespace Scenebox;

public sealed class PropHelper : Component, Component.ICollisionListener
{
    [RequireComponent] Prop Prop { get; set; }

    [Sync] NetDictionary<int, BodyInfo> NetworkedBodies { get; set; } = new();

    Vector3 _lastPosition = Vector3.Zero;
    Vector3 Velocity;
    ModelPhysics Physics;

    struct BodyInfo
    {
        public PhysicsBodyType Type;
        public Transform Transform;
    }

    protected override void OnStart()
    {
        Physics = Components.Get<ModelPhysics>();
        _lastPosition = Prop.Transform.Position;
        Velocity = 0;
    }

    protected override void OnFixedUpdate()
    {
        Velocity = (Prop.Transform.Position - _lastPosition) / Time.Delta;
        _lastPosition = Prop.Transform.Position;

        UpdateNetworkedBodies();
    }

    void UpdateNetworkedBodies()
    {
        if ( !Physics.IsValid() ) return;

        if ( !Network.IsOwner )
        {
            var rootBody = FindRootBody();

            foreach ( var (groupId, info) in NetworkedBodies )
            {
                var group = Physics.PhysicsGroup.GetBody( groupId );
                group.Transform = info.Transform;
                group.BodyType = info.Type;
            }

            if ( rootBody.IsValid() )
            {
                rootBody.Transform = Physics.Renderer.GameObject.Transform.World;
            }

            return;
        }

        foreach ( var body in Physics.PhysicsGroup.Bodies )
        {
            if ( body.GroupIndex == 0 ) continue;

            var tx = body.GetLerpedTransform( Time.Now );
            NetworkedBodies[body.GroupIndex] = new BodyInfo
            {
                Type = body.BodyType,
                Transform = tx
            };
        }
    }

    PhysicsBody FindRootBody()
    {
        var body = Physics.PhysicsGroup.Bodies.FirstOrDefault();
        if ( body == null ) return null;
        while ( body.Parent.IsValid() )
        {
            body = body.Parent;
        }
        return body;
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