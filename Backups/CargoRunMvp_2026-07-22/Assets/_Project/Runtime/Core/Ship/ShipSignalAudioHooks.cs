using UnityEngine;

namespace Bellerophon.Core.Ship
{
    public enum ShipSignalAudioCue
    {
        None,
        ShipInterior,
        ShipDamage,
        ExternalDanger,
        IntruderSignal
    }

    public sealed class ShipSignalAudioHooks : MonoBehaviour
    {
        [SerializeField] private AudioSource shipInteriorSource;
        [SerializeField] private AudioSource externalDangerSource;
        [SerializeField] private AudioSource intruderSignalSource;
        [SerializeField] private ShipSignalAudioCue lastCue;
        [SerializeField] private int shipDamageSignalCount;
        [SerializeField] private int externalDangerSignalCount;
        [SerializeField] private int intruderSignalCount;

        public ShipSignalAudioCue LastCue => lastCue;

        public int ShipDamageSignalCount => shipDamageSignalCount;

        public int ExternalDangerSignalCount => externalDangerSignalCount;

        public int IntruderSignalCount => intruderSignalCount;

        public void Configure(
            AudioSource interiorSource,
            AudioSource dangerSource,
            AudioSource intruderSource)
        {
            shipInteriorSource = interiorSource;
            externalDangerSource = dangerSource;
            intruderSignalSource = intruderSource;
        }

        public void TriggerShipInteriorHook()
        {
            lastCue = ShipSignalAudioCue.ShipInterior;
            PlayIfClipConfigured(shipInteriorSource);
        }

        public void TriggerShipDamageSignal()
        {
            shipDamageSignalCount++;
            lastCue = ShipSignalAudioCue.ShipDamage;
            PlayIfClipConfigured(shipInteriorSource);
        }

        public void TriggerExternalDangerSignal()
        {
            externalDangerSignalCount++;
            lastCue = ShipSignalAudioCue.ExternalDanger;
            PlayIfClipConfigured(externalDangerSource);
        }

        public void TriggerIntruderSignal()
        {
            intruderSignalCount++;
            lastCue = ShipSignalAudioCue.IntruderSignal;
            PlayIfClipConfigured(intruderSignalSource);
        }

        private static void PlayIfClipConfigured(AudioSource source)
        {
            if (source == null || source.clip == null)
            {
                return;
            }

            source.Play();
        }
    }
}
