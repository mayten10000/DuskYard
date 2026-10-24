using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace DuskYard {
internal static class DemoInput {
 public static bool GetKeyDown(KeyCode key) {
#if ENABLE_INPUT_SYSTEM
  var k=Keyboard.current;if(k==null)return false;
  switch(key){case KeyCode.Escape:return k.escapeKey.wasPressedThisFrame;case KeyCode.Space:return k.spaceKey.wasPressedThisFrame;case KeyCode.F1:return k.f1Key.wasPressedThisFrame;case KeyCode.F2:return k.f2Key.wasPressedThisFrame;case KeyCode.F3:return k.f3Key.wasPressedThisFrame;case KeyCode.F4:return k.f4Key.wasPressedThisFrame;default:return false;}
#else
  return Input.GetKeyDown(key);
#endif
 }
 public static bool GetMouseButtonDown(int button) {
#if ENABLE_INPUT_SYSTEM
  var m=Mouse.current;return m!=null&&(button==0?m.leftButton.wasPressedThisFrame:m.rightButton.wasPressedThisFrame);
#else
  return Input.GetMouseButtonDown(button);
#endif
 }
 public static float GetAxis(string axis) {
#if ENABLE_INPUT_SYSTEM
  var m=Mouse.current;return m==null?0:(axis=="Mouse X"?m.delta.ReadValue().x:m.delta.ReadValue().y)*.06f;
#else
  return Input.GetAxis(axis);
#endif
 }
 public static float GetAxisRaw(string axis) {
#if ENABLE_INPUT_SYSTEM
  var k=Keyboard.current;if(k==null)return 0;
  return axis=="Horizontal"?((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0)):((k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0));
#else
  return Input.GetAxisRaw(axis);
#endif
 }
}
}
