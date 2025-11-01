using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace CrystalShrine.GLUtils
{
    public class Camera
    {
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; } = Vector3.Zero;
        public Vector3 Front { get; private set; } = -Vector3.UnitZ;
        public Vector3 Up { get; private set; } = Vector3.UnitY;
        public Vector3 Right { get; private set; } = Vector3.UnitX;

        public bool IsFlying { get; set; } = false;
        public bool IsGrounded { get; set; } = false;
        public bool FlashlightOn { get; set; } = false;
        public bool IsOrbiting { get; set; } = false;
        public bool IsCrouching { get; set; } = false;
        public bool IsMoving { get; private set; } = false;

        private float _yaw = -90f;
        private float _pitch = 0f;
        private float _fov = 45f;

        private float _playerYaw = -90f;

        private float _speed = 15f;
        private float _sprintMultiplier = 2.0f;
        private float _crouchMultiplier = 0.5f;
        private float _sensitivity = 0.08f;

        private float _jumpForce = 8f;
        private float _gravity = 20f;

        private float _standingHeight = 2.5f;
        private float _crouchingHeight = 1.0f;
        private float _crouchSpeed = 8.0f;
        public float CurrentHeight { get; private set; }

        private float _zoomDistance = 0f;
        private float _targetZoomDistance = 0f;
        private readonly float _maxZoom = 25f;
        private readonly float _minZoom = 0f;
        private readonly float _zoomSpeed = 8f;

        // Collision detection
        private CollisionSystem _collisionSystem;
        private float _playerCollisionRadius = 1.5f;

        // Terrain reference for camera-floor collision
        private FloatingIsland _islandMesh;

        public float ZoomDistance => _zoomDistance;

        public Camera(Vector3 position)
        {
            Position = position;
            CurrentHeight = _standingHeight;
            UpdateVectors();
        }

        public void SetCollisionSystem(CollisionSystem collisionSystem)
        {
            _collisionSystem = collisionSystem;
        }

        public void SetTerrain(FloatingIsland island)
        {
            _islandMesh = island;
        }

        // Get the player model position
        public Vector3 GetPlayerPosition()
        {
            return Position;
        }

        public Matrix4 GetViewMatrix()
        {
            Vector3 cameraPos = Position;

            if (_zoomDistance > 0.05f)
                cameraPos -= Front * _zoomDistance;

            // Prevent camera from clipping below terrain only when not flying
            if (!IsFlying && _islandMesh != null)
            {
                float terrainHeight = _islandMesh.GetHeightAt(cameraPos.X, cameraPos.Z) - 3.5f + 1.0f;
                if (cameraPos.Y < terrainHeight)
                    cameraPos.Y = terrainHeight;
            }

            return Matrix4.LookAt(cameraPos, Position + Front, Up);
        }

        public Matrix4 GetProjectionMatrix(float aspectRatio)
            => Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(_fov),
                aspectRatio,
                0.1f, 2000f);

        public float GetPlayerYaw() => MathHelper.DegreesToRadians(_playerYaw);
        public float GetCameraYaw() => MathHelper.DegreesToRadians(_yaw);

        public void UpdateKeyboard(KeyboardState input, float deltaTime)
        {
            float velocity = _speed * deltaTime;

            bool isCrouching = input.IsKeyDown(Keys.LeftControl) || input.IsKeyDown(Keys.RightControl);
            bool isSprinting = input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift);

            IsCrouching = isCrouching && !IsFlying;

            float targetHeight = _standingHeight;
            if (isCrouching && !IsFlying)
            {
                velocity *= _crouchMultiplier;
                targetHeight = _crouchingHeight;
            }
            else if (isSprinting && !isCrouching)
            {
                velocity *= _sprintMultiplier;
            }

            if (CurrentHeight != targetHeight)
            {
                float oldHeight = CurrentHeight;
                float heightDelta = (targetHeight - CurrentHeight) * _crouchSpeed * deltaTime;
                CurrentHeight += heightDelta;

                if (MathF.Abs(CurrentHeight - targetHeight) < 0.01f)
                    CurrentHeight = targetHeight;

                if (!IsFlying)
                {
                    float actualHeightChange = CurrentHeight - oldHeight;
                    Position = new Vector3(Position.X, Position.Y + actualHeightChange, Position.Z);
                }
            }

            Vector3 movementFront = IsOrbiting
                ? new Vector3(MathF.Cos(MathHelper.DegreesToRadians(_playerYaw)), 0f, MathF.Sin(MathHelper.DegreesToRadians(_playerYaw)))
                : new Vector3(Front.X, 0f, Front.Z);

            Vector3 flatFront = Vector3.Normalize(movementFront);
            Vector3 flatRight = Vector3.Normalize(Vector3.Cross(flatFront, Vector3.UnitY));

            bool isMovingNow = false;
            Vector3 previousPosition = Position;

            if (input.IsKeyDown(Keys.W)) { Position += flatFront * velocity; isMovingNow = true; }
            if (input.IsKeyDown(Keys.S)) { Position -= flatFront * velocity; isMovingNow = true; }
            if (input.IsKeyDown(Keys.A)) { Position -= flatRight * velocity; isMovingNow = true; }
            if (input.IsKeyDown(Keys.D)) { Position += flatRight * velocity; isMovingNow = true; }

            if (_collisionSystem != null && !IsFlying)
            {
                Position = _collisionSystem.ResolveCollision(Position, previousPosition, _playerCollisionRadius);
            }

            IsMoving = isMovingNow;

            if (IsFlying)
            {
                if (input.IsKeyDown(Keys.Space))
                    Position += Vector3.UnitY * velocity;
                if (input.IsKeyDown(Keys.LeftControl) || input.IsKeyDown(Keys.RightControl))
                    Position -= Vector3.UnitY * velocity;

                Velocity = Vector3.Zero;
            }
            else
            {
                if (input.IsKeyDown(Keys.Space) && IsGrounded)
                {
                    Velocity = new Vector3(Velocity.X, _jumpForce, Velocity.Z);
                    IsGrounded = false;
                }

                Velocity = new Vector3(Velocity.X, Velocity.Y - _gravity * deltaTime, Velocity.Z);
                Position += new Vector3(0, Velocity.Y * deltaTime, 0);
            }

            _zoomDistance = MathHelper.Lerp(_zoomDistance, _targetZoomDistance, _zoomSpeed * deltaTime);
        }

        public void UpdateMouse(float deltaX, float deltaY)
        {
            deltaX *= _sensitivity;
            deltaY *= _sensitivity;

            _yaw += deltaX;
            _pitch -= deltaY;
            _pitch = MathHelper.Clamp(_pitch, -89f, 89f);

            if (!IsOrbiting)
                _playerYaw = _yaw;

            UpdateVectors();
        }

        public void UpdateScroll(float offset)
        {
            _targetZoomDistance -= offset * 1.5f;
            _targetZoomDistance = MathHelper.Clamp(_targetZoomDistance, _minZoom, _maxZoom);
        }

        private void UpdateVectors()
        {
            Vector3 front;
            front.X = MathF.Cos(MathHelper.DegreesToRadians(_yaw)) * MathF.Cos(MathHelper.DegreesToRadians(_pitch));
            front.Y = MathF.Sin(MathHelper.DegreesToRadians(_pitch));
            front.Z = MathF.Sin(MathHelper.DegreesToRadians(_yaw)) * MathF.Cos(MathHelper.DegreesToRadians(_pitch));
            Front = Vector3.Normalize(front);
            Right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
            Up = Vector3.Normalize(Vector3.Cross(Right, Front));
        }
    }
}