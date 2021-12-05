using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NeoFPS;

namespace WizardsCode.Versus
{
    /// <summary>
    /// Controls an individual building at runtime. Ensuring the right features are enabled for the current game mode and
    /// any available optimizations are applied.
    /// </summary>
    public class BuildingController : MonoBehaviour
    {
        [SerializeField, Tooltip("Ladders are only needed when running in FPS mode. Having a record of them here will allow them to be turned on and off.")]
        ContactLadder[] m_Ladders;

        GameManager manager;
        private void Awake()
        {
            manager = FindObjectOfType<GameManager>();
        }

        private void OnEnable()
        {
            manager.OnGameModeChanged += OnGameModeChanged;
        }

        private void OnDisable()
        {
            manager.OnGameModeChanged -= OnGameModeChanged;
        }

        private void OnGameModeChanged()
        {
            for (int l = 0; l < m_Ladders.Length; l++)
            {
                m_Ladders[l].gameObject.SetActive(manager.IsFpsMode);
            }
        }
    }
}
