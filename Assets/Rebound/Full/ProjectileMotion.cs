using UnityEngine;

namespace Rebound.Full
{
    // Shared by the combat pool and Workshop preview. All rates are in seconds/world units.
    public struct ProjectileMotion
    {
        public Vector2 Center,Velocity,Position;
        float age;Vector2 normal;
        public ProjectileMotion(Vector2 position,Vector2 velocity)
        {Center=Position=position;Velocity=velocity;age=0;normal=new Vector2(-velocity.y,velocity.x).normalized;}
        public Vector2 Step(WeaponData data,float dt,Vector2? target=null)
        {
            age+=dt;
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
            if(data!=null&&data.trajectory=="wave")Position+=normal*(Mathf.Sin(age*data.frequency*Mathf.PI*2)*data.amplitude);
            return Position;
        }
    }
}
