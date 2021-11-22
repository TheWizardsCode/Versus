using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WizardsCode.Versus.Weapons;
using NeoFPS;

namespace WizardsCode.Versus.Controller
{
    /// <summary>
    /// The animal controller is placed on each of the AI animals int he game and is responsible for managing their behaviour.
    /// </summary>
    public class AnimalController : RechargingHealthManager
    {
        public enum State { Idle, GatherRepellent, PlaceRepellentMine, Flee, Hide }
        public enum Faction { Cat, Dog, Neutral }
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

        [Header("Weapons")]
        [SerializeField, Tooltip("The mines this animal knows how to craft and plant.")]
        Mine m_RepellentMinePrefab;
        [SerializeField, Tooltip("The rate at which this animal will collect repellent for making weaponry. Measured in repellent per second.")]
        float m_RepellentGatheringSpeed = 0.2f;

        private BlockController blockController;
        private State currentState = State.Idle;
        private Vector3 moveTargetPosition;
        private float availableRepellent;

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
        }

        protected override void OnHealthChanged(float from, float to, bool critical, IDamageSource source)
        {
            if (to < from)
            {
                currentState = State.Flee;
                moveTargetPosition = GetNewWanderPosition();
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
                    availableRepellent += Time.deltaTime * m_RepellentGatheringSpeed;
                    Rotate();
                    Move();
                    if (Mathf.Approximately(Vector3.SqrMagnitude(moveTargetPosition - transform.position), 0))
                    {
                        currentState = State.Idle;
                    }
                    break;
                case State.PlaceRepellentMine:
                    Rotate();
                    Move();
                    if (Mathf.Approximately(Vector3.SqrMagnitude(moveTargetPosition - transform.position), 0))
                    {
                        Mine go = Instantiate<Mine>(m_RepellentMinePrefab);
                        go.transform.position = transform.position;
                        availableRepellent -= go.RequiredRepellent;
                        currentState = State.GatherRepellent;
                    }
                    break;
                case State.Flee:
                    Rotate();
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
            }
        }

        Vector3 GetNewWanderPosition()
        {
            return blockController.GetRandomPoint();
        }

        void Move(float speedMultiplier = 1)
        {
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
                        && (x1 >= 0 && x1 <= HomeBlock.CityController.Width)
                        && (y1 >= 0 && y1 <= HomeBlock.CityController.Depth)) {
                        if (HomeBlock.CityController.GetBlock(x1, y1).ControllingFaction == m_Faction)
                        {
                            targetBlock = HomeBlock.CityController.GetBlock(x1, y1);
                            break;
                        }
                    }

                    int x2 = x + d - i;
                    int y2 = y + i;

                    if ((x2 != x || y2 != y)
                        && (x2 >= 0 && x2 <= HomeBlock.CityController.Width)
                        && (y2 >= 0 && y2 <= HomeBlock.CityController.Depth))
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
                        && (x1 >= 0 && x1 <= HomeBlock.CityController.Width)
                        && (y1 >= 0 && y1 <= HomeBlock.CityController.Depth))
                    {
                        targetBlock = HomeBlock.CityController.GetBlock(x1, y1);
                        break;
                    }

                    int x2 = x + i;
                    int y2 = y - d + i;
                    if ((x2 != x || y2 != y)
                        && (x2 >= 0 && x2 <= HomeBlock.CityController.Width)
                        && (y2 >= 0 && y2 <= HomeBlock.CityController.Depth))
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
