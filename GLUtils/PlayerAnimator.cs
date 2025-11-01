using OpenTK.Mathematics;
using System;

namespace CrystalShrine.GLUtils
{
    public class PlayerAnimator
    {
        private float _animationTime = 0f;
        private float _lastSpeed = 0f;

        // Current state
        private float _currentBobHeight = 0f;
        private float _currentTiltX = 0f;  // Forward/back tilt
        private float _currentTiltZ = 0f;  // Side-to-side tilt
        private float _currentRotationY = 0f; // Body twist
        private float _currentScale = 1f;
        private float _verticalSquash = 0f;

        public void Update(float deltaTime, Vector3 velocity, bool isGrounded, bool isCrouching, bool isJumping)
        {
            _animationTime += deltaTime;

            // Calculate horizontal speed
            float horizontalSpeed = new Vector2(velocity.X, velocity.Z).Length;

            // Smooth speed transitions
            _lastSpeed = MathHelper.Lerp(_lastSpeed, horizontalSpeed, 10f * deltaTime);

            if (!isGrounded)
            {
                // Jump/Fall animation
                AnimateJump(velocity.Y, deltaTime);
            }
            else if (isCrouching)
            {
                // Crouch animation
                AnimateCrouch(horizontalSpeed, deltaTime);
            }
            else if (_lastSpeed > 0.1f)
            {
                // Determine if walking or running
                bool isRunning = _lastSpeed > 8f;

                if (isRunning)
                    AnimateRun(deltaTime);
                else
                    AnimateWalk(deltaTime);
            }
            else
            {
                // Idle animation
                AnimateIdle(deltaTime);
            }
        }

        private void AnimateWalk(float deltaTime)
        {
            float bobSpeed = 6f;
            float bobCycle = MathF.Sin(_animationTime * bobSpeed);

            // Vertical bob
            _currentBobHeight = MathF.Abs(bobCycle) * 0.12f;

            // Forward/back lean while walking
            _currentTiltX = bobCycle * 0.15f;

            // Side-to-side sway
            _currentTiltZ = MathF.Sin(_animationTime * bobSpeed * 0.5f) * 0.1f;

            // Slight body rotation/twist
            _currentRotationY = MathF.Sin(_animationTime * bobSpeed) * 0.08f;

            _currentScale = 1f;
            _verticalSquash = 0f;
        }

        private void AnimateRun(float deltaTime)
        {
            float bobSpeed = 10f;
            float bobCycle = MathF.Sin(_animationTime * bobSpeed);

            // More pronounced vertical bob
            _currentBobHeight = MathF.Abs(bobCycle) * 0.2f;

            // More aggressive forward lean
            _currentTiltX = 0.25f + (bobCycle * 0.12f);

            // Stronger side-to-side sway
            _currentTiltZ = MathF.Sin(_animationTime * bobSpeed * 0.5f) * 0.15f;

            // More body twist
            _currentRotationY = MathF.Sin(_animationTime * bobSpeed) * 0.12f;

            _currentScale = 1f;
            _verticalSquash = 0f;
        }

        private void AnimateIdle(float deltaTime)
        {
            float breathSpeed = 1.5f;
            float breathCycle = MathF.Sin(_animationTime * breathSpeed);

            // Gentle breathing bob
            _currentBobHeight = (breathCycle * 0.5f + 0.5f) * 0.03f;

            // Slight sway
            _currentTiltX = MathF.Sin(_animationTime * 0.8f) * 0.02f;
            _currentTiltZ = MathF.Sin(_animationTime * 0.6f) * 0.015f;

            // Minimal body rotation
            _currentRotationY = MathF.Sin(_animationTime * 0.5f) * 0.02f;

            // Breathing scale
            _currentScale = 1f + (breathCycle * 0.015f);
            _verticalSquash = 0f;
        }

        private void AnimateCrouch(float horizontalSpeed, float deltaTime)
        {
            // Crouched position with optional movement
            if (horizontalSpeed > 0.1f)
            {
                // Crouch walking - slower, more careful
                float bobSpeed = 4f;
                float bobCycle = MathF.Sin(_animationTime * bobSpeed);

                _currentBobHeight = MathF.Abs(bobCycle) * 0.06f;
                _currentTiltX = 0.3f + (bobCycle * 0.08f);
                _currentTiltZ = MathF.Sin(_animationTime * bobSpeed * 0.5f) * 0.08f;
                _currentRotationY = bobCycle * 0.05f;
            }
            else
            {
                // Crouch idle
                float breathCycle = MathF.Sin(_animationTime * 1.2f);
                _currentBobHeight = (breathCycle * 0.5f + 0.5f) * 0.02f;
                _currentTiltX = 0.25f;
                _currentTiltZ = 0f;
                _currentRotationY = 0f;
            }

            _currentScale = 0.75f;
            _verticalSquash = 0f;
        }

        private void AnimateJump(float verticalVelocity, float deltaTime)
        {
            // Anticipation and follow-through
            if (verticalVelocity > 2f)
            {
                // Going up - stretch and lean back
                _verticalSquash = MathHelper.Clamp(verticalVelocity * 0.04f, 0f, 0.3f);
                _currentTiltX = -0.2f;
                _currentRotationY = 0f;
            }
            else if (verticalVelocity < -2f)
            {
                // Falling - squash and lean forward
                _verticalSquash = MathHelper.Clamp(verticalVelocity * -0.03f, -0.25f, 0f);
                _currentTiltX = 0.3f;
                _currentRotationY = 0f;
            }
            else
            {
                // At apex of jump
                _verticalSquash = 0f;
                _currentTiltX = 0f;
                _currentRotationY = 0f;
            }

            _currentBobHeight = 0f;
            _currentTiltZ = 0f;
            _currentScale = 1f;
        }

        public Matrix4 GetAnimatedTransform(Vector3 position, float yaw, float baseScale)
        {
            // Calculate scales with squash/stretch
            float scaleY = _currentScale * (1f + _verticalSquash);
            float scaleXZ = _currentScale * (1f - _verticalSquash * 0.5f);

            // Estimate model height (adjust this if your model is different size)
            float modelHeight = 2.0f * baseScale;
            float pivotHeight = modelHeight * 0.5f;

            // Build transform matrix with proper order
            Matrix4 transform = Matrix4.Identity;

            // 1. Move pivot to center of model (so rotations happen around center)
            transform *= Matrix4.CreateTranslation(0f, pivotHeight, 0f);

            // 2. Apply body rotations (in local space)
            // Tilt forward/back (X-axis)
            transform *= Matrix4.CreateRotationX(_currentTiltX);

            // Tilt side-to-side (Z-axis) 
            transform *= Matrix4.CreateRotationZ(_currentTiltZ);

            // Body twist (Y-axis, local rotation)
            transform *= Matrix4.CreateRotationY(_currentRotationY);

            // 3. Scale (with squash/stretch and breathing) - after rotations
            transform *= Matrix4.CreateScale(scaleXZ, scaleY, scaleXZ);

            // 4. Move pivot back to feet
            transform *= Matrix4.CreateTranslation(0f, -pivotHeight, 0f);

            // 5. Apply base scale
            transform *= Matrix4.CreateScale(baseScale);

            // 6. Character world rotation (facing direction)
            transform *= Matrix4.CreateRotationY(yaw);

            // 7. Final world position with bob
            Vector3 finalPosition = position + new Vector3(0f, _currentBobHeight, 0f);
            transform *= Matrix4.CreateTranslation(finalPosition);

            return transform;
        }
    }
}