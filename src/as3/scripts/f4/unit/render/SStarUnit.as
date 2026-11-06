package f4.unit.render
{
   import f4.gfx.bmpdmc.BMPD2D;
   import flash.geom.Point;
   
   public class SStarUnit
   {
      
      public var bmpd2d:BMPD2D;
      
      public var point:Point;
      
      public var renderFrame:Boolean;
      
      public function SStarUnit(param1:BMPD2D, param2:Point)
      {
         super();
         this.bmpd2d = param1;
         this.point = param2;
         this.renderFrame = true;
      }
   }
}

