using Godot;

// All the physics stuff
public partial class EntityCharacterBody2D : CharacterBody2D
{
    // Gravity state
    [ExportGroup("Gravity Flip")]
    [Export] protected float _gravityFlipRotationSpeed = 10.0f;
    [Export] protected float _bodyFlipDelay = 0.0f;
    [Export] protected bool _maintainMomentumOnFlip = true;

    [ExportGroup("Visuals")]
    [Export] private Node2D _flipRoot;
    [Export] private AnimatedSprite2D _spriteNode;
    public AnimatedSprite2D SpriteNode => _spriteNode;
    [Export] private string _idleAnimation = "default";
    [Export] private string _walkFacingDirAnimation = "walk-facingdir";
    [Export] private string _walkNonFacingDirAnimation = "walk-nonfacingdir";

    [ExportGroup("Wall Slide Effects")]
    [Export] private Node2D _wallSlideDustPosition;
    [Export] private PackedScene _wallSlideDustScene;

    private CpuParticles2D _wallSlideDust;
    private bool _facingRight = true;
    public bool IsFacingRight => _facingRight;
    public Vector2 HorizontalDir => new Vector2(_gravityDirection.Y, -_gravityDirection.X);

    // Set by controller each frame for animation
    public Vector2 AimTarget { get; set; }

    [ExportGroup("Physics")]
    [Export] private CommonPhysicsDef _physicsDef;

    // Runtime data
    public bool GravityEnabled { get; set; } = true;

    // Movement
    protected int _moveDirection = 0;
    protected int _verticalMoveDirection = 0;

    // Wall sliding
    protected bool _isWallSliding = false;
    protected Vector2 _wallNormal = Vector2.Zero;
    protected float _wallJumpInputLockTimer = 0f;
    protected float _wallJumpPushAwayDurationTimer = 0f;
    public bool IsWallSliding => _isWallSliding;

    protected int _gravityRotation = 0;
    protected Vector2 _gravityDirection = Vector2.Down;
    protected Vector2 _upDirection = Vector2.Up;
    protected float _targetRotation = 0.0f;
    protected bool _isRotatingGravity = false;
    protected float _bodyFlipDelayTimer = 0.0f;

    public override void _Ready()
    {
        if (_physicsDef == null)
        {
            GD.PrintErr($"{Name}: No physics definition on EntytCharacterBody2d resource");
        }

        // Initialize graphics
        if (_wallSlideDustPosition != null && _wallSlideDustScene != null)
        {
            _wallSlideDust = _wallSlideDustScene.Instantiate<CpuParticles2D>();
            _wallSlideDust.Emitting = false;
            _wallSlideDustPosition.AddChild(_wallSlideDust);
        }
    }


    public override void _PhysicsProcess(double delta)
    {
        UpdateGravityRotation(delta);

        UpDirection = _upDirection;

        UpdateWallJumpTimers(delta);
        UpdateWallSliding();
        UpdateMovement(delta);

        MoveAndSlide();

        ZeroFloorVelocity();
        CheckHazardTiles();

        UpdateAnimation();
        
    }

    // ── Animation ─────────────────────────────────────────

    private void UpdateAnimation()
    {
        if (_spriteNode == null) return;

        if (_moveDirection != 0)
            _facingRight = _moveDirection > 0;

        if (_flipRoot != null)
            _flipRoot.Scale = new Vector2(_facingRight ? 1 : -1, 1);

        if (_moveDirection != 0 && IsOnFloor())
        {
            float aimDot = (AimTarget - GlobalPosition).Dot(HorizontalDir);
            bool aimToLocalRight = aimDot > 0;
            bool movingTowardAim = _facingRight == aimToLocalRight;

            _spriteNode.Play(movingTowardAim ? _walkFacingDirAnimation : _walkNonFacingDirAnimation);
        }
        else
        {
            _spriteNode.Play(_idleAnimation);
        }
    }

    protected virtual void UpdateGravityRotation(double delta)
    {
        if (!_isRotatingGravity) return;

        if (_bodyFlipDelayTimer > 0)
        {
            _bodyFlipDelayTimer -= (float)delta;
            return;
        }

        float angularDistance = Mathf.Abs(Mathf.AngleDifference(GlobalRotation, _targetRotation));

        if (angularDistance > 0.01f)
        {
            float direction = Mathf.Sign(Mathf.AngleDifference(GlobalRotation, _targetRotation));
            float stepAmount = _gravityFlipRotationSpeed * (float)delta;

            if (angularDistance <= stepAmount)
            {
                GlobalRotation = _targetRotation;
                _isRotatingGravity = false;
            }
            else
            {
                GlobalRotation += direction * stepAmount;
            }
        }
        else
        {
            GlobalRotation = _targetRotation;
            _isRotatingGravity = false;
        }
    }

    protected virtual void UpdateWallJumpTimers(double delta)
    {
        if (_wallJumpInputLockTimer > 0)
            _wallJumpInputLockTimer -= (float)delta;

        if (_wallJumpPushAwayDurationTimer > 0)
        {
            _wallJumpPushAwayDurationTimer -= (float)delta;

            if (_moveDirection != 0)
            {
                // Player can cancel horizontal momentum after wall jumping
                // by pressing any direction. This allows time to press the
                // opposite arrow key, matching HollowKnight feel.
                _wallJumpPushAwayDurationTimer = 0;
            }
        }
    }

    protected virtual void UpdateMovement(double delta)
    {
        Vector2 horizontalVelocity;
        if (_wallJumpInputLockTimer > 0 || _wallJumpPushAwayDurationTimer > 0)
        {
            float currentHorizontalSpeed = Velocity.Dot(HorizontalDir);
            horizontalVelocity = HorizontalDir * currentHorizontalSpeed;
        }
        else
        {
            horizontalVelocity = HorizontalDir * _moveDirection * _physicsDef.MoveSpeed;
        }

        Vector2 newVel = horizontalVelocity;

        if (!GravityEnabled)
        {
            // Flying: vertical movement driven by _verticalMoveDirection, no gravity
            newVel += _gravityDirection * _verticalMoveDirection * _physicsDef.MoveSpeed;
        }
        else if (_isWallSliding)
        {
            newVel += _gravityDirection * (_physicsDef.Gravity * _physicsDef.WallSlideSpeedFraction);
        }
        else
        {
            float velocityAlongGravity = Velocity.Dot(_gravityDirection);
            newVel += _gravityDirection * velocityAlongGravity;
            newVel += _gravityDirection * _physicsDef.Gravity * (float)delta;
        }

        Velocity = newVel;
    }

    protected void ZeroFloorVelocity()
    {
        if (IsOnFloor())
        {
            float gravityVelocity = Velocity.Dot(_gravityDirection);
            if (gravityVelocity > 0)
            {
                Velocity -= _gravityDirection * gravityVelocity;
            }
        }
    }

    protected virtual void UpdateWallSliding()
    {
        if (IsOnFloor())
        {
            SetWallSliding(false);
            return;
        }

        if (IsOnWall())
        {
            _wallNormal = GetWallNormal();

            float wallHorizontalDirection = _wallNormal.Dot(HorizontalDir);

            bool movingAwayFromWall = false;
            if (wallHorizontalDirection > 0.1f && _moveDirection > 0)
                movingAwayFromWall = true;
            else if (wallHorizontalDirection < -0.1f && _moveDirection < 0)
                movingAwayFromWall = true;

            float velocityAlongGravity = Velocity.Dot(_gravityDirection);
            if (velocityAlongGravity >= 0 && !movingAwayFromWall)
            {
                SetWallSliding(true);
            }
            else
            {
                SetWallSliding(false);
            }
        }
        else
        {
            SetWallSliding(false);
        }
    }

    protected virtual void SetWallSliding(bool sliding)
    {
        _isWallSliding = sliding;

        if (_wallSlideDust != null)
        {
            _wallSlideDust.Emitting = sliding;
            if (sliding)
                _wallSlideDust.Direction = _wallNormal;
        }
    }

    public virtual void Jump()
    {
        if (_isWallSliding)
        {
            Vector2 jumpDirection = -_gravityDirection;
            Vector2 pushDirection = _wallNormal;

            Velocity = pushDirection * _physicsDef.WallJumpPushAwayForce + jumpDirection * _physicsDef.WallJumpStrength;
            _isWallSliding = false;

            _wallJumpInputLockTimer = _physicsDef.WallJumpInputLockDuration;
            _wallJumpPushAwayDurationTimer = _physicsDef.WallJumpPushAwayDuration;
            return;
        }

        if (!IsOnFloor()) return;

        Velocity -= _gravityDirection * _physicsDef.JumpStrength;
    }

    public void CancelJump()
    {
        float velocityAlongGravity = Velocity.Dot(_gravityDirection);
        if (!IsOnFloor() && velocityAlongGravity < 0.0f)
        {
            Velocity -= _gravityDirection * velocityAlongGravity;
        }
    }

    public void StartMoveLeft()
    {
        _moveDirection = -1;
    }

    public void StartMoveRight()
    {
        _moveDirection = 1;
    }

    public void Stop()
    {
        _moveDirection = 0;
        _verticalMoveDirection = 0;
    }

    public void StartMoveUp()
    {
        _verticalMoveDirection = -1;
    }

    public void StartMoveDown()
    {
        _verticalMoveDirection = 1;
    }

    public void StopVertical()
    {
        _verticalMoveDirection = 0;
    }

    public void RotateGravityClockwise()
    {
        RotateGravity(90);
    }

    public void RotateGravityCounterClockwise()
    {
        RotateGravity(-90);
    }

    protected void RotateGravity(int degrees)
    {
        _gravityRotation = ((_gravityRotation + degrees + 360) % 360 / 90) * 90;

        switch (_gravityRotation)
        {
            case 0:
                _gravityDirection = new Vector2(0, 1);
                _targetRotation = 0;
                break;
            case 90:
                _gravityDirection = new Vector2(1, 0);
                _targetRotation = -Mathf.Pi / 2;
                break;
            case 180:
                _gravityDirection = new Vector2(0, -1);
                _targetRotation = Mathf.Pi;
                break;
            case 270:
                _gravityDirection = new Vector2(-1, 0);
                _targetRotation = Mathf.Pi / 2;
                break;
            default:
                GD.PrintErr("Unexpected gravity rotation value, clamping");
                _gravityRotation = (_gravityRotation + 45) / 90 * 90 % 360;
                RotateGravity(0);
                return;
        }
        _upDirection = -_gravityDirection;

        _isRotatingGravity = true;
        _bodyFlipDelayTimer = _bodyFlipDelay;

        if (_maintainMomentumOnFlip)
        {
            Velocity = Velocity.Rotated(Mathf.DegToRad(degrees));
        }
    }

    public int GetGravityRotation()
    {
        return _gravityRotation;
    }

    protected virtual void CheckHazardTiles()
    {
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            if (collision.GetCollider() is TileMapLayer tileMap)
            {
                var collisionPos = collision.GetPosition();
                var tileCoords = tileMap.LocalToMap(tileMap.ToLocal(collisionPos));
                var tileData = tileMap.GetCellTileData(tileCoords);
                if (tileData == null) continue;

                var hazardValue = tileData.GetCustomData("hazard_type");
                if (hazardValue.VariantType == Variant.Type.Int)
                {
                    var hazardType = (TileHazardType)(int)hazardValue;
                    if (hazardType != TileHazardType.None)
                    {
                        OnHazardContact(hazardType);
                        return;
                    }
                }
            }
        }
    }

    protected virtual void OnHazardContact(TileHazardType hazardType)
    {
        EventBus.Instance.Raise(new HazardContactEvent
        {
            EntityInstanceId = GetInstanceId(),
            HazardType = hazardType,
            Position = GlobalPosition
        });
    }
}
