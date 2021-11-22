using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WizardsCode.Versus.Controller;
using NeoFPS;

namespace WizardsCode.Versus.Weapons
{
    /// <summary>
    /// A mine will explode in a cloud of animal repellent.
    /// </summary>
    public class Mine : ExplosiveObject
    {
        [Header("Repellent")]
        [SerializeField, Tooltip("The kind of animal this repellent acts against.")]
        AnimalController.Faction m_RepelledType;
        [SerializeField, Tooltip("The amount of repellent that is needed before this kind of mine can be crafted.")]
        public float RequiredRepellent = 10;

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
