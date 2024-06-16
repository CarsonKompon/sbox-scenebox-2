using System.Text.Json.Serialization;
using Sandbox;

namespace Scenebox;

public static class SceneboxPreferences
{

    public static SceneboxSettings Settings
    {
        get
        {
            if ( _settings is null )
            {
                var file = "/settings/settings.json";
                _settings = FileSystem.Data.ReadJson( file, new SceneboxSettings() );
            }
            return _settings;
        }
    }
    static SceneboxSettings _settings;

    public static ChatSettings Chat
    {
        get
        {
            if ( _chatSettings is null )
            {
                var file = "/settings/chat.json";
                _chatSettings = FileSystem.Data.ReadJson( file, new ChatSettings() );
            }
            return _chatSettings;
        }
    }
    static ChatSettings _chatSettings;

    public static void Save()
    {
        FileSystem.Data.WriteJson( "/settings/settings.json", Settings );
        FileSystem.Data.WriteJson( "/settings/chat.json", Chat );
    }

}

public class SceneboxSettings
{

}

public class ChatSettings
{
    public bool ShowAvatars { get; set; } = true;
    public int FontSize { get; set; } = 16;
    public bool ChatSounds { get; set; } = true;
}