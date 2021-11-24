using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WizardsCode.Versus.Controllers;
using static WizardsCode.Versus.Controller.AnimalController;
using System;
using WizardsCode.Versus.Controller;
using static WizardsCode.Versus.Controller.BlockController;

namespace WizardsCode.Versus.AI
{
    /// <summary>
    /// The AI Director is responsible for the strategic planning of one of the factions.
    /// It will decide which blocks are to be taken/held and which can be lost.
    /// Individual AI faction members will then take action based on the directors guidance.
    /// </summary>
    public class Director : MonoBehaviour
    {
        [SerializeField, Tooltip("The faction this director is working for.")]
        Faction m_Faction = Faction.Dog;

        private CityController city;

        private void Start()
        {
            city = FindObjectOfType<CityController>();
            for (int x = 0; x < city.Width; x++)
            {
                for (int y = 0; y < city.Width; y++)
                {
                    city.GetBlock(x, y).OnBlockUpdated += OnBlockUpdate;
                }
            }
        }

        private void OnBlockUpdate(BlockController block, VersuseEvent versusEvent)
        {
            int balance = 0;
            if (m_Faction == Faction.Dog)
            {
                balance = block.Dogs.Count - block.Cats.Count;
            }
            else
            {
                throw new NotImplementedException("The AI Director is not implemented for cats at this time.");
            }

            if (balance >= block.FactionMembersForDominance && block.Cats.Count == 0)
            {
                block.SetPriority(m_Faction, Priority.Low);
            } else if (balance >= block.FactionMembersForDominance && block.Cats.Count > 0)
            {
                block.SetPriority(m_Faction, Priority.High);
            }
            else if (balance <= 0)
            {
                block.SetPriority(m_Faction, Priority.Low);
            }
            else if (balance <= block.FactionMembersForDominance / 2)
            {
                block.SetPriority(m_Faction, Priority.High);
            } else
            {
                block.SetPriority(m_Faction, Priority.Medium);
            }
        }
    }
}
