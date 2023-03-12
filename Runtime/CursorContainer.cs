using UnityEngine;
using UnityEngine.UIElements;

namespace PGIA
{
    /// <summary>
    /// 
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CursorContainer : MonoBehaviour
    {
        [Sirenix.OdinInspector.Required] public DragCursor CursorAsset;

        public void Start()
        {
            CursorAsset.Initialize(GetComponent<UIDocument>().rootVisualElement);
        }
    }
}
