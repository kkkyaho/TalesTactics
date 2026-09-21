// Minimal managed substitutes used ONLY by the engine-independent rule test runner.
// This does not validate Unity lifecycle, graphics, serialization or input.
using System;
namespace UnityEngine {
 public class Object { public static void DestroyImmediate(Object o){} }
 public class ScriptableObject:Object { public static T CreateInstance<T>() where T:ScriptableObject => (T)Activator.CreateInstance(typeof(T)); }
 public class Sprite:Object {} public class RuntimeAnimatorController:Object {} public class AudioClip:Object {}
 public class CreateAssetMenuAttribute:Attribute {public string menuName;}
 public class RangeAttribute:Attribute {public RangeAttribute(float a,float b){} }
 public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;} public static Color cyan=>new Color(0,1,1); }
 public struct Vector2Int:IEquatable<Vector2Int> {
  public int x,y; public Vector2Int(int x,int y){this.x=x;this.y=y;}
  public static Vector2Int zero=>new Vector2Int(0,0);public static Vector2Int up=>new Vector2Int(0,1);public static Vector2Int down=>new Vector2Int(0,-1);public static Vector2Int left=>new Vector2Int(-1,0);public static Vector2Int right=>new Vector2Int(1,0);
  public static Vector2Int operator +(Vector2Int a,Vector2Int b)=>new Vector2Int(a.x+b.x,a.y+b.y);public static Vector2Int operator -(Vector2Int a,Vector2Int b)=>new Vector2Int(a.x-b.x,a.y-b.y);public static Vector2Int operator -(Vector2Int a)=>new Vector2Int(-a.x,-a.y);
  public static bool operator ==(Vector2Int a,Vector2Int b)=>a.x==b.x&&a.y==b.y;public static bool operator !=(Vector2Int a,Vector2Int b)=>!(a==b);
  public bool Equals(Vector2Int b)=>this==b;public override bool Equals(object b)=>b is Vector2Int v&&this==v;public override int GetHashCode()=>HashCode.Combine(x,y);public override string ToString()=>$"({x},{y})";
 }
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} }
 public static class Mathf {
  public static int Clamp(int x,int a,int b)=>Math.Clamp(x,a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static int Abs(int x)=>Math.Abs(x);public static int RoundToInt(float f)=>(int)Math.Round(f);public static int CeilToInt(float f)=>(int)Math.Ceiling(f);
 }
 public static class Application { public static string persistentDataPath=>System.IO.Path.Combine(System.IO.Path.GetTempPath(),"TalesTacticsManagedChecks"); }
 public static class Debug {public static void LogWarning(object m)=>Console.WriteLine(m);}
 public static class JsonUtility {
  static System.Text.Json.JsonSerializerOptions opts=new System.Text.Json.JsonSerializerOptions{IncludeFields=true};
  public static string ToJson(object x,bool pretty=false)=>System.Text.Json.JsonSerializer.Serialize(x,opts);
  public static T FromJson<T>(string x)=>System.Text.Json.JsonSerializer.Deserialize<T>(x,opts);
 }
}
