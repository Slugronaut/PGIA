using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PGIA
{
    /// <summary>
    /// To be shared between ModelViews, this represents the ircon of an item as it is being dragged around.
    /// </summary>
    [CreateAssetMenu(fileName = "Drag Cursor", menuName = "PGIA/Drag Cursor")]
    public class DragCursor : ScriptableObject
    {
        #region Fields and Properties
        [Space(12)]
        [Tooltip("A UI document describing the cursor.")]
        public VisualTreeAsset CursorAsset;

        VisualElement MouseScreen;
        VisualElement CursorUI;
        bool Initialized = false;
        #endregion


        #region UNITY_EDITOR
#if UNITY_EDITOR
        /// <summary>
        /// We need this bit of editor-only logic so that we can reset the Initialized state in the editor
        /// since SO assets referenced in a scene will come into existance at asignment and persist until
        /// domain reload.
        /// </summary>
        private void OnEnable()
        {
            Initialized = false;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;

        }

        void HandlePlayModeChanged(PlayModeStateChange state)
        {
            Deinitialize();
        }
#endif
        #endregion


        #region Public Methods
        /// <summary>
        /// Currently, this is called by GridViewModel upon its own initialization.
        /// If it has already been called once by any source, further calls will do nothing.
        /// </summary>
        public void Initialize(UIDocument uiRoot)
        {
            if (Initialized) return;
            Initialized = true;

            MouseScreen = uiRoot.rootVisualElement;
            MouseScreen.RegisterCallback<PointerMoveEvent>(HandlePointerMove);
            //CursorScreen = cursorScreenRoot;
            CursorUI = CursorAsset.Instantiate();
            MouseScreen.Add(CursorUI);

            CursorUI.SetEnabled(true);
            CursorUI.pickingMode = PickingMode.Ignore; //this seems to be bugged. setting it via the stylesheets in the ui builder works though
            CursorUI.style.position = Position.Absolute;
            CursorUI.style.flexShrink = 0;
            CursorUI.style.flexGrow = 0;
            CursorUI.style.visibility = Visibility.Hidden;
            CursorUI.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
        }

        /// <summary>
        /// 
        /// </summary>
        public void Deinitialize()
        {
            if (Initialized)
            {
                CursorUI.UnregisterCallback<PointerMoveEvent>(HandlePointerMove);
                CursorUI.parent.Remove(CursorUI);
                MouseScreen = null;
                Initialized = false;
            }
        }

        /// <summary>
        /// Updates the cursor to match the state of the payload's icon and icon size.
        /// If the payload passed is null, the cursor is deactivated.
        /// </summary>
        /// <param name="dragState"></param>
        public void SyncCursorToDragState(DragPayload dragState)
        {
            if(dragState == null)
                CursorUI.style.visibility = Visibility.Hidden;
            else
            {
                CursorUI.style.visibility = Visibility.Visible;
                CursorUI.style.backgroundImage = new StyleBackground(dragState.Item.Shared.Icon);
                CursorUI.style.width = dragState.IconWidth;
                CursorUI.style.height = dragState.IconHeight;
            }
        }

        /// <summary>
        /// Positions the drag cursor icon at the given position in UIToolkit screen space in pixels.
        /// This means that x,y = (0, 0) starts at the top left of the screen.
        /// </summary>
        public void SetCursorPosition(Vector2 position)
        {
            CursorUI.style.left = new StyleLength(position.x - (CursorUI.style.width.value.value * 0.5f));
            CursorUI.style.top = new StyleLength(position.y - (CursorUI.style.height.value.value * 0.5f));
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void HandlePointerMove(PointerMoveEvent evt)
        {
            SetCursorPosition(evt.position);
        }

    }

}
