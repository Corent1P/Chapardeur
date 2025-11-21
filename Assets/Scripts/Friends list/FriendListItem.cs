using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class FriendListItem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image statusBackground;
    [SerializeField] private Button actionButton1;
    [SerializeField] private Button actionButton2;
    [SerializeField] private TMP_Text button1Text;
    [SerializeField] private TMP_Text button2Text;

    private Action button1Action;
    private Action button2Action;

    public void Setup(string displayName, string status, Color statusColor, Action onButton1, Action onButton2)
    {
 
        nameText.text = displayName;
        statusText.text = status;
        statusBackground.color = statusColor;

    
        button1Action = onButton1;
        button2Action = onButton2;

      
        bool isRequest = status.Contains("Pendiente");

        if (isRequest)
        {
            button1Text.text = " Aceptar";
            button2Text.text = " Rechazar";
        }
        else
        {

            button1Text.text = " Eliminar";
            button2Text.text = " Mensaje";
        }


        actionButton1.onClick.RemoveAllListeners();
        actionButton2.onClick.RemoveAllListeners();

        actionButton1.onClick.AddListener(() => button1Action?.Invoke());
        actionButton2.onClick.AddListener(() => button2Action?.Invoke());

  
        actionButton1.gameObject.SetActive(true);
        actionButton2.gameObject.SetActive(true);
    }

    public void UpdateStatus(string newStatus, Color newColor)
    {
        statusText.text = newStatus;
        statusBackground.color = newColor;
    }

    private void OnDestroy()
    {

        actionButton1.onClick.RemoveAllListeners();
        actionButton2.onClick.RemoveAllListeners();
    }
}