// ThirdPersonController.cs
using System;
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
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
        [Range(0, 0.3f)] public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 10.0f;

        [Header("Jump/Gravity")]
        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;
        public float JumpTimeout = 0.50f;
        public float FallTimeout = 0.15f;

        [Header("Grounded")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70f;
        public float BottomClamp = -30f;
        public float CameraAngleOverride = 0f;
        public bool LockCameraPosition = false;

        [Header("Roll")]
        public float RollCooldown = 1f;
        public float RollDuration = 0.6f;
        public float RollSpeed = 6f;

        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;
        private float _speed;
        private float _animationBlend;
        private float _targetRotation;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53f;
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private bool _isRolling = false;
        private float _lastRollTime = -10f;
        private float _lastSprintKeyTime = -1f;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private RigManager _rigManager;
        private Character _character;
        private GameObject _mainCamera;

        private bool _hasAnimator;
        private float targetSpeed = 2f;
        private bool _walking = false;

        private float _speedAnimationMultiplier = 0;
        private bool _aiming = false;
        private bool _sprinting = false;
        private float _aimLayerWieght = 0;

        private Vector2 _aimedMovingAnimtionsInput = Vector2.zero;
        private float aimRigWieght = 0;
        private float leftHandWeight = 0;

        private const float _threshold = 0.01f;

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
#endif
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            _input.OnSprintKeyPressed += HandleSprintKeyPressed;

            // 시작 보정: 3번(Secondary) 장착 시도
            TryEquipWeaponBySlot(2);
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

            if (_isRolling)
            {
                _character.weapon?.StopFiring();
                _input.shoot = false;
                return;
            }

            Move();
            Rotate();

            HandleReloadInput();
            HandleWeaponSlotInputs();

            HandleShooting();
        }

        private void LateUpdate() => CameraRotation();

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

            // 비조준/점프에서도 무기 설정에 따라 그립 유지
            bool keepGrip = armed &&
                            !_character.switchingWeapon &&
                            !_character.reloading &&
                            !_isRolling &&
                            (_aiming || _character.weapon.keepGripInAir);

            leftHandWeight = Mathf.Lerp(leftHandWeight, keepGrip ? 1f : 0f, 10f * Time.deltaTime);
            _rigManager.leftArmWeight = keepGrip ? 1f : 0f;

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
            targetSpeed = _walking ? WalkSpeed : RunSpeed;
            _speedAnimationMultiplier = _walking ? 1 : (_sprinting ? 3 : 2);

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
            if (_input.switchToPrimary) { _input.switchToPrimary = false; TryEquipWeaponBySlot(0); }
            else if (_input.switchToSecondary) { _input.switchToSecondary = false; TryEquipWeaponBySlot(1); }
            else if (_input.switchToThird) { _input.switchToThird = false; TryEquipWeaponBySlot(2); }
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

            if (weapon.fireMode == Weapon.FireMode.SemiAuto)
            {
                if (_input.shoot)
                {
                    weapon.StartFiring(_character, () => CameraManager.singleton.aimTargetPiont, this);
                    _input.shoot = false;
                    _rigManager.ApplyWeaponKick(weapon.handKick, weapon.bodyKick);
                }
            }
            else
            {
                if (_input.shoot) weapon.StartFiring(_character, () => CameraManager.singleton.aimTargetPiont, this);
                else weapon.StopFiring();
            }
        }

        private void Move()
        {
            if (_input.move == Vector2.zero) targetSpeed = 0f;

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;
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

            Vector3 inputDirection = new Vector3(_input.move.x, 0f, _input.move.y).normalized;

            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

                if (!_aiming) transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;

            _controller.Move(
                targetDirection.normalized * (_speed * Time.deltaTime) +
                new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime
            );

            if (_hasAnimator)
            {
                _animator.SetFloat("Speed", _animationBlend);
                _animator.SetFloat("MotionSpeed", inputMagnitude);
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
                float dtMul = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                _cinemachineTargetYaw += _input.look.x * CameraManager.singleton.sensitivity * dtMul;
                _cinemachineTargetPitch += _input.look.y * CameraManager.singleton.sensitivity * dtMul;
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation =
                Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0f);
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
            if (_hasAnimator) _animator.SetBool("Grounded", Grounded);
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool("Jump", false);
                    _animator.SetBool("FreeFall", false);
                }

                if (_verticalVelocity < 0f) _verticalVelocity = -2f;

                if (_input.jump && _jumpTimeoutDelta <= 0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    if (_hasAnimator) _animator.SetBool("Jump", true);
                }

                if (_jumpTimeoutDelta >= 0f) _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator) _animator.SetBool("FreeFall", true);
                }

                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
                _verticalVelocity += Gravity * Time.deltaTime;
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
                Vector3 camF = Quaternion.Euler(0, cameraY, 0) * Vector3.forward;
                Vector3 camR = Quaternion.Euler(0, cameraY, 0) * Vector3.right;
                rollDirection = (camF * moveInput.y + camR * moveInput.x).normalized;
                transform.rotation = Quaternion.LookRotation(rollDirection);
            }

            _input.aim = false;
            _input.canAim = false;
            CameraManager.singleton.aiming = false;

            _animator.SetTrigger("Roll");

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

        private void TryEquipWeaponBySlot(int slotIndex)
        {
            Weapon w = _character.GetWeaponBySlotIndex(slotIndex);
            if (w != null) _character.EquipWeapon(w);
            else Debug.LogWarning($"슬롯 {slotIndex + 1}에 장비 가능한 무기가 없습니다.");
        }
    }
}
