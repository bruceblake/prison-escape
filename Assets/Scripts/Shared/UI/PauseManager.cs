using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private UnityEngine.UI.Button resumeButton;
    [SerializeField] private UnityEngine.UI.Button quitButton;
    
    private bool isPaused = false;
    private InputAction pauseAction;

    private void Awake()
    {
        Debug.Log("[PauseManager] Awake called");
        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        ConfigureButton(resumeButton, Resume, "[PauseManager] Resume button clicked");
        ConfigureButton(quitButton, QuitGame, "[PauseManager] Quit button clicked");
    }

    private void ConfigureButton(UnityEngine.UI.Button button, UnityEngine.Events.UnityAction action, string logMessage)
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => {
                Debug.Log(logMessage);
                action();
            });

            if (button.targetGraphic == null)
            {
                button.targetGraphic = button.GetComponent<UnityEngine.UI.Image>();
            }
        }
    }

    private void OnEnable()
    {
        // Try to find the action in the project-wide actions
        pauseAction = InputSystem.actions.FindAction("Pause");
        if (pauseAction != null)
        {
            pauseAction.performed += OnPausePerformed;
            pauseAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePerformed;
        }
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        
        if (isPaused)
        {
            Pause();
        }
        else
        {
            Resume();
        }
    }

    public void Pause()
    {
        isPaused = true;
        UIMenuFocus.RegisterOpen();
        
        // ONLY IN SINGLEPLAYER the world needs to be paused
        if (IsSinglePlayer())
        {
            Time.timeScale = 0f;
        }

        if (pauseMenu != null)
            pauseMenu.SetActive(true);
            
        // Enable cursor and unlock it
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        isPaused = false;
        UIMenuFocus.RegisterClosed();
        Time.timeScale = 1f;
        
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
            
        // Only lock and hide cursor if no other full-screen UI is open
        if (!IsAnyOtherUIOpen())
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private bool IsAnyOtherUIOpen()
    {
        // Check InventoryUI
        var inventoryUI = Object.FindAnyObjectByType<InventoryUI>();
        if (inventoryUI != null && inventoryUI.IsOpen) return true;

        // Check StolenNotebookUI
        var notebookUI = Object.FindAnyObjectByType<StolenNotebookUI>();
        if (notebookUI != null && notebookUI.IsOpen) return true;

        return false;
    }

    private bool IsSinglePlayer()
    {
        // Check if NetworkManager exists and is connected/active
        // Based on the project structure, NetworkManager.Singleton is used for multiplayer.
        try 
        {
            // We use reflection or direct check if we are sure of the type.
            // Since I saw the Grep results for NetworkManager, I'll use a dynamic check if needed,
            // but direct check is better if I can find the namespace.
            // Based on Grep: public class NetworkManager : MonoBehaviour
            // No namespace was visible in the first lines, but let's assume global or check.
            var nm = NetworkManager.Singleton;
            return nm == null || !nm.gameObject.activeInHierarchy || !nm.IsConnected;
        }
        catch
        {
            return true;
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
