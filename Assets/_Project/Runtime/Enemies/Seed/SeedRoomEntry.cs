using System.Collections.Generic;
using System.Linq;
using Bellerophon.Enemies.Parvum;
using UnityEngine;

namespace Bellerophon.Enemies.Seed
{
    public readonly struct SeedRoomDoor
    {
        public readonly ParvumTarget Room;
        public readonly Vector3 Point, Inward;
        public readonly string Name;
        public SeedRoomDoor(ParvumTarget room, Vector3 point, Vector3 inward, string name)
        { Room=room; Point=point; Inward=inward; Name=room.transform.root.name+" / "+name+" / "+point.ToString("F2"); }
    }

    // Shared entry gate, independent of walking/flying and species combat priorities.
    // Call Observe every movement tick, TryComplete before resuming local objectives,
    // and defer food selection/contact while Pending. Never move the actor here.
    public sealed class SeedRoomEntry
    {
        public const float AdvanceDistance=2f;
        readonly List<SeedRoomDoor> doors=new List<SeedRoomDoor>();
        Vector3 previous;
        bool initialized;
        public IReadOnlyList<SeedRoomDoor> Doors=>doors;
        public SeedRoomDoor Door { get; private set; }
        public bool Pending { get; private set; }
        public bool Crossed { get; private set; }
        public string LastCompletedName { get; private set; }
        public float LastCompletedAdvance { get; private set; }
        public string Diagnostic { get; private set; }
        public float Depth(Vector3 position)=>Vector3.Dot(position-Door.Point,Door.Inward);
        public Vector3 Goal=>Door.Point+Door.Inward*(AdvanceDistance+.025f);
        public void Initialize(Vector3 position){previous=position;initialized=true;}
        public bool Observe(Vector3 position)
        {
            if(!initialized)Initialize(position);
            if(doors.Count==0)
                foreach(var group in ParvumTarget.Active.Where(x=>x.IsRoomWall).GroupBy(x=>x.transform.root))
                    foreach(var door in ParvumRoomEntrance.Read(group.Key))
                        doors.Add(new SeedRoomDoor(group.First(),door.Point,door.Inward,door.Name));
            bool changed=false;
            // A deferred food route can cross just outside the generic 1.6 m entrance
            // probe while still entering the real room. Once it is 2 m beyond the
            // tracked doorway, do not leave the creature indefinitely pending.
            if(Pending && !Crossed && Depth(position)>=AdvanceDistance &&
                Mathf.Abs(Vector3.Dot(position-Door.Point,Vector3.Cross(Vector3.up,Door.Inward)))<2.5f)
            {Crossed=true;Diagnostic+=$"; entered tracked doorway at {Time.time:F3}";changed=true;}
            if(Pending && Crossed && Depth(position)<-.1f){Pending=false;Crossed=false;changed=true;}
            foreach(var door in doors)
            {
                float before=Vector3.Dot(previous-door.Point,door.Inward),after=Vector3.Dot(position-door.Point,door.Inward);
                // Points lie on upper headers; warehouse headers are over 5m above walking feet.
                if(before>=0 || after<0 || Mathf.Abs(Vector3.Dot(position-door.Point,Vector3.Cross(Vector3.up,door.Inward)))>1.6f || position.y>door.Point.y+1 || position.y<door.Point.y-8)continue;
                Begin(door,true);changed=true;break;
            }
            previous=position;return changed;
        }
        public bool DeferMetal(ParvumTarget metal,Vector3 position)
        {
            if(Pending)return true;
            if(!metal || !metal.IsRoomWall)return false;
            foreach(var door in doors)
            {
                if(door.Room.Room!=metal.Room || door.Room.Ship!=metal.Ship)continue;
                float depth=Vector3.Dot(position-door.Point,door.Inward);
                if(depth<0 && depth>-5 && Mathf.Abs(Vector3.Dot(position-door.Point,Vector3.Cross(Vector3.up,door.Inward)))<2.5f && position.y<door.Point.y+1 && position.y>door.Point.y-8)
                {Begin(door,false);return true;}
            }
            return false;
        }
        void Begin(SeedRoomDoor door,bool crossed)
        {
            Door=door;Pending=true;Crossed=crossed;
            Diagnostic=$"{(crossed?"Entered":"Approaching")} {door.Name} at {Time.time:F3}";
        }
        public bool TryComplete(Vector3 position)
        {
            if(!Pending || !Crossed || Depth(position)<AdvanceDistance)return false;
            LastCompletedAdvance=Depth(position);LastCompletedName=Door.Name;
            Diagnostic+=$"; completed {LastCompletedAdvance:F4}m at {Time.time:F3}";
            Pending=false;Crossed=false;return true;
        }
    }
}
