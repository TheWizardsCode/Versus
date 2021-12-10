using UnityEngine;
using static WizardsCode.Versus.Controller.AnimalController;

namespace WizardsCode.Versus
{
    public interface IRepellentDrop
    {
        /// <summary>
        /// The time the object will exist before dissolving into nothing. Once picked up the TTL will no
        /// longer have an impact.
        /// </summary>
        public float TimeToLive { get; }
        /// <summary>
        /// The type of NPC that is repelled by this object.
        /// </summary>
        public Faction RepelsType { get; }
        /// <summary>
        /// The amount of repellent that is needed before this kind of mine can be crafted.
        /// </summary>
        public float RequiredRepellent { get; }

        public GameObject gameObject { get; }
    }
}
