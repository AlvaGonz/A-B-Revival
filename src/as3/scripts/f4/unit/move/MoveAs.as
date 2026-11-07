package f4.unit.move
{
   import f4.data.InitData;
   import flash.geom.Point;
   import flash.geom.Rectangle;
   
   public class MoveAs
   {
      public static const SIM_SCALE:Number = 0.6;
      
      public var point:Point;
      
      public var lastPoint:Point;
      
      public var spd:Point;
      
      public var acc:Point;
      
      public var spdp:Point;
      
      public var accp:Point;
      
      public var radFire:Number;
      
      public var radFace:Number;
      
      public var autof:Number;
      
      public var dxy:Point;
      
      public var lastRect:Rectangle;
      
      public var hitRect:Rectangle;
      
      public var udpoint:Point;
      
      private var _pidface:Number;
      
      public function MoveAs(param1:Number, param2:Number, param3:Number = 0, param4:Number = 0, param5:Number = 0, param6:Number = 0, param7:Number = 1)
      {
         super();
         this._pidface = InitData.RENDERDATA.PIDFACE;
         this.point = new Point(param1,param2);
         this.lastPoint = new Point(param1,param2);
         this.dxy = new Point();
         this.spd = new Point(param3,param4);
         this.acc = new Point(param5,param6);
         this.spdp = new Point(this.spd.x * Math.cos(this.spd.y),this.spd.x * Math.sin(this.spd.y));
         this.accp = new Point(this.acc.x * Math.cos(this.acc.y),this.acc.x * Math.sin(this.acc.y));
         this.autof = param7;
         this.radFire = this.radFace = 0;
         this.udpoint = new Point();
         this.hitRect = new Rectangle();
         this.lastRect = new Rectangle();
      }
      
      public function initSpd(param1:Number = 0, param2:Number = 0, param3:Number = 0, param4:Number = 0, param5:Number = 1) : void
      {
         this.spd.x = param1;
         this.spd.y = param2;
         this.acc.x = param3;
         this.acc.y = param4;
         this.r2d(this.spd,this.spdp);
         this.r2d(this.acc,this.accp);
         this.autof = param5;
      }
      
      public function d2r(param1:Point, param2:Point) : void
      {
         param2.x = Math.sqrt(param1.x * param1.x + param1.y * param1.y);
         param2.y = Math.atan2(param1.y,param1.x);
      }
      
      public function r2d(param1:Point, param2:Point) : void
      {
         param2.x = param1.x * Math.cos(param1.y);
         param2.y = param1.x * Math.sin(param1.y);
      }
      
      public function updateSpd() : void
      {
         this.dxy.x = this.dxy.y = 0;
         this.spdp.x += this.accp.x * 0.6;
         this.spdp.y += this.accp.y * 0.6;
         if(this.autof != 1)
         {
            this.spdp.x *= Math.pow(this.autof, 0.6);
            this.spdp.y *= Math.pow(this.autof, 0.6);
         }
      }
      
      public function runMove() : void
      {
         this.lastRect.x = this.hitRect.x;
         this.lastRect.y = this.hitRect.y;
         this.lastPoint.x = this.point.x;
         this.lastPoint.y = this.point.y;
         if(!(this.spdp.x == 0 && this.spdp.y == 0 && this.accp.x == 0 && this.accp.y == 0))
         {
            this.point.x += (this.spdp.x + this.dxy.x) * 0.6;
            this.point.y += (this.spdp.y + this.dxy.y) * 0.6;
            this.d2r(this.spdp,this.spd);
         }
         this.hitRect.x = this.point.x - this.hitRect.width / 2;
         this.hitRect.y = this.point.y - this.hitRect.height / 2;
      }
      
      public function updateFace() : void
      {
         if(this.dxy.x == 0 && this.dxy.y == 0)
         {
            this.radFace = this.spd.y;
            this.radFire = this.spd.y;
         }
         else
         {
            this.radFace = this.radFire = Math.atan2(this.spdp.y + this.dxy.y,this.spdp.x + this.dxy.x);
         }
      }
      
      public function initUdpoint(param1:Number, param2:Number) : void
      {
         this.udpoint.x = Math.sqrt(param1 * param1 + param2 * param2);
         this.udpoint.y = Math.atan2(param2,param1);
      }
      
      public function updateUdpoint(param1:Point, param2:Number) : void
      {
         var _loc3_:Number = Math.round(param2 / this._pidface) * this._pidface;
         this.point.x = param1.x + Math.cos(_loc3_ + this.udpoint.y) * this.udpoint.x + this.dxy.x;
         this.point.y = param1.y + Math.sin(_loc3_ + this.udpoint.y) * this.udpoint.x + this.dxy.y;
         this.dxy.y = this.dxy.x = 0;
      }
      
      public function makeHitRectFromWH(param1:Number, param2:Number) : void
      {
         this.hitRect.width = param1;
         this.hitRect.height = param2;
         this.lastRect.width = this.hitRect.width;
         this.lastRect.height = this.hitRect.height;
         this.hitRect.x = this.point.x - this.hitRect.width / 2;
         this.hitRect.y = this.point.y - this.hitRect.height / 2;
      }
   }
}

