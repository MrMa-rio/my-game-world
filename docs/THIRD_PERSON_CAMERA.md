# Third-Person Camera

## Runtime model

The Player camera keeps first-person and third-person as replaceable strategies. The third-person strategy follows the same rig model used by Unity Cinemachine Third Person Follow:

```text
tracked origin
  -> horizontal yaw
  -> shoulder offset
  -> vertical arm controlled by pitch
  -> camera distance
```

Input changes accumulated yaw and pitch. Tracking position uses a single exponential damping stage. Camera orientation comes directly from the orbit and is not recomputed from the collision-corrected position, preventing obstacle avoidance from shaking aim.

Collision uses a non-allocating SphereCast, ignores triggers and every collider under the tracked Actor. Occlusion pulls the camera inward immediately; returning to the configured distance is gradual. No second position damping is applied after collision resolution.

## References

- Unity Cinemachine 3.1 Third Person Follow: https://docs.unity.cn/Packages/com.unity.cinemachine@3.1/api/Unity.Cinemachine.CinemachineThirdPersonFollow.html
- Cinemachine Camera tracking targets and procedural motion: https://docs.unity.cn/Packages/com.unity.cinemachine@3.1/manual/CinemachineCamera.html
- Cinemachine Brain update modes and SmartUpdate guidance: https://docs.unity.cn/Packages/com.unity.cinemachine@3.1/api/Unity.Cinemachine.CinemachineBrain.html

## Current limitations

Camera shoulder switching, zoom, recentering, lock-on and camera impulse are not implemented yet. They can be introduced as camera capabilities without changing Actor locomotion.
