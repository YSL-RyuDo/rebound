using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Rebound.Full
{
    // A small, bounded arithmetic language. No C# compilation, reflection or external calls.
    // x/y are displacement in the launch direction/its left normal; t is seconds since spawn.
    public sealed class TrajectoryFormula
    {
        readonly Node x,y;
        static readonly Dictionary<string,TrajectoryFormula> cache=new Dictionary<string,TrajectoryFormula>();
        TrajectoryFormula(string horizontal,string vertical){x=new Parser(horizontal).Parse();y=new Parser(vertical).Parse();}
        public static TrajectoryFormula Compile(string horizontal,string vertical)
        {
            string key=horizontal+"\n"+vertical;
            if(cache.TryGetValue(key,out var found))return found;
            var result=new TrajectoryFormula(horizontal,vertical);
            if(cache.Count>=128)cache.Clear();cache.Add(key,result);return result;
        }
        public bool TryEvaluate(double t,double speed,WeaponData data,out Vector2 point)
        {
            var vars=new Variables{t=t,v=speed,a=data.amplitude,f=data.frequency,g=data.gravity,acc=data.acceleration};
            double px=x.Eval(vars),py=y.Eval(vars);
            bool valid=Finite(px)&&Finite(py)&&Math.Abs(px)<=10000&&Math.Abs(py)<=10000;
            point=valid?new Vector2((float)px,(float)py):Vector2.zero;return valid;
        }
        static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        public static void Validate(WeaponData data)
        {
            try
            {
                var formula=Compile(data.formulaX,data.formulaY);
                if(!formula.TryEvaluate(0,data.speed,data,out var origin)||origin.sqrMagnitude>0.00000001f)
                    throw new FormatException("x(0) and y(0) must both be 0 / 발사점은 (0, 0)이어야 합니다.");
                // A preview-time sample check is not a proof of continuity. Runtime evaluation is checked too.
                for(int i=1;i<=256;i++)
                    if(!formula.TryEvaluate(data.lifetime*i/256.0,data.speed,data,out _))
                        throw new FormatException("Undefined or excessive result within lifetime / 수명 내 계산 불가 또는 범위 초과.");
            }
            catch(FormatException e){throw new FormatException("Formula / 수식: "+e.Message);}
        }
        struct Variables{public double t,v,a,f,g,acc;}
        sealed class Node
        {
            public string op;public double value;public Node left,right,third;
            public double Eval(Variables vars)
            {
                switch(op)
                {
                    case "number":return value;
                    case "t":return vars.t;case "v":return vars.v;case "a":return vars.a;
                    case "f":return vars.f;case "g":return vars.g;case "acc":return vars.acc;
                    case "pi":return Math.PI;case "e":return Math.E;
                }
                double l=left.Eval(vars),r=right!=null?right.Eval(vars):0;
                if(!Finite(l)||!Finite(r))return double.NaN;
                switch(op)
                {
                    case "+":return l+r;case "-":return l-r;case "*":return l*r;
                    case "/":return r==0?double.NaN:l/r;case "^":return Math.Pow(l,r);
                    case "neg":return -l;case "sin":return Math.Sin(l);case "cos":return Math.Cos(l);
                    case "abs":return Math.Abs(l);case "sqrt":return Math.Sqrt(l);
                    case "min":return Math.Min(l,r);case "max":return Math.Max(l,r);
                    case "clamp":double hi=third.Eval(vars);return Finite(hi)&&r<=hi?Math.Max(r,Math.Min(hi,l)):double.NaN;
                    default:return double.NaN;
                }
            }
        }
        sealed class Parser
        {
            readonly string source;int at,nodes,depth;
            public Parser(string text)
            {
                if(string.IsNullOrWhiteSpace(text)||text.Length>256)throw new FormatException("Enter 1–256 characters per axis.");
                source=text;
            }
            FormatException Error(string text)=>new FormatException(text+" (column "+(at+1)+")");
            void Space(){while(at<source.Length&&char.IsWhiteSpace(source[at]))at++;}
            bool Take(char c){Space();if(at>=source.Length||source[at]!=c)return false;at++;return true;}
            void Expect(char c){if(!Take(c))throw Error("Expected '"+c+"'");}
            Node Make(string op,Node left=null,Node right=null,Node third=null,double value=0)
            {if(++nodes>96)throw Error("Expression exceeds 96 operations/values");return new Node{op=op,left=left,right=right,third=third,value=value};}
            public Node Parse(){var node=Sum();Space();if(at!=source.Length)throw Error("Unexpected character; use explicit * for multiplication");return node;}
            Node Sum(){var n=Product();while(true){if(Take('+'))n=Make("+",n,Product());else if(Take('-'))n=Make("-",n,Product());else return n;}}
            Node Product(){var n=Unary();while(true){if(Take('*'))n=Make("*",n,Unary());else if(Take('/'))n=Make("/",n,Unary());else return n;}}
            Node Unary()
            {
                if(++depth>20)throw Error("Expression nesting exceeds 20");
                Node n;if(Take('+'))n=Unary();else if(Take('-'))n=Make("neg",Unary());else n=Power();depth--;return n;
            }
            Node Power(){var n=Atom();if(Take('^'))n=Make("^",n,Unary());return n;}
            Node Atom()
            {
                Space();if(Take('(')){var n=Sum();Expect(')');return n;}
                if(at>=source.Length)throw Error("Expected a number, variable or function");
                if(char.IsDigit(source[at])||source[at]=='.')
                {
                    int start=at;
                    while(at<source.Length&&(char.IsDigit(source[at])||source[at]=='.'))at++;
                    if(at<source.Length&&(source[at]=='e'||source[at]=='E'))
                    {at++;if(at<source.Length&&(source[at]=='+'||source[at]=='-'))at++;while(at<source.Length&&char.IsDigit(source[at]))at++;}
                    if(!double.TryParse(source.Substring(start,at-start),NumberStyles.Float,CultureInfo.InvariantCulture,out var number)||!Finite(number))throw Error("Invalid number");
                    return Make("number",value:number);
                }
                if(!char.IsLetter(source[at]))throw Error("Unsupported character");
                int begin=at;while(at<source.Length&&char.IsLetter(source[at]))at++;
                string id=source.Substring(begin,at-begin).ToLowerInvariant();
                if(id=="t"||id=="v"||id=="a"||id=="f"||id=="g"||id=="acc"||id=="pi"||id=="e")return Make(id);
                int count=id=="clamp"?3:id=="min"||id=="max"?2:id=="sin"||id=="cos"||id=="abs"||id=="sqrt"?1:0;
                if(count==0)throw Error("Unknown variable/function: "+id);
                Expect('(');var first=Sum();Node second=null,third=null;
                if(count>1){Expect(',');second=Sum();}if(count>2){Expect(',');third=Sum();}Expect(')');
                return Make(id,first,second,third);
            }
        }
    }
}
