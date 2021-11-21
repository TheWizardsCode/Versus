using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static WizardsCode.Versus.Controllers.CityController;
using WizardsCode.Versus.Controllers;

namespace WizardsCode.Versus.Controller
{
    public class BlockController : MonoBehaviour
    {
        [HideInInspector, SerializeField, Tooltip("The z,y coordinates of this block within the city.")]
        public Vector2 Coordinates;
        [HideInInspector, SerializeField, Tooltip("The type of block this is. The block type dictates what is generated within the block.")]
        public BlockType BlockType;
        [SerializeField, Tooltip("The level of normalized level of influence that cats have on this block."), Range(0f, 1f)]
        public float CatInfluence = 0;
        [SerializeField, Tooltip("The level of normalized level of influence that dogs have on this block."), Range(0f, 1f)]
        public float DogInfluence = 0;
        [SerializeField, Tooltip("The mesh that will show the faction control.")]
        Transform m_FactionMap;

        int imageWidth = 100;
        int imageDepth = 100;
        private Mesh m_FactionMesh;
        private CityController cityController;

        private void Start()
        {
            m_FactionMesh = m_FactionMap.GetComponent<MeshFilter>().mesh;
            cityController = FindObjectOfType<CityController>();
        }

        private void Update()
        {
            //OPTIMIZATION: probably update the faction mesh on a longer cycle than every frame
            //OPTIMIZATION: if this is not in view of the camera there is no need to update the faction mesh
            Vector3[] vertices = m_FactionMesh.vertices;
            Color32[] colors = new Color32[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                // 0.5 is neutral, 0 is cat controlled, 1 is dog controlled
                float influence = 0.5f;
                if (DogInfluence > CatInfluence)
                {
                    influence = (DogInfluence - CatInfluence) / 2 + 0.5f;
                }
                else if (CatInfluence > DogInfluence)
                {
                    influence = 0.5f - (CatInfluence - DogInfluence) / 2;
                }
                colors[i] = cityController.m_FactionGradient.Evaluate(influence);
            }

            m_FactionMesh.colors32 = colors;
        }
    }
}
