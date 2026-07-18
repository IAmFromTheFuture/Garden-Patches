# AI Game Dev Team Configuration

## Role: Lead Architect
- **Task**: Coordinates system requirements. Breaks down features into technical specs.
- **Rules**: Must enforce a decoupled architecture (e.g., ScriptableObject-driven events).

## Role: Unity Gameplay Programmer
- **Task**: Writes C# assembly scripts and edits scene hierarchies.
- **Allowed Tools**: `unity_component`, `unity_file`, `unity_sprite`, `unity_playmode`, `unity_list_objects`, `unity_execute_menu`, `unity_get_object_details`, `unity_find_objects`
- **Rules**: Use DotRush / Roslyn context to avoid compilation errors. Never regenerate full files when a minor patch works.

## Role: QA Automation Agent
- **Task**: Enters Play Mode and evaluates console errors.
- **Allowed Tools**: `unity_playmode`, `unity_capture`, `unity_list_objects`