using System;
using UnityEngine;
using UnityEngine.UI;

namespace LasPalmeras
{
    /// <summary>Botón de un panel holográfico: se activa con clic (PC) o con la mirada sostenida / toque (VR).</summary>
    public class UiButton3D : Interactable
    {
        public Action onClick;
        public Image bg;
        public Text label;
        public Color normal = new Color(0.03f, 0.2f, 0.12f, 0.95f);
        public Color hover = new Color(0.14f, 0.6f, 0.34f, 1f);
        public Color disabledColor = new Color(0.12f, 0.14f, 0.13f, 0.9f);
        public bool disabled;

        protected override void OnAwake() { quiet = true; dwellTime = 1.0f; maxDistance = 8f; }

        public void Refresh()
        {
            if (bg == null) return;
            bg.color = disabled ? disabledColor : (Hovered ? hover : normal);
        }

        protected override void OnHoverChanged(bool on) { Refresh(); }

        protected override void Interact()
        {
            if (disabled) { AudioManager.Play("wrong", 0.6f); return; }
            AudioManager.Play("click");
            if (onClick != null) onClick();
        }
    }
}
