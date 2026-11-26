using System;
using System.Collections.Generic;
using UnityEngine;

internal static class Program
{
    private static int checks;
    private static void Main()
    {
        string[] captions = { "1","2","3","4","5","6","7","8","9","0","−","=","Q","E","T","Y","U","I","O","P" };
        for (int i=0;i<20;i++) Check(PlatformShortcuts.Caption("P"+(i+1))==captions[i],"Every platform has its actual key caption");
        Check(new HashSet<UnityEngine.InputSystem.Key>(PlatformShortcuts.Keys).Count==20,"All twenty shortcuts are distinct");
        foreach (bool diamond in new[] { false,true })
        foreach (bool tilted in new[] { false,true })
        {
            var normal = tilted ? new Vector3(0.2f,1,0.1f).normalized : Vector3.up;
            var polygon = diamond ? new[] {new Vector3(-1,0,0),new Vector3(0,0,-1),new Vector3(1,0,0),new Vector3(0,0,1)}
                : new[] {new Vector3(-1,0,-1),new Vector3(1,0,-1),new Vector3(1,0,1),new Vector3(-1,0,1)};
            for (int i=0;i<polygon.Length;i++) polygon[i].y=-(normal.x*polygon[i].x+normal.z*polygon[i].z)/normal.y;
            var duplicated = new List<Vector3>(polygon); duplicated.AddRange(polygon); // Mesh triangle vertices repeat.
            var face = new WalkableFace(); face.Set(duplicated,Vector3.zero,normal);
            for (int angle=0;angle<360;angle++)
            {
                float a=angle*MathF.PI/180;
                var direction=new Vector3(MathF.Cos(a),0,MathF.Sin(a));
                Vector3 side=Vector3.Cross(Vector3.up,direction).normalized;
                Vector3 dock=face.Dock(direction*10,WalkwayPath.Width,side);
                Check(dock.magnitude>0.4f,"Dock reaches the actual boundary instead of staying at the center");
                Check(MathF.Abs(Vector3.Dot(dock,normal))<0.0001f,"Dock lies on the animated face plane");
                foreach (float sign in new[] {-1f,1f})
                {
                    Vector3 corner=dock+side*(sign*WalkwayPath.Width/2);
                    Check(diamond ? MathF.Abs(corner.x)+MathF.Abs(corner.z)<=1.0001f
                        : MathF.Abs(corner.x)<=1.0001f&&MathF.Abs(corner.z)<=1.0001f,"Both walkway corners remain inside the landing");
                }
            }
        }
        Vector3 view=new Vector3(0,-1,1).normalized,right=Vector3.right,up=Vector3.Cross(view,right);
        foreach (float rise in new[] {-2f,0,0.02f,0.5f,3})
        foreach (bool perspective in new[] {false,true})
        {
            var first=new Vector3(-2,0,0); var last=new Vector3(2,rise,1);
            var path=new WalkwayPath(first,last,first+right*0.6f,last-right*0.6f,view,right,perspective,1);
            Check(Vector3.Distance(path.Sample(0),first)<0.00001f,"Path starts at the current foot pose");
            Check(Vector3.Distance(path.Sample(1),last)<0.00001f,"Path ends at the destination foot pose, including descending stairs");
            for (int i=0;i<=1000;i++)
            {
                Vector3 point=path.Sample(i/1000f);
                Check(float.IsFinite(point.x)&&float.IsFinite(point.y)&&float.IsFinite(point.z),"No invalid geometry along the walking path");
            }
            if (!perspective) continue;
            Vector3 delta=path.SeamFirst-path.SeamLast;
            Check(delta.magnitude>1.2f,"The seam genuinely separates its platforms in 3D");
            Check(MathF.Abs(Vector3.Dot(delta,right))<0.00001f&&MathF.Abs(Vector3.Dot(delta,up))<0.00001f,
                "The separated halves share EXACTLY one screen-space edge");
            Vector3 before=path.Sample(path.SeamPosition-0.000001f),after=path.Sample(path.SeamPosition+0.000001f);
            Check(MathF.Abs(Vector3.Dot(after-before,right))<0.0001f&&MathF.Abs(Vector3.Dot(after-before,up))<0.0001f,
                "The avatar crosses the depth seam without a screen-space teleport");
            var closed=new WalkwayPath(first,last,first+right*0.6f,last-right*0.6f,view,right,true,0);
            Check(MathF.Abs(Vector3.Dot(closed.SeamFirst-closed.SeamLast,right))>0.4f,"The inactive perspective shows a visible gap");
            var halfway=new WalkwayPath(first,last,first+right*0.6f,last-right*0.6f,view,right,true,0.5f);
            Check(MathF.Abs(MathF.Abs(Vector3.Dot(halfway.SeamFirst-halfway.SeamLast,right))-0.22f)<0.00001f,
                "The visible gap closes smoothly with the team's cube animation");
        }
        Console.WriteLine("Presentation geometry: "+checks+" checks passed (mathematics only; Unity rendering requires Play Mode).");
    }
    private static void Check(bool value,string message)
    {
        checks++;
        if (!value) throw new InvalidOperationException(message);
    }
}
