using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NeoFPS;
using WizardsCode.Versus.Controller;
using System;
using UnityEngine.UI;

namespace WizardsCode.Versus.FPS
{
    public class HudInfluenceController : PlayerCharacterHudBase
    {
        [SerializeField, Tooltip("The rect transform of the filled bar.")]
        RectTransform m_BarRect = null;
        [SerializeField, Tooltip("The rect transform of the filled bar.")]
        Text m_EnemyCountText = null;

        BlockController m_Block;

        public override void OnPlayerCharacterChanged(ICharacter character)
        {
            if (character == null) return;

            ((PlayerCharacter)character).OnCurrentBlockChanged += OnCurrentBlockChanged;
            OnCurrentBlockChanged(((PlayerCharacter)character).CurrentBlock);
        }

        private void OnCurrentBlockChanged(BlockController newBlock)
        {
            if (m_Block != null)
            {
                m_Block.OnBlockUpdated -= OnBlockUpdated;
            }

            if (newBlock)
            {
                m_Block = newBlock;
                m_Block.OnBlockUpdated += OnBlockUpdated;
            } else
            {
                m_Block = null;
            }
            UpdateGUI();
        }

        private void OnBlockUpdated(BlockController block, VersuseEvent versusEvent)
        {
            UpdateGUI();
        }

        private void UpdateGUI()
        {
            if (m_Block != null)
            {
                m_BarRect.localScale = new Vector2(1 - m_Block.NormalizedFactionInfluence, 1f);
                if (m_Block.GetEnemiesOf(AnimalController.Faction.Cat) != null)
                {
                    m_EnemyCountText.text = m_Block.GetEnemiesOf(AnimalController.Faction.Cat).Count.ToString();
                } else
                {
                    m_EnemyCountText.text = "0";
                }
            } else
            {
                m_BarRect.localScale = new Vector2(0.5f, 1f);
            }
        }
    }
}
