using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WizardsCode.Versus.Controller;
using System;

namespace WizardsCode.Versus
{
    /// <summary>
    /// The Event Logger records all events of interest in the game so that we can debug, as well as use them for informing the player of activities.
    /// </summary>
    public class EventLogger
    {
        Importance m_MinImportance;

        public EventLogger(Importance minImportance)
        {
            m_MinImportance = minImportance;
        }

        public void OnEventReceived(VersuseEvent versusEvent) {
            if ((int)versusEvent.Importance >= (int)m_MinImportance)
            {
                Debug.Log($"{versusEvent.GetType().Name} : {versusEvent.Description}");
            }
        }

        public void OnEventReceived(BlockController block, VersuseEvent versusEvent)
        {
            if ((int)versusEvent.Importance >= (int)m_MinImportance)
            {
                Debug.Log($"{versusEvent.GetType().Name} : {versusEvent.Description}");
            }
        }

        internal void OnBlockDominanceChanged(BlockController block, AnimalController.Faction previousDominantFaction)
        {
            if ((int)Importance.High >= (int)m_MinImportance)
            {
                Debug.Log($"Dominance Change: {block} has just been dominated by {block.DominantFaction}. Previously it was dominated by {previousDominantFaction}.");
            }
        }
    }
    public enum Importance { Low, Medium, High }

    public class VersuseEvent
    {
        public string Description;
        public Importance Importance;

        public VersuseEvent(string description, Importance importance)
        {
            Description = description;
            Importance = importance;
        }
    }

    public class BlockUpdateEvent : VersuseEvent
    {
        public BlockUpdateEvent(string description, Importance importance = Importance.Medium) : base(description, importance)
        {
        }
    }

    public class AnimalActionEvent : VersuseEvent
    {
        public AnimalActionEvent(string description, Importance importance = Importance.Medium) : base(description, importance)
        {
        }
    }
}
