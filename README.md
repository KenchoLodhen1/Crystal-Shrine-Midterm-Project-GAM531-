# 🧿 Crystal Shrine

Midterm Project GAM513 (OpenTK 3D Graphics)
Author: Kencho Lodhen
ID: 133572230
Section: NSA
Semester: Fall 2025
Date: October 31st 2025

------------------------------------------------------------------------

## 🏁 Overview

Crystal Shrine is a 3D interactive environment built using C#
(.NET 8) and OpenTK 4, featuring a mysterious floating island
that contains an ancient shrine.
The player can explore the island, activate elemental pillars, and
awaken a glowing crystal at the center.

The project demonstrates real-time 3D rendering, Phong lighting, camera
movement, collision systems, and object interaction mechanics.

------------------------------------------------------------------------

## 🎮 Gameplay Instructions

You are a wizard, find the crystal shrine in the center of the floating island. It is dark so be sure to
turn on your flashlight (press **F**). After locating the shrine, you must activate
the four elemental pillars around the crystal platform (press **E**). Once all pillars are
activated with the same element, approach the crystal to awaken it (press **E**). You can 
change the pillars elements by interacting with them again and awakening the crystal which
changes the crystal's color and light effects.

                # More Additional Controls Avaliable! #

------------------------------------------------------------------------

## ✨ Features

-   🌄 **Floating Island Terrain**
      Custom procedural mesh with height variation that has collision.
  
-   🏯 **Shrine, Props & Nature**
      Imported OBJ models for structures, huts,and pillars. Trees and rocks are randomly generated and rendered
      with rocks generating in different colors each time the game starts.

-   💎 **Crystal Activation Puzzle**
      Activate all 4 elemental pillars with the same element to awaken the center crystal. You can change
      Note that all the pillars must be the same and activated to trigger the crystal event.

-   💡 **Dynamic Lighting**
      Point lights and emissive materials for glowing crystal and pillars effects.

-   🔦 **Player Flashlight**
      Toggleable flashlight tied to camera direction with the player model following its direction 
      in third person.

-   🚶 **Camera Pov**
      Smooth movement, jumping, crouching and rotation via mouse input. Use scroll wheel to zoom in and out
      of third person view or first person view. While in third person view, hold right mouse to orbit camera around the player.

-   🪨 **Collision Detection**
      Prevents walking through rocks, trees and structures. Able to jump on top of the shrines platform and stand on it.

-   🌌 **Skybox System**
      Immersive background environment that smoothly handles all the sides so it doesn't have the square borders.

-   🤖 **Animation**
      Added animation to the model such as idle, sprinting, crouch, and jumping (in a T pose). The crystal when it's
      not activated moves horizontally like it's breathing and pulsing/spinning when its activated.

-   💨 **Free Roam/ Creative Mode**
      Pressing tab will toggle free roam mode where the player can fly around the island and explore without gravity or collision.
      Also in free roam mode the fog dissipates allowing for better viewing of the island and models.

-   ⚙️ **Interaction System**
      Press **E** to interact with pillars and crystal (once all pillars are activated with the same element) within a certain range.

------------------------------------------------------------------------

## 🕹️ Controls

  Key                 Action
  ------------------- ---------------------------------------
  **W / A / S / D**   Move Forward / Left / Back / Right
  **Space**           Jump
  **Shift**           Sprint
  **Ctrl**            Crouch
  **Mouse Move**      Look Around
  **Mouse Scroll**    Zoom In/Out
  **Right Mouse**     Orbit Camera (Third Person)
  **Tab**             Toggle Free Roam Mode
  **E**               Interact (Activate Pillars / Crystal)
  **F**               Toggle Flashlight
  **Esc**             Activate/Deactivate Cursor

------------------------------------------------------------------------

## 🧩 Puzzle Logic

-   There are **4 elemental pillars** around the platform.
-   Each pillar glows when activated.
-   Once all pillars are activated and are all the same element, the **central crystal** is ready
    to be awakened, once awaken it emits a radiant light and begins to pulse and spin.
-   This completes the shrine's awakening event.

------------------------------------------------------------------------

## 🧱 Technical Details

-   **Engine:** Custom engine built on **OpenTK 4**
-   **Language:** C# (.NET 8.0)
-   **Rendering:** OpenGL (VAOs, VBOs, EBOs)
-   **Lighting Model:** Phong with emissive and specular highlights
-   **Shaders:** GLSL (Vertex + Fragment)
-   **Assets:** `.obj` models and `.png` textures
-   **IDE:** Visual Studio

------------------------------------------------------------------------

## 🧰 How to Run

### Prerequisites

-   [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
-   Visual Studio 2022
-   OpenTK 4.x NuGet package installed
-   StbImageSharp NuGet package installed
-   System.Drawing.Common NuGet package installed

### Steps

1.  Clone or extract the project folder.\
2.  Open the `.sln` file in Visual Studio.\
3.  Set the startup project to **CrystalShrine**.\
4.  Press **F5** or run the project.

> ⚠️ Ensure the `Assets` folder remains in
> the root project directory. And also set the output directory to copy always
> for the all the assets and also shader files in Visual Studio to ensure they
> are available at runtime.

------------------------------------------------------------------------

## 📁 Project Structure

    CrystalShrine/
    │
    ├── Program.cs
    ├── Game.cs
    │
    ├── GL/
    │   ├── Mesh.cs
    │   ├── Shader.cs
    │   ├── Skybox.cs
    │   └── Texture.cs
    │
    ├── GLUtils/
    │   ├── Camera.cs
    │   ├── CollisionSystem.cs
    │   ├── FloatingIsland.cs
    │   ├── InteractableObjects.cs
    │   ├── OBJLoader.cs
    │   └── PlayerAnimator.cs
    │
    ├── Assets/
    │   ├── models/
    │   │   ├── nature/
    │   │   │   ├── rock_largeA.obj
    │   │   │   ├── rock_largeA.mtl
    │   │   │   ├── rock_largeB.obj
    │   │   │   ├── rock_largeB.mtl
    │   │   │   ├── rock_small.obj
    │   │   │   ├── rock_small.mtl
    │   │   │   ├── tree_small.obj
    │   │   │   ├── tree_small.mtl
    │   │   │   ├── tree_tall.obj
    │   │   │   └── tree_tall.mtl
    │   │   │
    │   │   ├── player/
    │   │   │   ├── monk_character.obj
    │   │   │   ├── monk_character.mtl
    │   │   │   └── texture_1.png
    │   │   │
    │   │   ├── props/
    │   │   │   ├── crystal/
    │   │   │   │   ├── scene3.obj
    │   │   │   │   ├── scene3.mtl
    │   │   │   │   └── 01_-_Default_baseColor.png
    │   │   │   │
    │   │   │   ├── hut/
    │   │   │   │   ├── scene.obj
    │   │   │   │   ├── scene.mtl
    │   │   │   │   ├── Ground_baseColor.png
    │   │   │   │   ├── Plants_baseColor.png
    │   │   │   │   ├── TileSet1_baseColor.png
    │   │   │   │   ├── TileSet2_baseColor.png
    │   │   │   │   ├── TileSet3_baseColor.png
    │   │   │   │   ├── TileSet4_baseColor.png
    │   │   │   │   ├── TileSet5_baseColor.png
    │   │   │   │   └── TileSet6_baseColor.png
    │   │   │   │
    │   │   │   ├── pillars/
    │   │   │   │   ├── scene2.obj
    │   │   │   │   ├── scene2.mtl
    │   │   │   │   ├── Metaltexture01_normal.png
    │   │   │   │   └── Metaltexture01_metallicRoughness.png
    │   │   │   │
    │   │   │   └── platform/
    │   │   │       ├── platform.obj
    │   │   │       └── platform.mtl
    │   │
    │   └── textures/
    │       ├── grass.png
    │       └── sky.png
    │
    ├── Shaders/
    │   ├── vertex.glsl
    │   ├── fragment.glsl
    │   ├── skybox_vertex.glsl
    │   ├── skybox_fragment.glsl
    │   └── crystal_fragment.glsl
    │
    ├── .gitignore
    └── README.md

------------------------------------------------------------------------

## 🧠 Learning Outcomes

This project demonstrates: - Implementing a 3D rendering pipeline in
OpenTK.
- Loading and transforming OBJ models.
- Managing lighting and shader uniform data.
- Handling camera input and real-time interactions.
- Building modular systems for collisions and interactions.

------------------------------------------------------------------------

## 🧩 Assets & Credits

### 🖼️ Textures
| Type | Source |
|------|---------|
| **Skybox (Night Sky)** | [ambientCG – NightSkyHDRI014](https://ambientcg.com/view?id=NightSkyHDRI014) |
| **Grass / Ground Texture** | [A Painting for the Artist – Free CC0 Grass Mud Leaves Texture](https://www.apaintingfortheartist.com/2023/07/20/a-free-cc0-public-domain-grass-mud-leaves-ground-foliage-photoshop-texture-and-3d-models-texture) |

------------------------------------------------------------------------

### 🗿 3D Models
| Model | Description | Source |
|--------|--------------|--------|
| **Nature Kit** | Trees, rocks, and environment assets | [Kenney.nl – Nature Kit](https://kenney.nl/assets/nature-kit) |
| **Player Character** | Monk 3D character model | [Sketchfab – Monk Character](https://sketchfab.com/3d-models/monk-character-8cacbd85a5b84f59a8c9000d7a6dcca2) |
| **Hut / Tavern** | Wooden building model used for props | [Sketchfab – Le Tonneau Tavern](https://sketchfab.com/3d-models/le-tonneau-tavern-c5d10210bd644c2c85e15e9bb219ef4c) |
| **Pillar Model** | Decorative medieval/sci-fi pillar used in puzzle | [Sketchfab – Medieval Sci-Fi Pillar](https://sketchfab.com/3d-models/medieval-sci-fi-pillar-60c1b1f74df24898a54968ec3008fc63) |
| **Crystal Platform** | Shrine base / temple ruins structure | [Sketchfab – Temple Ruins](https://sketchfab.com/3d-models/temple-ruins-6b3eb4e27e03485a886ce5304e95f897) |
| **Crystal Model** | Central magic crystal for the puzzle | [Sketchfab – Magic Diamond](https://sketchfab.com/3d-models/magic-diamond-6d3498168d5249d4a837f5a12bad69f3) |

------------------------------------------------------------------------

All assets are used under **free educational and CC0 licenses** for academic purposes.

------------------------------------------------------------------------

## 📜 Academic Integrity

This project represents my **own work** in accordance with **Seneca
Academic Policy**.
All imported assets are properly credited and used under free
educational licenses.

------------------------------------------------------------------------

### "Awaken the Crystal. Illuminate the Shrine."
