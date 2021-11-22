using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WizardsCode.Versus.Controller
{
    /// <summary>
    /// The animal controller is placed on each of the AI animals int he game and is responsible for managing their behaviour.
    /// </summary>
    public class AnimalController : MonoBehaviour
    {
        public enum Faction {  Cat, Dog }
        [SerializeField, Tooltip("The faction this animal belongs to and fights for.")]
        public Faction m_Faction;

        private void Update()
        {
            
        }
    }
}
