using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMenuOptionBehaviour : MonoBehaviour
{
    public int contextPanelID;

    public void ButtonOptionSelected()
    {
        Debug.Log($"[输入跟踪] 已点击菜单按钮。上下文面板编号={contextPanelID}。", this);
        UIManager.Instance.MenuButtonOptionSelected(contextPanelID);
    }
}
