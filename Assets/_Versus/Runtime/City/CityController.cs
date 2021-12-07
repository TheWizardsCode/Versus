using UnityEngine;
using System;
using Random = UnityEngine.Random;
using WizardsCode.Versus.Controller;
using static WizardsCode.Versus.Controller.AnimalController;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WizardsCode.Versus.Controllers

{
    /// <summary>
    /// The City Controller is responsible for creating the city at the start
    /// of the game and tracking which faction owns which territory. 
    /// Other objects can query the city controller to ask the status
    /// of any particular block in the city.
    /// </summary>
    public class CityController : MonoBehaviour
    {
        public enum BlockType { Suburban, OuterCity, City, InnerCity }
        [Header("Level Generation")]
        [SerializeField, Tooltip("The width of the city in blocks. A single block is 100 x 100, not including the roads connecting blocks.")]
        int m_CityWidth = 15;
        [SerializeField, Tooltip("The depth of the city in blocks. A single block is 100 x 100, not including the roads connecting blocks.")]
        int m_CityDepth = 15;
        [SerializeField, Tooltip("The size of a city block. It will be this size square.")]
        int m_BlockSize = 100;
        [SerializeField, Tooltip("The widh of a city road. note that this includes pavements/sidewalks.")]
        int m_RoadWidth = 10;
        [SerializeField, Tooltip("The prefabs to use when spawning suburban blocks. If you want a prefab to appear more frequently, have more instances.")]
        BlockController[] m_SuburbanBlockPrefabs;
        [SerializeField, Tooltip("The prefabs to use when spawning outer city blocks. If you want a prefab to appear more frequently, have more instances.")]
        BlockController[] m_OuterCityBlockPrefabs;
        [SerializeField, Tooltip("The prefabs to use when spawning city blocks. If you want a prefab to appear more frequently, have more instances.")]
        BlockController[] m_CityBlockPrefabs;
        [SerializeField, Tooltip("The prefabs to use when spawning inner city blocks. If you want a prefab to appear more frequently, have more instances.")]
        BlockController[] m_InnerCityBlockPrefabs;

        [Header("Factions")]
        [SerializeField, Tooltip("Cat prefab, used to instantiate cats into the world.")]
        AnimalController m_CatPrefab;
        [SerializeField, Tooltip("Dog prefab, used to instantiate dogs into the world.")]
        AnimalController m_DogPrefab;
        [SerializeField, Tooltip("The colour gradient to use on the faction map to indicate the cat vs dog influence on an area. 0.5 is nuetral, 0 is cat controlled, 1 is dog controlled.")]
        internal Gradient m_FactionGradient;

        [Header("Top Down")] 
        [SerializeField, Tooltip("Root object where city blocks are created")]
        Transform m_CityBlockRoot;
        Camera m_TopDownCamera;

        [Header("Debug")]
        [SerializeField, Tooltip("The minimum level of logging information to display in the console.")]
        Importance m_ConsoleLoggerMinImportance = Importance.Low;
        [SerializeField, Tooltip("If set to true the city will be generated at runtime. If set to false use the `Generate City` context menu item.")]
        bool m_GenerateAtRuntime = false;
        [SerializeField, Tooltip("If set to true AI will be generated when the game starts, otherwise none will be generated.")]
        bool m_GenerateAI = true;

        BlockController[,] cityBlocks;

        private EventLogger eventLogger;
        int nextCatID = 0;
        int nextDogID = 0;
        int[] factionPopulation;
        int[] maxFactionSize;

        public int Width {
            get { return m_CityWidth; }
        }

        public int Depth
        {
            get { return m_CityDepth; }
        }

        private void Start()
        {
            eventLogger = new EventLogger(m_ConsoleLoggerMinImportance);
            factionPopulation = new int[Enum.GetNames(typeof(Faction)).Length];
            maxFactionSize = new int[Enum.GetNames(typeof(Faction)).Length];

            if (m_GenerateAtRuntime)
            {
                GenerateCity();
            }

            cityBlocks = new BlockController[m_CityWidth, m_CityDepth];
            for (int i = 0; i < m_CityBlockRoot.childCount; i++)
            {
                BlockController block = m_CityBlockRoot.GetChild(i).GetComponent<BlockController>();
                cityBlocks[block.Coordinates.x, block.Coordinates.y] = block;
                if (m_GenerateAI)
                {
                    PopulateBlock(block);
                }
            }
        }

        internal int MaxFactionSize(Faction faction)
        {
            return maxFactionSize[(int)faction];
        }

        internal int GetPopulation(Faction faction)
        {
            return factionPopulation[(int)faction];
        }

        [ContextMenu("Generate City")]
        private void GenerateCity()
        {
            if (cityBlocks != null) {
                DestroyCity();
            }

            cityBlocks = new BlockController[m_CityDepth, m_CityWidth];
            int blockSize = m_BlockSize + (2 * m_RoadWidth);
            Vector3 center = new Vector3(blockSize * m_CityDepth / 2, 0, (blockSize * m_CityWidth) / 2);
            float maxDistanceFromCenter = Vector3.Distance(center, Vector3.zero);

            BlockController block;
            BlockType blockType = BlockType.Suburban;
            Vector3 position;
            float distanceFromCenter;
            Quaternion rotation;
            int blockIdx = 0;
            for (int y = 0; y < m_CityWidth; y++)
            {
                for (int x = 0; x < m_CityDepth; x++)
                {
                    position = new Vector3(x * blockSize, 0, y * blockSize);
                    rotation = Quaternion.identity;

                    distanceFromCenter = Vector3.Distance(position, center);
                    if (distanceFromCenter <= maxDistanceFromCenter / 4)
                    {
                        blockType = BlockType.InnerCity;
                    }
                    else if (distanceFromCenter <= maxDistanceFromCenter / 2)
                    {
                        blockType = BlockType.City;
                    }
                    else if (distanceFromCenter <= maxDistanceFromCenter / 1.5)
                    {
                        blockType = BlockType.OuterCity;
                    }
                    else
                    {
                        blockType = BlockType.Suburban;
                    }

                    switch (blockType)
                    {
                        case BlockType.Suburban:
                            block = Instantiate(m_SuburbanBlockPrefabs[Random.Range(0, m_SuburbanBlockPrefabs.Length)], position, rotation);
                            block.name = "Suburb " + blockIdx;
                            blockIdx++;
                            break;
                        case BlockType.OuterCity:
                            block = Instantiate(m_OuterCityBlockPrefabs[Random.Range(0, m_OuterCityBlockPrefabs.Length)], position, rotation);
                            block.name = "City Block " + blockIdx;
                            blockIdx++;
                            break;
                        case BlockType.City:
                            block = Instantiate(m_CityBlockPrefabs[Random.Range(0, m_CityBlockPrefabs.Length)], position, rotation);
                            block.name = "Inner City Block " + blockIdx;
                            blockIdx++;
                            break;
                        case BlockType.InnerCity:
                            block = Instantiate(m_InnerCityBlockPrefabs[Random.Range(0, m_InnerCityBlockPrefabs.Length)], position, rotation);
                            block.name = "Downtown Block " + blockIdx;
                            blockIdx++;
                            break;
                        default:
                            Debug.LogError("Unknown block type: " + blockType);
                            block = null;
                            break;
                    }

                    if (block == null) continue;

                    block.Coordinates = new Vector2Int(x, y);
                    block.BlockType = blockType;
                    block.transform.parent = m_CityBlockRoot;

                    cityBlocks[x, y] = block;
                }
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(gameObject);
#endif
        }

        [ContextMenu("Destroy City")]
        private void DestroyCity()
        {
            for (int i = 0; i < m_CityBlockRoot.childCount; i++)
            {
                DestroyImmediate(m_CityBlockRoot.GetChild(i).gameObject);
            }
        }

        private void PopulateBlock(BlockController block)
        {
            maxFactionSize[(int)block.DominantFaction] += block.FactionMembersSupported;

            block.OnBlockUpdated += eventLogger.OnEventReceived;
            block.OnBlockDominanceChanged += eventLogger.OnBlockDominanceChanged;
            block.OnBlockDominanceChanged += OnBlockOwnershipChanged;

            AnimalController animal;
            float catWeight = (float)((m_CityWidth - block.Coordinates.x) + (m_CityDepth - block.Coordinates.y)) / (m_CityWidth + m_CityDepth);
            float dogWeight = 1 - catWeight;
            int numOfCats = Random.Range(0, Mathf.RoundToInt(10 * catWeight));
            for (int i = 0; i < numOfCats; i++)
            {
                animal = SpawnCat(block);
            }

            int numOfDogs = Random.Range(0, Mathf.RoundToInt(10 * dogWeight));
            for (int i = 0; i < numOfDogs; i++)
            {
                animal = SpawnDog(block);
            }
        }

        private void OnBlockOwnershipChanged(BlockController block, Faction previousDominantFaction)
        {
            maxFactionSize[(int)previousDominantFaction] -= block.FactionMembersSupported;
            maxFactionSize[(int)block.DominantFaction] += block.FactionMembersSupported;
        }

        internal AnimalController SpawnCat(BlockController block)
        {
            AnimalController animal = Instantiate<AnimalController>(m_CatPrefab);
            animal.name = $"Cat {nextCatID}";
            nextCatID++;
            block.AddAnimal(animal);
            animal.OnAnimalAction += eventLogger.OnEventReceived;
            animal.transform.position = block.GetRandomPoint();
            animal.OnDeath += OnDeath;
            factionPopulation[(int)Faction.Cat]++;
            return animal;
        }

        internal AnimalController SpawnDog(BlockController block)
        {
            AnimalController animal = Instantiate<AnimalController>(m_DogPrefab);
            animal.name = $"Dog {nextDogID}";
            nextDogID++;
            block.AddAnimal(animal);
            animal.transform.position = block.GetRandomPoint();
            animal.OnAnimalAction += eventLogger.OnEventReceived;
            animal.OnDeath += OnDeath;
            factionPopulation[(int)Faction.Dog]++;
            return animal;
        }

        private void OnDeath(AnimalController animal)
        {
            factionPopulation[(int)animal.m_Faction]--;
        }

        internal BlockController GetBlock(int x, int y)
        {
            return cityBlocks[x, y];
        }

        internal BlockController GetBlock(Vector2Int coords)
        {
            return cityBlocks[coords.x, coords.y];
        }
    }
}
