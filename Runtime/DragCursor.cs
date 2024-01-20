using Sirenix.OdinInspector;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace PGIA
{
    /// <summary>
    /// To be shared between ModelViews, this represents the ircon of an item as it is being dragged around.
    /// This cursor is a persistent object that will exist from app start to app quit including in the editor itself.
    /// For this reason it is specially guarded so that once it has been initialized it will never repeat this process.
    /// </summary>
    [CreateAssetMenu(fileName = "Drag Cursor", menuName = "PGIA/Drag Cursor")]
    public class DragCursor : ScriptableObject
    {
        #region Fields and Properties
        [Space(12)]
        [Required]
        [Tooltip("A UI asset describing the cursor.")]
        public VisualTreeAsset CursorAsset;

        [Required]
        [Tooltip("A UI asset representing the entire screen. This will dynamically be restyled to cover the entire screen at all times and generally should just be left blank.")]
        public VisualTreeAsset ScreenAsset;

        [Required]
        [Tooltip("The UI settings used by this cursor.")]
        public PanelSettings PanelSettings;

        VisualElement MouseScreen;
        VisualElement CursorUI;
        UIDocument DocRoot;
        GameObject DocGO;
        bool Initialized = false;
        Vector2 CurrentPointerPos = new();
        #endregion


        #region Unity Events
        private void OnEnable()
        {
            //due to how scriptable objects work in the editor we need this guard
            //to ensure we are always inializing when in playmode and ONLY when in playmode
            if(Application.isPlaying)
                Initialize();
            #if UNITY_EDITOR
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
            #endif
        }

        private void OnDisable()
        {
            if(Application.isPlaying)
                Deinitialize();
            #if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            #endif

        }

        #if UNITY_EDITOR
        void HandlePlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                Initialize();
            else if(state == PlayModeStateChange.ExitingPlayMode)
                Deinitialize();
        }
        #endif
        #endregion



        #region Private Methods
        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="System.Exception"></exception>
        void Initialize()
        {
            if (Equals(CursorAsset, null) || Equals(ScreenAsset, null) || Equals(PanelSettings, null))
                throw new UnityException("You must supply a CursorAsset, ScreenAsset, and PanelSettings to the Drag Cursor.");

            if (Initialized) return;

            //create a dummy GameObject and add a UIDocument component to it.
            //this will be the container for the 'screen' that our cursor can be moved on
            DocGO = new GameObject("*** Cursor UI Doc ***");
            DocGO.hideFlags = HideFlags.DontSave; //HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(DocGO);
            DocRoot = DocGO.AddComponent<UIDocument>();
            DocRoot.panelSettings = PanelSettings;
            DocRoot.visualTreeAsset = ScreenAsset;

            //add an element to our doc root. this will represent the 'screen' for our cursor to move on.
            MouseScreen = new VisualElement();
            MouseScreen.name = "Screen";
            DocRoot.rootVisualElement.Add(MouseScreen);
            MouseScreen.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            MouseScreen.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            MouseScreen.style.backgroundColor = Color.clear;
            MouseScreen.pickingMode = PickingMode.Ignore;
            InputSystem.onEvent += HandlePointerEvent;

            //create our actual cursor ui element and make it a child of the screen element.
            CursorUI = CursorAsset.Instantiate();
            MouseScreen.Add(CursorUI);
            CursorUI.SetEnabled(true);
            CursorUI.pickingMode = PickingMode.Ignore; //this seems to be bugged. setting it via the stylesheets in the ui builder works though
            CursorUI.style.position = Position.Absolute;
            CursorUI.style.flexShrink = 0;
            CursorUI.style.flexGrow = 0;
            CursorUI.style.visibility = Visibility.Hidden;
            CursorUI.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));

            Initialized = true;
        }

        /// <summary>
        /// 
        /// </summary>
        void Deinitialize()
        {
            if (Initialized)
            {
                Initialized = false;
                InputSystem.onEvent -= HandlePointerEvent;
                CursorUI.parent.Remove(CursorUI);
                DestroyImmediate(DocGO);
                MouseScreen = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventPtr"></param>
        /// <param name="device"></param>
        void HandlePointerEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (!eventPtr.IsA<StateEvent>())
                return;

            var mouse = device as Mouse;
            if(mouse != null)
            {
                var pos = mouse.position;
                float posX = pos.x.value;
                float posY = Screen.height - pos.y.value;
                var pointerPos = new Vector2(posX, posY);
#if UNITY_EDITOR
                if (DocRoot == null)
                {
                    Debug.Log("<color=green>DocRoot not found for DragCursor (DragCursor.cs Line 158). Deinitializing the cursor now.</color>");
                    Deinitialize();
                }
#endif
                pointerPos = RuntimePanelUtils.ScreenToPanel(DocRoot.rootVisualElement.panel, pointerPos);
                SetCursorPosition(pointerPos);
                return;
            }

            var gamepad = device as Gamepad;
            if(gamepad != null)
            {
                //TODO: track position manually by checking delta here
                var deltaPos = gamepad.leftStick.ReadValueFromEvent(eventPtr);
                CurrentPointerPos += deltaPos;

                if (CurrentPointerPos.x < 0) CurrentPointerPos.x = 0;
                if (CurrentPointerPos.y < 0) CurrentPointerPos.y = 0;
                if (CurrentPointerPos.x > Screen.width) CurrentPointerPos.x = Screen.width;
                if (CurrentPointerPos.y > Screen.height) CurrentPointerPos.y = Screen.height;
                return;
            }
        }

#endregion


#region Public Methods
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

        /// <summary>
        /// Positions the drag cursor icon at the given position in UIToolkit screen space in pixels.
        /// This means that x,y = (0, 0) starts at the top left of the screen.
        /// </summary>
        public void SetVirtualCursorPosition(Vector2 position)
        {
            CursorUI.style.left = new StyleLength(position.x - (CursorUI.style.width.value.value * 0.5f));
            CursorUI.style.top = new StyleLength(position.y - (CursorUI.style.height.value.value * 0.5f));
        }
#endregion

    }

}
