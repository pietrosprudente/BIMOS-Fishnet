using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BIMOS
{
    public class GrabHandler : MonoBehaviour
    {
        [SerializeField]
        private Hand _hand;

        [SerializeField]
        private Transform _grabBounds;

        [SerializeField]
        private HandPose _hoverHandPose, _defaultGrabHandPose;

        [SerializeField]
        private AudioClip[] _grabSounds, _releaseSounds;

        [SerializeField]
        private bool ForceGrab = true;

        public Grab _chosenGrab;
        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (_hand.CurrentGrab) //If the hand isn't holding something
                return;

            _chosenGrab = GetChosenGrab(); //Get the grab the player is hovering over

            bool grabInRange = _chosenGrab && _hand.HandInputReader.Grip < 0.5f;
            _hand.HandAnimator.HandPose = grabInRange ? _hoverHandPose : _hand.HandAnimator.DefaultHandPose;
        }

        public void ApplyGrabPose(HandPose handPose)
        {
            if (!handPose)
                handPose = _defaultGrabHandPose;

            _hand.HandAnimator.HandPose = handPose;
        }

        private Grab GetChosenGrab()
        {
            List<Collider> grabColliders = new();
            if (Physics.SphereCast(_grabBounds.position, 0.6f, _grabBounds.eulerAngles, out RaycastHit hit, 3f, Physics.AllLayers, QueryTriggerInteraction.Collide) && ForceGrab)
            {
                grabColliders.Add(hit.collider);
            }
            grabColliders = Physics.OverlapBox(_grabBounds.position, _grabBounds.localScale / 2, _grabBounds.rotation, Physics.AllLayers, QueryTriggerInteraction.Collide).ToList(); //Get all grabs in the grab bounds
            float highestRank = 0;
            Grab highestRankGrab = null;

            foreach (Collider grabCollider in grabColliders) //Loop through found grab colliders to find grab with highest rank
            {
                Grab grab = grabCollider.GetComponent<Grab>();

                if (!grab)
                    grab = grabCollider.GetComponentInParent<Grab>();

                if (!grab)
                    continue;

                if (!grab.enabled)
                    continue;

                if (!grab || !(grab.IsLeftHanded && _hand.IsLeftHand || grab.IsRightHanded && !_hand.IsLeftHand)) //If grab exists and is for the appropriate hand
                    continue;

                float grabRank = grab.CalculateRank(_hand.PalmTransform);

                if (grabRank <= highestRank || grabRank <= 0f)
                    continue;

                highestRank = grabRank;
                highestRankGrab = grab;
            }

            return highestRankGrab; //Return the grab with the highest rank
        }

        public void AttemptGrab()
        {
            if (!_chosenGrab)
                return;

            _chosenGrab.OnGrab(_hand);
            _audioSource.PlayOneShot(Utilities.RandomAudioClip(_grabSounds));
        }

        public void AttemptRelease()
        {
            if (!_hand.CurrentGrab)
                return;

            _hand.CurrentGrab.OnRelease(_hand, true);
            _audioSource.PlayOneShot(Utilities.RandomAudioClip(_releaseSounds));
        }

        private void OnDisable()
        {
            AttemptRelease();
        }
    }
}
