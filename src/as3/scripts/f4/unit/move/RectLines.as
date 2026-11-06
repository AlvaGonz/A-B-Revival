package f4.unit.move
{
   import flash.geom.Point;
   import flash.geom.Rectangle;
   import fullshit.tool.LineCP;
   
   public class RectLines
   {
      
      public var lines:Array;
      
      public function RectLines()
      {
         super();
         this.lines = [new LineCP(new Point(),new Point()),new LineCP(new Point(),new Point()),new LineCP(new Point(),new Point()),new LineCP(new Point(),new Point())];
      }
      
      public function updateRect(param1:Rectangle) : void
      {
         (this.lines[0] as LineCP).initXY(param1.x,param1.y,param1.x,param1.y + param1.height);
         (this.lines[1] as LineCP).initXY(param1.x,param1.y + param1.height,param1.x + param1.width,param1.y + param1.height);
         (this.lines[2] as LineCP).initXY(param1.x + param1.width,param1.y + param1.height,param1.x + param1.width,param1.y);
         (this.lines[3] as LineCP).initXY(param1.x + param1.width,param1.y,param1.x,param1.y);
      }
   }
}

