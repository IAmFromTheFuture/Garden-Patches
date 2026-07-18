# Skill: Build and Verify Prototype

When executing a feature development cycle, adhere to this strict loop:
1. **Analyze**: Use `unity_file` to list your active scripts or scenes.
2. **Implement**: Write or patch C# files cleanly using Unity-optimized `.csproj` files via the Antigravity Unity extension (DotRush-compatible).
3. **Generate Scene Layout**: Call `unity_component` or `unity_tilemap` to spawn required GameObjects, spawners, and UI Toolkits directly into the open scene.
4. **Compile Check**: Wait for Unity compilation to finish without errors.
5. **Auto-Playtest**: Trigger `unity_playmode` (action: `play`), wait 5 seconds, watch the logs, and then call `unity_playmode` (action: `stop`).
6. **Fix**: If any exceptions pop up in the console stream, immediately patch the code and re-test.