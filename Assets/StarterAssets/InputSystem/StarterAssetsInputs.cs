using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool aim;
		public bool shoot;
		public bool walk;
		public bool reload;
		public float switchWeapon;
        public bool sprintPressedThisFrame;
        public event Action OnSprintKeyPressed;
        public bool switchToPrimary;
        public bool switchToSecondary;
        public bool switchToThird;
        public bool canAim = true; // 조준 허용 여부
        [Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM

        public void OnAim(InputValue value)
        {
            if (!canAim) return;        // 구르기 중에는 조준 차단
            AimInput(value.isPressed);
        }

        public void OnShoot(InputValue value)
		{
            ShootInput(value.isPressed);
        }
        public void OnReload(InputValue value)
        {
            ReloadInput(value.isPressed);
        }
        public void OnWalk(InputValue value)
		{
            WalkInput(value.isPressed);
        }
        public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}
        public void OnSwitchWeapon(InputValue value)
        {
            SwitchWeaponInput(value.Get<float>());
        }
        public void OnSwitchToPrimary(InputValue value)
        {
            Debug.Log("Pressed 1 Key");
            switchToPrimary = value.isPressed;
        }

        public void OnSwitchToSecondary(InputValue value)
        {
            Debug.Log("Pressed 2 Key");
            switchToSecondary = value.isPressed;
        }
        public void OnSwitchToThird(InputValue value)
        {
            Debug.Log("Pressed 3 Key");
            switchToThird = value.isPressed;
        }
        public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

        public void OnSprint(InputValue value)
        {
            bool pressed = value.isPressed;

            if (pressed && OnSprintKeyPressed != null)
            {
                OnSprintKeyPressed.Invoke(); // 눌린 순간만 감지
            }

            sprint = pressed;
        }
#endif


        public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}
        public void AimInput(bool newAimState)
        {
            aim = newAimState;
        }

        public void ShootInput(bool newShootState)
        {
            shoot = newShootState;
        }
        public void SwitchWeaponInput(float newSwitchWeaponState)
        {
            switchWeapon = newSwitchWeaponState;
        }

        public void ReloadInput(bool newReloadState)
        {
            reload = newReloadState;
        }
        public void WalkInput(bool newWalkState)
		{
            walk = newWalkState;
        }
        public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}