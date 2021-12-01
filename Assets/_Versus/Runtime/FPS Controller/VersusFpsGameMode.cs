using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NeoFPS.SinglePlayer;
using NeoFPS;
using WizardsCode.Versus.Controllers;
using NeoSaveGames.Serialization;
using WizardsCode.Versus.FPS;
using WizardsCode.Versus.Controller;

namespace WizardsCode.Versus
{
    public class VersusFpsGameMode : FpsGameMode
    {
        [Header("Neo FPS")]
        [SerializeField, Tooltip("Should the game mode automatically spawn a player character immediately on start.")]
        private bool m_SpawnOnStart = true;
        [SerializeField, NeoPrefabField(required = true), Tooltip("The player prefab to instantiate if none exists.")]
        private FpsSoloPlayerController m_PlayerPrefab = null;
        [SerializeField, NeoPrefabField(required = true), Tooltip("The character prefab to use.")]
        private PlayerCharacter m_CharacterPrefab = null;

        [Space]
        [Header("Debug")]
        [SerializeField, Tooltip("The block coordinates the character is spawned into. Normally this will be set at runtime but for debug purposes it is exposed here.")]
        Vector2Int m_SpawnBlockCoordinates = new Vector2Int(10, 10);

        CityController cityController;
        IController m_Player;

        public IController Player
        {
            get
            {
                if (m_Player == null)
                {
                        m_Player = InstantiatePlayer();
                }
                return m_Player;
            }
            protected set
            {
                m_Player = value;

                var playerComponent = m_Player as Component;
                if (playerComponent != null)
                {
                    var nsgo = playerComponent.GetComponent<NeoSerializedGameObject>();
                    if (nsgo.wasRuntimeInstantiated)
                        m_PersistentObjects[0] = nsgo;
                    else
                        m_PersistentObjects[0] = null;
                }
                else
                    m_PersistentObjects[0] = null;
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            cityController = FindObjectOfType<CityController>();

            if (m_SpawnOnStart) {
                inGame = true;
                Spawn(null);
            }
        }

        /// <summary>
        /// Spawn the player into FPS mode and configure the FPS game as in progress.
        /// <param name="block">The block the player is to spawn in. If this is null the block identified in the m_SpawnBlockCoordinates will be used.</param>
        /// </summary>
        public void Spawn(BlockController block)
        {
            if (!inGame) inGame = true;

            if (block == null)
            {
                block = cityController.GetBlock(m_SpawnBlockCoordinates);
            }

            NeoSerializedScene scene = GetComponent<NeoSerializedGameObject>().serializedScene;

            var prototype = GetPlayerCharacterPrototype(Player);
            SpawnPoint spawnPoint = block.GetFpsSpawnPoint();
            character = spawnPoint.SpawnCharacter(prototype, Player, true, scene);
            if (character != null)
            {
                ((PlayerCharacter)character).CurrentBlock = block;
            }
            else
            {
                Debug.LogError("No valid spawn points found");
            }
        }

        /// <summary>
        /// Despawn the FPS player.
        /// </summary>
        public void Despawn()
        {
            inGame = false;
            if (character != null)
            {
                Destroy(character.gameObject);
            }
        }

        protected override void ProcessOldPlayerCharacter(ICharacter oldCharacter)
        {
            if (oldCharacter != null)
                Destroy(oldCharacter.gameObject);
        }

        protected override IController InstantiatePlayer()
        {
            NeoSerializedGameObject nsgo = GetComponent<NeoSerializedGameObject>();
            if (nsgo != null && nsgo.serializedScene != null)
                return nsgo.serializedScene.InstantiatePrefab(m_PlayerPrefab);
            else
                return Instantiate(m_PlayerPrefab);
        }

        protected override ICharacter GetPlayerCharacterPrototype(IController player)
        {
            return m_CharacterPrefab;
        }

        #region PERSISTENCE

        private NeoSerializedGameObject[] m_PersistentObjects = new NeoSerializedGameObject[2];
        private ICharacter character;

        protected override NeoSerializedGameObject[] GetPersistentObjects()
        {
            if (m_PersistentObjects[0] != null && m_PersistentObjects[1] != null)
                return m_PersistentObjects;
            else
            {
                Debug.Log("No Persistence Save Objects. Does the scene have a SceneSaveInfo object correctly set up?");
                Debug.Log("m_PersistentObjects[0] != null: " + (m_PersistentObjects[0] != null));
                Debug.Log("m_PersistentObjects[1] != null: " + (m_PersistentObjects[1] != null));
                return null;
            }
        }

        protected override void SetPersistentObjects(NeoSerializedGameObject[] objects)
        {
            var controller = objects[0].GetComponent<IController>();
            if (controller != null)
            {
                Player = controller;

                var character = objects[1].GetComponent<ICharacter>();
                if (character != null)
                    Player.currentCharacter = character;
            }
        }

        #endregion
    }
}
