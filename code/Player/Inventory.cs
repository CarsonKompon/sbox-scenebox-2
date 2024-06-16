using System;
using System.Diagnostics;
using System.Linq;
using Sandbox;
using Sandbox.Diagnostics;

namespace Scenebox;

public sealed class Inventory : Component
{
	[RequireComponent] Player Player { get; set; }
	public IEnumerable<Weapon> Weapons => Player.Components.GetAll<Weapon>( FindMode.EnabledInSelfAndDescendants );

	[Property] public GameObject WeaponParent { get; set; }
	[Property] List<WeaponResource> StartingWeapons { get; set; } = new();
	public Weapon CurrentWeapon { get; private set; }

	public Action<Weapon> OnWeaponEquipped { get; set; }

	public int HoveredIndex { get; private set; } = 0;
	public Weapon HoveredWeapon => (HoveredIndex >= 0 && HoveredIndex < Weapons.Count()) ? Weapons.OrderBy( x => x.Resource.Slot ).ElementAt( HoveredIndex ) : null;
	public TimeSince timeSinceLastHover { get; private set; } = 10;

	protected override void OnStart()
	{
		if ( IsProxy ) return;

		foreach ( var weaponResource in StartingWeapons )
		{
			GiveWeapon( weaponResource );
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		if ( Input.Pressed( "Drop" ) && CurrentWeapon.IsValid() )
		{
			DropWeapon( CurrentWeapon.Id );
		}
	}

	public void CheckWeaponSwap()
	{
		if ( timeSinceLastHover < 3f && Input.Pressed( "Attack1" ) )
		{
			var weapon = HoveredWeapon;
			if ( weapon.IsValid() )
			{
				EquipWeapon( weapon );
			}
			Sound.Play( "ui.gmod.weapon.selected" );
			timeSinceLastHover = 10;
			Input.ReleaseAction( "Attack1" );
			HoveredIndex = -1;
			return;
		}

		for ( int i = 0; i < 10; i++ )
		{
			if ( Input.Pressed( $"Slot{i}" ) )
			{
				EquipSlot( i );
				return;
			}
		}

		var wheel = -Input.MouseWheel;

		if ( Input.Pressed( "NextSlot" ) ) wheel.y = -1;
		if ( Input.Pressed( "PrevSlot" ) ) wheel.y = 1;
		if ( wheel.y == 0f ) return;

		if ( HoveredIndex == -1 ) HoveredIndex = Weapons.OrderBy( x => x.Resource.Slot ).ToList().IndexOf( CurrentWeapon );

		var slotDelta = wheel.y > 0 ? 1 : -1;
		HoveredIndex += slotDelta;

		if ( HoveredIndex < 0 )
			HoveredIndex = Weapons.Count() - 1;
		else if ( HoveredIndex >= Weapons.Count() )
			HoveredIndex = 0;

		Sound.Play( "ui.gmod.weapon.hover" );
		timeSinceLastHover = 0;
	}

	public void Clear()
	{
		if ( IsProxy ) return;

		foreach ( var weapon in Weapons )
		{
			weapon.GameObject.Destroy();
			weapon.Enabled = false;
		}
	}

	public void RefillAmmo()
	{
		if ( IsProxy ) return;

		foreach ( var weapon in Weapons )
		{
			weapon.Ammo = weapon.Resource.ClipSize;
		}
	}

	public void EquipSlot( int slot )
	{
		Assert.True( !IsProxy );

		var weapons = Weapons.Where( x => x.Resource.Slot == slot ).ToArray();
		if ( weapons.Length == 0 ) return;

		if ( weapons.Length == 1 && CurrentWeapon == weapons[0] )
		{
			// TODO: Holder weapon?
		}

		var index = Array.IndexOf( weapons, HoveredWeapon );
		HoveredIndex = Weapons.OrderBy( x => x.Resource.Slot ).ToList().IndexOf( weapons[(index + 1) % weapons.Length] );

		Sound.Play( "ui.gmod.weapon.hover" );
		timeSinceLastHover = 0;
	}

	public void EquipWeapon( Weapon weapon )
	{
		Assert.True( !IsProxy );
		if ( !Weapons.Contains( weapon ) ) return;

		CurrentWeapon = weapon;
		weapon.Equip();
		OnWeaponEquipped?.Invoke( weapon );
		HoveredIndex = Weapons.OrderBy( x => x.Resource.Slot ).ToList().IndexOf( weapon );
	}

	public void HolsterWeapon()
	{
		Assert.True( !IsProxy );

		if ( !CurrentWeapon.IsValid() ) return;

		CurrentWeapon.Unequip();
		CurrentWeapon = null;
	}

	public void RemoveWeapon( Weapon weapon )
	{
		Assert.True( !IsProxy );

		if ( !Weapons.Contains( weapon ) ) return;

		if ( CurrentWeapon == weapon )
		{
			var otherWeapons = Weapons.Where( x => x != weapon );
			var orderedBySlot = otherWeapons.OrderBy( x => x.Resource.Slot );
			var targetWeapon = orderedBySlot.FirstOrDefault();

			if ( targetWeapon.IsValid() )
			{
				EquipWeapon( targetWeapon );
			}
		}

		weapon.GameObject.Destroy();
	}

	public void RemoveWeapon( WeaponResource resource )
	{
		var weapon = Weapons.FirstOrDefault( w => w.Resource == resource );
		if ( !weapon.IsValid() ) return;
		RemoveWeapon( weapon );
	}

	public Weapon GiveWeapon( WeaponResource resource, bool makeActive = true )
	{
		if ( HasWeapon( resource ) ) return null;

		if ( !resource.MainPrefab.IsValid() )
		{
			Log.Error( $"Weapon {resource.Name} has no MainPrefab" );
			return null;
		}

		var weaponGameObject = resource.MainPrefab.Clone( new CloneConfig()
		{
			Transform = new(),
			Parent = WeaponParent
		} );

		var weaponComponent = weaponGameObject.Components.Get<Weapon>( FindMode.EverythingInSelfAndDescendants );
		weaponGameObject.NetworkSpawn( Player.Network.OwnerConnection );

		if ( makeActive ) EquipWeapon( weaponComponent );

		return weaponComponent;
	}

	public bool HasWeapon( WeaponResource resource )
	{
		return Weapons.Any( w => w.Enabled && w.Resource == resource );
	}

	[Broadcast]
	public void DropWeapon( Guid weaponId )
	{
		if ( !Networking.IsHost ) return;
		var weapon = Scene.Directory.FindComponentByGuid( weaponId ) as Weapon;
		if ( !weapon.IsValid() ) return;

		var tr = Scene.Trace.Ray( new Ray( Player.Head.Transform.Position, Player.Direction.Forward ), 128 )
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.WithoutTags( "trigger" )
			.Run();

		var position = tr.Hit ? (tr.HitPosition + tr.Normal * weapon.Resource.WorldModel.Bounds.Size.Length) : (Player.Head.Transform.Position + Player.Direction.Forward * 32);
		var rotation = Rotation.From( 0, Player.Direction.yaw + 90, 90 );

		var baseVelocity = Player.CharacterController.Velocity;
		// TODO: Spawn dropped weapon!!

		RemoveWeapon( weapon );
	}


}
