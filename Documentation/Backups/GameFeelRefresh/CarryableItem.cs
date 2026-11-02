using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CompanySim
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CarryableItem : InteractableBehaviour
    {
        public string uid, definitionId;
        [Range(0,100)] public float durability = 100;
        public bool Loaded { get; private set; }
        public int CargoSlot { get; private set; } = -1;
        Rigidbody body;
        Collider[] colliders;
        public string DisplayName => FindFirstObjectByType<CompanyCatalog>()?.Item(definitionId)?.displayName ?? definitionId;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();
            IgnorePlayerCollision();
        }
        public void IgnorePlayerCollision()
        {
            // Physics.IgnoreCollision is reset when an item collider is disabled.
            // Keep raycasts usable while letting players step through loose gear.
            foreach (var player in FindObjectsByType<PlayerInteractor>(FindObjectsSortMode.None))
            {
                var controller=player.GetComponent<CharacterController>();
                if (!controller) continue;
                foreach (var col in colliders)
                    if (col.enabled) Physics.IgnoreCollision(controller,col,true);
            }
        }
        public override string Prompt(PlayerInteractor player) => player.HeldItem ? "" : "E — " + DisplayName + " al";
        public override void Interact(PlayerInteractor player)
        {
            if (!player.HeldItem && !Loaded) player.Hold(this);
        }
        public void AttachToHand(Transform hand)
        {
            Loaded = false; CargoSlot = -1;
            body.isKinematic = true;
            foreach (var col in colliders) col.enabled = false;
            transform.SetParent(hand, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        public void Drop(Vector3 position)
        {
            Loaded = false; CargoSlot = -1;
            transform.SetParent(null, true);
            transform.position = position;
            body.isKinematic = false;
            foreach (var col in colliders) col.enabled = true;
            IgnorePlayerCollision();
        }
        public void LoadInto(Transform socket, int slot)
        {
            Loaded = true; CargoSlot = slot;
            body.isKinematic = true;
            foreach (var col in colliders) col.enabled = false;
            transform.SetParent(socket, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }
}
