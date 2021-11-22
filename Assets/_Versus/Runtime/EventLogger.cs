using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WizardsCode.Versus
{
    /// <summary>
    /// The Event Logger records all events of interest in the game so that we can debug, as well as use them for informing the player of activities.
    /// </summary>
    public class EventLogger
    {
        public void OnEventReceived(VersuseEvent versusEvent) {
            Debug.Log($"{versusEvent.Description}");
        }
    }

    public struct VersuseEvent
    {
        public string Description;

        public VersuseEvent(string description)
        {
            Description = description;
        }
    }
}
