using System;
using System.Net.Http;
using Sandbox;
using Sandbox.Citizen;

namespace Scenebox;

public sealed class Player : Component
{
	[RequireComponent] CharacterController CharacterController { get; set; }

	[Property, Group( "References" )] public GameObject Head { get; set; }
	[Property, Group( "References" )] public GameObject Body { get; set; }
	[Property, Group( "References" )] public CitizenAnimationHelper AnimationHelper { get; set; }

	[Property, Group( "Movement" )] public float GroundControl { get; set; } = 4.0f;
	[Property, Group( "Movement" )] public float AirControl { get; set; } = 0.1f;
	[Property, Group( "Movement" )] public float Speed { get; set; } = 160f;
	[Property, Group( "Movement" )] public float RunSpeed { get; set; } = 290f;
	[Property, Group( "Movement" )] public float WalkSpeed { get; set; } = 90f;
	[Property, Group( "Movement" )] public float JumpForce { get; set; } = 400f;

	[Sync] public float Height { get; set; } = 1f;
	public float CrouchHeight = 64f;

	public bool IsFirstPerson = true;
	[Sync] public bool IsCrouching { get; set; } = false;
	[Sync] public bool IsSprinting { get; set; } = false;
	[Sync] public Vector3 WishVelocity { get; set; } = Vector3.Zero;
	[Sync] public Angles Direction { get; set; } = Angles.Zero;

	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			IsSprinting = Input.Down( "Run" );
			if ( Input.Pressed( "Jump" ) ) Jump();

			UpdateCamera();
			UpdateCrouch();
		}

		UpdateAnimations();
		RotateBody();
	}

	protected override void OnFixedUpdate()
	{
		CharacterController.Height = CrouchHeight * Height;

		if ( !IsProxy )
		{
			if ( Input.Pressed( "View" ) ) IsFirstPerson = !IsFirstPerson;

			BuildWishVelocity();
			Move();
		}
	}

	void Move()
	{
		var gravity = Scene.PhysicsWorld.Gravity;

		if ( CharacterController.IsOnGround )
		{
			CharacterController.Velocity = CharacterController.Velocity.WithZ( 0 );
			CharacterController.Accelerate( WishVelocity );
			CharacterController.ApplyFriction( GroundControl );
		}
		else
		{
			CharacterController.Velocity += gravity * Time.Delta * 0.5f;
			CharacterController.Accelerate( WishVelocity );
			CharacterController.ApplyFriction( AirControl );
		}

		CharacterController.Move();

		if ( CharacterController.IsOnGround )
		{
			CharacterController.Velocity = CharacterController.Velocity.WithZ( 0 );
		}
		else
		{
			CharacterController.Velocity += gravity * Time.Delta * 0.5f;
		}
	}

	void Jump()
	{
		if ( !CharacterController.IsOnGround ) return;

		CharacterController.Punch( Vector3.Up * JumpForce );
		BroadcastJump();
	}

	void BuildWishVelocity()
	{
		Vector3 wishVelocity = 0;

		var rot = Head.Transform.Rotation;
		if ( Input.Down( "Forward" ) ) wishVelocity += rot.Forward;
		if ( Input.Down( "Backward" ) ) wishVelocity += rot.Backward;
		if ( Input.Down( "Left" ) ) wishVelocity += rot.Left;
		if ( Input.Down( "Right" ) ) wishVelocity += rot.Right;

		wishVelocity = wishVelocity.WithZ( 0 );

		if ( !wishVelocity.IsNearZeroLength ) wishVelocity = wishVelocity.Normal;

		if ( IsCrouching ) wishVelocity *= WalkSpeed;
		else if ( IsSprinting ) wishVelocity *= RunSpeed;
		else wishVelocity *= Speed;

		WishVelocity = wishVelocity;
	}

	void UpdateCamera()
	{
		var eyeAngles = Head.Transform.Rotation.Angles();
		eyeAngles.pitch += Input.MouseDelta.y * 0.1f;
		eyeAngles.yaw -= Input.MouseDelta.x * 0.1f;
		eyeAngles.roll = 0f;
		eyeAngles.pitch = eyeAngles.pitch.Clamp( -89.9f, 89.9f );
		Head.Transform.Rotation = eyeAngles;

		var camPos = Head.Transform.Position;
		if ( !IsFirstPerson )
		{
			var camForward = eyeAngles.Forward;
			var camTrace = Scene.Trace.Ray( camPos, camPos - (camForward * 250) )
				.WithoutTags( "player", "trigger" )
				.Run();

			if ( camTrace.Hit )
			{
				camPos = camTrace.HitPosition + camTrace.Normal;
			}
			else
			{
				camPos = camTrace.EndPosition;
			}

			AnimationHelper.Target.RenderType = ModelRenderer.ShadowRenderType.On;
		}
		else
		{
			AnimationHelper.Target.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
		}

		Scene.Camera.Transform.Position = camPos;
		Scene.Camera.Transform.Rotation = eyeAngles;
		Direction = eyeAngles;
	}

	void UpdateCrouch()
	{
		IsCrouching = Input.Down( "Duck" );
		CrouchHeight = CrouchHeight.LerpTo( IsCrouching ? 32f : 64f, 1f - MathF.Pow( 0.5f, Time.Delta * 25f ) );
		Head.Transform.LocalPosition = Head.Transform.LocalPosition.WithZ( CrouchHeight );
	}

	void UpdateAnimations()
	{
		if ( AnimationHelper is null ) return;

		AnimationHelper.WithWishVelocity( WishVelocity );
		AnimationHelper.WithVelocity( CharacterController.Velocity );
		AnimationHelper.AimAngle = Head.Transform.Rotation;
		AnimationHelper.IsGrounded = CharacterController.IsOnGround;
		AnimationHelper.WithLook( Head.Transform.Rotation.Forward );
		AnimationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
		AnimationHelper.DuckLevel = IsCrouching ? 1f : 0f;
	}

	void RotateBody()
	{
		if ( Body is null ) return;

		var targetAngle = new Angles( 0, Head.Transform.Rotation.Yaw(), 0 ).ToRotation();
		float rotateDiff = Body.Transform.Rotation.Distance( targetAngle );

		if ( rotateDiff > 50f || CharacterController.Velocity.Length > 10f )
		{
			Body.Transform.Rotation = Rotation.Lerp( Body.Transform.Rotation, targetAngle, Time.Delta * 10f );
		}
		else
		{
			Body.Transform.Rotation = targetAngle;
		}
	}

	[Broadcast]
	void BroadcastJump()
	{
		AnimationHelper?.TriggerJump();
	}
}
