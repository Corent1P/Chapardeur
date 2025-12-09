using UnityEngine;

public class MobileControlsManager : MonoBehaviour
{
    [SerializeField] private GameObject mobileCanvasParams;

    private void Start()
    {
        bool showMobileControls = false;

        #if UNITY_ANDROID || UNITY_IOS
            showMobileControls = true;
        #endif

        #if UNITY_EDITOR
            // showMobileControls = true; 
        #endif

        if (mobileCanvasParams != null)
        {
            mobileCanvasParams.SetActive(showMobileControls);
        }
        else
        {
            gameObject.SetActive(showMobileControls);
        }
    }
}