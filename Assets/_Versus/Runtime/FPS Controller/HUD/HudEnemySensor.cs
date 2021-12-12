using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WizardsCode.Versus.Controller;
using NeoFPS;
using WizardsCode.Versus.FPS;
using UnityEngine.UI;

namespace WizardsCode.Versus
{
    public class HudEnemySensor : PlayerCharacterHudBase
    {
        [SerializeField, Tooltip("The enemy marker prefab to use on the HUD to indicate an enemy.")]
        Image m_EnemyMarkerPrefab;
        [SerializeField, Tooltip("Should markers be clamped to the screen?")]
        bool clampToScreen = true;
        [SerializeField, Tooltip("If clamping to the the screen what border size should we use.")] 
        Vector2 clampBorderSize = new Vector2(10, 10);
        [SerializeField, Tooltip("The offset should be used to ensure the enemy marker is position correctly.")]
        Vector3 offset = Vector3.zero;

        List<Image> m_Markers = new List<Image>();
        BlockController m_Block;
        PlayerCharacter player;
        private Camera mainCamera;

        protected override void Start()
        {
            base.Start();
        }

        public override void OnPlayerCharacterChanged(ICharacter character)
        {
            if (character == null) return;

            player = ((PlayerCharacter)character);
            player.OnCurrentBlockChanged += OnCurrentBlockChanged;
            OnCurrentBlockChanged(player.CurrentBlock);
            mainCamera = Camera.main;
        }

        private void OnCurrentBlockChanged(BlockController newBlock)
        {
            if (newBlock)
            {
                m_Block = newBlock;
            }
            else
            {
                m_Block = null;
            }
        }

        private void OnGUI()
        {
            if (!m_Block) return;

            for (int i = 0; i < m_Block.GetEnemiesOf(AnimalController.Faction.Cat).Count || i < m_Markers.Count; i++)
            {
                if (i >= m_Block.GetEnemiesOf(AnimalController.Faction.Cat).Count)
                {
                    m_Markers[i].gameObject.SetActive(false);
                }
                else
                {
                    if (m_Block.GetEnemiesOf(AnimalController.Faction.Cat)[i] != null)
                    {
                        if (i >= m_Markers.Count)
                        {
                            m_Markers.Add(Instantiate(m_EnemyMarkerPrefab, gameObject.transform));
                        }
                        m_Markers[i].gameObject.SetActive(true);

                        Vector3 noClampPosition = mainCamera.WorldToScreenPoint(m_Block.GetEnemiesOf(AnimalController.Faction.Cat)[i].transform.position + offset);
                        m_Markers[i].rectTransform.position = new Vector3(Mathf.Clamp(noClampPosition.x, 0 + clampBorderSize.x, Screen.width - clampBorderSize.x),
                                                                    Mathf.Clamp(noClampPosition.y, 0 + clampBorderSize.y, Screen.height - clampBorderSize.y),
                                                                      noClampPosition.z);
                    }
                }
            }
        }
    }
}
