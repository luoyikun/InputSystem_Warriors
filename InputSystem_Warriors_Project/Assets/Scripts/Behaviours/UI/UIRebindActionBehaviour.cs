using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class UIRebindActionBehaviour : MonoBehaviour
{   
    //Input System Stored Data
    private InputActionAsset focusedInputActionAsset;
    private PlayerInput focusedPlayerInput;
    private InputAction focusedInputAction;
    private InputActionRebindingExtensions.RebindingOperation rebindOperation;

    [Header("Rebind Settings")]
    // 要重新绑定的输入动作名称。在 Inspector 中赋值，必须与当前焦点玩家的 Input Actions 资源中的动作名称一致（例如 "Attack"）。
    public string actionName;

    [Header("Device Display Settings")]
    public DeviceDisplayConfigurator deviceDisplaySettings;

    [Header("UI Display - Action Text")]
    public TextMeshProUGUI actionNameDisplayText;

    [Header("UI Display - Binding Text or Icon")]
    public TextMeshProUGUI bindingNameDisplayText;
    public Image bindingIconDisplayImage;

    [Header("UI Display - Buttons")]
    public GameObject rebindButtonObject;
    public GameObject resetButtonObject;

    [Header("UI Display - Listening Text")]
    public GameObject listeningForInputObject;


    public void UpdateBehaviour()
    {   
        GetFocusedPlayerInput();
        SetupFocusedInputAction();
        UpdateActionDisplayUI();
        UpdateBindingDisplayUI();
    }

    void GetFocusedPlayerInput()
    {
        PlayerController focusedPlayerController = GameManager.Instance.GetFocusedPlayerController();
        focusedPlayerInput = focusedPlayerController.GetPlayerInput();
    }

    void SetupFocusedInputAction()
    {
        focusedInputAction = focusedPlayerInput.actions.FindAction(actionName);
    }

    public void ButtonPressedStartRebind()
    {
        StartRebindProcess();
    }

    void StartRebindProcess()
    {
        Debug.Log($"[输入跟踪] 开始重新绑定。玩家={GameManager.Instance.GetFocusedPlayerController().GetPlayerID() + 1}，动作='{focusedInputAction.name}'，正在等待输入。", this);
        
        ToggleGameObjectState(rebindButtonObject, false);
        ToggleGameObjectState(resetButtonObject, false);
        ToggleGameObjectState(listeningForInputObject, true);

        
        rebindOperation = focusedInputAction.PerformInteractiveRebinding()
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithControlsExcluding("<Gamepad>/Start")
            .WithControlsExcluding("<Keyboard>/p")
            .WithControlsExcluding("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => RebindCompleted(operation));

        rebindOperation.Start();
    }


    void RebindCompleted(InputActionRebindingExtensions.RebindingOperation operation)
    {
        Debug.Log($"[输入跟踪] 重新绑定完成。动作='{focusedInputAction.name}'，捕获的输入路径='{operation.selectedControl?.path}'。", this);
        rebindOperation.Dispose();
        rebindOperation = null;

        ToggleGameObjectState(rebindButtonObject, true);
        ToggleGameObjectState(resetButtonObject, true);
        ToggleGameObjectState(listeningForInputObject, false);

        UpdateActionDisplayUI();
        UpdateBindingDisplayUI();
    }

    public void ButtonPressedResetBinding()
    {
        ResetBinding();
    }

    public void ResetBinding()
    {
        Debug.Log($"[输入跟踪] 已重置按键绑定。玩家={GameManager.Instance.GetFocusedPlayerController().GetPlayerID() + 1}，动作='{focusedInputAction.name}'。", this);
        InputActionRebindingExtensions.RemoveAllBindingOverrides(focusedInputAction);
        UpdateBindingDisplayUI();
    }



    void UpdateActionDisplayUI()
    {
        actionNameDisplayText.SetText(actionName);
    }

    void UpdateBindingDisplayUI()
    {

        int controlBindingIndex = focusedInputAction.GetBindingIndexForControl(focusedInputAction.controls[0]);
        string currentBindingInput = InputControlPath.ToHumanReadableString(focusedInputAction.bindings[controlBindingIndex].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
        
        Sprite currentDisplayIcon = deviceDisplaySettings.GetDeviceBindingIcon(focusedPlayerInput, currentBindingInput);

        if(currentDisplayIcon)
        {
            ToggleGameObjectState(bindingNameDisplayText.gameObject, false);
            ToggleGameObjectState(bindingIconDisplayImage.gameObject, true);
            bindingIconDisplayImage.sprite = currentDisplayIcon;
        } else if(currentDisplayIcon == null)
        {
            ToggleGameObjectState(bindingNameDisplayText.gameObject, true);
            ToggleGameObjectState(bindingIconDisplayImage.gameObject, false);
            bindingNameDisplayText.SetText(currentBindingInput);
        }

    }


    void ToggleGameObjectState(GameObject targetGameObject, bool newState)
    {
        targetGameObject.SetActive(newState);
    }



    
}
