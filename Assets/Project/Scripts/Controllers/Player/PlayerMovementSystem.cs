using PrisonLife.Core;
using PrisonLife.Models;
using UnityEngine;

namespace PrisonLife.Controllers.Player
{
    public class PlayerMovementSystem
    {
        readonly PlayerModel playerModel;
        readonly IMover mover;
        Transform cachedCameraTransform;

        public PlayerMovementSystem(PlayerModel _playerModel, IMover _mover)
        {
            playerModel = _playerModel;
            mover = _mover;
        }

        public void Tick(Vector2 _joystickInput)
        {
            if (playerModel == null || mover == null) return;

            if (_joystickInput.sqrMagnitude < 0.0001f)
            {
                mover.SetVelocity(Vector3.zero);
                return;
            }

            Vector3 worldDirection = ConvertJoystickToCameraRelativeDirection(_joystickInput);
            Vector3 worldVelocity = worldDirection * playerModel.MoveSpeed.Value;
            mover.SetVelocity(worldVelocity);
        }

        Vector3 ConvertJoystickToCameraRelativeDirection(Vector2 _joystickInput)
        {
            if (cachedCameraTransform == null && Camera.main != null)
            {
                cachedCameraTransform = Camera.main.transform;
            }

            Vector3 cameraForwardOnGround;
            Vector3 cameraRightOnGround;

            if (cachedCameraTransform != null)
            {
                cameraForwardOnGround = Vector3.ProjectOnPlane(cachedCameraTransform.forward, Vector3.up).normalized;
                cameraRightOnGround = Vector3.ProjectOnPlane(cachedCameraTransform.right, Vector3.up).normalized;
                if (cameraForwardOnGround.sqrMagnitude < 0.0001f) cameraForwardOnGround = Vector3.forward;
                if (cameraRightOnGround.sqrMagnitude < 0.0001f) cameraRightOnGround = Vector3.right;
            }
            else
            {
                cameraForwardOnGround = Vector3.forward;
                cameraRightOnGround = Vector3.right;
            }

            return cameraForwardOnGround * _joystickInput.y + cameraRightOnGround * _joystickInput.x;
        }
    }
}
