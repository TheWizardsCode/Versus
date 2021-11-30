using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WizardsCode.Versus.Controller;
using NeoFPS;
using System;

namespace WizardsCode.Versus.Weapons
{
    /// <summary>
    /// A mine will explode in a cloud of animal repellent.
    /// </summary>
    public class Mine : ExplosiveObject
    {
        [Header("Mine")]
        [SerializeField, Tooltip("The time to live for this mine. If it has not been triggered in this time it will just 'fade away' and no longer be effective.")]
        float m_TimeToLive = 10;

        [Header("Repellent")]
        [SerializeField, Tooltip("The kind of animal this repellent acts against.")]
        AnimalController.Faction m_RepelledType;
        [SerializeField, Tooltip("The amount of repellent that is needed before this kind of mine can be crafted.")]
        public float RequiredRepellent = 10;

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

        private void OnTriggerEnter(Collider other)
        {
            AnimalController animal = other.GetComponentInParent<AnimalController>();
            if (animal && animal.m_Faction == m_RepelledType)
            {
                AddDamage(50);
            }
        }
    }
}
