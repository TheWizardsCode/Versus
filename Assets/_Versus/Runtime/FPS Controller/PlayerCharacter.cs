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
        public BlockController CurrentBlock
        {
            get; internal set;
        }
    }
}
