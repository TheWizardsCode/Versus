using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NeoFPS;
using WizardsCode.Versus.Controller;
using System;

namespace WizardsCode.Versus.FPS
{
    public class HudInfluenceController : PlayerCharacterHudBase
    {
        [SerializeField, Tooltip("The rect transform of the filled bar.")]
        RectTransform m_BarRect = null;

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

            m_Block = newBlock;
            m_Block.OnBlockUpdated += OnBlockUpdated;
            UpdateGUI();
        }

        private void OnBlockUpdated(VersuseEvent versusEvent)
        {
            UpdateGUI();
        }

        private void UpdateGUI()
        {
            m_BarRect.localScale = new Vector2(1 - m_Block.NormalizedFactionInfluence, 1f);
        }
    }
}
