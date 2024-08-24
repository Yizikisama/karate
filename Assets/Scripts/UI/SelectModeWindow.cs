using System;

namespace PunctualSolutions.Boxing.UI
{
    public class SelectModeWindow : MonoSingleton<SelectModeWindow>
    {
        public void SetPointClick()
        {
            LinkWindow.Instance.gameObject.SetActive(true);
            Destroy(gameObject);
        }

        public void TriggerPointClick()
        {
            WaitLinkWindow.Instance.gameObject.SetActive(true);
            WaitLinkWindow.Instance.Init();
            Destroy(gameObject);
        }
    }
}