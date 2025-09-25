# Copilot Instructions for dev-sandbox (Godot 4)

## Project Overview
- **Engine:** Godot 4.x, 3D game prototype/sandbox.
- **Main Directories:**
  - `Player/`: Player character scripts, shaders, and scene.
  - `Outlines/`: Outline/highlight logic, materials, and scripts for 3D selection effects.
  - `Levels/`: Level scenes (e.g., `sandbox_level.tscn`).
  - `Tutorials used/`: References and workflow notes.

## Key Patterns & Architecture
- **Player Movement:**
  - `Player/player.gd` implements advanced 3D movement (jump, dash, coyote time, camera control) using Godot's `CharacterBody3D`.
  - Camera pivots and spring arm are used for 3rd-person camera.
  - Movement and camera settings are exposed via `@export` for easy tuning in the editor.
- **Outline/Selection System:**
  - `Outlines/select_highlight.gd` manages mouse-over and selection highlighting for 3D objects.
  - Uses Godot's `material_overlay` to swap outline/selection materials.
  - Connects to `PhysicsBody3D` signals for mouse events.
  - Materials for outlines are in `Outlines/` (e.g., `OutlineMaterial.tres`).
- **Input:**
  - Custom input actions are defined in `project.godot` under `[input]` (e.g., `ui_left`, `ui_right`, `ui_up`, `ui_down`, `scroll_forward`, `scroll_backward`).
  - Supports both keyboard and gamepad.

## Developer Workflows
- **Scene Editing:**
  - Use Godot Editor to modify scenes (`.tscn`) and tune exported variables.
- **Running the Game:**
  - Launch via Godot Editor (no custom build scripts).
- **Adding Outlines:**
  - Reference the YouTube tutorial in `Tutorials used/Links.txt` for outline setup.
  - When exporting meshes from Blender, apply the Weighted Normals modifier to avoid shading issues.

## Project Conventions
- **Exported Variables:** Use `@export` for all tunable gameplay/camera parameters.
- **Node Paths:** Use `@onready var` for node references, matching scene hierarchy.
- **Materials:** Store all outline/selection materials in `Outlines/`.
- **Tutorial References:** Keep workflow notes and links in `Tutorials used/Links.txt`.

## Integration & Extensibility
- **To add new selectable objects:**
  - Attach `select_highlight.gd` to a `Node3D` with a `PhysicsBody3D` child.
  - Assign `mesh`, `outline_material`, `selected_material`, and `body` in the inspector.
- **To extend player movement:**
  - Modify or add exported variables in `player.gd`.
  - Use Godot's signal system for cross-component communication.

---

For more details, see `Player/player.gd`, `Outlines/select_highlight.gd`, and `Tutorials used/Links.txt`.
