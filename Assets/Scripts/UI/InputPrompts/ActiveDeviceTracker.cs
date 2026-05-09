using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;
using UnityServiceLocator;

namespace UI.InputPrompts
{
    public class ActiveDeviceTracker : MonoBehaviour
    {
        public static event Action<ActiveDevice> DeviceChanged;
        public static ActiveDevice Current { get; private set; } = ActiveDevice.KeyboardMouse;

        [SerializeField] private ButtonPromptDatabase m_database;
        [SerializeField] private InputActionAsset m_actionAsset;
        [SerializeField] private string m_keyboardSchemeName = "Keyboard&Mouse";
        [SerializeField] private string m_gamepadSchemeName = "Gamepad";

        private void Awake()
        {
            ServiceLocator.Global.Register(this);
            if (m_database != null) ButtonPromptParser.Database = m_database;
            if (m_actionAsset != null) ButtonPromptParser.LabelResolver = ResolveLabel;
        }

        private string ResolveLabel(string actionName, ActiveDevice device)
        {
            var action = m_actionAsset.FindAction(actionName);
            if (action == null) return null;
            string scheme = device == ActiveDevice.KeyboardMouse ? m_keyboardSchemeName : m_gamepadSchemeName;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].isComposite || action.bindings[i].isPartOfComposite) continue;
                if (!action.bindings[i].groups.Contains(scheme)) continue;
                return action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions);
            }
            return null;
        }

        private void OnEnable()
        {
            InputSystem.onEvent += OnInputEvent;
        }

        private void OnDisable()
        {
            InputSystem.onEvent -= OnInputEvent;
        }

        private static void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (device == null) return;
            if (eventPtr.type != StateEvent.Type && eventPtr.type != DeltaStateEvent.Type) return;

            bool hasMeaningfulInput = false;
            foreach (var _ in eventPtr.EnumerateChangedControls(device, magnitudeThreshold: 0.5f))
            {
                hasMeaningfulInput = true;
                break;
            }
            if (!hasMeaningfulInput) return;

            ActiveDevice next = Classify(device);
            if (next == Current) return;

            Current = next;
            DeviceChanged?.Invoke(next);
        }

        private static ActiveDevice Classify(InputDevice device)
        {
            switch (device)
            {
                case Keyboard:
                case Mouse:
                    return ActiveDevice.KeyboardMouse;
                case DualShockGamepad:
                    return ActiveDevice.PlayStation;
                case SwitchProControllerHID:
                    return ActiveDevice.Switch;
                case XInputController:
                case Gamepad:
                    return ActiveDevice.Xbox;
                default:
                    return Current;
            }
        }
    }
}
