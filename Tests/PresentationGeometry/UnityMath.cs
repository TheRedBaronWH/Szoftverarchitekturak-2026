// Test-only arithmetic shim. This file is outside Assets and is never compiled into Unity.
// It exercises geometry without claiming to test Unity rendering or input hardware.
using System;
namespace UnityEngine
{
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 zero => new Vector3(0,0,0);
        public static Vector3 up => new Vector3(0,1,0);
        public static Vector3 right => new Vector3(1,0,0);
        public float sqrMagnitude => x*x+y*y+z*z;
        public float magnitude => MathF.Sqrt(sqrMagnitude);
        public Vector3 normalized => magnitude > 0.000001f ? this/magnitude : zero;
        public static Vector3 operator +(Vector3 a,Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b) => new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float k) => new Vector3(a.x*k,a.y*k,a.z*k);
        public static Vector3 operator /(Vector3 a,float k) => new Vector3(a.x/k,a.y/k,a.z/k);
        public static float Dot(Vector3 a,Vector3 b) => a.x*b.x+a.y*b.y+a.z*b.z;
        public static Vector3 Cross(Vector3 a,Vector3 b) => new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public static Vector3 ProjectOnPlane(Vector3 v,Vector3 n) => v-n*(Dot(v,n)/Dot(n,n));
        public static float Distance(Vector3 a,Vector3 b) => (a-b).magnitude;
        public static Vector3 Lerp(Vector3 a,Vector3 b,float t) => a+(b-a)*Mathf.Clamp01(t);
    }
    public static class Mathf
    {
        public static float Abs(float x) => MathF.Abs(x);
        public static float Min(float a,float b) => MathF.Min(a,b);
        public static float Max(float a,float b) => MathF.Max(a,b);
        public static int CeilToInt(float a) => (int)MathF.Ceiling(a);
        public static float Clamp01(float a) => Math.Clamp(a,0,1);
        public static float Lerp(float a,float b,float t) => a+(b-a)*Clamp01(t);
        public static int Clamp(int a,int b,int c) => Math.Clamp(a,b,c);
    }
}
namespace UnityEngine.InputSystem
{
    public enum Key { Digit1,Digit2,Digit3,Digit4,Digit5,Digit6,Digit7,Digit8,Digit9,Digit0,Minus,Equals,Q,E,T,Y,U,I,O,P,
        Numpad1,Numpad2,Numpad3,Numpad4,Numpad5,Numpad6,Numpad7,Numpad8,Numpad9,Numpad0 }
    public sealed class TestKey { public string displayName => null; }
    public sealed class Keyboard
    {
        public static Keyboard current => null;
        public TestKey this[Key key] => new TestKey();
    }
}
