using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace CrystalShrine.GLUtils
{
    public class InteractableObject
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public Vector3 Position { get; set; }
        public float InteractRadius { get; set; }
        public Vector3 CurrentEmissiveColor { get; set; }
        public Vector3 TargetEmissiveColor { get; set; }
        public int ColorIndex { get; set; } = 0;
        public int ModelIndex { get; set; }
        public bool IsActive { get; set; } = false;

        // Animation Support
        public Vector3 BasePosition { get; set; }
        public Matrix4 Transform { get; set; } = Matrix4.Identity;

        private float _fadeSpeed = 2.5f;

        public InteractableObject(string id, string type, Vector3 position, float interactRadius, Vector3 initialColor, int modelIndex)
        {
            Id = id;
            Type = type;
            Position = position;
            BasePosition = position;
            InteractRadius = interactRadius;
            CurrentEmissiveColor = initialColor;
            TargetEmissiveColor = initialColor;
            ModelIndex = modelIndex;
        }

        public bool IsInRange(Vector3 playerPos)
        {
            float distance = (Position - playerPos).Length;
            return distance <= InteractRadius;
        }

        public void Update(float deltaTime)
        {
            // Smoothly lerp emissive to target color
            CurrentEmissiveColor = Vector3.Lerp(CurrentEmissiveColor, TargetEmissiveColor, deltaTime * _fadeSpeed);
        }
    }

    public class InteractionSystem
    {
        private readonly List<InteractableObject> _interactables = new List<InteractableObject>();

        private readonly Vector3[] _neonColors = new Vector3[]
        {
            new Vector3(0.0f, 1.0f, 0.0f), // Green
            new Vector3(0.0f, 0.4f, 1.0f), // Blue
            new Vector3(1.0f, 0.0f, 0.0f), // Red
            new Vector3(1.0f, 0.6f, 0.0f), // Orange
            new Vector3(0.6f, 0.0f, 1.0f), // Purple
        };

        public void AddInteractable(string id, string type, Vector3 position, float interactRadius, Vector3 initialColor, int modelIndex)
        {
            var interactable = new InteractableObject(id, type, position, interactRadius, Vector3.Zero, modelIndex);
            _interactables.Add(interactable);
        }

        public InteractableObject GetLookedAtInteractable(Vector3 cameraPos, Vector3 cameraFront, float maxDistance = 10f)
        {
            InteractableObject closest = null;
            float closestDist = maxDistance;

            foreach (var interactable in _interactables)
            {
                Vector3 toObject = interactable.Position - cameraPos;
                float projectionLength = Vector3.Dot(toObject, cameraFront);
                if (projectionLength < 0) continue;

                Vector3 closestPoint = cameraPos + cameraFront * projectionLength;
                float distanceToRay = (interactable.Position - closestPoint).Length;

                if (distanceToRay <= interactable.InteractRadius && projectionLength < closestDist)
                {
                    closest = interactable;
                    closestDist = projectionLength;
                }
            }

            return closest;
        }

        // Activation Logic
        public void HandleInteraction(InteractableObject obj, ref int activePillars, ref bool crystalAwakened)
        {
            if (obj.Type == "pillar")
            {
                if (!obj.IsActive)
                {
                    activePillars++;
                    obj.IsActive = true;
                }

                obj.ColorIndex = (obj.ColorIndex + 1) % _neonColors.Length;
                obj.TargetEmissiveColor = _neonColors[obj.ColorIndex];
                Console.WriteLine($"[PILLAR] {obj.Id} changed to color index {obj.ColorIndex}");
            }

            if (obj.Type == "crystal")
            {
                var pillars = _interactables.FindAll(p => p.Type == "pillar" && p.IsActive);
                if (pillars.Count < 4)
                {
                    Console.WriteLine("[CRYSTAL] Not all pillars are active yet.");
                    return;
                }

                int colorIndex = pillars[0].ColorIndex;
                bool allSame = pillars.TrueForAll(p => p.ColorIndex == colorIndex);

                if (!allSame)
                {
                    Console.WriteLine("[CRYSTAL] Pillars are mismatched — no reaction.");
                    return;
                }

                crystalAwakened = true;
                obj.TargetEmissiveColor = _neonColors[colorIndex];
                Console.WriteLine($"[CRYSTAL] Resonating with color index {colorIndex}!");
            }
        }

        // New Utility Helpers
        public List<InteractableObject> GetAllInteractables() => _interactables;

        public InteractableObject GetInteractableById(string id)
            => _interactables.Find(obj => obj.Id == id);

        public bool TryGetInteractable(string id, out InteractableObject obj)
        {
            obj = _interactables.Find(i => i.Id == id);
            return obj != null;
        }

        public void UpdateTransform(string id, Matrix4 transform)
        {
            var obj = _interactables.Find(i => i.Id == id);
            if (obj != null)
                obj.Transform = transform;
        }
    }
}
