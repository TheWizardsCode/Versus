using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static WizardsCode.Versus.Controllers.CityController;
using WizardsCode.Versus.Controllers;
using static WizardsCode.Versus.Controller.AnimalController;
using System.Text;

namespace WizardsCode.Versus.Controller
{
    public class BlockController : MonoBehaviour
    {
        [HideInInspector, SerializeField, Tooltip("The z,y coordinates of this block within the city.")]
        public Vector2Int Coordinates;
        [SerializeField, Tooltip("Block size in units.")]
        internal Vector2 m_Size = new Vector2(100, 100);
        [HideInInspector, SerializeField, Tooltip("The type of block this is. The block type dictates what is generated within the block.")]
        public BlockType BlockType;
        [SerializeField, Tooltip("The mesh that will show the faction control.")]
        Transform m_FactionMap;
        [SerializeField, Tooltip("The frequency, in seconds, the faction map should be updated. This is a costly operation on large maps so don't make it too frequent.")]
        float m_FactionMapUpdateFrequency = 1f;
        [SerializeField, Tooltip("The number of excess faction members that need to be present for dominance. This is used to calculate a factions influence on the block. If a faction has a majority of this many on a block then it is considered to have dominance.")]
        int m_FactionMembersNeededForControl = 5;

        private Mesh m_FactionMesh;

        private List<AnimalController> m_DogsPresent = new List<AnimalController>();
        private List<AnimalController> m_CatsPresent = new List<AnimalController>();

        public delegate void OnBlockUpdatedDelegate(VersuseEvent versusEvent);
        public OnBlockUpdatedDelegate OnBlockUpdated;

        private float timeOfNextFactionMapUpdate = 0;
        private Faction previousFaction;

        public CityController CityController { get; private set; }

        public Faction ControllingFaction
        {
            get
            {
                if (NormalizedFactionInfluence <= 0.1f)
                {
                    return Faction.Cat;
                }
                else if (NormalizedFactionInfluence >= 0.9f)
                {
                    return Faction.Dog;
                } else
                {
                    return Faction.Neutral;
                }
            }
        }

        private void Start()
        {
            m_FactionMesh = m_FactionMap.GetComponent<MeshFilter>().mesh;
            CityController = FindObjectOfType<CityController>();
        }

        private void OnTriggerEnter(Collider other)
        {
            AnimalController animal = other.GetComponentInParent<AnimalController>();
            if (animal)
            {
                animal.HomeBlock.RemoveAnimal(animal);
                AddAnimal(animal);
            }
        }

        internal void AddAnimal(AnimalController animal)
        {
            animal.transform.SetParent(transform);
            animal.transform.position = GetRandomPoint();
            animal.HomeBlock = this;

            switch (animal.m_Faction) {
                case AnimalController.Faction.Cat:
                    m_CatsPresent.Add(animal);
                    break;
                case AnimalController.Faction.Dog:
                    m_DogsPresent.Add(animal);
                    break;
            }

            OnBlockUpdated(new BlockUpdateEvent($"{animal.m_Faction} moved into {ToString()}."));
        }

        internal void RemoveAnimal(AnimalController animal)
        {
            switch (animal.m_Faction)
            {
                case AnimalController.Faction.Cat:
                    m_CatsPresent.Remove(animal);
                    break;
                case AnimalController.Faction.Dog:
                    m_DogsPresent.Remove(animal);
                    break;
            }

            OnBlockUpdated(new BlockUpdateEvent($"{animal.m_Faction} moved out of {ToString()}."));
        }

        /// <summary>
        /// Gets a random point within this block that an animal might want to go to.
        /// </summary>
        /// <returns></returns>
        internal Vector3 GetRandomPoint()
        {
            //TODO: more intelligent spawning location, currently animals can spawn on top of one another, inside buildings and more.
            return transform.position +  new Vector3(Random.Range(-m_Size.x / 2, m_Size.x / 2), 0, Random.Range(-m_Size.y / 2, m_Size.y / 2));
        }

        private void Update()
        {
            if (Time.timeSinceLevelLoad < timeOfNextFactionMapUpdate) return;
            timeOfNextFactionMapUpdate = m_FactionMapUpdateFrequency + Time.timeSinceLevelLoad;

            //OPTIMIZATION: if this is not in view of the camera there is no need to update the faction mesh
            Vector3[] vertices = m_FactionMesh.vertices;
            Color32[] colors = new Color32[vertices.Length];

            Color32 blockColor = CityController.m_FactionGradient.Evaluate(NormalizedFactionInfluence);
            for (int i = 0; i < vertices.Length; i++)
            {
                colors[i] = blockColor;
            }

            m_FactionMesh.colors32 = colors;

            if (ControllingFaction != previousFaction)
            {
                switch (ControllingFaction)
                {
                    case Faction.Cat:
                        OnBlockUpdated(new BlockUpdateEvent($"The cats have taken {ToString()}.", Importance.High));
                        break;
                    case Faction.Dog:
                        OnBlockUpdated(new BlockUpdateEvent($"The dogs have taken {ToString()}.", Importance.High));
                        break;
                    case Faction.Neutral:
                        if (previousFaction == Faction.Cat)
                        {
                            OnBlockUpdated(new BlockUpdateEvent($"The dogs have weakened the cats hold on {ToString()}, it is now a neutral zone.", Importance.High));
                        } else
                        {
                            OnBlockUpdated(new BlockUpdateEvent($"The cats have weakened the dogs hold on {ToString()}, it is now a neutral zone (Normalized Influence: {NormalizedFactionInfluence}).", Importance.High));
                        }
                        break;
                }
                previousFaction = ControllingFaction;
            }
        }

        /// <summary>
        /// Get a normalized value that represents the influence of each faction on this block.
        /// 0.5 is neutral, 0 is cat controlled, 1 is dog controlled
        /// </summary>
        /// <returns>0.5 is neutral, 0 is cat controlled, 1 is dog controlled</returns>
        internal float NormalizedFactionInfluence
        {
            get
            {
                if (m_DogsPresent.Count == m_CatsPresent.Count)
                {
                    return 0.5f;
                } else if (m_DogsPresent.Count > m_CatsPresent.Count)
                {
                    float influence = (float)(m_DogsPresent.Count - m_CatsPresent.Count) / m_FactionMembersNeededForControl;
                    return 0.5f + influence;
                }
                else
                {
                    float influence = (float)(m_CatsPresent.Count - m_DogsPresent.Count) / m_FactionMembersNeededForControl;
                    return 0.5f - influence;
                }
            }
        }

        public override string ToString()
        {
            return $"{name} {Coordinates}.";
        }
    }
}
