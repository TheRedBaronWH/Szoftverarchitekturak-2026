using System;
using UnityEngine;

internal static class NavigationTests
{
    public static void Run()
    {
        int cases=0;
        foreach (Vector2 key in new[] {new Vector2(1,0),new Vector2(-1,0),new Vector2(0,1),new Vector2(0,-1)})
        {
            Require(ScreenNavigation.IsBetter(new Vector2(key.x*50,key.y*50),key,0.35f,float.PositiveInfinity,out _),"An open path in the pressed direction is selectable");
            Require(!ScreenNavigation.IsBetter(new Vector2(-key.x*50,-key.y*50),key,0.35f,float.PositiveInfinity,out _),"A key cannot select the opposite direction");
            Require(ScreenNavigation.IsBetter(new Vector2(key.x*20,key.y*20),key,1,10000,out _),"Collinear branches prefer the closer destination");
            Require(!ScreenNavigation.IsBetter(new Vector2(key.x*100,key.y*100),key,1,400,out _),"A distant collinear platform cannot displace the nearer choice");
            cases+=4;
        }
        for (int i=0;i<360;i++)
        {
            float angle=i*MathF.PI/180;var target=new Vector2(MathF.Cos(angle)*100,MathF.Sin(angle)*100);
            bool selectable=false;
            foreach (Vector2 key in new[] {new Vector2(1,0),new Vector2(-1,0),new Vector2(0,1),new Vector2(0,-1)})
                selectable |= ScreenNavigation.IsBetter(target,key,0.35f,float.PositiveInfinity,out _);
            Require(selectable,"Every screen-space direction is covered by at least one arrow");cases++;
        }
        Require(!ScreenNavigation.IsBetter(new Vector2(0,0),new Vector2(1,0),0.35f,float.PositiveInfinity,out _),"An overlapping projection has no false directional move");
        Console.WriteLine("Optional arrow navigation: "+(cases+1)+" checks passed.");
    }
    private static void Require(bool value,string message) { if(!value)throw new InvalidOperationException(message); }
}
