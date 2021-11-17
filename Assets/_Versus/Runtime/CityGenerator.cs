using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;
using WizardsCode.Versus.Controller;

namespace WizardsCode.Versus.Controllers

{
    /// <summary>
    /// The City Controller is responsible for creating the city at the start
    /// of the game and tracking which faction owns which territory. 
    /// Other objects can query the city controller to ask the status
    /// of any particular block in the city.
    /// </summary>
    public class CityGenerator : MonoBehaviour
    {
        public enum BlockType {  Suburban, OuterCity, City, InnerCity }
        [Header("Level Generation")]
        [SerializeField, Tooltip("The width of the city in blocks. A single block is 100 x 100, not including the roads connecting blocks.")]
        int m_CityWidth = 15;
        [SerializeField, Tooltip("The depth of the city in blocks. A single block is 100 x 100, not including the roads connecting blocks.")]
        int m_CityDepth = 15;
        [SerializeField, Tooltip("The size of a city block. It will be this size square.")]
        int m_BlockSize = 100;
        [SerializeField, Tooltip("The widh of a city road. note that this includes pavements/sidewalks.")]
        int m_RoadWidth = 10;
        [SerializeField, Tooltip("The prefabs to use when spawning suburban blocks. If you want a prefab to appear more frequently, have more instances.")]
        BlockController[] m_SuburbanBlockPrefabs;
        [SerializeField, Tooltip("The prefabs to use when spawning outer city blocks. If you want a prefab to appear more frequently, have more instances.")]
        BlockController[] m_OuterCityBlockPrefabs;
        [SerializeField, Tooltip("The prefabs to use when spawning city blocks. If you want a prefab to appear more frequently, have more instances.")]
        BlockController[] m_CityBlockPrefabs;
        [SerializeField, Tooltip("The prefabs to use when spawning inner city blocks. If you want a prefab to appear more frequently, have more instances.")]
        BlockController[] m_InnerCityBlockPrefabs;
        [SerializeField, Tooltip("The texture that is used to overlay the cat/dog control map on this block.")]
        public Transform mapPlane;

        [Header("Top Down")]
        Camera m_TopDownCamera;

        BlockController[,] cityBlocks;

        private void Start()
        {
            cityBlocks = new BlockController[m_CityDepth, m_CityWidth];
            int blockSize = m_BlockSize + (2 * m_RoadWidth);
            Vector3 center = new Vector3(blockSize * m_CityDepth / 2, 0, (blockSize * m_CityWidth) / 2);
            float maxDistanceFromCenter = Vector3.Distance(center, Vector3.zero);

            float territoryDepth = blockSize * m_CityDepth;
            float territoryWidth = blockSize * m_CityWidth;
            mapPlane.localScale = new Vector3(territoryDepth / 10, 1, territoryWidth / 10);
            mapPlane.position = new Vector3((territoryDepth / 2) - blockSize, 0.1f, (territoryWidth / 2) - blockSize);

            BlockController block;
            BlockType blockType = BlockType.Suburban;
            Vector3 position;
            float distanceFromCenter;
            Quaternion rotation;
            for (int z = 0; z < m_CityWidth; z++)
            {
                for (int x = 0; x < m_CityWidth; x++)
                {
                    position = new Vector3(x * blockSize, 0, z * blockSize);
                    rotation = Quaternion.identity;

                    distanceFromCenter = Vector3.Distance(position, center);
                    if (distanceFromCenter <= maxDistanceFromCenter / 4)
                    {
                        blockType = BlockType.InnerCity;
                    }
                    else if (distanceFromCenter <= maxDistanceFromCenter / 2)
                    {
                        blockType = BlockType.City;
                    }
                    else if (distanceFromCenter <= maxDistanceFromCenter / 1.5)
                    {
                        blockType = BlockType.OuterCity;
                    }
                    else
                    {
                        blockType = BlockType.Suburban;
                    }

                    switch (blockType) {
                        case BlockType.Suburban:
                            block = Instantiate(m_SuburbanBlockPrefabs[Random.Range(0, m_SuburbanBlockPrefabs.Length)], position, rotation);
                            break;
                        case BlockType.OuterCity:
                            block = Instantiate(m_OuterCityBlockPrefabs[Random.Range(0, m_OuterCityBlockPrefabs.Length)], position, rotation);
                            break;
                        case BlockType.City:
                            block = Instantiate(m_CityBlockPrefabs[Random.Range(0, m_CityBlockPrefabs.Length)], position, rotation);
                            break;
                        case BlockType.InnerCity:
                            block = Instantiate(m_InnerCityBlockPrefabs[Random.Range(0, m_InnerCityBlockPrefabs.Length)], position, rotation);
                            break;
                        default:
                            Debug.LogError("Unkown block type: " + blockType);
                            block = null;
                            break;
                    }

                    block.Coordinates = new Vector2(x, z);
                    block.BlockType = blockType;
                    block.CatInfluence = 1;
                    block.DogInfluence = 0;

                    cityBlocks[x, z] = block;
                }
            }
        }

        private void Update()
        {
            int blockSize = m_BlockSize + (2 * m_RoadWidth);
            Texture2D texture = new Texture2D((int)mapPlane.localScale.x, (int)mapPlane.localScale.z);
            Color[] pixels = new Color[(int)mapPlane.localScale.x * (int)mapPlane.localScale.z];
             
            for (int y = 0; y < m_CityWidth; y++)
            {
                for (int x = 0; x < m_CityWidth; x++)
                {
                    Color color = GetTerritoryColour(x, y);
                    for (int texX = x * blockSize; texX < x * blockSize; texX++)
                    {
                        for (int texY = y * blockSize; texX < y * blockSize; texY++)
                        {
                            pixels[texX + texY * (int)mapPlane.localScale.z] = color;
                        }
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();

            mapPlane.GetComponent<Renderer>().material.mainTexture = texture;
        }

        Color GetTerritoryColour(int x, int y)
        {
            return new Color(cityBlocks[x, y].CatInfluence * 256, 0, cityBlocks[x, y].DogInfluence * 256, 100);
        }
    }
}
