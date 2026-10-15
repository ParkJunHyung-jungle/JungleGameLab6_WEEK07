using System.Collections;

using UnityEngine;

using KeyboardModeling;

namespace Game.Player
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class WorkbenchKeyboardHolder : MonoBehaviour, IKeyboardHolder
    {
        private const float ROTATION_DURATION = 0.35f;

        [Header("Keyboard")]
        private Rigidbody _keyboardBody;

        void OnTriggerEnter(Collider other)
        {
            if (_keyboardBody != null)
                return;

            Rigidbody body = other.attachedRigidbody;
            if (body == null || body.isKinematic || body.GetComponent<KeyboardPickupTarget>() == null)
                return;

            _keyboardBody = body;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.useGravity = false;
            body.transform.SetParent(transform, true);

            BoxCollider keyboardCollider = body.GetComponent<BoxCollider>();
            Vector3 colliderCenter = Vector3.Scale(keyboardCollider.center, body.transform.lossyScale);
            body.position = transform.position - body.rotation * colliderCenter;
            StartCoroutine(RotateKeyboardToHolder(body, keyboardCollider));

            KeyboardProtectionCover cover = body.GetComponentInChildren<KeyboardProtectionCover>();
            if (cover != null)
                cover.RemoveFrom(body.transform);
        }

        /// <summary>
        /// keyboardCollider의 중심을 holder 위치에 유지하며 body를 목표 회전까지 보간한다.
        /// body의 현재 회전에서 시작하고, 키보드가 해제되면 회전을 중단한다.
        /// </summary>
        private IEnumerator RotateKeyboardToHolder(Rigidbody body, BoxCollider keyboardCollider)
        {
            Quaternion startRotation = body.rotation;
            Quaternion targetRotation = transform.rotation * Quaternion.Euler(-57f, 0f, 0f);
            if (Quaternion.Dot(startRotation, targetRotation) < 0f)
            {
                targetRotation = new Quaternion(
                    -targetRotation.x,
                    -targetRotation.y,
                    -targetRotation.z,
                    -targetRotation.w);
            }

            Vector3 colliderCenter = Vector3.Scale(keyboardCollider.center, body.transform.lossyScale);
            float elapsed = 0f;
            while (_keyboardBody == body && elapsed < ROTATION_DURATION)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / ROTATION_DURATION));
                Quaternion rotation = Quaternion.Slerp(startRotation, targetRotation, progress);
                body.rotation = rotation;
                body.position = transform.position - rotation * colliderCenter;
                yield return null;
            }

            if (_keyboardBody != body)
                yield break;

            body.rotation = targetRotation;
            body.position = transform.position - targetRotation * colliderCenter;
        }

        /// <summary>
        /// body가 이 holder에 고정된 키보드인지 확인해 보관 상태를 해제한다.
        /// body를 사용하며, 해제되면 이후 키보드가 holder에 다시 고정될 수 있다.
        /// </summary>
        public void ReleaseKeyboard(Rigidbody body)
        {
            if (_keyboardBody == body)
                _keyboardBody = null;
        }
    }
}
