namespace Scenebox.Tools;

public class ToolControlAttribute : System.Attribute
{
    public string Input { get; }
    public string Description { get; }

    public ToolControlAttribute( string input, string desc )
    {
        Input = input;
        Description = desc;
    }
}