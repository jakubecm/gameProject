using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Plays the player's footsteps by listening to <see cref="FirstPersonController.Footstep"/> and
    /// <see cref="FirstPersonController.Landed"/>. Without clips it stays silent; the sounds themselves are
    /// chosen in task 4.10. Each step picks a random clip (never the same twice in a row) with a little
    /// pitch and volume variation, quieter when crouching and louder when sprinting.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class FootstepAudio : MonoBehaviour
    {
        [SerializeField] private FirstPersonController controller;
        [SerializeField] private AudioClip[] footstepClips = new AudioClip[0];
        [SerializeField] private AudioClip landingClip;

        [SerializeField, Range(0f, 1f)] private float volume = 0.6f;
        [SerializeField, Range(0f, 1f)] private float crouchVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float sprintVolume = 0.85f;
        [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.06f;
        [SerializeField, Range(0f, 0.3f)] private float volumeVariation = 0.1f;

        private AudioSource source;
        private int lastClip = -1;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            if (controller == null)
            {
                controller = GetComponentInParent<FirstPersonController>();
            }
        }

        private void OnEnable()
        {
            if (controller == null) return;
            controller.Footstep += OnFootstep;
            controller.Landed += OnLanded;
        }

        private void OnDisable()
        {
            if (controller == null) return;
            controller.Footstep -= OnFootstep;
            controller.Landed -= OnLanded;
        }

        private void OnFootstep(FootstepEvent step)
        {
            if (footstepClips == null || footstepClips.Length == 0) return;

            int index = Random.Range(0, footstepClips.Length);
            if (footstepClips.Length > 1 && index == lastClip)
            {
                index = (index + 1) % footstepClips.Length;
            }
            lastClip = index;

            float baseVolume = step.Crouching ? crouchVolume : step.Sprinting ? sprintVolume : volume;
            Play(footstepClips[index], baseVolume);
        }

        private void OnLanded(float impactSpeed)
        {
            if (landingClip == null) return;
            Play(landingClip, Mathf.Clamp01(volume * impactSpeed / 5f));
        }

        private void Play(AudioClip clip, float baseVolume)
        {
            if (clip == null) return;
            source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            source.PlayOneShot(clip, Mathf.Clamp01(baseVolume * (1f + Random.Range(-volumeVariation, volumeVariation))));
        }
    }
}
