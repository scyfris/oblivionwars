using Godot;

public partial class EntityCharacterBody2D : CharacterBody2D
{
    [Export] protected CharacterDefinition _definition;

    // Runtime data (replaces IGameEntity)
    protected EntityRuntimeData _runtimeData;
    public EntityRuntimeData RuntimeData => _runtimeData;
    public CharacterDefinition Definition => _definition;

    // Movement
    protected int _moveDirection = 0;

    // Wall sliding
    protected bool _isWallSliding = false;
    protected Vector2 _wallNormal = Vector2.Zero;
    protected float _wallJumpInputLockTimer = 0f;
    protected float _wallJumpPushAwayDurationTimer = 0f;
    public bool IsWallSliding => _isWallSliding;

    // Gravity state
    [ExportGroup("Gravity Flip")]
    [Export] protected float _gravityFlipRotationSpeed = 10.0f;
    [Export] protected float _bodyFlipDelay = 0.0f;
    [Export] protected bool _maintainMomentumOnFlip = true;

    protected int _gravityRotation = 0;
    protected Vector2 _gravityDirection = Vector2.Down;
    protected Vector2 _upDirection = Vector2.Up;
    protected float _targetRotation = 0.0f;
    protected bool _isRotatingGravity = false;
    protected float _bodyFlipDelayTimer = 0.0f;

    public override void _Ready()
    {
        InitializeRuntimeData();
    }

    protected virtual void InitializeRuntimeData()
    {
        if (_definition != null)
        {
            _runtimeData = new EntityRuntimeData
            {
                EntityId = _definition.EntityId,
                RuntimeInstanceId = GetInstanceId(),
                CurrentHealth = _definition.MaxHealth,
                MaxHealth = _definition.MaxHealth,
                Definition = _definition
            };
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
        float currentGravity = _isWallSliding
            ? _definition.Gravity * _definition.WallSlideSpeedFraction
            : _definition.Gravity;

        Vector2 horizontalDirection = new Vector2(_gravityDirection.Y, -_gravityDirection.X);

        Vector2 horizontalVelocity;
        if (_wallJumpInputLockTimer > 0 || _wallJumpPushAwayDurationTimer > 0)
        {
            float currentHorizontalSpeed = Velocity.Dot(horizontalDirection);
            horizontalVelocity = horizontalDirection * currentHorizontalSpeed;
        }
        else
        {
            horizontalVelocity = horizontalDirection * _moveDirection * _definition.MoveSpeed;
        }

        Vector2 newVel = horizontalVelocity;

        float velocityAlongGravity = Velocity.Dot(_gravityDirection);
        newVel += _gravityDirection * velocityAlongGravity;

        if (!_isWallSliding)
        {
            newVel += _gravityDirection * currentGravity * (float)delta;
        }
        else
        {
            newVel = horizontalVelocity + _gravityDirection * (_definition.Gravity * _definition.WallSlideSpeedFraction);
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

            Vector2 horizontalDirection = new Vector2(_gravityDirection.Y, -_gravityDirection.X);
            float wallHorizontalDirection = _wallNormal.Dot(horizontalDirection);

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
    }

    public virtual void Jump()
    {
        if (_isWallSliding)
        {
            Vector2 jumpDirection = -_gravityDirection;
            Vector2 pushDirection = _wallNormal;

            Velocity = pushDirection * _definition.WallJumpPushAwayForce + jumpDirection * _definition.WallJumpStrength;
            _isWallSliding = false;

            _wallJumpInputLockTimer = _definition.WallJumpInputLockDuration;
            _wallJumpPushAwayDurationTimer = _definition.WallJumpPushAwayDuration;
            return;
        }

        if (!IsOnFloor()) return;

        Velocity -= _gravityDirection * _definition.JumpStrength;
    }

    public void CancelJump()
    {
        float velocityAlongGravity = Velocity.Dot(_gravityDirection);
        if (!IsOnFloor() && velocityAlongGravity < 0.0f)
        {
            Velocity -= _gravityDirection * velocityAlongGravity;
        }
    }

    public void MoveLeft()
    {
        _moveDirection = -1;
    }

    public void MoveRight()
    {
        _moveDirection = 1;
    }

    public void Stop()
    {
        _moveDirection = 0;
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
