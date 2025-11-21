using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private bool cursorVisible = true;
    [SerializeField] private CursorLockMode cursorLockMode = CursorLockMode.None;

    void Start()
    {
        UpdateCursorState();
    }

    void Update()
    {
        // Verificar constantemente el estado del cursor
        if (Cursor.visible != cursorVisible || Cursor.lockState != cursorLockMode)
        {
            UpdateCursorState();
        }
    }

    private void UpdateCursorState()
    {
        Cursor.visible = cursorVisible;
        Cursor.lockState = cursorLockMode;
    }

    // Método público para cambiar el estado del cursor
    public void SetCursorState(bool visible, CursorLockMode lockMode = CursorLockMode.None)
    {
        cursorVisible = visible;
        cursorLockMode = lockMode;
        UpdateCursorState();
    }
}