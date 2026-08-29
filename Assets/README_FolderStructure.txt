Subway-Dash Folder Structure
===========================
Assets/
  Scenes/          - Unity scenes (SampleScene.unity)
  Scripts/
    Player/        - PlayerController.cs
    Core/          - GameConstants, shared utils
    Managers/      - GameManager etc (future)
  Prefabs/         - Player, Floor, Obstacles prefabs (future)
  Materials/       - Floor, Player materials
  Data/            - ScriptableObjects
  Editor/          - Editor tools
  Settings/        - URP / InputSystem settings

Player sits on Floor:
  Floor: Position (0, -0.5, 0), Scale (20, 1, 20) -> top at y=0
  Player: Position (0, 1, 0), CharacterController height=2 center=(0,1,0) -> bottom at y=0
