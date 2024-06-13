
using System;
using Sandbox.Citizen;

namespace Scenebox;

public class Weapon : Component
{
    [Property] public WeaponResource Resource { get; set; }

    [Property] public SkinnedModelRenderer ModelRenderer { get; set; }
    [Property] protected CitizenAnimationHelper.HoldTypes HoldType { get; set; } = CitizenAnimationHelper.HoldTypes.Pistol;

    [Property, Group( "Sounds" )] public SoundEvent EquipSound { get; set; }

    [Property, Group( "References" )] public GameObject Muzzle { get; set; }

    public Player Player => Components.Get<Player>( FindMode.EverythingInAncestors );

    public Player Owner => Scene.Directory.FindComponentByGuid( OwnerId ) as Player;
    [Sync] public Guid OwnerId { get; set; }

    [Sync, Change( nameof( OnIsEquippedChanged ) )]
    public bool IsEquipped { get; private set; }

    public int Ammo { get; set; } = 0;
    public int AmmoReserve { get; set; } = 0;

    public ViewModel ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;

            if ( _viewModel.IsValid() )
            {
                _viewModel.Weapon = this;
            }
        }
    }
    private ViewModel _viewModel;

    bool _wasEquipped = false;
    bool _hasStarted = false;

    protected override void OnStart()
    {
        _wasEquipped = IsEquipped;
        _hasStarted = true;

        if ( IsEquipped )
            OnEquip();
        else
            OnUnequip();
    }

    [Authority]
    public void Equip()
    {
        if ( IsEquipped ) return;

        if ( Player.IsValid() )
        {
            var weapons = Player.Inventory.Weapons.ToList();

            foreach ( var weapon in weapons )
            {
                weapon.Unequip();
            }
        }

        IsEquipped = true;
        Player.CurrentHoldType = HoldType;
    }

    [Authority]
    public void Unequip()
    {
        if ( !IsEquipped ) return;

        IsEquipped = false;
    }

    private void OnIsEquippedChanged( bool oldValue, bool newValue )
    {
        if ( !_hasStarted ) return;
        if ( IsEquipped == _wasEquipped ) return;

        switch ( _wasEquipped )
        {
            case false when IsEquipped:
                OnEquip();
                break;
            case true when !IsEquipped:
                OnUnequip();
                break;
        }

        _wasEquipped = IsEquipped;
    }

    public void ClearViewModel()
    {
        if ( ViewModel.IsValid() )
        {
            ViewModel.GameObject.Destroy();
        }
    }

    public void CreateViewModel( bool playEquipEffects = true )
    {
        if ( !Player.IsValid() ) return;

        ClearViewModel();
        UpdateRenderMode();

        if ( Resource.ViewModelPrefab.IsValid() )
        {
            var viewModelGameObject = Resource.ViewModelPrefab.Clone( new CloneConfig()
            {
                Transform = new(),
                Parent = Player.FirstPersonView,
                StartEnabled = true
            } );

            var viewModelComponent = viewModelGameObject.Components.Get<ViewModel>();
            ViewModel = viewModelComponent;
            viewModelGameObject.BreakFromPrefab();
        }

        if ( !playEquipEffects ) return;
        if ( EquipSound is null ) return;

        var sound = Sound.Play( EquipSound, Transform.Position );
        if ( !sound.IsValid() ) return;

        sound.ListenLocal = !IsProxy;
    }

    protected void UpdateRenderMode()
    {
        if ( !IsProxy )
            ModelRenderer.RenderType = Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly;
        else
            ModelRenderer.RenderType = Sandbox.ModelRenderer.ShadowRenderType.On;
    }

    protected virtual void OnEquip()
    {
        if ( Player.IsValid() && Player.IsFirstPerson )
            CreateViewModel();

        if ( ModelRenderer.IsValid() )
            ModelRenderer.Enabled = true;
    }

    protected virtual void OnUnequip()
    {
        if ( ModelRenderer.IsValid() ) ModelRenderer.Enabled = false;

        ClearViewModel();
    }

    protected override void OnDestroy()
    {
        ClearViewModel();
    }
}