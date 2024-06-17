namespace Scenebox.Tools;

public abstract class BaseTool
{
    public Toolgun Toolgun;

    public virtual void OnEquip()
    {

    }

    public virtual void OnUnequip()
    {

    }

    public virtual void PrimaryUseStart()
    {

    }

    public virtual void PrimaryUseUpdate()
    {

    }

    public virtual void PrimaryUseEnd()
    {

    }

    public virtual void SecondaryUseStart()
    {

    }

    public virtual void SecondaryUseUpdate()
    {

    }

    public virtual void SecondaryUseEnd()
    {

    }

    public string GetName()
    {
        return TypeLibrary.GetAttribute<ToolAttribute>( GetType() ).Title;
    }

    public string GetDescription()
    {
        return TypeLibrary.GetAttribute<ToolAttribute>( GetType() ).Description;
    }

    public string GetGroup()
    {
        return TypeLibrary.GetAttribute<ToolAttribute>( GetType() ).Group;
    }

    public List<ToolControlAttribute> GetControls()
    {
        return TypeLibrary.GetAttributes<ToolControlAttribute>( GetType() ).ToList();
    }
}