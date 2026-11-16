using UnityEngine;

namespace LasPalmeras
{
    /// <summary>Ficha_Apertura_01..05: abre la ficha para revisar sus datos.</summary>
    public class FichaItem : Interactable
    {
        public FichaData data;
        protected override void OnAwake() { prompt = "Revisar ficha de apertura"; maxDistance = 4.2f; }
        protected override void Interact() { GameManager.I.ui.OpenFicha(data); }
    }
}
