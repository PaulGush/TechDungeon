using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static InputSystem_Actions;

namespace Input
{
    public interface IInputReader
    {
        Vector2 MoveDirection { get; }
        void EnablePlayerActions();
    }
    
    [CreateAssetMenu(fileName = "InputReader", menuName = "Input/InputReader")]
    public class InputReader : ScriptableObject, IInputReader, IPlayerActions
    {
        public UnityAction<Vector2> Move = delegate {  };
        public UnityAction Attack = delegate {  };
        public UnityAction Next = delegate {  };
        public UnityAction Previous = delegate {  };
        public UnityAction Interact = delegate {  };
        public UnityAction AltInteract = delegate {  };
        public UnityAction Inventory = delegate {  };
        public UnityAction InventoryReleased = delegate {  };
        public UnityAction Roll = delegate {  };
        public UnityAction Look = delegate {  };
        
        private InputSystem_Actions m_inputActions;
        
        public Vector2 MoveDirection => m_inputActions.Player.Move.ReadValue<Vector2>();
        public Vector2 LookDirection => m_inputActions.Player.Look.ReadValue<Vector2>();

        public bool IsMoveInputPressed => m_inputActions.Player.Move.IsPressed();
        public bool IsAttackHeld => m_inputActions != null && m_inputActions.Player.Attack.IsPressed();
        public bool IsPlayerActionsEnabled => m_inputActions != null && m_inputActions.Player.enabled;
        public void EnablePlayerActions()
        {
            if (m_inputActions == null)
            {
                m_inputActions = new InputSystem_Actions();
                m_inputActions.Player.SetCallbacks(this);
            }
            m_inputActions.Enable();
        }
        
        public void DisablePlayerActions()
        {
            m_inputActions.Disable();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            Move?.Invoke(context.ReadValue<Vector2>());
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
            {
                Attack?.Invoke();
            }
        }
        
        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
            {
                Interact?.Invoke();
            }
        }
        
        public void OnPrevious(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
            {
                Previous?.Invoke();
            }
        }

        public void OnNext(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
            {
                Next?.Invoke();
            }
        }
        
        public void OnSprint(InputAction.CallbackContext context)
        {

        }

        public void OnAltInteract(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
            {
                AltInteract?.Invoke();
            }
        }
        public void OnInventory(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
                Inventory?.Invoke();
            else if (context.phase == InputActionPhase.Canceled)
                InventoryReleased?.Invoke();
        }

        public void OnRoll(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
            {
                Roll?.Invoke();
            }
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            Look?.Invoke();
        }
    }
}