using UnityEngine;
using WizardsCode.Versus.Weapons;
using NeoFPS;
using System;
using Random = UnityEngine.Random;
using System.Collections.Generic;

namespace WizardsCode.Versus.Controller
{
    /// <summary>
    /// The animal controller is placed on each of the AI animals in the game and is responsible for managing their behaviour.
    /// </summary>
    public class AnimalController : RechargingHealthManager
    {
        public enum State { Idle, GatherRepellent, PlaceRepellentMine, Flee, Hide, Attack }
        public enum Faction { Cat, Dog, Neutral }

        [Space]

        [Header("Attributes")]
        [SerializeField, Tooltip("If set to true then the rate of resource gathering will be randomly set upon creation of an object with this controller attached. If set to false then the rate can be set here.")]
        bool randomizeReppelentGatheringSpeed = true;
        [SerializeField, Tooltip("The maximum rate at which this animal will collect repellent for making weaponry. Measured in repellent per second. If randomizeReppelentGatheringSpeed is true this value will be randomized between 0.01 and this value. If set to false it will be this amount precisely.")]
        float m_RepellentGatheringSpeed;

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
        [SerializeField, Tooltip("The mines this animal knows how to craft and plant.")]
        Mine m_RepellentMinePrefab;

        private BlockController blockController;
        internal State currentState = State.Idle;
        internal Transform target;
        private float sqrChaseDistance = 0;
        private float sqrAttackDistance = 0;
        private float timeOfNextAttack = 0;
        private float timeOfNextEnemyScan = 0;
        private Vector3 moveTargetPosition;
        private float availableRepellent;

        public delegate void OnAnimalActionDelegate(VersuseEvent versusEvent);
        public OnAnimalActionDelegate OnAnimalAction;

        internal BlockController HomeBlock
        {
            get { return m_HomeBlock; }
            set { m_HomeBlock = value; }
        }

        bool CanPlaceRepellentTrigger
        {
            get
            {
                return m_RepellentMinePrefab != null && availableRepellent >= m_RepellentMinePrefab.RequiredRepellent;
            }
        }

        private void Start()
        {
            blockController = GetComponentInParent<BlockController>();
            sqrChaseDistance = m_ChaseDistance * m_ChaseDistance;
            sqrAttackDistance = m_AttackDistance * m_AttackDistance;
        }

        protected override void OnHealthChanged(float from, float to, bool critical, IDamageSource source)
        {
            if (to < from && to > 0)
            {
                currentState = State.Flee;
                moveTargetPosition = GetNewWanderPosition();
                OnAnimalAction(new AnimalActionEvent($"{ToString()} as been hit by {from - to} units of repellent. They are fleeing from the source but not yet giving up this block."));
            }

            base.OnHealthChanged(from, to, critical, source);
        }

        private void Update()
        {
            if (!isAlive)
            {
                SetHealth(1, false, null);
                isAlive = true;
                currentState = State.Flee;
                moveTargetPosition = GetFriendlyPositionOrDie();
                OnAnimalAction(new AnimalActionEvent($"{ToString()} has been hit by too much repellent. They are fleeing from the block."));
            }

            if (Time.timeSinceLevelLoad > timeOfNextEnemyScan)
            {
                ScanForEnemies();
                timeOfNextEnemyScan = Time.timeSinceLevelLoad + m_EnemyScanFrequency * Random.Range(0.9f, 1.1f);
            }

            switch (currentState)
            {
                case State.Idle:
                    if (CanPlaceRepellentTrigger)
                    {
                        currentState = State.PlaceRepellentMine;
                    }
                    else if (Random.value < 0.02f)
                    {
                        currentState = State.GatherRepellent;
                        moveTargetPosition = GetNewWanderPosition();
                    }
                    break;
                case State.GatherRepellent:
                    if (randomizeReppelentGatheringSpeed)
                    {
                        availableRepellent += Time.deltaTime * Random.Range(0.01f, m_RepellentGatheringSpeed);
                    } else
                    {
                        availableRepellent += Time.deltaTime * m_RepellentGatheringSpeed;
                    }
                    Move();
                    if (Mathf.Approximately(Vector3.SqrMagnitude(moveTargetPosition - transform.position), 0))
                    {
                        currentState = State.Idle;
                    }
                    break;
                case State.PlaceRepellentMine:
                    Move();
                    if (Mathf.Approximately(Vector3.SqrMagnitude(moveTargetPosition - transform.position), 0))
                    {
                        Mine go = Instantiate<Mine>(m_RepellentMinePrefab);
                        go.transform.position = transform.position;
                        availableRepellent -= go.RequiredRepellent;
                        currentState = State.GatherRepellent;
                        OnAnimalAction(new AnimalActionEvent($"{ToString()} placed a repellent mine at {transform.position}.", Importance.Low));
                    }
                    break;
                case State.Flee:
                    Move(2);
                    if (Mathf.Approximately(Vector3.SqrMagnitude(moveTargetPosition - transform.position), 0))
                    {
                        currentState = State.Hide;
                    }
                    break;
                case State.Hide:
                    if (health >= healthMax)
                    {
                        currentState = State.Idle;
                    }
                    break;
                case State.Attack:
                    float distanceFromHome = Vector3.SqrMagnitude(blockController.transform.position - transform.position);
                    if (distanceFromHome > sqrChaseDistance)
                    {
                        currentState = State.Idle;
                        break;
                    }

                    float distanceToTarget = Vector3.SqrMagnitude(target.position - transform.position);
                    if (distanceToTarget < sqrAttackDistance)
                    {
                        if (Time.timeSinceLevelLoad > timeOfNextAttack)
                        {
                            target.GetComponentInChildren<IDamageHandler>().AddDamage(m_Damage);
                            timeOfNextAttack = Time.timeSinceLevelLoad + m_AttackFrequency;
                        }
                    }
                    else
                    {
                        moveTargetPosition = target.position;
                        Move(2);
                    }
                    break;
            }
        }

        /// <summary>
        /// If the animal is not already in an attack state, or in a Flee state, scan the area for enemies. If one is spotted within a reasonable range then make it a target and attack.
        /// </summary>
        void ScanForEnemies()
        {
            if (currentState == State.Attack || currentState == State.Flee) return;

            List<AnimalController> enemies = blockController.GetEnemiesOf(m_Faction);
            float sqrDistance = float.MaxValue;
            AnimalController nearest = null;
            for (int i = 0; i < enemies.Count; i++)
            {
                float distance = Vector3.SqrMagnitude(enemies[i].transform.position - transform.position);
                if (distance < sqrDistance)
                {
                    nearest = enemies[i];
                    sqrDistance = distance;
                }
            }

            if (sqrDistance < m_ChaseDistance / 2)
            {
                target = nearest.transform;
                currentState = State.Attack;
            }
        }

        Vector3 GetNewWanderPosition()
        {
            return blockController.GetRandomPoint();
        }

        void Move(float speedMultiplier = 1)
        {
            Rotate();
            float step = m_Speed * speedMultiplier * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, moveTargetPosition, step);
        }

        void Rotate()
        {
            Vector3 targetDirection = moveTargetPosition - transform.position;
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
                        if (HomeBlock.CityController.GetBlock(x1, y1).ControllingFaction == m_Faction)
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
                        if (HomeBlock.CityController.GetBlock(x2, y2).ControllingFaction == m_Faction)
                        {
                            targetBlock = HomeBlock.CityController.GetBlock(x2, y2);
                            break;
                        }
                    }
                }

                if (targetBlock != null) break;

                for (int i = 1; i < d; i++)
                {
                    int x1 = x - i;
                    int y1 = y + d - i;

                    if ((x1 != x || y1 != y)
                        && (x1 >= 0 && x1 < HomeBlock.CityController.Width)
                        && (y1 >= 0 && y1 < HomeBlock.CityController.Depth))
                    {
                        targetBlock = HomeBlock.CityController.GetBlock(x1, y1);
                        break;
                    }

                    int x2 = x + i;
                    int y2 = y - d + i;
                    if ((x2 != x || y2 != y)
                        && (x2 >= 0 && x2 < HomeBlock.CityController.Width)
                        && (y2 >= 0 && y2 < HomeBlock.CityController.Depth))
                    {
                        targetBlock = HomeBlock.CityController.GetBlock(x2, y2);
                        break;
                    }
                }

                if (targetBlock != null) break;
            }

            if (targetBlock != null)
            {
                return targetBlock.GetRandomPoint();
            } else
            {
                Die();
                return Vector3.zero; // this will never be used as the Die method will destroy the animal
            }
        }

        void Die() {
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(moveTargetPosition, 0.5f);
        }
    }
}
