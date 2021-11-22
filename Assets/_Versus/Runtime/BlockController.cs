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
        [SerializeField, Tooltip("The mesh that will show the faction control.")]
        Transform m_FactionMap;

        private Mesh m_FactionMesh;
        private CityController cityController;

        private List<AnimalController> m_DogsPresent = new List<AnimalController>();
        private List<AnimalController> m_CatsPresent = new List<AnimalController>();

        public float CatInfluence
        {
            get
            {
                if (m_CatsPresent.Count + m_DogsPresent.Count > 0)
                {
                    return m_CatsPresent.Count / (m_CatsPresent.Count + m_DogsPresent.Count);
                }
                else
                {
                    return 0;
                }
            }
        }
        public float DogInfluence
        {
            get
            {
                if (m_CatsPresent.Count + m_DogsPresent.Count > 0)
                {
                    return m_DogsPresent.Count / (m_CatsPresent.Count + m_DogsPresent.Count);
                } else
                {
                    return 0;
                }
            }
        }

        private void Start()
        {
            m_FactionMesh = m_FactionMap.GetComponent<MeshFilter>().mesh;
            cityController = FindObjectOfType<CityController>();
        }

        internal void AddCat(AnimalController cat)
        {
            m_CatsPresent.Add(cat);
            cat.transform.SetParent(transform);
        }

        internal void AddDog(AnimalController dog)
        {
            m_DogsPresent.Add(dog);
            dog.transform.SetParent(transform);
        }

        private void Update()
        {
            //OPTIMIZATION: probably update the faction mesh on a longer cycle than every frame
            //OPTIMIZATION: if this is not in view of the camera there is no need to update the faction mesh
            Vector3[] vertices = m_FactionMesh.vertices;
            Color32[] colors = new Color32[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                colors[i] = cityController.m_FactionGradient.Evaluate(NormalizedFactioninfluence);
            }

            m_FactionMesh.colors32 = colors;
        }

        /// <summary>
        /// Get a normalized value that represents the influence of each faction on this block.
        /// 0.5 is neutral, 0 is cat controlled, 1 is dog controlled
        /// </summary>
        /// <returns>0.5 is neutral, 0 is cat controlled, 1 is dog controlled</returns>
        internal float NormalizedFactioninfluence
        {
            get
            {
                float influence = 0.5f;
                if (DogInfluence > CatInfluence)
                {
                    influence = (DogInfluence - CatInfluence) / 2 + 0.5f;
                }
                else if (CatInfluence > DogInfluence)
                {
                    influence = 0.5f - (CatInfluence - DogInfluence) / 2;
                }

                return influence;
            }
        }
    }
}
