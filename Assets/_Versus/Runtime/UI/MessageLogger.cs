using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WizardsCode.Versus.Controller;

namespace WizardsCode.Versus
{
    /// <summary>
    /// Responsible for logging messages to the UI
    /// Provide it with a prefab for the text UI and a container
    /// </summary>
    public class MessageLogger
    {
        private int maxMessages = 100;
        private List<Message> messageList = new List<Message>();

        private GameObject _eventMessageTextPrefab;
        private GameObject _eventMessageContainer;
        
        public MessageLogger(GameObject prefab, GameObject container)
        {
            _eventMessageTextPrefab = prefab;
            _eventMessageContainer = container;
        }

        public int MaxMessages
        {
            get { return maxMessages;}
            set { maxMessages = value; }
        }

        public void OnBlockUpdated(BlockController block, VersuseEvent versusEvent)
        {
            SendMessageToChat($"{versusEvent.Description}");
        }

        public void SendMessageToChat(string text)
        {
            Debug.Log($"message log size: {messageList.Count}");
            if(messageList.Count >= maxMessages)
            {
                if (messageList[0].textObject != null)
                {
                    Object.Destroy(messageList[0].textObject.gameObject);
                    messageList.Remove(messageList[0]);
                }
            }
            var newMessage = new Message();
            newMessage.text = text;
            // TODO pool objects if we have some sort of pooling system available
            var newText = Object.Instantiate(_eventMessageTextPrefab, _eventMessageContainer.transform);
            newMessage.textObject = newText.GetComponent<TextMeshProUGUI>();
            newMessage.textObject.text = newMessage.text;
            messageList.Add(newMessage);
        }
    }

    public class Message
    {
        public string text;
        public TextMeshProUGUI textObject;
    }
}