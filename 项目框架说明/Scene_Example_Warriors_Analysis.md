
# Scene_Example_Warriors 运作分析（输入系统）

## 场景定位

场景文件：`Assets/Scenes/Scene_Example_Warriors.unity`。这是一个基于 Unity Input System 的 Warriors 示例，场景包含 GameManager、UI/EventSystem、摄像机与界面对象；玩家控制器以场景内对象或玩家预制体提供。

场景序列化的 `GameManager` 配置为：`currentGameMode: 0`（`SinglePlayer`）、`inScenePlayer` 已赋值、`playerPrefab` 已赋值、`numberOfPlayers: 2`、`spawnRingRadius: 2`。因此当前实际启动使用场景内玩家；`numberOfPlayers` 和 `playerPrefab` 是切换到 `LocalMultiplayer` 时才会被使用的配置。

## 启动与对象关系

`GameManager.Start()` 依次调用 `SetupBasedOnGameState()` 与 `SetupUI()`。

- 单人模式：把场景内玩家的 `PlayerController` 放入活动玩家列表，按 ID 0 初始化玩家行为，然后设置单人摄像机。
- 本地多人模式：销毁场景内玩家，按 `numberOfPlayers` 在以 `spawnRingCenter` 为中心、半径 `spawnRingRadius` 的圆周上实例化 `playerPrefab`，逐个赋 ID 并初始化。
- 玩家初始化由 `PlayerController.SetupPlayer()` 完成：初始化移动、动画和设备视觉；设备显示会读取玩家当前 `PlayerInput` 的控制方案/设备。
- UI 初始化由 `UIManager` 转发给 `UIMenuBehaviour`。菜单默认关闭。

## 输入配置

动作资源：`Assets/Input/InputActions_Player.inputactions`。玩家预制体的 `PlayerInput` 引用此资源，默认 Action Map 是 `Player Controls`，通知方式配置为 Send Messages，因此动作名映射到同对象上的 `On<ActionName>` 方法。

### Player Controls

| Action          | 类型/值         | 绑定                                  | 代码响应                           |
| --------------- | --------------- | ------------------------------------- | ---------------------------------- |
| `Movement`    | Value / Vector2 | WASD 2D Composite、Gamepad Left Stick | `PlayerController.OnMovement`    |
| `Attack`      | Button（Press） | E、Gamepad South                      | `PlayerController.OnAttack`      |
| `TogglePause` | Button（Press） | P、Gamepad Start                      | `PlayerController.OnTogglePause` |
| `Look`        | Value / Vector2 | Mouse Delta、Gamepad Right Stick      | 当前控制脚本没有消费该动作         |

### Menu Controls

| Action          | 用途             | 主要绑定                               |
| --------------- | ---------------- | -------------------------------------- |
| `Navigate`    | EventSystem 导航 | Gamepad 左摇杆/D-pad、键盘 WASD/方向键 |
| `Point`       | 指针位置         | Mouse Position、Touch Position         |
| `Left Click`  | 指针点击         | Mouse Left Button、Touch Press         |
| `Submit`      | 确认             | 通用 Submit、Gamepad South             |
| `Cancel`      | 取消/返回        | `*/{Cancel}`                         |
| `TogglePause` | 关闭暂停菜单     | P、Gamepad Start                       |

资源定义了 Gamepad、Keyboard And Mouse、Touchscreen 三个 Control Scheme。场景玩家预制体上的 `PlayerInput` 初始使用 `Player Controls`；实际设备分配和自动控制方案选择由 PlayerInput/Input System 运行时管理。

## 输入到角色行为的调用链

```text
设备输入
  -> PlayerInput（InputActions_Player）
  -> PlayerController.OnMovement / OnAttack / OnTogglePause
       Movement：保存为 rawInputMovement
       Attack：started 时触发 PlayAttackAnimation()
       TogglePause：started 时调用 GameManager.TogglePauseState(this)
  -> PlayerController.Update()
       Lerp 平滑 raw -> smooth
       把平滑方向交给移动和动画行为
  -> PlayerMovementBehaviour.FixedUpdate()
       以摄像机水平朝向变换输入方向
       Rigidbody.MovePosition / MoveRotation
```

移动输入先由二维输入转为 XZ 平面向量，再用 `Vector3.Lerp` 按 `movementSmoothingSpeed` 平滑。移动方向基于摄像机的 forward/right（清除 Y 分量），所以方向相对于当前游戏摄像机。动画使用平滑向量的 magnitude 更新移动动画。攻击动作仅在 `started` 阶段触发攻击动画。

## 暂停与菜单输入切换

按下当前玩家的 `TogglePause` 后，`GameManager.TogglePauseState()` 翻转暂停状态：

1. 设置 `Time.timeScale` 为 0 或 1。
2. 对非焦点玩家调用 `SetInputActiveState(isPaused)`：暂停时 `DeactivateInput()`，恢复时 `ActivateInput()`。
3. 焦点玩家暂停时切换到 `Menu Controls`，恢复时切回 `Player Controls`。
4. 更新菜单显示状态。

打开菜单时，`UIMenuBehaviour` 重置上下文面板、设置默认选中项、更新 EventSystem 输入模块的 `actionsAsset` 为焦点玩家的 Action Asset、更新焦点玩家显示及重绑定 UI。这样菜单导航使用暂停玩家自己的 Input Actions。再次按 P/Start 时，菜单 Map 中的 `TogglePause` 回调到同一个 `OnTogglePause`，可以关闭菜单并恢复游戏输入。

## 设备显示、连接变化与重绑定

- `PlayerController.OnControlsChanged()` 在当前控制方案变化时更新玩家设备显示；之后调用 `RemoveAllBindingOverrides()` 清除当前 Action Map 的重绑定覆盖。
- 设备丢失时显示断连名称和颜色；设备恢复后延迟 0.1 秒刷新显示。
- `UIRebindActionBehaviour` 从 `GameManager` 取得焦点玩家的 `PlayerInput`，按 `actionName` 查找动作，使用 `PerformInteractiveRebinding()` 捕获新输入。监听时排除 Mouse Position/Delta、Gamepad Start、键盘 P/Escape，避免把指针或暂停/取消键重新绑定。
- 重绑定完成后刷新文本/图标；重置按钮会对该 Action 执行 `RemoveAllBindingOverrides`。
- 绑定显示优先尝试根据设备映射显示图标，否则显示可读按键名。

## 关键脚本

- `Assets/Scripts/Managers/GameManager.cs`：游戏模式、玩家创建、暂停和焦点玩家管理。
- `Assets/Scripts/Behaviours/Player/PlayerController.cs`：PlayerInput 回调、输入平滑、Action Map 切换、设备变化处理。
- `Assets/Scripts/Behaviours/Player/PlayerMovementBehaviour.cs`：摄像机相对移动和转向。
- `Assets/Scripts/Managers/EventSystemManager.cs`：EventSystem 选中对象和 UI Input Module 的动作资源切换。
- `Assets/Scripts/Behaviours/UI/UIMenuBehaviour.cs`：暂停菜单显示、玩家输入资源绑定和重绑定面板刷新。
- `Assets/Scripts/Behaviours/UI/UIRebindActionBehaviour.cs`：交互式重绑定。
- `Assets/Prefabs/Warrior/Player_Warrior.prefab`：PlayerInput 组件与玩家控制器/子行为引用。

## 阅读代码时的注意点

1. `Look` 已在动作资源中定义，但当前玩家脚本没有对应 `OnLook`，也没有相机视角消费逻辑。
2. `OnControlsChanged()` 会清除当前 Action Map 的全部 Binding Overrides。设备/控制方案变化可能因此丢失用户已重绑定的键位；这是目前代码明确执行的行为。
3. `UIRebindActionBehaviour.UpdateBindingDisplayUI()` 直接访问 `focusedInputAction.controls[0]`，并根据该 control 查绑定索引。若动作当前没有可用 control，或动作有复合/多设备绑定，显示逻辑可能无法正确反映想要展示的那条绑定。
4. `GameManager.TogglePauseState()` 每次调用都翻转状态，没有额外防重入保护；暂停和恢复路径依赖 PlayerInput 的 Action Map 及时切换。
5. `NumberOfConnectedDevices()` 返回全局 `InputSystem.devices.Count`，统计的是 Input System 已知设备数量，不等同于已配对/参与游戏的玩家数。

## 快速调试路径

- 确认运行模式：检查场景 GameManager 的 `currentGameMode` 与 `inScenePlayer`。
- 确认玩家动作：检查 `Player_Warrior` 的 PlayerInput 是否引用 `InputActions_Player`，默认 Map 是否为 `Player Controls`。
- 确认回调：对 `OnMovement`、`OnAttack`、`OnTogglePause` 下断点。
- 确认暂停菜单：检查 `SwitchCurrentActionMap` 后的 Map，以及 EventSystem `InputSystemUIInputModule.actionsAsset` 是否指向焦点玩家资源。
- 确认重绑定：检查 Action 的 `bindings` 是否有 override，及控制方案变化时 override 是否被清除。

## 按钮重新映射是如何实现的

重映射由暂停菜单里的 `UIRebindActionBehaviour` 完成，实际输入修改使用 Unity Input System 的交互式重绑定 API。

1. **确定目标玩家与动作**：打开暂停菜单时，`UIMenuBehaviour` 调用 `UIPanelRebindBehaviour.UpdateRebindActions()` 刷新各个重绑定条目。每个条目通过 `GameManager.GetFocusedPlayerController()` 取得当前暂停菜单所属玩家，再取其 `PlayerInput`。Inspector 配置的 `actionName` 用于 `focusedPlayerInput.actions.FindAction(actionName)` 查找目标 Action。
2. **开始监听**：重绑定按钮调用 `ButtonPressedStartRebind()`，随后 `PerformInteractiveRebinding()` 创建 `RebindingOperation` 并启动监听。界面隐藏“重绑”和“重置”按钮，显示正在监听的提示。
3. **过滤不允许的输入**：监听排除鼠标位置、鼠标移动量、Gamepad Start、键盘 P 和 Escape，避免将指针输入或暂停/取消按键设为新绑定。`.OnMatchWaitForAnother(0.1f)` 让系统在短暂间隔内等待额外匹配输入，适用于组合输入匹配。
4. **完成并刷新显示**：匹配输入后 `OnComplete` 调用 `RebindCompleted()`，释放操作对象、恢复按钮状态，并刷新动作名和绑定显示。显示逻辑将绑定路径转换成人类可读名称，再通过 `DeviceDisplayConfigurator` 查找对应图标；有图标时显示图标，否则显示文字。
5. **重置绑定**：重置按钮调用 `ResetBinding()`，对当前 Action 执行 `RemoveAllBindingOverrides(focusedInputAction)`，移除该 Action 的 overrides，然后刷新显示，回到 `.inputactions` 资源定义的默认绑定。

### 绑定修改保存范围

当前代码把重绑定应用于焦点玩家的运行时 `InputAction` 实例，没有调用 `SaveBindingOverridesAsJson`、PlayerPrefs 或文件存储，因此没有显式的跨运行持久化步骤。场景结束或 Action Asset 实例重建后能否保留，不能由这段 UI 逻辑保证。

另一个相关行为是 `PlayerController.OnControlsChanged()` 在控制方案变化时调用 `RemoveAllBindingOverrides(playerInput.currentActionMap)`。这会清掉当前 Map 中所有 Action 的 overrides，因此设备/控制方案切换可能让该玩家刚做的重绑定消失。

### 相关界面配置入口

- 每个 `UIRebindActionBehaviour` 组件的 `actionName` 指定需要重绑的动作；动作名需与 `.inputactions` 中名称一致。
- UI Button 的 OnClick 应分别连接 `ButtonPressedStartRebind()` 和 `ButtonPressedResetBinding()`。
- `UIPanelRebindBehaviour` 的 `uiRebindActionBehaviours` 数组控制打开菜单时要刷新的重绑定条目。
