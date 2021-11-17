using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static WizardsCode.Versus.Controllers.CityGenerator;

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

        int imageWidth = 100;
        int imageDepth = 100;
    }
}
