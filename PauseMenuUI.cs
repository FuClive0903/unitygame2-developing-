using System;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private InputActionReference pauseAction;
    public bool isopen;
    
    private void OnEnable()
    {
        if (pauseAction != null)
            pauseAction.action.Enable();
    }
    private void OnDisable()
    {
        if (pauseAction != null)
            pauseAction.action.Disable();
    }
    private void Start()
    {
        isopen = false;
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            
        }
    }

    private void Update()
    {
        if (pauseAction == null) return;

        if (pauseAction.action.WasPressedThisFrame())
        {
            if (isopen)
                ClosePauseMenu();
            else
                OpenPauseMenu();
        }

    }
    public void ClosePauseMenu()
    {
        if (pausePanel == null) return;
        pausePanel.SetActive(false);
        isopen=false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenPauseMenu()
    {
        if (pausePanel == null) return;
        pausePanel.SetActive(true);
        isopen = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
}

