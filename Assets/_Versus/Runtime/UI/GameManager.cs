using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using WizardsCode.Versus.Controller;
using static WizardsCode.Versus.Controller.AnimalController;
using static WizardsCode.Versus.Controller.BlockController;

namespace WizardsCode.Versus
{
    /// <summary>
    /// The GameManager manages the entire user experience and game in the top down view. 
    /// User input and coordination of the UI is all managed here.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum GameMode { TopDown, FPS }
        [Header("Top Down Mode")]
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
        [SerializeField, Tooltip("The RectTransform used to show/hide the tooltip.")]
        private RectTransform m_BlockTooltip;
        [SerializeField, Tooltip("The text component within which the tooltip information for a block will be displayed.")] 
        private TextMeshProUGUI m_BlockContent;

        public delegate void OnGameModeChangedDelegate();
        public OnGameModeChangedDelegate OnGameModeChanged;

        private GameMode currentGameMode;
        public bool IsTopDownMode
        {
            get { return currentGameMode == GameMode.TopDown; }
        }
        public bool IsFpsMode
        {
            get { return currentGameMode == GameMode.FPS; }
        }

        private void Start()
        {
            // Hide the tooltip on startup
            m_BlockTooltip.gameObject.SetActive(false);
            EnableTopDownMode();
        }

        private void Update()
        {
            if (currentGameMode == GameMode.TopDown)
            {
                Ray ray = m_TopDownCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    var blockController = hit.collider.GetComponentInParent<BlockController>();
                    if (blockController != null)
                    {
                        string blockDescription = string.Empty;
                        if (blockController.DominantFaction == Faction.Neutral) {
                            blockDescription += $"<color=#00ff00ff>No</color> dominant faction.{Environment.NewLine}";
                        }
                        else if(blockController.DominantFaction == Faction.Dog)
                        {
                            blockDescription += $"<color=#00ffffff>Dogs</color> are the dominant faction.{Environment.NewLine}";
                        }
                        else if (blockController.DominantFaction == Faction.Cat)
                        {
                            blockDescription += $"<color=#ff00ffff>Cats</color> are the dominant faction.{Environment.NewLine}";
                        }
                        blockDescription += $"<color=#00ffffff>{blockController.Dogs.Count}/{blockController.FactionMembersSupported}</color> Dogs present with {blockController.GetPriority(Faction.Dog)} priority.{Environment.NewLine}";
                        blockDescription += $"<color=#ff00ffff>{blockController.Cats.Count}/{blockController.FactionMembersSupported}</color> Cats present with {blockController.GetPriority(Faction.Cat)} priority.{Environment.NewLine}";
                        blockDescription += $"Type: {blockController.BlockType}{Environment.NewLine}";
                        blockDescription += $"{Environment.NewLine}";
                        blockDescription += $"<size=20><color=#00ff00ff>Left Click to enter FPS mode in this block.</color></size>{Environment.NewLine}";
                        blockDescription += "<size=20><color=#00ff00ff>Right Click to cycle block priority.</color></size>";
                        m_BlockContent.text = blockDescription;
                        // TODO need to position it so it doesn't go offscreen when hovering over blocks near the edge
                        m_BlockTooltip.position = Input.mousePosition;
                        m_BlockTooltip.gameObject.SetActive(true);
                        if (Input.GetMouseButtonDown(0))
                        {
                            EnableFpsMode(blockController);
                        }
                        if (Input.GetMouseButtonDown(1))
                        {
                            TogglePriority(blockController);
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

        private void TogglePriority(BlockController blockController)
        {
            int pri = (int)blockController.GetPriority(Faction.Cat);
            pri++;
            if (pri > Enum.GetNames(typeof(Faction)).Length)
            {
                pri = 0;
            }
            blockController.SetPriority(Faction.Cat, (Priority)pri);
        }

        public void EnableFpsMode(BlockController block)
        {
            currentGameMode = GameMode.FPS;
            ConfigureGameObjects(block);
            if (OnGameModeChanged != null) OnGameModeChanged.Invoke(); 
        }

        public void EnableTopDownMode()
        {
            currentGameMode = GameMode.TopDown;
            ConfigureGameObjects(null);
            if (OnGameModeChanged != null) OnGameModeChanged.Invoke();
        }

        private void ConfigureGameObjects(BlockController block)
        {
            m_TopDownUI.gameObject.SetActive(currentGameMode == GameMode.TopDown);
            m_TopDownCamera.gameObject.SetActive(currentGameMode == GameMode.TopDown);
            m_FpsHUD.gameObject.SetActive(currentGameMode == GameMode.FPS);
            if (currentGameMode == GameMode.TopDown)
            {
                m_FpsGameMode.Despawn();
            } else
            {
                m_FpsGameMode.Spawn(block);
            }
        }
    }
}
