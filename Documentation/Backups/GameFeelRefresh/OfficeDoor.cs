using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CompanySim
{
    public sealed class OfficeDoor : InteractableBehaviour
    {
        public float openAngle = 95, speed = 160;
        bool open;
        Quaternion closedRotation;
        void Awake() { closedRotation = transform.localRotation; }
        void Update()
        {
            var target = closedRotation * Quaternion.Euler(0, open ? openAngle : 0, 0);
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, target, speed * Time.deltaTime);
        }
        public override string Prompt(PlayerInteractor player) => open ? "E — Kapıyı kapat" : "E — Kapıyı aç";
        public override void Interact(PlayerInteractor player) { open = !open; }
    }
}
