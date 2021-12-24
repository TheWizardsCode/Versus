using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static WizardsCode.Versus.Controllers.CityController;
using WizardsCode.Versus.Controllers;
using static WizardsCode.Versus.Controller.AnimalController;
using System.Text;
using NeoFPS;
using WizardsCode.Versus.FPS;

namespace WizardsCode.Versus.Controller
{
    public class BlockController : MonoBehaviour
    {
        public enum Priority { Low, Medium, High, Breed};

        [Header("Meta Data")]
        [HideInInspector, SerializeField, Tooltip("The x,y coordinates of this block within the city.")]
        public Vector2Int Coordinates;
        [SerializeField, Tooltip("Block size in units.")]
        internal Vector2 m_Size = new Vector2(100, 100);
        [HideInInspector, SerializeField, Tooltip("The type of block this is. The block type dictates what is generated within the block.")]
        public BlockType BlockType;
        [SerializeField, Tooltip("How many faction members this block can support. This is the contribution to the total number of faction members possible, it is not a limit on the number of members in this block.")]
        int m_MaxFactionMembersSupported = 10;

        [Header("UX")]
        [SerializeField, Tooltip("The mesh that will show the faction control.")]
        Transform m_FactionMap;
        [SerializeField, Tooltip("The frequency, in seconds, the faction map should be updated. This is a costly operation on large maps so don't make it too frequent.")]
        float m_FactionMapUpdateFrequency = 1f;

        [Header("Debug")]
        [SerializeField, Tooltip("READ ONLY: This text field will be updated with a description of the blocks status in play mode.")]
        [TextArea(6,10)]
        string m_DebugInfo = "Available in Play Mode only.";

        private List<AnimalController> m_DogsPresent = new List<AnimalController>();
        private List<AnimalController> m_CatsPresent = new List<AnimalController>();
        Priority m_CatPriority = Priority.Medium;
        Priority m_DogPriority = Priority.Medium;

        SpawnPoint m_FpsSpawnPoint;
        private Mesh m_FactionMesh;

        public delegate void OnBlockUpdatedDelegate(BlockController block, VersuseEvent versusEvent);
        public OnBlockUpdatedDelegate OnBlockUpdated;

        public delegate void OnBlockDominanceChangedDelegate(BlockController block, Faction previousDominantFaction);
        public OnBlockDominanceChangedDelegate OnBlockDominanceChanged;

        private float timeOfNextFactionMapUpdate = 0;
        private Faction previousFaction;

        public CityController CityController { get; private set; }
        /// <summary>
        /// Returns the number of faction members that can be supported by this block. This is the
        /// number of faction members added to the maximum number possible in the faction when this
        /// block is dominated by that faction.
        /// faction.
        /// </summary>
        public int FactionMembersSupported
        {
            get { return m_MaxFactionMembersSupported; }
        }
        /// <summary>
        /// Returns the number of faction members needed to ensure dominance int his block.
        /// To have dominance the faction must have this many more members present than any other
        /// faction.
        /// </summary>
        public int FactionMembersForDominance
        {
            get { return m_MaxFactionMembersSupported / 2; }
        }
        /// <summary>
        /// Get a list of all the Dats that currently consider this block their home.
        /// </summary>
        // TODO We should not be hard coding the management of animals, but rather managing them through their faction, e.g. GetInhabitants(Faction faction)
        public List<AnimalController> Cats
        {
            get { return m_CatsPresent; }
        }
        /// <summary>
        /// Get a list of all the Dogs that currently consider this block their home.
        /// </summary>
        // TODO We should not be hard coding the management of animals, but rather managing them through their faction, e.g. GetInhabitants(Faction faction)
        public List<AnimalController> Dogs
        {
            get { return m_DogsPresent; }
        }

        /// <summary>
        /// Return any player character that is in this block. Null if no character present.
        /// </summary>
        public PlayerCharacter Player
        {
            get;
            set;
        }

        public Faction DominantFaction
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
            previousFaction = Faction.Neutral;
            m_FactionMesh = m_FactionMap.GetComponent<MeshFilter>().mesh;
            CityController = FindObjectOfType<CityController>();
        }

        /// <summary>
        /// Set the current priority rating for a specific faction. Priorities are set by the player or AI director
        /// and influence the behaviour of AI agents in the game.
        /// </summary>
        /// <param name="faction"></param>
        /// <param name="priority"></param>
        internal void SetPriority(Faction faction, Priority priority)
        {
            if (faction == Faction.Cat)
            {
                m_CatPriority = priority;
            } else
            {
                m_DogPriority = priority;
            }
        }

        /// <summary>
        /// Get the current priority rating for a specific faction. Priorities are set by the player or AI director
        /// and influence the behaviour of AI agents in the game.
        /// </summary>
        /// <param name="faction">The faction we want to get the priority for.</param>
        /// <returns></returns>
        internal Priority GetPriority(Faction faction)
        {
            if (faction == Faction.Cat)
            {
                return m_CatPriority;
            }
            else
            {
                return m_DogPriority;
            }
        }

        internal SpawnPoint GetFpsSpawnPoint()
        {
            if (m_FpsSpawnPoint == null)
            {
                m_FpsSpawnPoint = transform.GetComponentInChildren<SpawnPoint>();
            }
            return m_FpsSpawnPoint;
        }

        private void OnTriggerEnter(Collider other)
        {
            AnimalController animal = other.GetComponentInParent<AnimalController>();
            if (animal)
            {
                if (animal.HomeBlock == this)
                {
                    return;
                }
                else if (animal.currentState != State.Attack && animal.HomeBlock != this)
                {
                    if (animal.HomeBlock != null)
                    {
                        animal.HomeBlock.RemoveAnimal(animal);
                    }
                    AddAnimal(animal);
                    return;
                }
            }

            PlayerCharacter character = other.GetComponentInChildren<PlayerCharacter>();
            if (character)
            {
                character.CurrentBlock = this;
                Player = character;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            AnimalController animal = other.GetComponentInParent<AnimalController>();
            if (animal && animal.HomeBlock == this)
            {
                if (animal.m_Faction == Faction.Dog)
                {
                    m_DogsPresent.Remove(animal);
                }
                else
                {
                    m_CatsPresent.Remove(animal);
                }
                return;
            }

            PlayerCharacter character = other.GetComponentInChildren<PlayerCharacter>();
            if (character)
            {
                Player = null;
            }
        }

        internal void AddAnimal(AnimalController animal)
        {
            switch (animal.m_Faction)
            {
                case AnimalController.Faction.Cat:
                    if (m_CatsPresent.Contains(animal)) return;
                    break;
                case AnimalController.Faction.Dog:
                    if (m_DogsPresent.Contains(animal)) return;
                    break;
            }

            animal.transform.SetParent(transform);
            animal.HomeBlock = this;
            animal.OnDeath += OnDeath;

            switch (animal.m_Faction) {
                case AnimalController.Faction.Cat:
                    m_CatsPresent.Add(animal);
                    break;
                case AnimalController.Faction.Dog:
                    m_DogsPresent.Add(animal);
                    break;
            }

            OnBlockUpdated(this, new BlockUpdateEvent($"{animal} moved into {ToString()}"));
        }

        private void OnDeath(AnimalController animal)
        {
            RemoveAnimal(animal);
        }

        internal void RemoveAnimal(AnimalController animal)
        {
            animal.OnDeath -= OnDeath;

            switch (animal.m_Faction)
            {
                case AnimalController.Faction.Cat:
                    m_CatsPresent.Remove(animal);
                    break;
                case AnimalController.Faction.Dog:
                    m_DogsPresent.Remove(animal);
                    break;
            }

            OnBlockUpdated(this, new BlockUpdateEvent($"{animal} moved out of {ToString()}"));
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
            if (Time.timeSinceLevelLoad >= timeOfNextFactionMapUpdate) {
                timeOfNextFactionMapUpdate = m_FactionMapUpdateFrequency + Time.timeSinceLevelLoad;
                UpdateFactionInfluence();
            }
        }

        private void UpdateFactionInfluence()
        {
            Vector3[] vertices = m_FactionMesh.vertices;
            Color32[] colors = new Color32[vertices.Length];

            Color32 blockColor = CityController.m_FactionGradient.Evaluate(NormalizedFactionInfluence);
            for (int i = 0; i < vertices.Length; i++)
            {
                colors[i] = blockColor;
            }

            m_FactionMesh.colors32 = colors;

            if (DominantFaction != previousFaction)
            {
                switch (DominantFaction)
                {
                    case Faction.Cat:
                        OnBlockDominanceChanged(this, previousFaction);
                        break;
                    case Faction.Dog:
                        OnBlockDominanceChanged(this, previousFaction);
                        break;
                    case Faction.Neutral:
                        if (previousFaction == Faction.Cat)
                        {
                            OnBlockDominanceChanged(this, previousFaction);
                        }
                        else
                        {
                            OnBlockDominanceChanged(this, previousFaction);
                        }
                        break;
                }
                previousFaction = DominantFaction;
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
                float m_CurrentInfluence = 0.5f;
                if (m_DogsPresent.Count == m_CatsPresent.Count)
                {
                    m_CurrentInfluence = 0.5f;
                } else if (m_DogsPresent.Count > m_CatsPresent.Count)
                {
                    float influence = (float)(m_DogsPresent.Count - m_CatsPresent.Count) / FactionMembersForDominance;
                    m_CurrentInfluence = Mathf.Clamp01(0.5f + (influence / 2));
                }
                else
                {
                    float influence = (float)(m_CatsPresent.Count - m_DogsPresent.Count) / FactionMembersForDominance;
                    m_CurrentInfluence = Mathf.Clamp01(0.5f - (influence / 2));
                }

                return m_CurrentInfluence;
            }
        }

        internal List<AnimalController> GetEnemiesOf(Faction m_Faction)
        {
            if (m_Faction == Faction.Cat)
            {
                return m_DogsPresent;
            } else
            {
                return m_CatsPresent;
            }
        }

        internal List<AnimalController> GetFriendsOf(Faction m_Faction)
        {
            if (m_Faction == Faction.Dog)
            {
                return m_DogsPresent;
            }
            else
            {
                return m_CatsPresent;
            }
        }

        public override string ToString()
        {
            return $"{name} {Coordinates}";
        }
    }
}
