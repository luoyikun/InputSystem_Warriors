using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{

    //Player ID
    private int playerID;

    [Header("Sub Behaviours")]
    public PlayerMovementBehaviour playerMovementBehaviour;
    public PlayerAnimationBehaviour playerAnimationBehaviour;
    public PlayerVisualsBehaviour playerVisualsBehaviour;


    [Header("Input Settings")]
    public PlayerInput playerInput;
    public float movementSmoothingSpeed = 1f;
    private Vector3 rawInputMovement;
    private Vector3 smoothInputMovement;
    
    //Action Maps
    private string actionMapPlayerControls = "Player Controls";
    private string actionMapMenuControls = "Menu Controls";

    //Current Control Scheme
    private string currentControlScheme;


    //This is called from the GameManager; when the game is being setup.
    public void SetupPlayer(int newPlayerID)
    {
        playerID = newPlayerID;

        currentControlScheme = playerInput.currentControlScheme;

        Debug.Log($"[输入跟踪] 玩家 {playerID + 1} 已初始化。当前动作映射='{playerInput.currentActionMap?.name}'，控制方案='{currentControlScheme}'，已配对设备='{GetPairedDeviceNames()}'，全部动作映射='{GetActionMapNames()}'。", this);

        playerMovementBehaviour.SetupBehaviour();
        playerAnimationBehaviour.SetupBehaviour();
        playerVisualsBehaviour.SetupBehaviour(playerID, playerInput);
    }


    //INPUT SYSTEM ACTION METHODS --------------

    //This is called from PlayerInput; when a joystick or arrow keys has been pushed.
    //It stores the input Vector as a Vector3 to then be used by the smoothing function.


    public void OnMovement(InputAction.CallbackContext value)
    {
        Vector2 inputMovement = value.ReadValue<Vector2>();
        rawInputMovement = new Vector3(inputMovement.x, 0, inputMovement.y);

        if (value.started || value.canceled)
        {
            Debug.Log($"[输入跟踪] 玩家 {playerID + 1} 输入动作 '{value.action.name}'，阶段={value.phase}，来源='{value.control?.path}'，值={inputMovement}。", this);
        }
    }

    //This is called from PlayerInput, when a button has been pushed, that corresponds with the 'Attack' action
    public void OnAttack(InputAction.CallbackContext value)
    {
        Debug.Log($"[输入跟踪] 玩家 {playerID + 1} 输入动作 '{value.action.name}'，阶段={value.phase}，来源='{value.control?.path}'。", this);
        if(value.started)
        {
            playerAnimationBehaviour.PlayAttackAnimation();
        }
    }

    //This is called from Player Input, when a button has been pushed, that correspons with the 'TogglePause' action
    public void OnTogglePause(InputAction.CallbackContext value)
    {
        Debug.Log($"[输入跟踪] 玩家 {playerID + 1} 输入动作 '{value.action.name}'，阶段={value.phase}，来源='{value.control?.path}'。", this);
        if(value.started)
        {
            GameManager.Instance.TogglePauseState(this);
        }
    }

    public void OnNavigate(InputAction.CallbackContext value)
    {
        if (value.started || value.canceled)
        {
            Debug.Log($"[输入跟踪] 玩家 {playerID + 1} 菜单输入动作 '{value.action.name}'，阶段={value.phase}，来源='{value.control?.path}'。", this);
        }
    }

    public void OnSubmit(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            Debug.Log($"[输入跟踪] 玩家 {playerID + 1} 菜单输入动作 '{value.action.name}'，阶段={value.phase}，来源='{value.control?.path}'。", this);
        }
    }

    public void OnCancel(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            Debug.Log($"[输入跟踪] 玩家 {playerID + 1} 菜单输入动作 '{value.action.name}'，阶段={value.phase}，来源='{value.control?.path}'。", this);
        }
    }





    //INPUT SYSTEM AUTOMATIC CALLBACKS --------------

    //This is automatically called from PlayerInput, when the input device has changed
    //(IE: Keyboard -> Xbox Controller)
    public void OnControlsChanged()
    {

        if(playerInput.currentControlScheme != currentControlScheme)
        {
            string previousControlScheme = currentControlScheme;
            currentControlScheme = playerInput.currentControlScheme;

            Debug.Log($"[输入跟踪] 玩家 {playerID + 1} 控制方案从 '{previousControlScheme}' 切换为 '{currentControlScheme}'，当前动作映射的所有按键覆盖将被清除。", this);

            playerVisualsBehaviour.UpdatePlayerVisuals();
            RemoveAllBindingOverrides();
        }
    }

    //This is automatically called from PlayerInput, when the input device has been disconnected and can not be identified
    //IE: Device unplugged or has run out of batteries



    public void OnDeviceLost()
    {
        Debug.LogWarning($"[输入跟踪] 玩家 {playerID + 1} 的输入设备已断开。", this);
        playerVisualsBehaviour.SetDisconnectedDeviceVisuals();
    }


    public void OnDeviceRegained()
    {
        Debug.Log($"[输入跟踪] 玩家 {playerID + 1} 的输入设备已重新连接。", this);
        StartCoroutine(WaitForDeviceToBeRegained());
    }

    IEnumerator WaitForDeviceToBeRegained()
    {
        yield return new WaitForSeconds(0.1f);
        playerVisualsBehaviour.UpdatePlayerVisuals();
    }






    //Update Loop - Used for calculating frame-based data
    void Update()
    {
        CalculateMovementInputSmoothing();
        UpdatePlayerMovement();
        UpdatePlayerAnimationMovement();
    }

    //Input's Axes values are raw


    void CalculateMovementInputSmoothing()
    {
        
        smoothInputMovement = Vector3.Lerp(smoothInputMovement, rawInputMovement, Time.deltaTime * movementSmoothingSpeed);

    }

    void UpdatePlayerMovement()
    {
        playerMovementBehaviour.UpdateMovementData(smoothInputMovement);
    }

    void UpdatePlayerAnimationMovement()
    {
        playerAnimationBehaviour.UpdateMovementAnimation(smoothInputMovement.magnitude);
    }


    public void SetInputActiveState(bool gameIsPaused)
    {
        Debug.Log($"[输入跟踪] 玩家 {playerID + 1} 的输入已因暂停状态而{(gameIsPaused ? "停用" : "启用")}。", this);
        switch (gameIsPaused)
        {
            case true:
                playerInput.DeactivateInput();
                break;

            case false:
                playerInput.ActivateInput();
                break;
        }
    }

    void RemoveAllBindingOverrides()
    {
        InputActionRebindingExtensions.RemoveAllBindingOverrides(playerInput.currentActionMap);
    }



    //Switching Action Maps ----


    
    public void EnableGameplayControls()
    {
        playerInput.SwitchCurrentActionMap(actionMapPlayerControls);
        Debug.Log($"[输入跟踪] 玩家 {playerID + 1} 已切换到动作映射 '{playerInput.currentActionMap.name}'。", this);
    }

    public void EnablePauseMenuControls()
    {
        playerInput.SwitchCurrentActionMap(actionMapMenuControls);
        Debug.Log($"[输入跟踪] 玩家 {playerID + 1} 已切换到动作映射 '{playerInput.currentActionMap.name}'。", this);
    }

    string GetPairedDeviceNames()
    {
        if (playerInput.devices.Count == 0)
        {
            return "none";
        }

        string[] deviceNames = new string[playerInput.devices.Count];
        for (int i = 0; i < playerInput.devices.Count; i++)
        {
            deviceNames[i] = playerInput.devices[i].displayName;
        }

        return string.Join(", ", deviceNames);
    }

    string GetActionMapNames()
    {
        var actionMaps = playerInput.actions.actionMaps;
        string[] actionMapNames = new string[actionMaps.Count];
        for (int i = 0; i < actionMaps.Count; i++)
        {
            actionMapNames[i] = actionMaps[i].name;
        }

        return string.Join(", ", actionMapNames);
    }


    //Get Data ----
    public int GetPlayerID()
    {
        return playerID;
    }

    public InputActionAsset GetActionAsset()
    {
        return playerInput.actions;
    }

    public PlayerInput GetPlayerInput()
    {
        return playerInput;
    }


}
