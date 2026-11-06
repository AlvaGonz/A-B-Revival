package f4.unit.render
{
   import f4.data.InitData;
   import f4.unit.move.MoveAs;
   import flash.geom.Point;
   import flash.geom.Rectangle;
   
   public class Cam
   {
      
      public var rect:Rectangle;
      
      public var wh:Point;
      
      public var aim:Point;
      
      public var mv:MoveAs;
      
      public var spd:Number;
      
      public var last:Point;
      
      public var crossPoint:Point;
      
      public var screenMoveNum:Number = 3.5;
      
      public var shake:Number = 0;
      
      public function Cam(param1:Rectangle, param2:Point, param3:Point, param4:Number = 0.75)
      {
         super();
         this.rect = param1;
         this.wh = param2;
         this.aim = param3;
         this.spd = param4;
         this.last = new Point(this.rect.x,this.rect.y);
         this.crossPoint = new Point(this.aim.x + this.rect.x,this.aim.y + this.rect.y);
      }
      
      public function runCam() : void
      {
         var _loc1_:Number = NaN;
         var _loc2_:Number = NaN;
         this.last.x = this.rect.x;
         this.last.y = this.rect.y;
         this.crossPoint.x = this.aim.x + this.rect.x;
         this.crossPoint.y = this.aim.y + this.rect.y;
         var _loc3_:Number = InitData.RENDERDATA.W / 2;
         var _loc4_:Number = InitData.RENDERDATA.H / 2;
         var _loc5_:Number = _loc3_ - this.aim.x < 0 ? this.aim.x - _loc3_ : _loc3_ - this.aim.x;
         var _loc6_:Number = _loc4_ - this.aim.y < 0 ? this.aim.y - _loc4_ : _loc4_ - this.aim.y;
         _loc1_ = this.mv.point.x + (this.crossPoint.x - this.mv.point.x) / ((_loc3_ - _loc5_) / (_loc3_ / 2) + this.screenMoveNum);
         _loc2_ = this.mv.point.y + (this.crossPoint.y - this.mv.point.y) / ((_loc4_ - _loc6_) / (_loc4_ / 2) + this.screenMoveNum);
         this.rect.x += (_loc1_ - (this.rect.x + this.rect.width / 2)) * this.spd;
         this.rect.y += (_loc2_ - (this.rect.y + this.rect.height / 2)) * this.spd;
         if(this.shake > 0)
         {
            this.rect.x += Math.random() * this.shake * 2 - this.shake;
            this.rect.y += Math.random() * this.shake * 2 - this.shake;
            this.shake *= 0.75;
            if(this.shake < 0.1)
            {
               this.shake = 0;
            }
         }
         if(this.rect.x < 0)
         {
            this.rect.x = 0;
         }
         else if(this.rect.x + this.rect.width > this.wh.x)
         {
            this.rect.x = this.wh.x - this.rect.width;
         }
         if(this.rect.y < 0)
         {
            this.rect.y = 0;
         }
         else if(this.rect.y + this.rect.height > this.wh.y)
         {
            this.rect.y = this.wh.y - this.rect.height;
         }
      }
      
      public function addShake(param1:Number, param2:Number, param3:Number) : void
      {
         if(!(param1 < this.rect.x || param1 > this.rect.x + this.rect.width || param2 < this.rect.y || param2 > this.rect.y + this.rect.height))
         {
            this.shake += param3;
         }
      }
   }
}

