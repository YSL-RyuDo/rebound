using UnityEngine;

namespace Rebound.Full
{
    // Shared by the combat pool and Workshop preview. All rates are in seconds/world units.
    public struct ProjectileMotion
    {
        public Vector2 Center,Velocity,Position;
        double age;Vector2 normal,origin,forward;float initialSpeed;TrajectoryFormula formula;
        public bool Invalid {get;private set;}
        public ProjectileMotion(Vector2 position,Vector2 velocity)
        {Center=Position=origin=position;Velocity=velocity;age=0;initialSpeed=velocity.magnitude;forward=initialSpeed>0?velocity.normalized:Vector2.right;normal=new Vector2(-forward.y,forward.x);formula=null;Invalid=false;}
        public Vector2 Step(WeaponData data,float dt,Vector2? target=null)
        {
            if(Invalid||dt<=0)return Position;
            age+=dt;
            if(data!=null&&data.motionMode=="formula")
            {
                if(formula==null)
                {
                    try{formula=TrajectoryFormula.Compile(data.formulaX,data.formulaY);}
                    catch(System.FormatException){Invalid=true;return Position;}
                }
                if(!formula.TryEvaluate(age,initialSpeed,data,out var local)){Invalid=true;return Position;}
                var next=origin+forward*local.x+normal*local.y;
                Velocity=(next-Position)/dt;Center=Position=next;return Position;
            }
            if(data!=null)
            {
                float speed=Mathf.Clamp(Velocity.magnitude+data.acceleration*dt,0,60);
                Vector2 direction=Velocity.sqrMagnitude>0?Velocity.normalized:Vector2.right;
                if(data.trajectory=="homing"&&target.HasValue)
                {
                    float angle=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
                    var delta=target.Value-Center;
                    float next=Mathf.MoveTowardsAngle(angle,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,data.turnRate*dt);
                    direction=new Vector2(Mathf.Cos(next*Mathf.Deg2Rad),Mathf.Sin(next*Mathf.Deg2Rad));
                }
                Velocity=direction*speed;
                if(data.trajectory=="arc")Velocity+=Vector2.down*data.gravity*dt;
            }
            Center+=Velocity*dt;Position=Center;
            if(data!=null&&data.trajectory=="wave")Position+=normal*(Mathf.Sin((float)(age*data.frequency*Mathf.PI*2))*data.amplitude);
            return Position;
        }
    }
}
