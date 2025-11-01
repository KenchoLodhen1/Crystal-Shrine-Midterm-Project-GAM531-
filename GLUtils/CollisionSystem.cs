using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace CrystalShrine.GLUtils
{
    public class CollisionObject
    {
        public Vector3 Position { get; set; }
        public float Radius { get; set; }
        public string Type { get; set; }
        public bool IsPlatform { get; set; } = false;
        public Vector3 PlatformSize { get; set; } = Vector3.Zero;

        public CollisionObject(Vector3 position, float radius, string type = "object", bool isPlatform = false, Vector3? platformSize = null)
        {
            Position = position;
            Radius = radius;
            Type = type;
            IsPlatform = isPlatform;
            PlatformSize = platformSize ?? new Vector3(radius * 2, 2f, radius * 2);
        }

        public bool Intersects(CollisionObject other)
        {
            float distance = (Position - other.Position).Length;
            return distance < (Radius + other.Radius);
        }

        public bool Intersects(Vector3 point, float pointRadius)
        {
            float distance = (Position - point).Length;
            return distance < (Radius + pointRadius);
        }

        // Check if a point is on top of this platform
        public bool IsOnTop(Vector3 point, float playerHeight)
        {
            if (!IsPlatform) return false;

            // Check if player is within X/Z bounds of platform
            float halfWidth = PlatformSize.X / 2f;
            float halfDepth = PlatformSize.Z / 2f;

            bool withinXBounds = Math.Abs(point.X - Position.X) <= halfWidth;
            bool withinZBounds = Math.Abs(point.Z - Position.Z) <= halfDepth;

            if (!withinXBounds || !withinZBounds) return false;

            // Check if player is at the right height (on top of platform)
            float platformTop = Position.Y + PlatformSize.Y;
            float playerBottom = point.Y;

            // Player is on platform if their feet are close to the platform top
            return Math.Abs(playerBottom - platformTop) < 0.5f;
        }

        // Get the height of the platform at a given X/Z position
        public float GetHeightAt(Vector3 point)
        {
            if (!IsPlatform) return float.MinValue;

            float halfWidth = PlatformSize.X / 2f;
            float halfDepth = PlatformSize.Z / 2f;

            bool withinXBounds = Math.Abs(point.X - Position.X) <= halfWidth;
            bool withinZBounds = Math.Abs(point.Z - Position.Z) <= halfDepth;

            if (withinXBounds && withinZBounds)
            {
                return Position.Y + PlatformSize.Y;
            }

            return float.MinValue;
        }
    }

    public class CollisionSystem
    {
        private List<CollisionObject> _collisionObjects = new List<CollisionObject>();

        public void AddObject(Vector3 position, float radius, string type = "object")
        {
            _collisionObjects.Add(new CollisionObject(position, radius, type));
        }
        public void AddPlatform(Vector3 position, Vector3 size, string type = "platform")
        {
            var platformObj = new CollisionObject(position, size.X / 2f, type);
            platformObj.IsPlatform = true;
            platformObj.PlatformSize = size;
            _collisionObjects.Add(platformObj);
        }

        public bool CheckCollision(Vector3 position, float radius, string ignoreType = null)
        {
            foreach (var obj in _collisionObjects)
            {
                if (ignoreType != null && obj.Type == ignoreType)
                    continue;

                if (obj.Intersects(position, radius))
                    return true;
            }
            return false;
        }

        public Vector3 ResolveCollision(Vector3 position, Vector3 previousPosition, float radius)
        {
            foreach (var obj in _collisionObjects)
            {
                // Skip platform vertical collision - handled separately
                if (obj.IsPlatform) continue;

                float distance = (position - obj.Position).Length;

                if (distance < (radius + obj.Radius))
                {
                    Vector3 direction = Vector3.Normalize(position - obj.Position);
                    float overlap = (radius + obj.Radius) - distance;
                    position = obj.Position + direction * (obj.Radius + radius + 0.1f);
                }
            }
            return position;
        }

        // Get platform height at position (returns null if not on platform)
        public float? GetPlatformHeightAt(Vector3 position)
        {
            float? highestPlatform = null;

            foreach (var obj in _collisionObjects)
            {
                if (!obj.IsPlatform) continue;

                float halfWidth = obj.PlatformSize.X / 2f;
                float halfDepth = obj.PlatformSize.Z / 2f;

                bool withinXBounds = Math.Abs(position.X - obj.Position.X) <= halfWidth;
                bool withinZBounds = Math.Abs(position.Z - obj.Position.Z) <= halfDepth;

                if (withinXBounds && withinZBounds)
                {
                    float platformTop = obj.Position.Y + obj.PlatformSize.Y;
                    if (!highestPlatform.HasValue || platformTop > highestPlatform.Value)
                    {
                        highestPlatform = platformTop;
                    }
                }
            }

            return highestPlatform;
        }

        public void Clear()
        {
            _collisionObjects.Clear();
        }

        public int Count => _collisionObjects.Count;
    }
}