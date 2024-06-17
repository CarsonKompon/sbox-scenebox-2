using System;
using Sandbox.Audio;

namespace Scenebox;

public sealed class UndoManager : Component
{
    public static UndoManager Instance { get; private set; }
    public record Entry( List<Guid> ids, Action undo );

    List<Entry> Stack = new();

    protected override void OnAwake()
    {
        Instance = this;
    }

    protected override void OnUpdate()
    {
        if ( Input.Pressed( "Undo" ) )
        {
            Undo();
        }
    }

    public void Add( Guid id, Action undo )
    {
        Add( new List<Guid>() { id }, undo );
    }

    public void Add( List<Guid> ids, Action undo )
    {
        Stack.Add( new Entry( ids, undo ) );
    }

    public void AddGameObject( Guid id )
    {
        Add( new List<Guid>() { id }, () => GameManager.Instance?.BroadcastDestroyObject( id ) );
    }

    public void Undo()
    {
        if ( Stack.Count == 0 ) return;

        Sound.Play( "ui.gmod.undo" ).TargetMixer = Mixer.FindMixerByName( "UI" );
        NotificationPanel.Instance?.AddEntry( "undo", "Undone Prop", 3f, false );

        var entry = Stack[Stack.Count - 1];
        entry.undo();
        Stack.RemoveAt( Stack.Count - 1 );
    }
}