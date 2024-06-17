using System;
using Sandbox.Network;
using System.Threading.Tasks;

namespace Scenebox;

public partial class GameManager : Component, Component.INetworkListener
{
    public static GameManager Instance { get; private set; }

    [Property] public GameObject PlayerPrefab { get; set; }
    [Property] public List<GameObject> SpawnPoints { get; set; }
    [Property, Group( "Prefabs" )] public GameObject DecalObject { get; set; }
    [Property, Group( "Prefabs" )] public GameObject RemoverDestroyParticle { get; set; }

    [Sync] public NetList<string> Packages { get; set; }


    protected override void OnAwake()
    {
        Instance = this;
    }

    protected override void OnUpdate()
    {
        ThumbnailCache.CheckTextureQueue();

        if ( !Player.Local.IsValid() && (Input.Pressed( "Jump" ) || Input.Pressed( "Attack1" )) )
        {
            SpawnPlayer( Connection.Local );
        }
    }

    protected override async Task OnLoad()
    {
        if ( Scene.IsEditor )
            return;

        if ( !GameNetworkSystem.IsActive )
        {
            LoadingScreen.Title = "Creating Lobby";
            await Task.DelayRealtimeSeconds( 0.1f );
            GameNetworkSystem.CreateLobby();
        }
    }

    public void OnActive( Connection channel )
    {
        Log.Info( $"Player '{channel.DisplayName}' has joined the game" );

        SpawnPlayer( channel );
    }

    void SpawnPlayer( Connection channel )
    {
        if ( PlayerPrefab is null )
            return;

        var existingPlayer = Scene.GetAllComponents<Player>().FirstOrDefault( x => x.Network.OwnerId == channel.Id );
        existingPlayer?.Kill();

        var startLocation = FindSpawnLocation().WithScale( 1 );

        var player = PlayerPrefab.Clone( startLocation, name: $"Player - {channel.DisplayName}" );
        player.NetworkSpawn( channel );
    }

    Transform FindSpawnLocation()
    {
        if ( SpawnPoints is not null && SpawnPoints.Count > 0 )
        {
            return Random.Shared.FromList( SpawnPoints, default ).Transform.World;
        }

        var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
        if ( spawnPoints.Length > 0 )
        {
            return Random.Shared.FromArray( spawnPoints ).Transform.World;
        }

        return Transform.World;
    }

    public GameObject SpawnModel( Model model, Vector3 position, Rotation rotation )
    {
        var gameObject = new GameObject();
        gameObject.Name = model?.ResourceName ?? "";

        gameObject.Transform.Position = position;
        gameObject.Transform.Rotation = rotation;

        if ( model == null || model.Physics?.Parts.Count() > 0 )
        {
            var prop = gameObject.Components.Create<Prop>();
            prop.Model = model;
            gameObject.Components.Create<PropHelper>();
        }
        else
        {
            var renderer = gameObject.Components.Create<ModelRenderer>();
            renderer.Model = model;
            var collider = gameObject.Components.Create<BoxCollider>();
            collider.Center = model.Bounds.Center;
            collider.Scale = model.Bounds.Size;
            gameObject.Components.Create<Rigidbody>();
        }

        gameObject.NetworkSpawn();
        gameObject.Network.SetOwnerTransfer( OwnerTransfer.Takeover );
        gameObject.Network.SetOrphanedMode( NetworkOrphaned.Host );

        return gameObject;
    }

    public LegacyParticleSystem CreateParticleSystem( string particle, Vector3 pos, Rotation rot, float decay = 5f )
    {
        var gameObject = Scene.CreateObject();
        gameObject.Transform.Position = pos;
        gameObject.Transform.Rotation = rot;

        var p = gameObject.Components.Create<LegacyParticleSystem>();
        p.Particles = ParticleSystem.Load( particle );
        gameObject.Transform.ClearInterpolation();

        gameObject.Components.GetOrCreate<DestroyAfter>().Time = 2f;

        return p;
    }

    [Broadcast]
    public void SpawnCloudModel( string cloudModel, Vector3 position, Rotation rotation )
    {
        if ( !Networking.IsHost ) return;

        var gameObject = SpawnModel( null, position, rotation );
        var propHelper = gameObject.Components.Get<PropHelper>();
        propHelper?.SetCloudModel( cloudModel );
    }

    [Broadcast]
    public void SpawnDecal( string decalPath, Vector3 position, Vector3 normal, Guid parentId = default )
    {
        if ( string.IsNullOrWhiteSpace( decalPath ) ) decalPath = "decals/bullethole.decal";
        position += normal;
        var decalObject = DecalObject.Clone( position, Rotation.LookAt( -normal ) );
        var parent = Scene.Directory.FindByGuid( parentId );
        if ( parent.IsValid() ) decalObject.SetParent( parent );
        decalObject.Name = decalPath;
        if ( !string.IsNullOrWhiteSpace( decalPath ) )
        {
            var renderer = decalObject.Components.Get<DecalRenderer>();
            var decal = ResourceLibrary.Get<DecalDefinition>( decalPath );
            if ( decal is not null )
            {
                var entry = decal.Decals.OrderBy( x => Random.Shared.Float() ).FirstOrDefault();
                renderer.Material = entry.Material;
                var width = entry.Width.GetValue();
                var height = entry.Height.GetValue();
                renderer.Size = new Vector3(
                    width,
                    entry.KeepAspect ? width : height,
                    entry.Depth.GetValue()
                );
                var fadeAfter = decalObject.Components.GetOrCreate<FadeAfter>();
                fadeAfter.Time = entry.FadeTime.GetValue();
                fadeAfter.FadeTime = entry.FadeDuration.GetValue();
            }
        }
    }

    [Broadcast]
    public void BroadcastDestroyObject( Guid objectId )
    {
        var obj = Scene.Directory.FindByGuid( objectId );
        if ( obj.IsValid() )
        {
            obj.Destroy();
        }
    }

    [Broadcast]
    public void BroadcastAddTag( Guid objectId, string tag )
    {
        Scene.Directory.FindByGuid( objectId )?.Tags?.Add( tag );
    }

    [Broadcast]
    public void BroadcastRemoveTag( Guid objectId, string tag )
    {
        Scene.Directory.FindByGuid( objectId )?.Tags?.Remove( tag );
    }

    [Broadcast]
    public void BroadcastSetTag( Guid objectId, string tag, bool state )
    {
        var obj = Scene.Directory.FindByGuid( objectId );
        if ( obj.IsValid() )
        {
            if ( state )
            {
                obj.Tags.Add( tag );
            }
            else
            {
                obj.Tags.Remove( tag );
            }
        }
    }

    [Broadcast]
    public void BroadcastAddHighlight( Guid objectId, Color color, Color obscuredColor, float width )
    {
        var obj = Scene.Directory.FindByGuid( objectId );
        if ( obj.IsValid() )
        {
            var outline = obj.Components.GetOrCreate<HighlightOutline>();
            outline.Color = color;
            outline.ObscuredColor = obscuredColor;
            outline.Width = width;
        }
    }

    [Broadcast]
    public void BroadcastRemoveHighlight( Guid objectId )
    {
        var obj = Scene.Directory.FindByGuid( objectId );
        if ( obj.IsValid() )
        {
            obj.Components.Get<HighlightOutline>()?.Destroy();
        }
    }

    [Broadcast]
    public void BroadcastDestroyObjectEffect( Vector3 position, Rotation rotation, Vector3 size )
    {
        var destroyEffect = RemoverDestroyParticle.Clone( position, rotation );
        destroyEffect.BreakFromPrefab();
        var emitter = destroyEffect.Components.Get<ParticleBoxEmitter>( FindMode.EverythingInSelfAndDescendants );
        destroyEffect.Transform.Position = position;
        destroyEffect.Transform.Rotation = rotation;
        if ( size.Length < 100f )
        {
            emitter.Burst = size.Length * 2f;
        }
        emitter.Size = size;
        emitter.Enabled = true;
    }
}
