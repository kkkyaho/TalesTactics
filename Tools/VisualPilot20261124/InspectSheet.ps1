param([Parameter(Mandatory=$true)][string]$Image,[Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$pilotRefs=@([Drawing.Bitmap].Assembly.Location)+@(Get-ChildItem -LiteralPath (Join-Path $PSHOME 'ref') -Filter '*.dll' | ForEach-Object FullName)+@([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -like 'System.Private.Windows*' } | ForEach-Object Location)
Add-Type -ReferencedAssemblies $pilotRefs -TypeDefinition @'
using System;
using System.Drawing;
using System.Collections.Generic;
public static class VisualPilotRaster {
 public class Cell { public int row,column,x,y,width,height,opaque,edgeAlpha,top,bottom,bootLeft,bootRight; public float pivotX,pivotY; }
 public class Result { public int width,height,transparent,solid; public List<Cell> cells=new List<Cell>(); }
 public static Result Scan(string path) {
  using(var b=new Bitmap(path)){
   var result=new Result{width=b.Width,height=b.Height};
   for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){var p=b.GetPixel(x,y);if(p.A==0)result.transparent++;if(p.A>=128)result.solid++;}
   for(int row=0;row<4;row++)for(int col=0;col<4;col++){
    int x0=col*b.Width/4,x1=(col+1)*b.Width/4,y0=row*b.Height/4,y1=(row+1)*b.Height/4;
    var c=new Cell{row=row,column=col,x=x0,y=y0,width=x1-x0,height=y1-y0,top=y1,bottom=y0,bootLeft=x1,bootRight=x0};
    for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++){
     var p=b.GetPixel(x,y);
     if(x==x0||x==x1-1||y==y0||y==y1-1)c.edgeAlpha=Math.Max(c.edgeAlpha,p.A);
     if(p.A<128)continue;c.opaque++;
     int max=Math.Max(p.R,Math.Max(p.G,p.B)),min=Math.Min(p.R,Math.Min(p.G,p.B));
     if(max-min<75 && x>x0+c.width*.18f && x<x0+c.width*.8f){c.top=Math.Min(c.top,y);c.bottom=Math.Max(c.bottom,y);}
    }
    for(int y=Math.Max(y0,c.bottom-10);y<=c.bottom;y++)for(int x=x0;x<x1;x++){
     var p=b.GetPixel(x,y);int max=Math.Max(p.R,Math.Max(p.G,p.B)),min=Math.Min(p.R,Math.Min(p.G,p.B));
     if(p.A>200 && max-min<60 && max<140){c.bootLeft=Math.Min(c.bootLeft,x);c.bootRight=Math.Max(c.bootRight,x);}
    }
    c.pivotX=((c.bootLeft+c.bootRight)*.5f-x0)/c.width;
    c.pivotY=(y1-c.bottom-1f)/c.height;
    result.cells.Add(c);
   }
   return result;
  }
 }
}
'@
$pilotResult=[VisualPilotRaster]::Scan((Resolve-Path -LiteralPath $Image).Path)
$pilotRecord=[ordered]@{source=(Resolve-Path -LiteralPath $Image).Path;sha256=(Get-FileHash -LiteralPath $Image -Algorithm SHA256).Hash;coordinates='Cell y/top/bottom use top-left pixels; pivots are normalized bottom-left. Boot anchors are a draft optical measurement, not semantic detection.';measurement=$pilotResult}
[IO.File]::WriteAllText($Output,($pilotRecord|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
$pilotResult.cells | Select-Object row,column,edgeAlpha,top,bottom,bootLeft,bootRight,pivotX,pivotY | Format-Table
