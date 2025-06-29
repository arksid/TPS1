﻿using System;
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

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

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private bool _isRolling = false;
        private float _lastRollTime = -10f;
        private float _lastSprintKeyTime = -1f;

        public float RollCooldown = 1f;
        public float RollDuration = 0.6f;
        public float RollSpeed = 6f;

        private int rollHash; // 애니메이션 트리거
        private bool _isInvincible = false;
        public float InvincibleDuration = 0.4f;


        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

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
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            _input.OnSprintKeyPressed += HandleSprintKeyPressed;
            rollHash = Animator.StringToHash("Roll");
        }

        private void Update()
        {
            bool armed = _character.weapon != null;

            _aiming = _input.aim;
            _sprinting = _input.sprint && _aiming == false;

            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            CameraManager.singleton.aiming = _aiming;
            _animator.SetFloat("Armed", armed ? 1f : 0f);
            _animator.SetFloat("Aimed", _input.aim ? 1f : 0f);

            _aimLayerWieght = Mathf.Lerp(_aimLayerWieght, _character.switchingWeapon || (armed && (_aiming || _character.reloading)) ? 1f : 0f, 10 * Time.deltaTime);
            _animator.SetLayerWeight(1, _aimLayerWieght);

            aimRigWieght = Mathf.Lerp(aimRigWieght, armed && _aiming &&  !_character.reloading ? 1f : 0f, 10f * Time.deltaTime);
            leftHandWeight = Mathf.Lerp(leftHandWeight, armed&& _character.switchingWeapon == false && !_character.reloading && (_aiming || (_controller.isGrounded && _character.weapon.type == Weapon.Handle.TwoHanded))  ? 1f : 0f, 10f * Time.deltaTime);


            _rigManager.aimTarget = CameraManager.singleton.aimTargetPiont;
            _rigManager.aimWeight = aimRigWieght;
            _rigManager.leftHandWeight = leftHandWeight;

            if (_input.walk)
            {
                _input.walk = false;
                _walking = !_walking;
            }
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
            _aimedMovingAnimtionsInput = Vector2.Lerp(_aimedMovingAnimtionsInput, _input.move.normalized * _speedAnimationMultiplier,SpeedChangeRate * Time.deltaTime);
            _animator.SetFloat("Speed_X", _aimedMovingAnimtionsInput.x);
            _animator.SetFloat("Speed_Y", _aimedMovingAnimtionsInput.y);





        

            // 사격 로직
            if (armed && !_character.reloading && _aiming)
            {
                var weapon = _character.weapon;

                if (weapon.fireMode == Weapon.FireMode.SemiAuto)
                {
                    if (_input.shoot) // 클릭한 프레임에만 발사
                    {
                        _character.weapon.StartFiring(_character, () => CameraManager.singleton.aimTargetPiont, this);
                        _input.shoot = false; // 클릭 한 번만 처리
                        _rigManager.ApplyWeaponKick(weapon.handKick, weapon.bodyKick);
                    }
                }
                else if (weapon.fireMode == Weapon.FireMode.Burst || weapon.fireMode == Weapon.FireMode.FullAuto)
                {
                    if (_input.shoot)
                    {
                        _character.weapon.StartFiring(_character, () => CameraManager.singleton.aimTargetPiont, this);
                    }
                    else
                    {
                        weapon.StopFiring();
                    }
                }
            }

            if (_input.reload && !_character.reloading)
            {
                _input.reload = false;
                _character.weapon?.StopFiring(); // 발사 중단
                _character.Reload();
            }

            if (_input.switchToPrimary)
            {
                _input.switchToPrimary = false;
                TryEquipWeaponBySlot(0); // 1번 슬롯: 주무기
            }
            else if (_input.switchToSecondary)
            {
                _input.switchToSecondary = false;
                TryEquipWeaponBySlot(1); // 2번 슬롯: 주무기
            }
            else if (_input.switchToThird)
            {
                _input.switchToThird = false;
                TryEquipWeaponBySlot(2); // 3번 슬롯: 보조무기
            }

            if (_isRolling) return;

            Move();
            Rotate();
        }
        private void TryEquipWeaponBySlot(int slotIndex)
        {
            Weapon weapon = _character.GetWeaponBySlotIndex(slotIndex);
            if (weapon != null)
            {
                _character.EquipWeapon(weapon);
            }
            else
            {
                Debug.LogWarning($"슬롯 {slotIndex + 1}에 장비 가능한 무기가 없습니다.");
            }
        }


        private void Rotate()
        {
            if(_aiming)
            {
                Vector3 aimTarget = CameraManager.singleton.aimTargetPiont;
                aimTarget.y = transform.position.y; 
                Vector3 aimDirection = (aimTarget - transform.position).normalized;
                transform.forward = Vector3.Lerp(transform.forward, aimDirection, AimRotationSpeed * Time.deltaTime);
            }
        }
        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * CameraManager.singleton.sensitivity * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * CameraManager.singleton.sensitivity * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
 
            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, _input.move == Vector2.zero ? 0 : _speedAnimationMultiplier, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                if (_aiming == false)
                {
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }
                   
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }
        private void HandleSprintKeyPressed()
        {
            //  공중 상태에서는 구르기 금지
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

            // 1. 방향 계산
            Vector2 moveInput = _input.move;
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);

            Vector3 rollDirection;
            if (inputDirection.sqrMagnitude < 0.01f)
            {
                rollDirection = transform.forward;
            }
            else
            {
                //  카메라 기준 방향 계산
                float cameraY = CameraManager.maincamera.transform.eulerAngles.y;
                Vector3 camForward = Quaternion.Euler(0, cameraY, 0) * Vector3.forward;
                Vector3 camRight = Quaternion.Euler(0, cameraY, 0) * Vector3.right;
                rollDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

                //  플레이어가 그 방향을 바라보게 회전
                transform.rotation = Quaternion.LookRotation(rollDirection);
            }

            // 3. 조준 상태 해제
            _input.aim = false;
            _input.canAim = false;
            CameraManager.singleton.aiming = false;

            // 무적 시작
            _isInvincible = true;
            _character.isInvincible = true;
            Invoke(nameof(EndInvincibility), InvincibleDuration);

            _animator.SetTrigger("Roll");

            // 4. 애니메이션
            _animator.SetTrigger("Roll");

            // 5. 실제 이동
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
        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
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