using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NeoFPS;
using WizardsCode.Versus.Controller;

namespace WizardsCode.Versus
{
    public class RepellentPickup : InventoryItemPickup, IRepellentDrop
    {
        [Header("Repellent Pickup")]
        [SerializeField, Tooltip("The time to live for this repellent object. If it has not been triggered in this time it will just 'fade away' and no longer be effective.")]
        float m_TimeToLive = 10;
        [Header("Repellent")]
        [SerializeField, Tooltip("The kind of animal this repellent acts against.")]
        AnimalController.Faction m_RepelledType;
        [SerializeField, Tooltip("The amount of repellent that is needed before this kind of repellent can be crafted.")]
        public float m_RequiredRepellent = 10;

        public float TimeToLive { get { return m_TimeToLive; } }

        public AnimalController.Faction RepelsType { get { return m_RepelledType; } }

        public float RequiredRepellent { get { return m_RequiredRepellent; } }

        private void OnEnable()
        {
            StartCoroutine(TimeToLiveCo());
        }

        private IEnumerator TimeToLiveCo()
        {
            yield return new WaitForSeconds(m_TimeToLive);

            //OPTIMIZATION Use an object pool
            Destroy(gameObject);
        }
    }
}
