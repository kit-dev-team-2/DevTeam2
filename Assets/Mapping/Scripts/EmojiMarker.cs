using UnityEngine;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    public class EmojiMarker : MonoBehaviour
    {
        private string _soundLabel;
        private OVRCameraRig m_camera;

        private void Update()
        {
            if (!m_camera)
            {
                m_camera = FindFirstObjectByType<OVRCameraRig>();
            }
            else
            {
                transform.LookAt(m_camera.centerEyeAnchor);
            }
        }

        public void SetSoundLabel(string label)
        {
            _soundLabel = label;
        }

        public string GetSoundLabel()
        {
            return _soundLabel;
        }
    }
}
