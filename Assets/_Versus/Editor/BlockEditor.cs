using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using WizardsCode.Versus.Controller;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace WizardsCode.Versus
{
    [CustomEditor(typeof(BlockController))]
    public class BlockEditor : Editor
    {
        public VisualTreeAsset m_InspectorXML;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement myInspector = new VisualElement();
            m_InspectorXML.CloneTree(myInspector);

            VisualElement inspectorFoldout = myInspector.Q("DefaultInspector");
            InspectorElement.FillDefaultInspector(inspectorFoldout, serializedObject, this);

            return myInspector;
        }
    }
}
