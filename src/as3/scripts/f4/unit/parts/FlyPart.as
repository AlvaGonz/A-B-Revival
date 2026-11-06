package f4.unit.parts
{
   import f4.unit.fire.FirePort;
   import f4.unit.fire.FirePorts;
   import f4.unit.move.MoveAs;
   import f4.unit.render.Render;
   import f4.unit.upgrade.FlyData;
   import flash.geom.Point;
   
   public class FlyPart
   {
      
      public var render:Render;
      
      public var firePorts:FirePorts;
      
      public var partMove:MoveAs;
      
      public var data:FlyData;
      
      public var particlePort:FirePort;
      
      public var main:Boolean;
      
      public var index:int;
      
      public var hitRectOrg:Point;
      
      public var hitpoints4:Array;
      
      public var hitpoints4OrgRad:Array;
      
      public var dis:Number;
      
      public var minx:Number;
      
      public var miny:Number;
      
      public var maxx:Number;
      
      public var maxy:Number;
      
      public function FlyPart()
      {
         super();
         this.partMove = new MoveAs(0,0);
         this.hitRectOrg = new Point();
      }
      
      public function initPartPoint(param1:Number, param2:Number) : void
      {
         this.partMove.initUdpoint(param1,param2);
      }
      
      public function make4() : void
      {
         this.dis = Math.sqrt(this.hitRectOrg.x / 2 * (this.hitRectOrg.x / 2) + this.hitRectOrg.y / 2 * (this.hitRectOrg.y / 2));
         this.hitpoints4OrgRad = [Math.atan2(-this.hitRectOrg.y / 2,-this.hitRectOrg.x / 2),Math.atan2(this.hitRectOrg.y / 2,-this.hitRectOrg.x / 2),Math.atan2(this.hitRectOrg.y / 2,this.hitRectOrg.x / 2),Math.atan2(-this.hitRectOrg.y / 2,this.hitRectOrg.x / 2)];
         this.hitpoints4 = [new Point(-this.hitRectOrg.x / 2,-this.hitRectOrg.y / 2),new Point(this.hitRectOrg.x / 2,-this.hitRectOrg.y / 2),new Point(this.hitRectOrg.x / 2,this.hitRectOrg.y / 2),new Point(-this.hitRectOrg.x / 2,this.hitRectOrg.y / 2)];
      }
      
      public function turn4(param1:Number) : void
      {
         var _loc4_:Point = null;
         var _loc2_:int = -1;
         var _loc3_:int = 4;
         this.minx = int.MAX_VALUE;
         this.miny = int.MAX_VALUE;
         this.maxx = int.MIN_VALUE;
         this.maxy = int.MIN_VALUE;
         while(++_loc2_ < _loc3_)
         {
            _loc4_ = this.hitpoints4[_loc2_];
            _loc4_.x = Math.cos(this.hitpoints4OrgRad[_loc2_] + param1 + this.partMove.radFace) * this.dis;
            _loc4_.y = Math.sin(this.hitpoints4OrgRad[_loc2_] + param1 + this.partMove.radFace) * this.dis;
            if(_loc4_.x < this.minx)
            {
               this.minx = _loc4_.x;
            }
            if(_loc4_.x > this.maxx)
            {
               this.maxx = _loc4_.x;
            }
            if(_loc4_.y < this.miny)
            {
               this.miny = _loc4_.y;
            }
            if(_loc4_.y > this.maxy)
            {
               this.maxy = _loc4_.y;
            }
         }
      }
   }
}

