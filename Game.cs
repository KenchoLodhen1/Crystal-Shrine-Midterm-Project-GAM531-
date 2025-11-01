using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.IO;
using System.Collections.Generic;

namespace CrystalShrine
{
    public class Game : GameWindow
    {
        private GLUtils.Shader _shader;
        private GLUtils.Camera _camera;
        private GLUtils.Texture _floorTexture;
        private GLUtils.Skybox _skybox;
        private GLUtils.FloatingIsland _islandMesh;
        private GLUtils.OBJModel _player;
        private GLUtils.Texture _playerTexture;
        private GLUtils.PlayerAnimator _playerAnimator;
        private GLUtils.CollisionSystem _collisionSystem;
        private GLUtils.Shader _crystalShader;

        private GLUtils.InteractionSystem _interactionSystem;
        private GLUtils.InteractableObject _currentLookTarget = null;
        private bool _wasEPressed = false;

        private int _activePillars = 0;
        private bool _crystalAwakened = false;

        private Matrix4 _model;
        private Matrix4 _view;
        private Matrix4 _projection;

        private Vector2 _lastMousePos;
        private bool _firstMove = true;

        private float _elapsedTime = 0f;

        private List<(GLUtils.Mesh mesh, Vector3 color, Matrix4 transform, bool useVertexColors)> _objects;

        // Multi-material models
        private List<(GLUtils.OBJModel model, Matrix4 transform)> _multiMaterialObjects;

        public Game(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(0.05f, 0.05f, 0.1f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../"));

            // Initialize collision system
            _collisionSystem = new GLUtils.CollisionSystem();

            // Initialize interaction system
            _interactionSystem = new GLUtils.InteractionSystem();

            // Initialize objects lists
            _objects = new List<(GLUtils.Mesh, Vector3, Matrix4, bool)>();
            _multiMaterialObjects = new List<(GLUtils.OBJModel, Matrix4)>();

            // Skybox
            var skyTex = Path.Combine(basePath, "Assets", "textures", "sky.png");
            _skybox = new GLUtils.Skybox(
                skyTex,
                Path.Combine(basePath, "Shaders", "skybox_vertex.glsl"),
                Path.Combine(basePath, "Shaders", "skybox_fragment.glsl")
            );

            // Shader
            _shader = new GLUtils.Shader(
                Path.Combine(basePath, "Shaders", "vertex.glsl"),
                Path.Combine(basePath, "Shaders", "fragment.glsl")
            );
            _shader.Use();

            // Crystal shader
            _crystalShader = new GLUtils.Shader(
                Path.Combine(basePath, "Shaders", "vertex.glsl"),
                Path.Combine(basePath, "Shaders", "crystal_fragment.glsl")
            );

            // Player model and texture
            string playerModelPath = Path.Combine(basePath, "Assets", "models", "player", "monk_character.obj");

            Console.WriteLine($"Loading player from: {playerModelPath}");
            _player = GLUtils.OBJLoader.Load(playerModelPath);

            // Texture from MTL first
            if (_player.SubMeshes.Count > 0 && _player.SubMeshes[0].Material.Texture != null)
            {
                Console.WriteLine($"Using texture from MTL for player");
                _playerTexture = _player.SubMeshes[0].Material.Texture;
            }
            else
            {
                // Fallback to texture path
                string playerTexPath = Path.Combine(basePath, "Assets", "models", "player", "texture_1.png");
                Console.WriteLine($"Using fallback texture: {playerTexPath}");

                if (File.Exists(playerTexPath))
                {
                    _playerTexture = new GLUtils.Texture(playerTexPath);
                }
                else
                {
                    Console.WriteLine("[ERROR] Player texture not found!");
                }
            }

            // Initialize player animator
            _playerAnimator = new GLUtils.PlayerAnimator();

            // Island
            _islandMesh = new GLUtils.FloatingIsland(resolution: 240, size: 224f, height: 2f);
            _model = Matrix4.CreateScale(1.2f) * Matrix4.CreateTranslation(0f, -3.5f, 0f);

            // Grass texture
            var texturePath = Path.Combine(basePath, "Assets", "textures", "grass.png");
            _floorTexture = new GLUtils.Texture(texturePath);
            _shader.SetInt("texture0", 0);

            // Load multi-material prop model (hut)
            string propPath = Path.Combine(basePath, "Assets", "models", "props", "hut", "scene.obj");

            if (File.Exists(propPath))
            {
                var prop = GLUtils.OBJLoader.Load(propPath);

                if (prop != null)
                {
                    float y = _islandMesh.GetHeightAt(-70f, 5f) - 3.8f;
                    Vector3 position = new Vector3(-50f, y, 70f);

                    // Transform and rotation
                    Matrix4 propTransform =
                        Matrix4.CreateScale(0.05f) *
                        Matrix4.CreateRotationY(MathHelper.DegreesToRadians(90f)) *
                        Matrix4.CreateTranslation(position);

                    _multiMaterialObjects.Add((prop, propTransform));

                    // === Add multiple colliders for the hut ===
                    float hutCollisionScale = 1.3f;

                    // Main building body
                    _collisionSystem.AddObject(position + new Vector3(0f, 2f, 0f), 16f * hutCollisionScale, "hut");

                    // Porch / stairs
                    _collisionSystem.AddObject(position + new Vector3(5f, 1f, 5f), 5f * hutCollisionScale, "hut");

                    // Back wall / extension
                    _collisionSystem.AddObject(position + new Vector3(-4f, 2f, -3f), 12f * hutCollisionScale, "hut");

                    // Side barrels / crates
                    _collisionSystem.AddObject(position + new Vector3(2f, 1f, -5f), 2f * hutCollisionScale, "hut");

                    Console.WriteLine($"[PROP] Hut placed at {position} with 4 collision spheres");
                }
            }
            else
            {
                Console.WriteLine("[WARN] Scene model not found at " + propPath);
            }

            // Load Platform (center)
            string platformPath = Path.Combine(basePath, "Assets", "models", "props", "platform", "platform.obj");
            if (File.Exists(platformPath))
            {
                Console.WriteLine("[PROP] Loading platform from: " + platformPath);
                var platformModel = GLUtils.OBJLoader.Load(platformPath);
                if (platformModel != null)
                {
                    Console.WriteLine($"[PROP] Platform model loaded with {platformModel.SubMeshes.Count} submeshes");

                    // Center position
                    Vector3 centerPos = new Vector3(0f, 0f, 30f);
                    float y = _islandMesh.GetHeightAt(centerPos.X, centerPos.Z) - 3.8f;
                    Vector3 platformPosition = new Vector3(centerPos.X, y, centerPos.Z);

                    float platformScale = 6.0f;

                    Matrix4 platformTransform =
                        Matrix4.CreateScale(platformScale) *
                        Matrix4.CreateRotationY(MathHelper.DegreesToRadians(0f)) *
                        Matrix4.CreateTranslation(platformPosition);

                    _multiMaterialObjects.Add((platformModel, platformTransform));

                    // Add collision for platform
                    Vector3 platformSize = new Vector3(30f, 3.5f, 30f);
                    _collisionSystem.AddPlatform(platformPosition, platformSize, "platform");

                    Console.WriteLine($"[PROP] Platform placed at center: {platformPosition}");
                }
                else
                {
                    Console.WriteLine("[ERROR] Platform model failed to load!");
                }
            }
            else
            {
                Console.WriteLine("[WARN] Platform model not found at: " + platformPath);
            }

            // Load Crystal (center of platform)
            string crystalPath = Path.Combine(basePath, "Assets", "models", "props", "crystal", "scene3.obj");
            if (File.Exists(crystalPath))
            {
                Console.WriteLine("[PROP] Loading crystal from: " + crystalPath);
                var crystalModel = GLUtils.OBJLoader.Load(crystalPath);

                if (crystalModel != null)
                {
                    Console.WriteLine($"[PROP] Crystal model loaded with {crystalModel.SubMeshes.Count} submeshes");

                    // Platform center position
                    Vector3 platformCenter = new Vector3(0f, 0f, 30f);
                    float platformY = _islandMesh.GetHeightAt(platformCenter.X, platformCenter.Z) - 3.8f;

                    // Place crystal above platform
                    float crystalHeightOffset = 5.5f;
                    Vector3 crystalPosition = new Vector3(platformCenter.X, platformY + crystalHeightOffset, platformCenter.Z);

                    float crystalScale = 10.0f;
                    float crystalRotation = 0f;

                    Matrix4 crystalTransform =
                        Matrix4.CreateScale(crystalScale) *
                        Matrix4.CreateRotationY(MathHelper.DegreesToRadians(crystalRotation)) *
                        Matrix4.CreateTranslation(crystalPosition);

                    // Store the index where crystal will be in the list
                    int crystalModelIndex = _multiMaterialObjects.Count;
                    _multiMaterialObjects.Add((crystalModel, crystalTransform));

                    // Mark this as a crystal for special rendering
                    _interactionSystem.AddInteractable(
                        "crystal_center",
                        "crystal",
                        crystalPosition,
                        5.0f,
                        new Vector3(0.4f, 0.8f, 1.0f),
                        crystalModelIndex
                    );

                    _interactionSystem.GetInteractableById("crystal_center").BasePosition = crystalPosition;

                    // Collision for crystal
                    _collisionSystem.AddObject(crystalPosition, 2.0f, "crystal");

                    Console.WriteLine($"[PROP] Crystal placed at center: {crystalPosition}");
                }
                else
                {
                    Console.WriteLine("[ERROR] Crystal model failed to load!");
                }
            }
            else
            {
                Console.WriteLine("[WARN] Crystal model not found at: " + crystalPath);
            }

            // Load 4 Pillars at corners of platform
            string pillarsPath = Path.Combine(basePath, "Assets", "models", "props", "pillars", "scene2.obj");
            if (File.Exists(pillarsPath))
            {
                Console.WriteLine("[PROP] Loading pillars from: " + pillarsPath);
                var pillarsModel = GLUtils.OBJLoader.Load(pillarsPath);

                if (pillarsModel != null)
                {
                    Console.WriteLine($"[PROP] Pillars model loaded with {pillarsModel.SubMeshes.Count} submeshes");

                    // Platform center and dimensions (matching the platform collision box)
                    Vector3 platformCenter = new Vector3(0f, 0f, 30f);
                    float platformWidth = 25f;
                    float platformDepth = 25f;

                    // Calculate the actual platform Y position (same as platform placement)
                    float platformY = _islandMesh.GetHeightAt(platformCenter.X, platformCenter.Z) - 5f;

                    // Distance from center to pillar position
                    float pillarDistanceFromCenter = 18f;
                    float halfWidth = pillarDistanceFromCenter;
                    float halfDepth = pillarDistanceFromCenter;

                    // 4 corners at the exact edges of the platform
                    Vector3[] offsets =
                    {
                        new Vector3(-halfWidth, 0f, -halfDepth),  // Southwest corner
                        new Vector3( halfWidth, 0f, -halfDepth),  // Southeast corner
                        new Vector3(-halfWidth, 0f,  halfDepth),  // Northwest corner
                        new Vector3( halfWidth, 0f,  halfDepth),  // Northeast corner
                    };

                    float pillarScale = 2.0f;
                    float colliderRadius = 2.5f;

                    for (int i = 0; i < offsets.Length; i++)
                    {
                        float worldX = platformCenter.X + offsets[i].X;
                        float worldZ = platformCenter.Z + offsets[i].Z;

                        Vector3 pos = new Vector3(worldX, platformY, worldZ);

                        Console.WriteLine($"[PROP] Placing pillar {i} at X:{worldX:F1}, Y:{platformY:F1}, Z:{worldZ:F1}");

                        float rotationAngle = 0f;
                        switch (i)
                        {
                            case 0: rotationAngle = 45f; break;
                            case 1: rotationAngle = 135f; break;
                            case 2: rotationAngle = -45f; break;
                            case 3: rotationAngle = -135f; break;
                        }

                        Matrix4 t =
                            Matrix4.CreateScale(pillarScale) *
                            Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotationAngle)) *
                            Matrix4.CreateTranslation(pos);

                        // Store the index where this pillar will be in the list
                        int modelIndex = _multiMaterialObjects.Count;
                        _multiMaterialObjects.Add((pillarsModel, t));

                        // Add collision
                        _collisionSystem.AddObject(pos + new Vector3(0f, 3f, 0f), colliderRadius, "pillar");

                        // Add as interactable with initial cyan neon color (from MTL)
                        Vector3 interactPosition = pos + new Vector3(0f, 3f, 0f);
                        _interactionSystem.AddInteractable(
                            $"pillar_{i}",
                            "pillar",
                            interactPosition,
                            5.0f,
                            new Vector3(0.58f, 1.0f, 1.0f),
                            modelIndex
                        );
                    }

                    Console.WriteLine($"[PROP] Placed 4 pillars at corners of platform");
                }
                else
                {
                    Console.WriteLine("[ERROR] Pillars model failed to load!");
                }
            }
            else
            {
                Console.WriteLine("[WARN] Pillars model not found at: " + pillarsPath);
            }

            // Nature objects with collision detection
            string modelPath = Path.Combine(basePath, "Assets", "models", "nature");

            var natureAssets = new (string obj, float scale, int count, float collisionRadius, bool randomizeColor)[]
            {
                ("tree_small.obj", 6f, 200, 2.0f, false),
                ("tree_tall.obj", 8f, 175, 2.5f, false),
                ("rock_largeA.obj", 5f, 160, 3.0f, true),
                ("rock_largeB.obj", 5f, 160, 3.0f, true),
                ("rock_small.obj", 3.5f, 200, 1.5f, true),
            };

            // Rock color palette
            Vector3[] rockColors = new Vector3[]
            {
                new Vector3(0.5f, 0.5f, 0.5f),   // Gray
                new Vector3(0.4f, 0.4f, 0.4f),   // Dark gray
                new Vector3(0.6f, 0.6f, 0.6f),   // Light gray
                new Vector3(0.6f, 0.4f, 0.2f),   // Brown
                new Vector3(0.7f, 0.5f, 0.3f),   // Light brown
                new Vector3(0.45f, 0.35f, 0.25f) // Dark brown
            };

            // Exclusion zone around platform
            Vector3 exclusionCenter = new Vector3(0f, 0f, 30f);
            float exclusionRadius = 35f;

            Random rng = new Random();
            foreach (var asset in natureAssets)
            {
                string objPath = Path.Combine(modelPath, asset.obj);

                if (!File.Exists(objPath))
                {
                    Console.WriteLine($"[WARN] Missing model: {objPath}");
                    continue;
                }

                var model = GLUtils.OBJLoader.Load(objPath);
                Console.WriteLine($"[NATURE] {asset.obj} - Color: R:{model.DiffuseColor.X:F2} G:{model.DiffuseColor.Y:F2} B:{model.DiffuseColor.Z:F2}");

                int placedCount = 0;
                int attempts = 0;
                int maxAttempts = asset.count * 20;

                while (placedCount < asset.count && attempts < maxAttempts)
                {
                    attempts++;

                    // Match spawn bounds to actual island scale
                    float halfSize = _islandMesh.GetHalfSize();
                    float scaleFactor = _model.M11;
                    float spawnMargin = 10f;
                    float spawnRange = (halfSize * scaleFactor) - spawnMargin;

                    float x = (float)(rng.NextDouble() * spawnRange * 2f - spawnRange);
                    float z = (float)(rng.NextDouble() * spawnRange * 2f - spawnRange);

                    // Check distance from exclusion center
                    float distanceFromCenter = MathF.Sqrt(
                        MathF.Pow(x - exclusionCenter.X, 2) +
                        MathF.Pow(z - exclusionCenter.Z, 2)
                    );

                    // Skip if too close to exclusion center
                    if (distanceFromCenter < exclusionRadius)
                    {
                        continue;
                    }

                    float y = _islandMesh.GetHeightAt(x, z) - 3.5f;

                    if (y > -10f)
                    {
                        float rotation = (float)(rng.NextDouble() * MathF.PI * 2f);
                        float scale = asset.scale * (0.8f + (float)rng.NextDouble() * 0.4f);
                        float collisionRadius = asset.collisionRadius * (scale / asset.scale);

                        Vector3 position = new Vector3(x, y, z);

                        // Check for collisions with existing objects
                        if (!_collisionSystem.CheckCollision(position, collisionRadius))
                        {
                            Matrix4 transform =
                                Matrix4.CreateScale(scale) *
                                Matrix4.CreateRotationY(rotation) *
                                Matrix4.CreateTranslation(position);

                            if (asset.randomizeColor)
                            {
                                // Randomize entire rock color once
                                Vector3 randomColor = rockColors[rng.Next(rockColors.Length)];

                                foreach (var submesh in model.SubMeshes)
                                {
                                    _objects.Add((submesh.Mesh, randomColor, transform, false));
                                }
                            }
                            else
                            {
                                // Keep each submesh's material color (for trees)
                                foreach (var submesh in model.SubMeshes)
                                {
                                    _objects.Add((submesh.Mesh, submesh.Material.DiffuseColor, transform, true));
                                }
                            }

                            _collisionSystem.AddObject(position, collisionRadius, asset.obj);
                            placedCount++;
                        }
                    }
                }
            }

            Console.WriteLine($"[NATURE] Spawning complete with {exclusionRadius}u exclusion radius around platform");

            // Camera
            _camera = new GLUtils.Camera(new Vector3(0f, 0f, 120f));
            _camera.SetTerrain(_islandMesh);
            _camera.SetCollisionSystem(_collisionSystem);
            CursorState = CursorState.Grabbed;
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            _elapsedTime += (float)args.Time;

            // Crystal Animation
            if (_interactionSystem.TryGetInteractable("crystal_center", out var crystal))
            {
                if (!_crystalAwakened)
                {
                    // Breathing motion (slow vertical up/down)
                    float hoverHeight = (float)Math.Sin(_elapsedTime * 2f) * 1f
                    + (float)Math.Sin(_elapsedTime * 0.7f) * 0.1f;
                    Matrix4 baseTransform =
                        Matrix4.CreateScale(8.5f) *
                        Matrix4.CreateTranslation(crystal.BasePosition + new Vector3(0f, hoverHeight, 0f));

                    _interactionSystem.UpdateTransform("crystal_center", baseTransform);
                }
                else
                {
                    // Spinning when activated
                    float rotationSpeed = 1.5f;
                    Matrix4 spinTransform =
                        Matrix4.CreateScale(8.5f) *
                        Matrix4.CreateRotationY(_elapsedTime * rotationSpeed) *
                        Matrix4.CreateTranslation(crystal.BasePosition);

                    _interactionSystem.UpdateTransform("crystal_center", spinTransform);
                }
            }

            if (!IsFocused) return;

            if (KeyboardState.IsKeyPressed(Keys.Escape))
            {
                CursorState = CursorState == CursorState.Grabbed ? CursorState.Normal : CursorState.Grabbed;
                _firstMove = true;
            }

            if (KeyboardState.IsKeyPressed(Keys.Tab))
            {
                _camera.IsFlying = !_camera.IsFlying;
                Console.WriteLine($"Flight mode: {(_camera.IsFlying ? "ON" : "OFF")}");
            }

            if (KeyboardState.IsKeyPressed(Keys.F))
            {
                _camera.FlashlightOn = !_camera.FlashlightOn;
                Console.WriteLine($"Flashlight: {(_camera.FlashlightOn ? "ON" : "OFF")}");
            }

            // Right-click orbit mode (only in 3rd person)
            if (_camera.ZoomDistance > 1.5f)
            {
                _camera.IsOrbiting = MouseState.IsButtonDown(MouseButton.Right);
            }
            else
            {
                _camera.IsOrbiting = false;
            }

            _camera.UpdateKeyboard(KeyboardState, (float)args.Time);

            // scroll wheel zoom
            float scrollDelta = MouseState.ScrollDelta.Y;
            if (Math.Abs(scrollDelta) > 0.01f)
                _camera.UpdateScroll(scrollDelta);

            // Check what the player is looking at
            _currentLookTarget = _interactionSystem.GetLookedAtInteractable(
                _camera.Position,
                _camera.Front,
                10f
            );

            // Handle E key interaction
            bool isEPressed = KeyboardState.IsKeyDown(Keys.E);
            if (isEPressed && !_wasEPressed && _currentLookTarget != null)
            {
                var target = _currentLookTarget;

                // Let the system handle activation and glow fade logic
                _interactionSystem.HandleInteraction(target, ref _activePillars, ref _crystalAwakened);

                // Debug info
                if (target.Type == "pillar")
                    Console.WriteLine($"[DEBUG] Interacted with {target.Id} (pillar). Active pillars: {_activePillars}/4");

                if (target.Type == "crystal" && _crystalAwakened)
                    Console.WriteLine("[DEBUG] Crystal interaction triggered — it's glowing now!");
            }

            _wasEPressed = isEPressed;

            // Terrain collision
            if (!_camera.IsFlying)
            {
                float terrainHeight = _islandMesh.GetHeightAt(_camera.Position.X, _camera.Position.Z);
                float groundLevel = terrainHeight - 3.5f + _camera.CurrentHeight;

                // Check if standing on a platform
                float? platformHeight = _collisionSystem.GetPlatformHeightAt(_camera.Position);

                // Use platform height if it's higher than terrain and player is close to it
                if (platformHeight.HasValue)
                {
                    float platformGroundLevel = platformHeight.Value + _camera.CurrentHeight;

                    // Only snap to platform if player is falling onto it or already on it
                    if (_camera.Position.Y <= platformGroundLevel + 2f && platformGroundLevel > groundLevel)
                    {
                        groundLevel = platformGroundLevel;
                    }
                }

                if (_camera.Position.Y <= groundLevel)
                {
                    _camera.Position = new Vector3(_camera.Position.X, groundLevel, _camera.Position.Z);
                    _camera.Velocity = new Vector3(_camera.Velocity.X, 0f, _camera.Velocity.Z);
                    _camera.IsGrounded = true;
                }
                else _camera.IsGrounded = false;
            }

            // Update player animations
            _playerAnimator.Update(
                (float)args.Time,
                _camera.Velocity,
                _camera.IsGrounded,
                _camera.IsCrouching,
                !_camera.IsGrounded
            );

            // Mouse look
            var mouse = MouseState;
            if (_firstMove)
            {
                _lastMousePos = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
            }
            else if (CursorState == CursorState.Grabbed)
            {
                var deltaX = mouse.X - _lastMousePos.X;
                var deltaY = mouse.Y - _lastMousePos.Y;
                _lastMousePos = new Vector2(mouse.X, mouse.Y);
                _camera.UpdateMouse(deltaX, deltaY);
            }

            // Smoothly update all emissive transitions
            foreach (var obj in _interactionSystem.GetAllInteractables())
            {
                obj.Update((float)args.Time);
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _projection = _camera.GetProjectionMatrix(Size.X / (float)Size.Y);
            _view = _camera.GetViewMatrix();

            _skybox.Draw(_view, _projection);

            _shader.Use();
            _shader.SetVector3("emissiveColor", Vector3.Zero);
            _shader.SetMatrix4("view", _view);
            _shader.SetMatrix4("projection", _projection);
            _shader.SetVector3("viewPos", _camera.Position);
            _shader.SetBool("isFlying", _camera.IsFlying);

            // Flashlight direction uses camera look direction
            _shader.SetBool("flashlightOn", _camera.FlashlightOn && !_camera.IsOrbiting);
            _shader.SetVector3("flashlightPos", _camera.Position);
            _shader.SetVector3("flashlightDir", _camera.Front);
            _shader.SetFloat("flashlightIntensity", 4.0f);

            // Draw island terrain with texture
            _shader.SetBool("useTexture", true);
            _shader.SetBool("useVertexColors", false);
            _shader.SetMatrix4("model", _model);
            _shader.SetInt("texture0", 0);
            _floorTexture.Use(TextureUnit.Texture0);
            _islandMesh.Draw();

            // Draw nature objects
            _shader.SetBool("useTexture", false);
            foreach (var (mesh, color, transform, useVertexColors) in _objects)
            {
                _shader.SetMatrix4("model", transform);
                _shader.SetBool("useVertexColors", useVertexColors);

                if (!useVertexColors)
                    _shader.SetVector3("objectColor", color);

                mesh.Draw();
            }

            _shader.SetBool("useTexture", true);
            _shader.SetBool("useVertexColors", false);

            // Set up dynamic point lights from pillars and crystal
            var lightSources = _interactionSystem.GetAllInteractables()
                .FindAll(obj => obj.Type == "pillar" || obj.Type == "crystal");

            _shader.SetInt("numPointLights", lightSources.Count);

            for (int i = 0; i < lightSources.Count; i++)
            {
                var light = lightSources[i];
                _shader.SetVector3($"pointLightPositions[{i}]", light.Position);
                _shader.SetVector3($"pointLightColors[{i}]", light.CurrentEmissiveColor);

                // Crystal gets higher intensity than pillars
                float intensity = light.Type == "crystal" ? 8.0f : 5.0f;
                _shader.SetFloat($"pointLightIntensities[{i}]", intensity);
            }

            // Disable backface culling for platform (renders both sides)
            GL.Disable(EnableCap.CullFace);

            // Draw multi-material objects (hut, platform, pillars, crystal)
            for (int i = 0; i < _multiMaterialObjects.Count; i++)
            {
                var (model, transform) = _multiMaterialObjects[i];
                _shader.SetMatrix4("model", transform);

                var interactable = _interactionSystem.GetAllInteractables()
                    .Find(obj => obj.ModelIndex == i);

                bool isCrystal = interactable != null && interactable.Type == "crystal";
                bool isPillar = interactable != null && interactable.Type == "pillar";

                if (isCrystal)
                    continue;

                foreach (var submesh in model.SubMeshes)
                {
                    bool isNeonMaterial = submesh.Material.EmissiveColor.Length > 0.1f;
                    bool hasEmissive = submesh.Material.EmissiveColor.Length > 0.001f;

                    if (isPillar && isNeonMaterial)
                    {
                        _shader.SetVector3("emissiveColor", interactable.CurrentEmissiveColor * 5.0f);
                    }
                    else
                    {
                        _shader.SetVector3("emissiveColor", submesh.Material.EmissiveColor * 5.0f);
                    }

                    if (submesh.Material.Texture != null)
                    {
                        _shader.SetBool("useTexture", true);
                        _shader.SetInt("texture0", 0);
                        submesh.Material.Texture.Use(TextureUnit.Texture0);
                    }
                    else
                    {
                        _shader.SetBool("useTexture", false);
                        _shader.SetBool("useVertexColors", true);
                    }

                    submesh.Mesh.Draw();
                }
            }

            // Draw the crystal with special shader
            var crystal = _interactionSystem.GetAllInteractables()
                .Find(obj => obj.Type == "crystal");

            if (crystal != null && crystal.ModelIndex >= 0 && crystal.ModelIndex < _multiMaterialObjects.Count)
            {
                var (crystalModel, _) = _multiMaterialObjects[crystal.ModelIndex];
                Matrix4 crystalTransform = crystal.Transform;

                _crystalShader.Use();

                GL.Enable(EnableCap.Blend);
                GL.DepthMask(false);
                GL.Enable(EnableCap.DepthTest);

                _crystalShader.SetMatrix4("view", _view);
                _crystalShader.SetMatrix4("projection", _projection);
                _crystalShader.SetMatrix4("model", crystalTransform);
                _crystalShader.SetVector3("viewPos", _camera.Position);
                _crystalShader.SetVector3("lightPos", new Vector3(0f, 100f, 0f));
                _crystalShader.SetVector3("lightColor", new Vector3(1f, 1f, 1f));

                // 1️⃣ Draw translucent crystal body (no texture)
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                _crystalShader.SetBool("useTexture", false);
                _crystalShader.SetVector3("baseColor", new Vector3(0.4f, 0.8f, 1.0f));
                _crystalShader.SetFloat("transparency", 0.6f);
                _crystalShader.SetFloat("shininess", 64.0f);
                _crystalShader.SetFloat("glowStrength", 0.3f);

                foreach (var sub in crystalModel.SubMeshes)
                {
                    string matName = sub.Material?.Name ?? "";
                    if (matName == "01_-_Default") continue;
                    sub.Mesh.Draw();
                }

                // 2️⃣ Draw the glowing rune ring (texture plane)
                var ring = crystalModel.SubMeshes.Find(s => (s.Material?.Name ?? "") == "01_-_Default");
                if (ring != null)
                {
                    // Additive blend for glow overlay
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);

                    _crystalShader.SetBool("useTexture", true);
                    _crystalShader.SetInt("texture0", 0);
                    ring.Material.Texture.Use(TextureUnit.Texture0);

                    _crystalShader.SetVector3("baseColor", new Vector3(0.8f, 1.0f, 1.0f));
                    _crystalShader.SetFloat("transparency", 0.0f);
                    _crystalShader.SetFloat("glowStrength", 1.2f);

                    ring.Mesh.Draw();
                }

                // Restore normal render state
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                GL.DepthMask(true);
                GL.Disable(EnableCap.Blend);

                _crystalShader.SetMatrix4("view", _view);
                _crystalShader.SetMatrix4("projection", _projection);
                _crystalShader.SetMatrix4("model", crystalTransform);
                _crystalShader.SetVector3("viewPos", _camera.Position);
                _crystalShader.SetVector3("lightPos", new Vector3(0f, 100f, 0f));
                _crystalShader.SetVector3("lightColor", new Vector3(1f, 1f, 1f));

                // Add emissive glow to crystal
                _crystalShader.SetVector3("emissiveColor", crystal.CurrentEmissiveColor * 1.0f);
            }

            // Re-enable backface culling for other objects
            GL.Enable(EnableCap.CullFace);

            // Reset emissive for player
            _shader.SetVector3("emissiveColor", Vector3.Zero);

            // Draw player model in 3rd person with animations
            if (_camera.ZoomDistance > 1.5f)
            {
                _shader.Use();
                _shader.SetBool("useTexture", true);
                _shader.SetBool("useVertexColors", false);

                // Reset all lighting to normal levels
                _shader.SetVector3("emissiveColor", Vector3.Zero);
                _shader.SetBool("flashlightOn", _camera.FlashlightOn);
                _shader.SetBool("isFlying", false);
                _shader.SetInt("numPointLights", 0);
                _shader.SetFloat("flashlightIntensity", 4.0f);

                Vector3 playerPosition = _camera.GetPlayerPosition();
                float playerY = playerPosition.Y - _camera.CurrentHeight + 0.5f;
                float playerYaw = -_camera.GetPlayerYaw() + MathF.PI / 2f;

                Matrix4 playerTransform = _playerAnimator.GetAnimatedTransform(
                    new Vector3(playerPosition.X, playerY, playerPosition.Z),
                    playerYaw,
                    1.5f
                );

                _shader.SetMatrix4("model", playerTransform);
                _shader.SetInt("texture0", 0);
                _playerTexture.Use(TextureUnit.Texture0);

                _player.Mesh.Draw();
            }

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            _islandMesh?.Dispose();
        }
    }
}
