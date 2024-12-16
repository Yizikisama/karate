using UnityEngine;
using UnityEngine.UI;

namespace PunctualSolutions.Boxing
{
    public class InteractionController : MonoBehaviour
    {
    public Button linkButton;                 // Link按钮
    public Dropdown interactionDropdown;      // 下拉菜单
    public GameObject userA;                  // 用户A的GameObject，用于控制交互模式

    void Start()
    {
        // 初始时隐藏下拉菜单
        interactionDropdown.gameObject.SetActive(false);

        // 为Link按钮添加监听器
        linkButton.onClick.AddListener(OnLinkButtonClick);

        // 为下拉菜单添加监听器
        interactionDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    void OnLinkButtonClick()
    {
        // 点击Link按钮时，显示下拉菜单
        interactionDropdown.gameObject.SetActive(true);
    }

    void OnDropdownValueChanged(int index)
    {
        switch (index)
        {
            case 0:
                SetTargetSettingMode();
                break;
            case 1:
                SetTextCommandMode();
                break;
            case 2:
                SetVoiceCommandMode();
                break;
            default:
                Debug.LogWarning("未知的交互模式");
                break;
        }
    }

    void SetTargetSettingMode()
    {
        Debug.Log("选择了目标设置模式");
        // 在这里添加设置用户A为目标设置模式的逻辑
    }

    void SetTextCommandMode()
    {
        Debug.Log("选择了文本命令模式");
        // 在这里添加设置用户A为文本命令模式的逻辑
    }

    void SetVoiceCommandMode()
    {
        Debug.Log("选择了语音命令模式");
        // 在这里添加设置用户A为语音命令模式的逻辑
    }
    }
}
