using System;
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        public float WalkSpeed = 2.0f;
        public float RunSpeed = 4f;
        public float SprintSpeed = 5.335f;

        public float AimRotationSpeed = 20f;

        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;

        [Space(10)]
        public float JumpTimeout = 0.50f;
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        public float CameraAngleOverride = 0.0f;
        public bool LockCameraPosition = false;

        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private bool _isRolling = false;
        private float _lastRollTime = -10f;
        private float _lastSprintKeyTime = -1f;

        [Header("Roll")]
        public float RollCooldown = 1f;
        public float RollDuration = 0.6f;
        public float RollSpeed = 6f;

        private int rollHash;
        private bool _isInvincible = false;
        public float InvincibleDuration = 0.4f;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        // (Raycast용 변수는 남겨두어도 무방하지만, 트리거 방식에서는 사용하지 않음)
        [Header("Interaction Settings")]
        [SerializeField] private float interactRange = 3f;
        [SerializeField] private LayerMask interactLayer;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private RigManager _rigManager;
        private Character _character;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;
        private float targetSpeed = 2;
        private bool _walking = false;

        private float _speedAnimationMultiplier = 0;
        private bool _aiming = false;
        private bool _sprinting = false;
        private float _aimLayerWieght = 0;

        private Vector2 _aimedMovingAnimtionsInput = Vector2.zero;
        private float aimRigWieght = 0;
        private float leftHandWeight = 0;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private bool Armed => _character != null && _character.weapon != null;
        private bool CanAim => _input != null && _input.canAim;
        private bool CanFire => Armed && _aiming && !_character.reloading && !_isRolling;

        private void Awake()
        {
            _rigManager = GetComponent<RigManager>();
            _character = GetComponent<Character>();

            _mainCamera = CameraManager.maincamera.gameObject;
            CameraManager.playerCamera.m_Follow = CinemachineCameraTarget.transform;
            CameraManager.aimingCamera.m_Follow = CinemachineCameraTarget.transform;
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            _input.OnSprintKeyPressed += HandleSprintKeyPressed;
            rollHash = Animator.StringToHash("Roll");
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);
            _aiming = _input.aim && CanAim;
            _sprinting = _input.sprint && !_aiming;

            JumpAndGravity();
            GroundedCheck();

            HandleCameraAimingAndLayers();
            HandleWalkToggle();
            UpdateTargetSpeedAndAnimationMultiplier();

            Move();
            Rotate();

            HandleReloadInput();
            HandleWeaponSlotInputs();

            HandleShooting();
            HandleInteraction();
        }

        private void LateUpdate()
        {
            CameraRotation();
            ApplyRecoil(); // 반동 적용
        }

        private void ApplyRecoil()
        {
            // Weapon.cs 에서 누적된 반동을 카메라에 반영
            _cinemachineTargetYaw += Weapon.recoilX;
            _cinemachineTargetPitch -= Weapon.recoilY;

            // 프리셋의 복원 속도로 서서히 회복
            Weapon.recoilX = Mathf.Lerp(Weapon.recoilX, 0, Time.deltaTime * Weapon.recoveryX);
            Weapon.recoilY = Mathf.Lerp(Weapon.recoilY, 0, Time.deltaTime * Weapon.recoveryY);
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void HandleCameraAimingAndLayers()
        {
            CameraManager.singleton.aiming = _aiming;

            bool armed = Armed;
            _animator.SetFloat("Armed", armed ? 1f : 0f);
            _animator.SetFloat("Aimed", _aiming ? 1f : 0f);

            _aimLayerWieght = Mathf.Lerp(
                _aimLayerWieght,
                _character.switchingWeapon || (armed && (_aiming || _character.reloading)) ? 1f : 0f,
                10f * Time.deltaTime
            );
            _animator.SetLayerWeight(1, _aimLayerWieght);

            aimRigWieght = Mathf.Lerp(
                aimRigWieght,
                armed && _aiming && !_character.reloading ? 1f : 0f,
                10f * Time.deltaTime
            );

            leftHandWeight = Mathf.Lerp(
                leftHandWeight,
                armed && !_character.switchingWeapon && !_character.reloading &&
                (_aiming || (_controller.isGrounded && _character.weapon.type == Weapon.Handle.TwoHanded))
                    ? 1f : 0f,
                10f * Time.deltaTime
            );

            _rigManager.aimTarget = CameraManager.singleton.aimTargetPiont;
            _rigManager.aimWeight = aimRigWieght;
            _rigManager.leftHandWeight = leftHandWeight;
        }

        private void HandleWalkToggle()
        {
            if (_input.walk)
            {
                _input.walk = false;
                _walking = !_walking;
            }
        }

        private void UpdateTargetSpeedAndAnimationMultiplier()
        {
            targetSpeed = RunSpeed;

            if (_sprinting)
            {
                targetSpeed = SprintSpeed;
                _speedAnimationMultiplier = 3;
            }
            else if (_walking)
            {
                targetSpeed = WalkSpeed;
                _speedAnimationMultiplier = 1;
            }
            else
            {
                _speedAnimationMultiplier = 2;
            }

            _aimedMovingAnimtionsInput = Vector2.Lerp(
                _aimedMovingAnimtionsInput,
                _input.move.normalized * _speedAnimationMultiplier,
                SpeedChangeRate * Time.deltaTime
            );

            _animator.SetFloat("Speed_X", _aimedMovingAnimtionsInput.x);
            _animator.SetFloat("Speed_Y", _aimedMovingAnimtionsInput.y);
        }

        private void HandleReloadInput()
        {
            if (_input.reload && !_character.reloading)
            {
                _input.reload = false;
                _character.weapon?.StopFiring();
                _character.Reload();
            }
        }

        private void HandleWeaponSlotInputs()
        {
            if (_input.switchToPrimary)
            {
                _input.switchToPrimary = false;
                TryEquipWeaponBySlot(0);
            }
            else if (_input.switchToSecondary)
            {
                _input.switchToSecondary = false;
                TryEquipWeaponBySlot(1);
            }
            else if (_input.switchToThird)
            {
                _input.switchToThird = false;
                TryEquipWeaponBySlot(2);
            }
        }

        private void HandleShooting()
        {
            var weapon = _character.weapon;

            if (!CanFire)
            {
                weapon?.StopFiring();
                _input.shoot = false;
                return;
            }

            // 🔁 모든 발사모드 공통: 카메라 보정 타겟 사용
            Func<Vector3> getFinalTarget = () => CameraManager.singleton.GetFinalAimPoint(weapon.muzzle);

            if (weapon.fireMode == Weapon.FireMode.SemiAuto)
            {
                if (_input.shoot)
                {
                    weapon.StartFiring(
                        _character,
                        getFinalTarget,
                        this,
                        () => _aiming,
                        () => _input.move.magnitude,
                        () => _sprinting
                    );
                    _input.shoot = false;
                    _rigManager.ApplyWeaponKick(weapon.handKick, weapon.bodyKick);
                }
            }
            else // Burst / FullAuto
            {
                if (_input.shoot)
                {
                    weapon.StartFiring(
                        _character,
                        getFinalTarget,
                        this,
                        () => _aiming,
                        () => _input.move.magnitude,
                        () => _sprinting
                    );
                }
                else
                {
                    weapon.StopFiring();
                }
            }
        }

        private void Move()
        {
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, _input.move == Vector2.zero ? 0 : _speedAnimationMultiplier, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

                if (!_aiming)
                {
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            _controller.Move(
                targetDirection.normalized * (_speed * Time.deltaTime) +
                new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime
            );

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void Rotate()
        {
            if (_aiming)
            {
                Vector3 aimTarget = CameraManager.singleton.aimTargetPiont;
                aimTarget.y = transform.position.y;

                Vector3 aimDirection = (aimTarget - transform.position).normalized;
                transform.forward = Vector3.Lerp(transform.forward, aimDirection, AimRotationSpeed * Time.deltaTime);
            }
        }

        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * CameraManager.singleton.sensitivity * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * CameraManager.singleton.sensitivity * deltaTimeMultiplier;
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f
            );
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private void HandleSprintKeyPressed()
        {
            if (!Grounded) return;

            if (Time.time - _lastRollTime > RollCooldown)
            {
                if (Time.time - _lastSprintKeyTime < 0.3f)
                {
                    StartCoroutine(Roll());
                    _lastRollTime = Time.time;
                }
                _lastSprintKeyTime = Time.time;
            }
        }

        private IEnumerator Roll()
        {
            _isRolling = true;

            _character.weapon?.StopFiring();
            _input.shoot = false;

            Vector2 moveInput = _input.move;
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);

            Vector3 rollDirection;
            if (inputDirection.sqrMagnitude < 0.01f)
            {
                rollDirection = transform.forward;
            }
            else
            {
                float cameraY = CameraManager.maincamera.transform.eulerAngles.y;
                Vector3 camForward = Quaternion.Euler(0, cameraY, 0) * Vector3.forward;
                Vector3 camRight = Quaternion.Euler(0, cameraY, 0) * Vector3.right;
                rollDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

                transform.rotation = Quaternion.LookRotation(rollDirection);
            }

            _input.aim = false;
            _input.canAim = false;
            CameraManager.singleton.aiming = false;

            _isInvincible = true;
            _character.isInvincible = true;
            Invoke(nameof(EndInvincibility), InvincibleDuration);

            _animator.SetTrigger(rollHash);

            float timer = 0f;
            while (timer < RollDuration)
            {
                _controller.Move(rollDirection * RollSpeed * Time.deltaTime);
                timer += Time.deltaTime;
                yield return null;
            }

            _isRolling = false;
            _input.canAim = true;
        }

        private void EndInvincibility()
        {
            _isInvincible = false;
            _character.isInvincible = false;
        }

        private void TryEquipWeaponBySlot(int slotIndex)
        {
            Weapon weapon = _character.GetWeaponBySlotIndex(slotIndex);
            if (weapon != null)
            {
                // 이미 같은 슬롯이면 중복 방지
                if (_character.GetCurrentSlotIndex() == slotIndex)
                    return;

                // 애니메이션 기반 전환
                _character.HolsterWeapon();
                StartCoroutine(DelayedEquip(slotIndex, 0.5f)); // 0.5초 후 장착
            }
            else
            {
                Debug.LogWarning($"슬롯 {slotIndex + 1}에 장비 가능한 무기가 없습니다.");
            }
        }

        private IEnumerator DelayedEquip(int slotIndex, float delay)
        {
            yield return new WaitForSeconds(delay);
            _character.EquipWeapon(slotIndex);
        }

        // ✅ 트리거 방식: E키 → 캐릭터의 TryInteract() 호출
        private void HandleInteraction()
        {
            if (_input.interact)
            {
                _input.interact = false;
                _character.TryInteract(); // 가까운 무기가 등록돼 있으면 교체 수행
            }
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = Grounded ? transparentGreen : transparentRed;

            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius
            );
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = UnityEngine.Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }
}
