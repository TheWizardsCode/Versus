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
        public enum State { Idle, Wandering }
        [SerializeField, Tooltip("The speed this animal moves at under normal walking conditions.")]
        float m_Speed = 2;
        [SerializeField, Tooltip("The speed this animal rotates under normal walking conditions.")]
        float m_RotationSpeed = 25;

        private BlockController blockController;
        private State currentState = State.Idle;
        private float timeOfNextStateChange = 0;
        private Vector3 moveTargetPosition;

        public enum Faction {  Cat, Dog }
        [SerializeField, Tooltip("The faction this animal belongs to and fights for.")]
        public Faction m_Faction;

        private void Start()
        {
            blockController = GetComponentInParent<BlockController>();
        }

        private void Update()
        {
            if (Time.timeSinceLevelLoad < timeOfNextStateChange)
            {
                switch (currentState)
                {
                    case State.Idle:
                        break;
                    case State.Wandering:
                        Rotate();
                        Move();
                        if (Mathf.Approximately(Vector3.SqrMagnitude(moveTargetPosition - transform.position), 0))
                        {
                            timeOfNextStateChange = Time.timeSinceLevelLoad;
                        }
                        break;
                }
            }
            else
            {
                switch (currentState)
                {
                    case State.Idle:
                        currentState = State.Wandering;
                        moveTargetPosition = GetWanderPosition();
                        timeOfNextStateChange = float.MaxValue;
                        break;
                    case State.Wandering:
                        currentState = State.Idle;
                        timeOfNextStateChange = Time.timeSinceLevelLoad + Random.Range(2.5f, 7.5f);
                        break;
                }
            }

            Vector3 GetWanderPosition()
            {
                return blockController.GetRandomPoint();
            }

            void Move()
            {
                float step = m_Speed * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, moveTargetPosition, step);
            }

            void Rotate()
            {
                Vector3 targetDirection = moveTargetPosition - transform.position;
                float singleStep = m_RotationSpeed * Time.deltaTime;
                Vector3 newDirection = Vector3.RotateTowards(transform.forward, targetDirection, singleStep, 0.0f);
                transform.rotation = Quaternion.LookRotation(newDirection);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(moveTargetPosition, 0.5f);
        }
    }
}
