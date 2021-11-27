using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WizardsCode.Versus.Controllers;
using static WizardsCode.Versus.Controller.AnimalController;
using System;
using WizardsCode.Versus.Controller;
using static WizardsCode.Versus.Controller.BlockController;
using Random = UnityEngine.Random;

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
        [SerializeField, Tooltip("The maximum number of High Priority blocks allowed.")]
        int m_MaxHighPriority = 3;

        int blockCount;
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
            int friends;
            int enemies; 
            if (m_Faction == Faction.Dog)
            {
                friends = block.Dogs.Count;
                enemies = block.Cats.Count;
            }
            else
            {
                friends = block.Cats.Count;
                enemies = block.Dogs.Count;
            }

            if (block.GetPriority(m_Faction) == Priority.High)
            {
                blockCount--;
            }

            // If we have dominance and there are no enemies breed
            if (friends >= block.FactionMembersForDominance && enemies == 0)
            {

                if (city.GetPopulation(m_Faction) < city.MaxFactionSize(m_Faction))
                {
                    block.SetPriority(m_Faction, Priority.Breed);
                } else
                {
                    block.SetPriority(m_Faction, Priority.Medium);
                }
                return;
            }
            
            // if we have dominance, but there are enemies present hold
            if (friends - enemies >= block.FactionMembersForDominance && enemies > 0)
            {
                block.SetPriority(m_Faction, Priority.Medium);
                return;
            }

            // if they have dominance and we have none make it low
            if (enemies - friends >= block.FactionMembersForDominance && friends == 0)
            {
                block.SetPriority(m_Faction, Priority.Low);
                return;
            }

            // if they have dominance and we have a foothold consider making it a high pri target
            if (enemies - friends >= block.FactionMembersForDominance && friends == 0)
            {
                if (blockCount < m_MaxHighPriority)
                {
                    block.SetPriority(m_Faction, Priority.High);
                    blockCount++;
                }
                return;
            }

            // if it is neutral consider making it a high pri target
            if (blockCount < m_MaxHighPriority)
            {
                block.SetPriority(m_Faction, Priority.High);
                blockCount++;
                return;
            }

            // otherwise make it low
            block.SetPriority(m_Faction, Priority.Low);
        }
    }
}
