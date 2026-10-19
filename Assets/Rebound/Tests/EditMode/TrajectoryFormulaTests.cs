using System;
using System.IO;
using NUnit.Framework;
using Rebound.Full;
using UnityEngine;

public sealed class TrajectoryFormulaTests
{
    static WeaponData Formula(string x="v*t",string y="a*sin(2*pi*f*t)")=>new WeaponData{id="pulse",name="Formula",motionMode="formula",formulaX=x,formulaY=y};
    [Test]public void FormulaAndParameterWavesMatchAtRotatedLaunchAndDifferentFrameRates()
    {
        var param=new WeaponData{trajectory="wave",amplitude=.6f,frequency=2};var expr=Formula();
        Vector2 velocity=Quaternion.Euler(0,0,30)*Vector2.right*23,origin=new Vector2(-4,1);
        var a=new ProjectileMotion(origin,velocity);var b=a;var c=a;
        for(int i=0;i<100;i++){a.Step(param,.01f);b.Step(expr,.01f);Assert.Less(Vector2.Distance(a.Position,b.Position),.0001f);}
        for(int i=0;i<20;i++)c.Step(expr,.05f);
        Assert.Less(Vector2.Distance(b.Position,c.Position),.0001f);Assert.IsFalse(c.Invalid);
    }
    [Test]public void ArithmeticPrecedenceFunctionsAndScientificNotation()
    {
        var data=Formula();
        var f=TrajectoryFormula.Compile("-2^2 + 2^3^2 + 1e-2 + max(2,3)","clamp(sqrt(9)+abs(-2),0,4)+cos(pi)+sin(pi/2)");
        Assert.IsTrue(f.TryEvaluate(0,17,data,out var p));Assert.AreEqual(511.01f,p.x,.001f);Assert.AreEqual(4,p.y,.0001f);
        f=TrajectoryFormula.Compile("v*t+0.5*acc*t^2","-0.5*g*t^2");data.gravity=6;data.acceleration=2;
        Assert.IsTrue(f.TryEvaluate(2,10,data,out p));Assert.AreEqual(24,p.x);Assert.AreEqual(-12,p.y);
    }
    [TestCase("System.IO.File.ReadAllText(1)")][TestCase("2t")][TestCase("sin(t")]
    [TestCase("sin(t,2)")][TestCase("clamp(t,0)")][TestCase("1e999")][TestCase("unknown*t")]
    public void InvalidSyntaxIsRejected(string text){Assert.Throws<FormatException>(()=>TrajectoryFormula.Compile(text,"0"));}
    [Test]public void ComplexityAndInvalidResultsAreBounded()
    {
        Assert.Throws<FormatException>(()=>TrajectoryFormula.Compile(new string('(',24)+"t"+new string(')',24),"0"));
        Assert.Throws<FormatException>(()=>TrajectoryFormula.Compile(new string('t',257),"0"));
        Assert.Throws<FormatException>(()=>TrajectoryFormula.Validate(Formula("1/0","0")));
        Assert.Throws<FormatException>(()=>TrajectoryFormula.Validate(Formula("v*t","sqrt(-t)")));
        Assert.Throws<FormatException>(()=>TrajectoryFormula.Validate(Formula("v*t","cos(t)")),"Reject initial teleport");
        var w=Formula("v*t","t/(t-0.123)");var motion=new ProjectileMotion(Vector2.zero,Vector2.right*17);
        motion.Step(w,.123f);Assert.IsTrue(motion.Invalid);Assert.AreEqual(Vector2.zero,motion.Position);
    }
    [Test]public void FormulaRoundTripsThroughZipAndBadEditDoesNotReplaceSavedWeapon()
    {
        string root=Path.Combine(Path.GetTempPath(),"trajectory-formula-"+Guid.NewGuid().ToString("N"));
        try
        {
            var baseline=BuiltinContent.Create();var project=new WorkshopProject(Path.Combine(root,"draft"),baseline);
            project.Draft.manifest.targetGameVersion="1.1.0";
            var data=Formula("v*t","a*sin(2*pi*f*t)+0.2*sin(2*pi*5*t)");project.Apply("weapons",data);
            Assert.AreEqual("1.2.0",project.Draft.manifest.targetGameVersion);
            string expected=data.formulaY;data.formulaY="sqrt(-t)";Assert.Throws<FormatException>(()=>project.Apply("weapons",data));
            Assert.AreEqual(expected,project.Validate().Weapons["pulse"].formulaY);
            string zip=project.Export(Path.Combine(root,"exports"));var manager=new ModManager(Path.Combine(root,"mods"));string id=manager.Install(zip);manager.Scan(new[]{id});var content=manager.Load(baseline);
            Assert.IsEmpty(manager.Errors);Assert.AreEqual("formula",content.Weapons["pulse"].motionMode);Assert.AreEqual(expected,content.Weapons["pulse"].formulaY);
            var legacy=JsonUtility.FromJson<WeaponData>("{\"id\":\"legacy\",\"name\":\"Legacy\"}");Assert.AreEqual("parameters",legacy.motionMode);
        }
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
}
