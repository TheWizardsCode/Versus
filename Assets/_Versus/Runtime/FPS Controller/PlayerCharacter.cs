using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NeoFPS.SinglePlayer;
using WizardsCode.Versus.Controller;
using System;

namespace WizardsCode.Versus.FPS
{
    public class PlayerCharacter : FpsSoloCharacter
    {
        public delegate void OnBlockChangedDelegate(BlockController newBlock);
        /// <summary>
        /// Fired whenever the block the player is currently operating within changes.
        /// </summary>
        public OnBlockChangedDelegate OnCurrentBlockChanged;

        BlockController m_Block;
        /// <summary>
        /// The current block is the block the character is currently active within.
        /// </summary>
        public BlockController CurrentBlock
        {
            get
            {
                return m_Block;
            }
            internal set
            {
                if (m_Block != value) {
                    m_Block = value;
                    OnCurrentBlockChanged.Invoke(m_Block);
                }
            }
        }
    }
}
