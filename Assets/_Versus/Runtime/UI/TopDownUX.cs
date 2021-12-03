using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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

        [Header("Block Tooltip")]
        [SerializeField, Tooltip("")]
        private RectTransform m_BlockTooltip;
        [SerializeField, Tooltip("")] 
        private TextMeshProUGUI m_BlockContent;

        private void Start()
        {
            // Hide the tooltip on startup
            m_BlockTooltip.gameObject.SetActive(false);
            EnableTopDownMode();
        }

        private void Update()
        {
            if (m_IsTopDownMode)
            {
                Ray ray = m_TopDownCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    var blockController = hit.collider.GetComponentInParent<BlockController>();
                    if (blockController != null)
                    {
                        string blockDescription = string.Empty;
                        blockDescription += $"Coordinates: {blockController.Coordinates}{Environment.NewLine}";
                        blockDescription += $"Size: {blockController.m_Size}{Environment.NewLine}";
                        blockDescription += $"Type: {blockController.BlockType}{Environment.NewLine}";
                        blockDescription += $"Faction Members Supported: {blockController.FactionMembersSupported}{Environment.NewLine}";
                        blockDescription += $"Dominant Faction: {blockController.DominantFaction}{Environment.NewLine}";
                        blockDescription += $"Dogs: {blockController.Dogs.Count}{Environment.NewLine}";
                        blockDescription += $"Cats: {blockController.Cats.Count}{Environment.NewLine}";
                        blockDescription += "<color=\"green\">Left Click to enter FPS mode in this block</color>";
                        m_BlockContent.text = blockDescription;
                        // TODO need to position it so it doesn't go offscreen when hovering over blocks near the edge
                        m_BlockTooltip.position = Input.mousePosition;
                        m_BlockTooltip.gameObject.SetActive(true);
                        if (Input.GetMouseButtonDown(0))
                        {
                            EnableFpsMode(blockController);
                        }
                    }
                }
                else
                {
                    if (m_BlockTooltip.gameObject.activeSelf)
                    {
                        m_BlockTooltip.gameObject.SetActive(false);
                    }
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
