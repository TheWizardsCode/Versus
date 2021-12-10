using UnityEngine;
using WizardsCode.Versus.Weapons;
using NeoFPS;
using System;
using Random = UnityEngine.Random;
using System.Collections;
using System.Collections.Generic;
using WizardsCode.Versus.AI;
using static WizardsCode.Versus.Controller.BlockController;
using NeoSaveGames.Serialization;

namespace WizardsCode.Versus.Controller
{
    /// <summary>
    /// The animal controller is placed on each of the AI animals in the game and is responsible for managing their behaviour.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AnimalController : RechargingHealthManager
    {
        public enum State { Idle, GatherRepellent, PlaceRepellent, Flee, Hide, Attack, Expand, Breed }
        public enum Faction { Cat, Dog, Neutral }

        [Space]

        [Header("Animal AI")]
        [SerializeField, Tooltip("How frequently the animal should make decisions. Not that the actual time between decisions will be +/- 10% of this value.")]
        float m_DecisionFrequency = 0.25f;
        [SerializeField, Tooltip("If the animal is reduced to this level of health they will flee from an attack but stay within the block."), Range(0f, 1f)]
        float m_WeakFleeHealthPoint = 0.5f;
        [SerializeField, Tooltip("If the animal is reduced to this level of health they will flee from the block."), Range(0f, 1f)]
        float m_StrongFleeHealthPoint = 0.15f;
        [SerializeField, Tooltip("The colour gradient to use when the animal is considered healthy. A colour on this gradient will be chosen randomly at start.")]
        Gradient m_HealthyColourGradient;
        [SerializeField, Tooltip("The colour to use when the animal is fleeing from an attack, but is not yet fleeing the block.")]
        Color32 m_WeakFleeingColour = Color.yellow;
        [SerializeField, Tooltip("The colour to use when the animal is fleeing from the block.")]
        Color32 m_StrongFleeingColour = Color.white;

        [Header("Attributes")]
        [SerializeField, Tooltip("If set to true then the rate of resource gathering will be randomly set upon creation of an object with this controller attached. If set to false then the rate can be set here.")]
        bool randomizeReppelentGatheringSpeed = true;
        [SerializeField, Tooltip("The maximum rate at which this animal will collect repellent for making weaponry. Measured in repellent per second. If randomizeReppelentGatheringSpeed is true the amount collected will randomized between 0.01 and this value on start. Note that this max level will only be reached when the animal reaches its maximum level. If set to false it will be this amount precisely.")]
        float m_MaxRepellentGatheringSpeed = 6f;
        [SerializeReference, Tooltip("The repellent mine this animal knows how to craft and plant.")]
        Mine m_RepellentMine;
        [SerializeReference, Tooltip("The repellent ammo this animal knows how to craft and plant.")]
        RepellentPickup[] m_RepellentAmmoPickups;

        [Header("Faction")]
        [SerializeField, Tooltip("The faction this animal belongs to and fights for.")]
        public Faction m_Faction;
        [SerializeField, Tooltip("The block this animal considers home. This is where they live, but if their health falls below 0 they will flee to a safer place.")]
        BlockController m_HomeBlock;

        [Header("Movement")]
        [SerializeField, Tooltip("The speed this animal moves at under normal walking conditions.")]
        float m_Speed = 2;
        [SerializeField, Tooltip("The speed this animal rotates under normal walking conditions.")]
        float m_RotationSpeed = 25;

        [Header("Breeding")]
        [SerializeField, Tooltip("The chance that an animal will breed in when the blocks priority is set to Breed and the animal is otherwise in an Idle state."), Range(0f, 1f)]
        float m_BreedingChance = 0.02f;
        [SerializeField, Tooltip("The time, in seconds, this animal needs to be in the breeding state before a litter is born and raised. If they are attacked in this time they will not breed succesfully. We do not (currently?) track male and female animals separately.")]
        float m_BreedingDuration = 30;

        [Header("Attack")]
        [SerializeField, Tooltip("The average frequency at which the dog will scan for enemies. Note that when in FPS mode dogs will always attack a player who enters their block. This is for inter-animal fights.")]
        float m_EnemyScanFrequency = 2f;
        [SerializeField, Tooltip("The distance from a target the animal needs to be before it can attack.")]
        float m_AttackDistance = 0.1f;
        [SerializeField, Tooltip("The frequency at which this animal will be able to attack once in range.")]
        float m_AttackFrequency = 1.2f;
        [SerializeField, Tooltip("The damage done by this animal when it attacks")]
        float m_Damage = 7.5f;
        [SerializeField, Tooltip("The chase distance is how far from the center of the home block this animal will chase a target before giving up.")]
        float m_ChaseDistance = 100;
        [SerializeField, Tooltip("A collection of sounds, one of which will be played when the animal is chasing or attackng a target.")]
        AudioClip[] m_AttackSounds = new AudioClip[0];
        [SerializeField, Tooltip("A collection of sounds, one of which will be played when the animal decides to leave the city (dies).")]
        AudioClip[] m_DeathSounds = new AudioClip[0];

        public LevelSystem m_LevelSystem = new LevelSystem();

        public delegate void OnAnimalActionDelegate(VersuseEvent versusEvent);
        public OnAnimalActionDelegate OnAnimalAction;

        internal State currentState = State.Idle;
        private float minRepellentNeeded;
        internal Transform attackTarget;
        private float sqrChaseDistance = 0;
        private float sqrAttackDistance = 0;
        private float timeOfNextAttack = 0;
        private float aiUpdateDelay;
        private Coroutine aiCoroutine;
        private float availableRepellent;
        private float timeToRevaluateState = 0;
        private float currentSpeedMultiplier = 1;
        private Vector3 _moveTargetPosition;
        private MeshFilter[] m_MeshFilters;

        private AudioSource audioSource;

        public delegate void OnDeathDelegate(AnimalController animal);
        public OnDeathDelegate OnDeath;
        private float m_CurrentRepellentGatheringSpeed;

        /// <summary>
        /// The expand to block indicates the block that this Animal is planning on making its home.
        /// If this is null then the animal is perfectly happy in the current HomeBlock and will stay there.
        /// </summary>
        internal BlockController ExpandToBlock { get; set; }

        internal BlockController HomeBlock
        {
            get { return m_HomeBlock; }
            set { m_HomeBlock = value; }
        }

        internal Vector3 MoveTargetPosition
        {
            get { return _moveTargetPosition; }
            set
            {
                value.y = 0.07f;
                _moveTargetPosition = value;
            }
        }

        private void OnEnable()
        {
            aiUpdateDelay = Random.Range(m_DecisionFrequency * 0.9f, m_DecisionFrequency * 1.1f);
            aiCoroutine = StartCoroutine(ProcessAI());
            m_LevelSystem.OnLevelChanged += OnLevelChanged;
            m_CurrentRepellentGatheringSpeed = m_MaxRepellentGatheringSpeed;
        }

        private void OnDisable()
        {
            m_LevelSystem.OnLevelChanged += OnLevelChanged;
            StopAllCoroutines();
        }

        private void OnLevelChanged(object sender, EventArgs e)
        {
            m_CurrentRepellentGatheringSpeed = m_MaxRepellentGatheringSpeed * (float)(m_LevelSystem.Level / m_LevelSystem.MaxLevel);
        }

        bool CanPlaceRepellentTrigger
        {
            get
            {
                return availableRepellent >= minRepellentNeeded;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            m_MeshFilters = GetComponentsInChildren<MeshFilter>();
            SetColour(m_HealthyColourGradient.Evaluate(Random.value));
        }

        private void Start()
        {
            HomeBlock = GetComponentInParent<BlockController>();
            audioSource = GetComponent<AudioSource>();
            sqrChaseDistance = m_ChaseDistance * m_ChaseDistance;
            sqrAttackDistance = m_AttackDistance * m_AttackDistance;
            minRepellentNeeded = float.MaxValue;
            for (int i = 0; i < m_RepellentAmmoPickups.Length; i++)
            {
                if (m_RepellentAmmoPickups[i].RequiredRepellent < minRepellentNeeded)
                {
                    minRepellentNeeded = m_RepellentAmmoPickups[i].RequiredRepellent;
                }
            }

            if (m_RepellentMine.RequiredRepellent < minRepellentNeeded)
            {
                minRepellentNeeded = m_RepellentMine.RequiredRepellent;
            }
        }

        private void SetColour(Color32 color)
        {
            for (int i = 0; i < m_MeshFilters.Length; i++) {
                Vector3[] vertices = m_MeshFilters[i].mesh.vertices;
                Color32[] colors = new Color32[vertices.Length];

                for (int v = 0; v < vertices.Length; v++)
                {
                    colors[v] = color;
                }

                m_MeshFilters[i].mesh.colors32 = colors;
            }
        }

        protected override void OnHealthChanged(float from, float to, bool critical, IDamageSource source)
        {
            if (to < from && to > 0)
            {
                if (to < healthMax * m_StrongFleeHealthPoint)
                {
                    SetColour(m_StrongFleeingColour);
                    currentState = State.Flee;
                    MoveTargetPosition = GetFriendlyPositionOrDie();
                    OnAnimalAction(new AnimalActionEvent($"{ToString()} has been hit by {from - to} units of repellent. They are abandoning their expansion orders and seeking refuge if they can find it."));
                }
                else if (to < healthMax * m_WeakFleeHealthPoint)
                {

                    if (currentState == State.Expand)
                    {
                        OnAnimalAction(new AnimalActionEvent($"{ToString()} has been hit by {from - to} units of repellent. They are fleeing from the source but continuing to carry out their expansion orders."));
                    }
                    else
                    {
                        SetColour(m_WeakFleeingColour);
                        currentState = State.Flee;
                        MoveTargetPosition = GetNewWanderPositionWithinHomeBlock();
                        OnAnimalAction(new AnimalActionEvent($"{ToString()} has been hit by {from - to} units of repellent. They are fleeing from the source but staying within {HomeBlock} block for now."));
                    }
                }
            }

            base.OnHealthChanged(from, to, critical, source);
        }

        private void Update()
        {
            Move(currentSpeedMultiplier);
        }

        private IEnumerator ProcessAI()
        {
            yield return null;

            while (true) {
                if (!isAlive)
                {
                    SetHealth(1, false, null);
                    isAlive = true;
                    currentState = State.Flee;
                    MoveTargetPosition = GetFriendlyPositionOrDie();
                    OnAnimalAction(new AnimalActionEvent($"{ToString()} has been hit by too much repellent. They are fleeing from the block."));
                }

                ScanForEnemies();

                switch (currentState)
                {
                    case State.Idle:
                        UpdateIdleState();
                        break;
                    case State.GatherRepellent:
                        UpdateGatherRepellentState();
                        break;
                    case State.PlaceRepellent:
                        UpdatePlaceRepellent();
                        break;
                    case State.Flee:
                        currentSpeedMultiplier = 2;
                        if (Mathf.Approximately(Vector3.SqrMagnitude(MoveTargetPosition - transform.position), 0))
                        {
                            currentState = State.Hide;
                        }
                        break;
                    case State.Hide:
                        if (health >= healthMax * 0.8f)
                        {
                            currentState = State.Idle;
                        }
                        break;
                    case State.Attack:
                        UpdateAttackState();
                        break;
                    case State.Expand:
                        currentSpeedMultiplier = 1.5f;
                        float distanceToTarget = Vector3.SqrMagnitude(MoveTargetPosition - transform.position);
                        if (distanceToTarget < sqrAttackDistance)
                        {
                            currentState = State.Idle;
                        }
                        break;
                    case State.Breed:
                        UpdateBreedState();
                        break;
                }

                yield return new WaitForSeconds(aiUpdateDelay);
            }
        }

        private void UpdatePlaceRepellent()
        {
            currentSpeedMultiplier = 1;

            int dropIndex = int.MaxValue;
            if (HomeBlock.Player)
            {
                for (dropIndex = 0; dropIndex < m_RepellentAmmoPickups.Length; dropIndex++)
                {
                    if (m_RepellentAmmoPickups[dropIndex].RequiredRepellent < availableRepellent && Random.value < 1f / m_RepellentAmmoPickups.Length) break;
                }
            }

            if (Mathf.Approximately(Vector3.SqrMagnitude(MoveTargetPosition - transform.position), 0))
            {
                //OPTIMIZATION Use Pool
                GameObject go;
                if (dropIndex > m_RepellentAmmoPickups.Length)
                {
                    go = Instantiate<Mine>(m_RepellentMine).gameObject;
                    availableRepellent -= m_RepellentMine.RequiredRepellent;
                }
                else
                {
                    go = Instantiate(m_RepellentAmmoPickups[dropIndex].gameObject);
                    availableRepellent -= m_RepellentAmmoPickups[dropIndex].RequiredRepellent;
                }
                go.transform.position = transform.position;
                currentState = State.Idle;
                // TODO experience awarded should be configurable and/or a random range
                m_LevelSystem.AddExperience(2);
                OnAnimalAction(new AnimalActionEvent($"{ToString()} placed a repellent mine at {transform.position}.", Importance.Low));
            }
        }

        private void UpdateGatherRepellentState()
        {   
            if (randomizeReppelentGatheringSpeed)
            {
                currentSpeedMultiplier = 1;
                availableRepellent += Time.deltaTime * Random.Range(0.01f, m_CurrentRepellentGatheringSpeed);
            }
            else
            {
                availableRepellent += Time.deltaTime * m_CurrentRepellentGatheringSpeed;
            }
            if (Mathf.Approximately(Vector3.SqrMagnitude(MoveTargetPosition - transform.position), 0))
            {
                currentState = State.Idle;
            }
        }

        private void UpdateIdleState()
        {
            if (CanPlaceRepellentTrigger)
            {
                currentState = State.PlaceRepellent;
            }
            else if (HomeBlock.GetPriority(m_Faction) == Priority.Breed && Random.value < m_BreedingChance)
            {
                currentState = State.Breed;
                timeToRevaluateState = Time.timeSinceLevelLoad + m_BreedingDuration;
            }
            else if (Random.value < 0.02f)
            {
                if (health >= healthMax * 0.8f &&
                    HomeBlock.GetPriority(m_Faction) == Priority.Low)
                {
                    SetToExpandStateIfPossible();
                }
                else
                {
                    currentState = State.GatherRepellent;
                    MoveTargetPosition = GetNewWanderPositionWithinHomeBlock();
                }
            }
        }

        private void UpdateBreedState()
        {
            if (Time.timeSinceLevelLoad <= timeToRevaluateState)
            {
                return;
            }

            AnimalController newAnimal = null;
            if (m_Faction == Faction.Dog)
            {
                newAnimal = HomeBlock.CityController.SpawnDog(HomeBlock);
            }
            else if (m_Faction == Faction.Cat)
            {
                newAnimal = HomeBlock.CityController.SpawnCat(HomeBlock);
            }
            else
            {
                Debug.LogError($"Attempted to spawn an animal of an unkown faction ({m_Faction}).");
                return;
            }
            
            OnAnimalAction(new AnimalActionEvent($"{newAnimal} was born in {HomeBlock} and is being sent out to expand the factions control.", Importance.Medium));
            newAnimal.SetToExpandStateIfPossible(100);

            // TODO experience awarded should be configurable and/or a random range
            m_LevelSystem.AddExperience(1);

            currentState = State.Idle;
        }

        private void UpdateAttackState()
        {
            if (!attackTarget)
            {
                currentState = State.Idle;
                return;
            }

            float distanceFromHome = Vector3.SqrMagnitude(HomeBlock.transform.position - transform.position);
            if (distanceFromHome > sqrChaseDistance)
            {
                currentState = State.Idle;
                return;
            }

            float distanceToTarget = Vector3.SqrMagnitude(attackTarget.position - transform.position);


            //OPTIMIZATION: only play sounds if within hearing distance of the player
            if (!audioSource.isPlaying && m_AttackSounds.Length > 0 && Random.value > 0.1)
            {
                audioSource.clip = m_AttackSounds[Random.Range(0, m_AttackSounds.Length)];
                audioSource.Play();
            }

            if (distanceToTarget < sqrAttackDistance)
            {
                if (Time.timeSinceLevelLoad > timeOfNextAttack)
                {
                    attackTarget.GetComponentInChildren<IDamageHandler>().AddDamage(m_Damage);
                    timeOfNextAttack = Time.timeSinceLevelLoad + m_AttackFrequency;
                }
            }
            else
            {
                MoveTargetPosition = attackTarget.position;
                currentSpeedMultiplier = 2;
            }
        }

        /// <summary>
        /// If a block suitable for expansion is found within the desired range then set the animals state to expand into that block.
        /// Otherwise go to the Gather state.
        /// </summary>
        private void SetToExpandStateIfPossible(int range = 3)
        {
            ExpandToBlock = GetNearbyHighPriorityBlock(range);
            if (ExpandToBlock != null)
            {
                OnAnimalAction(new AnimalActionEvent($"{ToString()} is leaving {HomeBlock} in an attempt to take {ExpandToBlock} for the {m_Faction}s.", Importance.Medium));
                MoveTargetPosition = ExpandToBlock.GetRandomPoint();
                currentState = State.Expand;
            }
            else
            {
                currentState = State.GatherRepellent;
                MoveTargetPosition = GetNewWanderPositionWithinHomeBlock();
            }
        }

        /// <summary>
        /// If the animal is not already in an attack state, or in a Flee state, scan the area for enemies. If one is spotted within a reasonable range then make it a target and attack.
        /// </summary>
        void ScanForEnemies()
        {
            if (currentState == State.Attack || currentState == State.Flee) return;

            if (m_Faction == Faction.Dog && HomeBlock.Player)
            {
                currentState = State.Attack;
                attackTarget = HomeBlock.Player.transform;
            }

            List<AnimalController> enemies = HomeBlock.GetEnemiesOf(m_Faction);
            float sqrDistance = float.MaxValue;
            AnimalController nearest = null;
            for (int i = 0; i < enemies.Count; i++)
            {
                //OPTIMIZATION under some circumstance an enemy can be destroyed and still be present, not sure how. For now just skip it.
                if (enemies[i] == null)
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(enemies[i].transform.position - transform.position);
                if (distance < sqrDistance)
                {
                    nearest = enemies[i];
                    sqrDistance = distance;
                }
            }

            if (sqrDistance < m_ChaseDistance / 2)
            {
                attackTarget = nearest.transform;
                currentState = State.Attack;
            }
        }

        Vector3 GetNewWanderPositionWithinHomeBlock()
        {
            return HomeBlock.GetRandomPoint();
        }

        void Move(float speedMultiplier = 1)
        {
            Rotate();
            float step = m_Speed * speedMultiplier * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, MoveTargetPosition, step);
        }

        void Rotate()
        {
            Vector3 targetDirection = MoveTargetPosition - transform.position;
            float singleStep = m_RotationSpeed * Time.deltaTime;
            Vector3 newDirection = Vector3.RotateTowards(transform.forward, targetDirection, singleStep, 0.0f);
            transform.rotation = Quaternion.LookRotation(newDirection);
        }

        /// <summary>
        /// Find a block that is under the complete control of this faction.
        /// If no fully controlled block is found then the animal will die and exit the game.
        /// </summary>
        /// <returns>Either a nearby, fully controlled block friendly block, if one exists, or the animal will die</returns>
        private Vector3 GetFriendlyPositionOrDie()
        {
            if (HomeBlock.CityController.GetPopulation(m_Faction) > HomeBlock.CityController.MaxFactionSize(m_Faction))
            {
                OnAnimalAction(new AnimalActionEvent($"{ToString()} feels there is too much competition within the faction. They have given up the fight and left the city.", Importance.High));
                Die();
                return Vector3.zero; // this will never be used as the Die method will destroy the animal
            }

            int x = HomeBlock.Coordinates.x;
            int y = HomeBlock.Coordinates.y;

            BlockController targetBlock = null;
            int maxDistance = 7;
            for (int d = 1; d < maxDistance; d++)
            {
                for (int i = 0; i < d + 1; i++)
                {
                    int x1 = x - d + i;
                    int y1 = y - i;

                    if ((x1 != x || y1 != y)
                        && (x1 >= 0 && x1 < HomeBlock.CityController.Width)
                        && (y1 >= 0 && y1 < HomeBlock.CityController.Depth)) {
                        if (HomeBlock.CityController.GetBlock(x1, y1).DominantFaction == m_Faction)
                        {
                            targetBlock = HomeBlock.CityController.GetBlock(x1, y1);
                            break;
                        }
                    }

                    int x2 = x + d - i;
                    int y2 = y + i;

                    if ((x2 != x || y2 != y)
                        && (x2 >= 0 && x2 < HomeBlock.CityController.Width)
                        && (y2 >= 0 && y2 < HomeBlock.CityController.Depth))
                    {
                        if (HomeBlock.CityController.GetBlock(x2, y2).DominantFaction == m_Faction)
                        {
                            targetBlock = HomeBlock.CityController.GetBlock(x2, y2);
                            break;
                        }
                    }
                }
                
                if (targetBlock != null) break;
            }

            if (targetBlock != null)
            {
                return targetBlock.GetRandomPoint();
            } else
            {
                OnAnimalAction(new AnimalActionEvent($"{ToString()} was unable to find a nearby block to feel safe in. They have given up the fight and left the city.", Importance.High));
                Die();
                return Vector3.zero; // this will never be used as the Die method will destroy the animal
            }
        }
        
        /// <summary>
        /// Find a block that is within x blocks fo the current home block and is marked as high priority by the director.
        /// If no block is found then return null.
        /// </summary>
        /// <returns>A high priority block within x blocks of the current homes directory, or null if none found.</returns>
        private BlockController GetNearbyHighPriorityBlock(int maxDistance = 3)
        {
            if (HomeBlock == null) return null;

            int x = HomeBlock.Coordinates.x;
            int y = HomeBlock.Coordinates.y;

            BlockController targetBlock = null;
            for (int d = 1; d < maxDistance; d++)
            {
                for (int i = 0; i < d + 1; i++)
                {
                    int x1 = x - d + i;
                    int y1 = y - i;

                    if ((x1 != x || y1 != y)
                        && (x1 >= 0 && x1 < HomeBlock.CityController.Width)
                        && (y1 >= 0 && y1 < HomeBlock.CityController.Depth))
                    {
                        if (HomeBlock.CityController.GetBlock(x1, y1).GetPriority(m_Faction) == Priority.High)
                        {
                            targetBlock = HomeBlock.CityController.GetBlock(x1, y1);
                            break;
                        }
                    }

                    int x2 = x + d - i;
                    int y2 = y + i;

                    if ((x2 != x || y2 != y)
                        && (x2 >= 0 && x2 < HomeBlock.CityController.Width)
                        && (y2 >= 0 && y2 < HomeBlock.CityController.Depth))
                    {
                        if (HomeBlock.CityController.GetBlock(x2, y2).GetPriority(m_Faction) == Priority.High)
                        {
                            targetBlock = HomeBlock.CityController.GetBlock(x2, y2);
                            break;
                        }
                    }
                }

                if (targetBlock != null) break;
            }
            return targetBlock;
        }

        void Die()
        {
            if (m_DeathSounds.Length > 0)
            {
                audioSource.clip = m_DeathSounds[Random.Range(0, m_DeathSounds.Length)];
                audioSource.Play();
            }
            HomeBlock.RemoveAnimal(this);
            if (OnDeath != null)
            {
                OnDeath.Invoke(this);
            }
            Destroy(gameObject, 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(MoveTargetPosition, 0.5f);
        }

        public override string ToString()
        {
            return name;
        }
    }
}
