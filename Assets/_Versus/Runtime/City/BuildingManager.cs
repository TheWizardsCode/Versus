using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NeoFPS;
using WizardsCode.Versus.Controller;

namespace WizardsCode.Versus
{
    /// <summary>
    /// Manages the creation of a building plot and the turning on/off of features for different game modes.
    /// </summary>
    public class BuildingManager : MonoBehaviour
    {
        [SerializeField, Tooltip("A list of building models available for this building. On start one of these will be chosen at random and enabled. Others will be disabled.")]
        Transform[] m_Buildings;

        private void OnEnable()
        {
            int idx = Random.Range(0, m_Buildings.Length);
            for (int i = 0; i < m_Buildings.Length; i++)
            {
                m_Buildings[i].gameObject.SetActive(i == idx);
            }
        }
    }
}
