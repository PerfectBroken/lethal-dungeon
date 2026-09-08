using System;
using System.Linq;
using System.Collections.Generic;
namespace LethalDungeon.Domain.Dungeons
{
 internal static class PatternValidation
 {
  internal static void Validate(RoomPattern p){
   void Require(bool v,string message){if(!v)throw new ArgumentException(p.Room.Id+": "+message);}
   Require(!string.IsNullOrWhiteSpace(p.PrefabKey)&&p.Revision>0,"resource/revision");
   Require(p.Rotations.Count>0&&p.Rotations.Distinct().Count()==p.Rotations.Count&&p.Rotations.All(q=>q>=0&&q<4),"rotations");
   Require(p.Cells.Count>0&&p.Cells.Count<=16&&p.Cells.All(c=>Math.Abs(c.X)<=256&&Math.Abs(c.Y)<=256&&Math.Abs(c.Z)<=256),"cells");
   Require(p.Cells.Select(c=>(c.X,c.Z)).Distinct().Count()==p.Cells.Count,"duplicate XZ cell");
   Require(p.Cells.All(c=>(c.X-p.Cells[0].X)%16==0&&(c.Z-p.Cells[0].Z)%16==0),"cell lattice");
   Require(p.Edges.Count==p.Cells.Count-1,"template must be tree");var seen=new HashSet<(int,int)>();
   foreach(var edge in p.Edges){Require(edge.A>=0&&edge.A<p.Cells.Count&&edge.B>=0&&edge.B<p.Cells.Count&&edge.A!=edge.B,"edge reference");Require(seen.Add((Math.Min(edge.A,edge.B),Math.Max(edge.A,edge.B))),"duplicate edge");var a=p.Cells[edge.A];var b=p.Cells[edge.B];Require(Math.Abs(a.X-b.X)+Math.Abs(a.Z-b.Z)==16&&Math.Abs(a.Y-b.Y)<=8,"nonadjacent cells");}
   var visited=new HashSet<int>{0};bool more=true;while(more){more=false;foreach(var e in p.Edges){if(visited.Contains(e.A)&&visited.Add(e.B))more=true;if(visited.Contains(e.B)&&visited.Add(e.A))more=true;}}Require(visited.Count==p.Cells.Count,"disconnected template");
   Require(p.SocketCells.Keys.OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(p.Room.Sockets.Select(s=>s.Id).OrderBy(x=>x,StringComparer.Ordinal)),"socket mapping");
   foreach(var s in p.Room.Sockets){int i=p.SocketCells[s.Id];Require(i>=0&&i<p.Cells.Count,"socket cell index");var c=p.Cells[i];var n=LayoutGeometry.Normal(s.Facing);Require(s.Position.Equals(new GridPoint(c.X+n.X*8,c.Y,c.Z+n.Z*8)),"socket must be centred on template cell face");Require(!p.Cells.Any(other=>other.X==c.X+n.X*16&&other.Z==c.Z+n.Z*16),"socket faces internal cell");}
   foreach(var b in p.Room.Boxes)for(int x=b.Min.X;x<b.Max.X;x++)for(int z=b.Min.Z;z<b.Max.Z;z++)Require(p.Cells.Any(c=>x>=c.X-8&&x<c.X+8&&z>=c.Z-8&&z<c.Z+8),"occupancy outside matched footprint");
   if(p.Cells.Select(c=>c.Y).Distinct().Count()>1){Require(p.Room.Sockets.Count==2&&p.Cells.Max(c=>c.Y)-p.Cells.Min(c=>c.Y)==8,"transition needs two sockets and one floor rise");Require(Math.Abs(p.Room.Sockets[0].Position.Y-p.Room.Sockets[1].Position.Y)==8,"transition endpoints");}
  }
 }
}
