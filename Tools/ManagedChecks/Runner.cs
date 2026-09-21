using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
public static class Runner {
 public static int Main(){int passed=0,failed=0;var type=typeof(TalesTactics.Tests.BattleRuleTests);foreach(var test in type.GetMethods().Where(m=>m.GetCustomAttribute<TestAttribute>()!=null)){var instance=Activator.CreateInstance(type);try{type.GetMethod("Setup").Invoke(instance,null);test.Invoke(instance,null);Console.WriteLine("PASS "+test.Name);passed++;}catch(Exception e){Console.WriteLine("FAIL "+test.Name+" : "+(e.InnerException??e).Message);failed++;}finally{type.GetMethod("Cleanup").Invoke(instance,null);}}Console.WriteLine($"RESULT {passed} passed, {failed} failed (managed rules only; Unity runtime not exercised)");return failed==0?0:1;}
}
