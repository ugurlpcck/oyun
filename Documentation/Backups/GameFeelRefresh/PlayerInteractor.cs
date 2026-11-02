using UnityEngine;
using UnityEngine.InputSystem;

namespace CompanySim
{
    public abstract class InteractableBehaviour : MonoBehaviour
    {
        public abstract string Prompt(PlayerInteractor player);
        public abstract void Interact(PlayerInteractor player);
    }

    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        public const float BodyHeight=1.72f, BodyRadius=.28f, EyeHeight=1.57f;
        public Camera viewCamera;
        public ComputerUI ui;
        public float walkSpeed = 3.2f, runSpeed = 5.0f, lookSensitivity = .11f;
        public float interactionDistance = 2.4f, gravity = -20f;
        public Key interactKey = Key.E, dropKey = Key.F, rotateKey = Key.R;
        public bool InputLocked { get; set; }
        public CarryableItem HeldItem { get; private set; }
        CharacterController controller;
        Transform holdPoint;
        float pitch, verticalVelocity;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            holdPoint = new GameObject("Equipment_HoldPoint").transform;
            holdPoint.SetParent(viewCamera.transform, false);
            holdPoint.localPosition = new Vector3(.32f, -.29f, .82f);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Start()
        {
            foreach (var item in FindObjectsByType<CarryableItem>(FindObjectsSortMode.None))
                item.IgnorePlayerCollision();
        }

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;
            if (InputLocked) return;

            Vector2 delta = mouse.delta.ReadValue() * lookSensitivity;
            transform.Rotate(0, delta.x, 0);
            pitch = Mathf.Clamp(pitch - delta.y, -82, 82);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);

            Vector2 move = Vector2.zero;
            if (kb[Key.W].isPressed) move.y++;
            if (kb[Key.S].isPressed) move.y--;
            if (kb[Key.D].isPressed) move.x++;
            if (kb[Key.A].isPressed) move.x--;
            float speed = kb[Key.LeftShift].isPressed ? runSpeed : walkSpeed;
            Vector3 horizontal = transform.TransformDirection(new Vector3(move.x, 0, move.y).normalized) * speed;
            verticalVelocity = controller.isGrounded ? -.8f : verticalVelocity + gravity * Time.deltaTime;
            controller.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);

            InteractableBehaviour target = null;
            if (Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward,
                    out RaycastHit hit, interactionDistance, ~0, QueryTriggerInteraction.Collide))
                target = hit.collider.GetComponentInParent<InteractableBehaviour>();
            ui.SetPrompt(target ? target.Prompt(this) : HeldItem ? "F — Eşyayı bırak  •  R — Döndür" : "");
            if (kb[interactKey].wasPressedThisFrame && target) target.Interact(this);
            if (kb[dropKey].wasPressedThisFrame && HeldItem) DropHeld();
            if (kb[rotateKey].wasPressedThisFrame && HeldItem) HeldItem.transform.Rotate(Vector3.up, 90, Space.World);
            if (kb[Key.Tab].wasPressedThisFrame) ui.TogglePreparation();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (kb[Key.F9].wasPressedThisFrame) ui.Runtime.DevCompleteFieldJob();
#endif
        }

        public void Hold(CarryableItem item)
        {
            if (HeldItem) return;
            HeldItem = item;
            item.AttachToHand(holdPoint);
        }

        public void DropHeld()
        {
            if (!HeldItem) return;
            var item = HeldItem; HeldItem = null;
            item.Drop(viewCamera.transform.position + viewCamera.transform.forward * 1.15f);
            if (ui.audioFeedback) ui.audioFeedback.Drop();
            ui.Runtime.WorldChanged();
        }

        public CarryableItem TakeHeld()
        {
            var item = HeldItem; HeldItem = null;
            return item;
        }

        public void SetCursor(bool free)
        {
            InputLocked = free;
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }
    }
}
