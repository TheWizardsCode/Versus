using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WizardsCode.Versus
{
    /// <summary>
    /// The TopDownUX manages the entire user experience in the top down view. 
    /// User input and coordination of the UI is all managed here.
    /// </summary>
    public class TopDownUX : MonoBehaviour
    {
        [SerializeField, Tooltip("Camera used in top down view.")]
        Camera m_TopDownCamera;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = m_TopDownCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    Debug.Log("Clicked on " + hit.ToString());
                }
            }
        }
    }
}
