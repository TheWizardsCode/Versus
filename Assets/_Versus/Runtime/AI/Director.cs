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
        [SerializeField, Tooltip("A list of currently high priority blocks.")]
        List<BlockController> m_HighPriorityBlocks = new List<BlockController>();

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
            int friends = 0;
            int enemies = 0; 
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
                m_HighPriorityBlocks.Remove(block);
            }

            // If we have dominance and there are no enemies breed
            if (friends - enemies >= block.FactionMembersForDominance && enemies == 0)
            {
                block.SetPriority(m_Faction, Priority.Breed);
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
                if (m_HighPriorityBlocks.Count < m_MaxHighPriority && Random.value < GetImportanceRating(block))
                {
                    block.SetPriority(m_Faction, Priority.High);
                    m_HighPriorityBlocks.Add(block);
                } else
                {
                    block.SetPriority(m_Faction, Priority.Medium);
                }
                return;
            }

            // if it is neutral consider making it a high pri target
            if (m_HighPriorityBlocks.Count < m_MaxHighPriority && Random.value < GetImportanceRating(block))
            {
                block.SetPriority(m_Faction, Priority.High);
                m_HighPriorityBlocks.Add(block);
                return;
            }

            // otherwise make it medium
            block.SetPriority(m_Faction, Priority.Medium);
        }

        /// <summary>
        /// Get a normalized importance rating for a given block. This will be calculated based on the directors strategy.
        /// </summary>
        /// <param name="block">A value from 0 (unimportant) to 1 (critical)</param>
        /// <returns></returns>
        private float GetImportanceRating(BlockController block)
        {
            float importance = 0;
            float sqrDistanceFromBase;
            float maxSqrDistance = Vector2.SqrMagnitude(Vector2.zero - new Vector2(city.Depth, city.Width));

            if (m_Faction == Faction.Cat)
            {
                sqrDistanceFromBase = Vector2.SqrMagnitude(new Vector2(city.Depth, city.Width) - block.Coordinates);
            } else
            {
                sqrDistanceFromBase = Vector2.SqrMagnitude(Vector2.zero - block.Coordinates);
            }

            if (sqrDistanceFromBase > 0)
            {
                importance = (sqrDistanceFromBase / maxSqrDistance) / 4;
            }

            Debug.Log($"Importance of {block} to {m_Faction}s is {importance}");
            return importance;
        }
    }
}
