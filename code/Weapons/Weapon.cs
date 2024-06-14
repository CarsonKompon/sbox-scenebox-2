
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

    [Sync]
    public bool IsEquipped
    {
        get => _isEquipped;
        set
        {
            _isEquipped = value;

            ModelRenderer.Enabled = _isEquipped;
        }
    }
    bool _isEquipped;

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
    bool _hasStarted = false;

    protected override void OnStart()
    {
        _hasStarted = true;

        if ( IsEquipped )
            OnEquip();
        else
            OnUnequip();
    }

    public virtual void Update() { }
    public virtual void FixedUpdate() { }

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

        OnEquip();
    }

    [Authority]
    public void Unequip()
    {
        if ( !IsEquipped ) return;

        IsEquipped = false;

        OnUnequip();
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

        BroadcastSetVisible( true );
    }

    protected virtual void OnUnequip()
    {
        BroadcastSetVisible( false );

        ClearViewModel();
    }

    protected override void OnDestroy()
    {
        ClearViewModel();
    }

    [Broadcast]
    void BroadcastSetVisible( bool visible )
    {
        if ( ModelRenderer.IsValid() ) ModelRenderer.Enabled = visible;
    }
}