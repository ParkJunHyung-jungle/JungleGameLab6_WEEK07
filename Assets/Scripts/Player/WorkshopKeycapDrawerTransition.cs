using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace Game.Player
{
    [DefaultExecutionOrder(260)]
    [DisallowMultipleComponent]
    public sealed class WorkshopKeycapDrawerTransition : MonoBehaviour
    {
        [Header("Workshop")]
        [SerializeField] private WorkshopViewController _workshopView;
        [SerializeField] private WorkshopDrawerInput _drawerInput;
        [SerializeField] private Camera _outputCamera;
        [SerializeField] private Transform _keycapDrawer;

        [Header("Screen Tray")]
        [SerializeField] private RectTransform _trayRoot;
        [SerializeField] private CanvasGroup _trayGroup;
        [SerializeField] private KeyboardEditButton _closeButton;

        [Header("Transition")]
        [SerializeField, Min(0.01f)] private float _pullDistance = 0.13f;
        [SerializeField, Min(0.01f)] private float _pullDuration = 0.24f;
        [SerializeField, Min(0.01f)] private float _travelDuration = 0.62f;
        [SerializeField, Min(0.01f)] private float _uiDuration = 0.30f;
        [SerializeField, Range(0.02f, 0.2f)] private float _screenMargin = 0.06f;
        private readonly List<Vector3> _visualCorners = new List<Vector3>();
        private Renderer[] _renderers;
        private Collider[] _colliders;
        private bool[] _rendererStates;
        private bool[] _colliderStates;
        private Transform _closedParent;
        private Vector3 _closedPosition;
        private Quaternion _closedRotation;
        private Vector3 _closedScale;
        private Vector3 _worldScale;
        private Vector2 _trayHome;
        private Canvas _canvas;
        private Coroutine _transition;
        private DrawerState _state;
        private bool _hasClosedPose;
        private int _fittedWidth;
        private int _fittedHeight;
        private ScrollRect _catalogScroll;

        public bool CanInteractWithTray => _state == DrawerState.Open && _drawerInput.IsInputAllowed;
        public bool IsTrayVisible => _trayGroup.alpha > 0f;

        /// <summary>기존 키캡 scroll을 트레이 입력 대상으로 연결한다. 현재 전환 상태로 스크롤 허용 여부를 즉시 맞춘다.</summary>
        public void ConnectCatalogScroll(ScrollRect scroll)
        {
            _catalogScroll = scroll;
            UpdateTrayInput();
        }

        /// <summary>기존 열림 상태와 입력 조건으로 트레이 버튼 및 목록 스크롤 입력만 갱신한다.</summary>
        private void UpdateTrayInput()
        {
            _trayGroup.interactable = CanInteractWithTray;
            if (_catalogScroll != null) _catalogScroll.enabled = CanInteractWithTray;
        }

        private enum DrawerState { Closed, Opening, Open, Closing }

        void Awake()
        {
            _canvas = _trayRoot.GetComponentInParent<Canvas>().rootCanvas;
            _trayHome = _trayRoot.anchoredPosition;
            _renderers = _keycapDrawer.GetComponentsInChildren<Renderer>(true);
            _colliders = _keycapDrawer.GetComponentsInChildren<Collider>(true);
            _rendererStates = new bool[_renderers.Length];
            _colliderStates = new bool[_colliders.Length];
            foreach (MeshFilter filter in _keycapDrawer.GetComponentsInChildren<MeshFilter>(true))
            {
                Bounds bounds = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    _visualCorners.Add(_keycapDrawer.InverseTransformPoint(filter.transform.TransformPoint(point)));
                }
            }
            SetTrayVisible(false);
        }

        void OnEnable()
        {
            _drawerInput.DrawerClicked += HandleDrawerClicked;
            _closeButton.Clicked += HandleCloseClicked;
        }

        void LateUpdate()
        {
            if (_state != DrawerState.Closed && !_workshopView.IsKeyboardEditing)
                RestoreClosed();
            UpdateTrayInput();
            if (_state != DrawerState.Closed && (_fittedWidth != _outputCamera.pixelWidth || _fittedHeight != _outputCamera.pixelHeight))
                FitTray();
        }

        /// <summary>category가 키캡이고 기존 입력이 허용되면 닫힌 서랍을 열거나 열린 수납칸의 닫기를 시작한다.</summary>
        private void HandleDrawerClicked(WorkshopDrawerCategory category)
        {
            if (category != WorkshopDrawerCategory.Keycaps || !_drawerInput.IsInputAllowed) return;
            if (_state == DrawerState.Closed)
            {
                CaptureClosedPose();
                BeginTransition(true);
            }
            else if (_state == DrawerState.Open) BeginTransition(false);
        }

        /// <summary>button의 닫기 요청을 기존 집중 및 조립 입력 조건으로 검사하고 열린 트레이의 복귀를 시작한다.</summary>
        private void HandleCloseClicked(KeyboardEditButton button)
        {
            if (_state == DrawerState.Open && _drawerInput.IsInputAllowed) BeginTransition(false);
        }

        /// <summary>이동 전 부모와 로컬 자세 및 표시·선택 상태를 저장한다. 다음 복귀는 현재 수납장 부모를 기준으로 수행한다.</summary>
        private void CaptureClosedPose()
        {
            _closedParent = _keycapDrawer.parent;
            _closedPosition = _keycapDrawer.localPosition;
            _closedRotation = _keycapDrawer.localRotation;
            _closedScale = _keycapDrawer.localScale;
            _worldScale = _keycapDrawer.lossyScale;
            for (int i = 0; i < _renderers.Length; i++) _rendererStates[i] = _renderers[i].enabled;
            for (int i = 0; i < _colliders.Length; i++) _colliderStates[i] = _colliders[i].enabled;
            _hasClosedPose = true;
        }

        /// <summary>opening에 따라 열기 또는 닫기 코루틴을 시작하고 모든 서랍의 호버와 클릭을 잠근다.</summary>
        private void BeginTransition(bool opening)
        {
            _state = opening ? DrawerState.Opening : DrawerState.Closing;
            _drawerInput.SetTransitionLocked(true);
            _trayGroup.interactable = false;
            UpdateTrayInput();
            _transition = StartCoroutine(Transition(opening));
        }

        /// <summary>opening 순서에 맞춰 실제 인출·화면 밖 이동·트레이 UI를 교대한다. Time.deltaTime으로만 시간을 누적한다.</summary>
        private IEnumerator Transition(bool opening)
        {
            if (opening)
            {
                yield return MoveDrawer(false, false);
                yield return MoveDrawer(true, false);
                SetDrawerVisible(false);
                _drawerInput.SetDrawerOpen(WorkshopDrawerCategory.Keycaps, true);
                FitTray();
                _trayRoot.anchoredPosition = HiddenTrayPosition();
                SetTrayVisible(true);
                yield return MoveTray(true);
            }
            else
            {
                yield return MoveTray(false);
                SetTrayVisible(false);
                Pose pulled = PulledPose();
                _keycapDrawer.SetPositionAndRotation(ExitPosition(pulled), pulled.rotation);
                SetDrawerVisible(true);
                yield return MoveDrawer(true, true);
                yield return MoveDrawer(false, true);
                ApplyClosedPose();
                _drawerInput.SetDrawerOpen(WorkshopDrawerCategory.Keycaps, false);
            }
            _state = opening ? DrawerState.Open : DrawerState.Closed;
            _transition = null;
            _drawerInput.SetTransitionLocked(false);
            UpdateTrayInput();
        }

        /// <summary>travel이면 화면 밖 구간을, 아니면 로컬 인출 구간을 reverse 방향에 맞춰 보간한다. 모델 스케일은 유지한다.</summary>
        private IEnumerator MoveDrawer(bool travel, bool reverse)
        {
            float duration = travel ? _travelDuration : _pullDuration;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
                float amount = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                if (reverse) amount = 1f - amount;
                if (travel)
                {
                    Pose pulled = PulledPose();
                    _keycapDrawer.SetPositionAndRotation(Vector3.Lerp(pulled.position, ExitPosition(pulled), amount), pulled.rotation);
                }
                else
                {
                    _keycapDrawer.localPosition = _closedPosition + _closedRotation * Vector3.back * (_pullDistance * amount);
                    _keycapDrawer.localRotation = _closedRotation;
                }
                yield return null;
            }
        }

        /// <summary>현재 부모와 저장된 로컬 자세 및 인출 거리로 인출 완료 월드 자세를 반환한다.</summary>
        private Pose PulledPose()
        {
            return new Pose(_closedParent.TransformPoint(_closedPosition + _closedRotation * Vector3.back * _pullDistance),
                _closedParent.rotation * _closedRotation);
        }

        /// <summary>pulled 자세의 모든 메시 경계를 출력 카메라에 투영해 화면 왼쪽 바깥의 위치를 반환한다. 깊이는 유지한다.</summary>
        private Vector3 ExitPosition(Pose pulled)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(pulled.position, pulled.rotation, _worldScale);
            float right = float.NegativeInfinity;
            float top = float.NegativeInfinity;
            float far = 0f;
            foreach (Vector3 corner in _visualCorners)
            {
                Vector3 point = _outputCamera.WorldToViewportPoint(matrix.MultiplyPoint3x4(corner));
                right = Mathf.Max(right, point.x);
                top = Mathf.Max(top, point.y);
                far = Mathf.Max(far, point.z);
            }
            Vector3 origin = _outputCamera.ViewportToWorldPoint(new Vector3(0f, 0f, far));
            Vector3 width = _outputCamera.ViewportToWorldPoint(new Vector3(1f, 0f, far)) - origin;
            Vector3 height = _outputCamera.ViewportToWorldPoint(new Vector3(0f, 1f, far)) - origin;
            return pulled.position - width * Mathf.Max(0f, right + _screenMargin) - height * Mathf.Max(0f, top - 0.25f);
        }

        /// <summary>entering 방향으로 화면용 트레이를 보간한다. 화면 밖 도착 뒤에만 호출자가 표현을 교체한다.</summary>
        private IEnumerator MoveTray(bool entering)
        {
            float elapsed = 0f;
            while (elapsed < _uiDuration)
            {
                elapsed = Mathf.Min(_uiDuration, elapsed + Time.deltaTime);
                float amount = Mathf.SmoothStep(0f, 1f, elapsed / _uiDuration);
                _trayRoot.anchoredPosition = Vector2.Lerp(HiddenTrayPosition(), _trayHome, entering ? amount : 1f - amount);
                yield return null;
            }
        }

        /// <summary>현재 트레이 크기와 배율로 테두리까지 왼쪽 밖에 놓이는 화면 UI 위치를 반환한다.</summary>
        private Vector2 HiddenTrayPosition()
        {
            return new Vector2(-_trayRoot.rect.width * _trayRoot.localScale.x - 32f, -_trayRoot.rect.height * _trayRoot.localScale.y * 0.12f);
        }

        /// <summary>CanvasScaler 기준 크기와 현재 키보드의 화면 왼쪽 경계로 트레이 배율을 맞춰 조립 영역을 확보한다.</summary>
        private void FitTray()
        {
            _fittedWidth = _outputCamera.pixelWidth;
            _fittedHeight = _outputCamera.pixelHeight;
            RectTransform canvasRect = (RectTransform)_canvas.transform;
            float available = canvasRect.rect.width * 0.25f;
            if (_workshopView.EditingKeyboard != null)
                foreach (Renderer renderer in _workshopView.EditingKeyboard.GetComponentsInChildren<Renderer>())
                {
                    if (!renderer.enabled) continue;
                    Bounds bounds = renderer.bounds;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        Vector3 point = bounds.center + Vector3.Scale(bounds.extents,
                            new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                        Vector3 screen = _outputCamera.WorldToScreenPoint(point);
                        if (screen.z > _outputCamera.nearClipPlane) available = Mathf.Min(available, screen.x / _canvas.scaleFactor - 16f);
                    }
                }
            float fit = Mathf.Min(1f, (available - _trayHome.x) / _trayRoot.rect.width,
                (canvasRect.rect.height - _trayHome.y - 100f) / _trayRoot.rect.height);
            _trayRoot.localScale = Vector3.one * Mathf.Max(0.1f, fit);
        }

        /// <summary>visible로 트레이의 표시와 해당 사각형 내부의 Raycast만 변경한다. 내부 처리 컴포넌트는 중단하지 않는다.</summary>
        private void SetTrayVisible(bool visible)
        {
            _trayGroup.alpha = visible ? 1f : 0f;
            _trayGroup.blocksRaycasts = visible;
            _trayGroup.interactable = false;
        }

        /// <summary>visible에 따라 실제 서랍의 저장된 Renderer·Collider 상태를 복구하거나 표시와 선택만 숨긴다.</summary>
        private void SetDrawerVisible(bool visible)
        {
            for (int i = 0; i < _renderers.Length; i++) _renderers[i].enabled = visible && _rendererStates[i];
            for (int i = 0; i < _colliders.Length; i++) _colliders[i].enabled = visible && _colliderStates[i];
        }

        /// <summary>저장된 부모와 로컬 위치·회전·스케일 및 표시 상태를 정확히 복원한다.</summary>
        private void ApplyClosedPose()
        {
            _keycapDrawer.SetParent(_closedParent, false);
            _keycapDrawer.SetLocalPositionAndRotation(_closedPosition, _closedRotation);
            _keycapDrawer.localScale = _closedScale;
            SetDrawerVisible(true);
        }

        /// <summary>진행 중 전환을 중단하고 3D 서랍과 화면용 UI 및 입력 잠금을 닫힌 기본 상태로 복구한다.</summary>
        private void RestoreClosed()
        {
            if (_transition != null) StopCoroutine(_transition);
            _transition = null;
            if (_hasClosedPose && _keycapDrawer != null && _closedParent != null) ApplyClosedPose();
            _state = DrawerState.Closed;
            if (_trayGroup != null) SetTrayVisible(false);
            if (_catalogScroll != null) _catalogScroll.enabled = false;
            if (_drawerInput != null)
            {
                _drawerInput.SetDrawerOpen(WorkshopDrawerCategory.Keycaps, false);
                _drawerInput.SetTransitionLocked(false);
            }
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) RestoreClosed();
        }

        void OnDisable()
        {
            _drawerInput.DrawerClicked -= HandleDrawerClicked;
            _closeButton.Clicked -= HandleCloseClicked;
            RestoreClosed();
        }
    }
}
