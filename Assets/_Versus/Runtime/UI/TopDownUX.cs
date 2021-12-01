using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WizardsCode.Versus.Controller;

namespace WizardsCode.Versus
{
    /// <summary>
    /// The TopDownUX manages the entire user experience in the top down view. 
    /// User input and coordination of the UI is all managed here.
    /// </summary>
    public class TopDownUX : MonoBehaviour
    {
        [Header("Top Down Mode")]
        [SerializeField, Tooltip("If true then the player is in top down mode, if false they are in FPS mode.")]
        bool m_IsTopDownMode = true;
        [SerializeField, Tooltip("Camera used in top down view.")]
        Camera m_TopDownCamera;
        [SerializeField, Tooltip("The top down data ui to be displayed whenever top down mode is enabled.")]
        RectTransform m_TopDownUI;

        [Header("FPS Mode")]
        [SerializeField, Tooltip("The parent object containing the NeoFPSGameMode and other FPS specific objects. These will be enabled when entering the FPS mode.")]
        VersusFpsGameMode m_FpsGameMode;
        [SerializeField, Tooltip("The FPS HUD that should be displayed whenever FPS mode is enabled.")]
        RectTransform m_FpsHUD;

        private void Start()
        {
            EnableTopDownMode();
        }

        private void Update()
        {
            if (m_IsTopDownMode && Input.GetMouseButtonDown(0))
            {
                Ray ray = m_TopDownCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    EnableFpsMode(hit.collider.GetComponentInParent<BlockController>());
                }
            }
        }

        public void EnableFpsMode(BlockController block)
        {
            m_IsTopDownMode = false;
            ConfigureGameObjects(block);
        }

        public void EnableTopDownMode()
        {
            m_IsTopDownMode = true;
            ConfigureGameObjects(null);
        }

        private void ConfigureGameObjects(BlockController block)
        {
            m_TopDownUI.gameObject.SetActive(m_IsTopDownMode);
            m_TopDownCamera.gameObject.SetActive(m_IsTopDownMode);
            m_FpsHUD.gameObject.SetActive(!m_IsTopDownMode);
            if (m_IsTopDownMode)
            {
                m_FpsGameMode.Despawn();
            } else
            {
                m_FpsGameMode.Spawn(block);
            }
        }
    }
}
