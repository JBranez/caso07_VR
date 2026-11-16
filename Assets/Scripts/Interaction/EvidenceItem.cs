using System;
using UnityEngine;

namespace LasPalmeras
{
    /// <summary>Pista del escenario (contrato común: EvidenceItem).</summary>
    public class EvidenceItem : Interactable
    {
        public string evidenceId;
        public string title;
        [TextArea] public string body;
        public bool registerOnInspect = true;
        public Action<EvidenceItem> handler;

        protected override void Interact()
        {
            if (handler != null) { handler(this); return; }
            Inspect();
        }

        public void Inspect()
        {
            var gm = GameManager.I;
            if (registerOnInspect && !string.IsNullOrEmpty(evidenceId)) gm.RegisterEvidence(evidenceId);
            gm.ui.OpenCard(title, body);
        }
    }
}
