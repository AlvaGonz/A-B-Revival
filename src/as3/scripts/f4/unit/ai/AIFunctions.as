package f4.unit.ai
{
   import f4.unit.move.MoveAs;
   import f4.unit.upgrade.FlyData;
   import flash.geom.Point;
   import fullshit.tool.MathTool;
   import fullshit.tool.StaticShit;
   
   public class AIFunctions
   {
      
      public var rotLimit:Number;
      
      public var sinMv:SinMove;
      
      public var drad:Number = 0;
      
      private var _mathTool:MathTool = StaticShit.MATHTOOL;
      
      public function AIFunctions(param1:Number = 0.17453292519943295)
      {
         super();
         this.rotLimit = param1 * 0.6;
      }
      
      public function updateData(param1:FlyData) : void
      {
         this.rotLimit = (param1.turn.x / param1.mass) * 0.6;
      }
      
      public function closeToRadSR(param1:MoveAs, param2:Number, param3:int) : void
      {
         if(param3 == 0)
         {
            param2 += Math.PI;
         }
         else if(param3 == 2)
         {
            param2 += Math.PI / 2;
         }
         else if(param3 == 3)
         {
            param2 -= Math.PI / 2;
         }
         param2 = this._mathTool.formatRad(param2 - param1.radFace);
         if(param2 < 0)
         {
            if(-param2 < this.rotLimit)
            {
               param1.radFace += param2;
            }
            else
            {
               param1.radFace += -this.rotLimit;
            }
         }
         else if(param2 < this.rotLimit)
         {
            param1.radFace += param2;
         }
         else
         {
            param1.radFace += this.rotLimit;
         }
         param1.radFace = this._mathTool.formatRad(param1.radFace);
         this.drad = this._mathTool.formatRad(param1.radFace - param2);
         this.drad = this.drad < 0 ? -this.drad : this.drad;
      }
      
      public function closeToRadSimple(param1:MoveAs, param2:Point, param3:int) : void
      {
         var _loc4_:Number = Math.atan2(param2.y - param1.point.y,param2.x - param1.point.x);
         if(param3 == 0)
         {
            _loc4_ += Math.PI;
         }
         else if(param3 == 2)
         {
            _loc4_ += Math.PI / 2;
         }
         else if(param3 == 3)
         {
            _loc4_ -= Math.PI / 2;
         }
         _loc4_ = this._mathTool.formatRad(_loc4_ - param1.radFace);
         if(_loc4_ < 0)
         {
            if(-_loc4_ < this.rotLimit)
            {
               param1.radFace += _loc4_;
            }
            else
            {
               param1.radFace += -this.rotLimit;
            }
         }
         else if(_loc4_ < this.rotLimit)
         {
            param1.radFace += _loc4_;
         }
         else
         {
            param1.radFace += this.rotLimit;
         }
         param1.radFace = this._mathTool.formatRad(param1.radFace);
         this.drad = this._mathTool.formatRad(param1.radFace - _loc4_);
         this.drad = this.drad < 0 ? -this.drad : this.drad;
      }
      
      public function closeToRad(param1:MoveAs, param2:Point, param3:int) : void
      {
         var _loc4_:Number = Math.atan2(param2.y - param1.point.y,param2.x - param1.point.x);
         if(param3 == 0)
         {
            _loc4_ += Math.PI;
         }
         else if(param3 == 2)
         {
            _loc4_ += Math.PI / 2;
         }
         else if(param3 == 3)
         {
            _loc4_ -= Math.PI / 2;
         }
         _loc4_ = this._mathTool.formatRad(_loc4_ - param1.radFace);
         if(_loc4_ < 0)
         {
            if(-_loc4_ < this.rotLimit)
            {
               param1.radFace += _loc4_;
            }
            else
            {
               param1.radFace += -this.rotLimit;
            }
         }
         else if(_loc4_ < this.rotLimit)
         {
            param1.radFace += _loc4_;
         }
         else
         {
            param1.radFace += this.rotLimit;
         }
         param1.radFace = this._mathTool.formatRad(param1.radFace);
         if(param1.acc.x > 0)
         {
            param1.acc.y = param1.radFace;
            param1.r2d(param1.acc,param1.accp);
         }
         if(this.sinMv)
         {
            this.sinMv.runSin(param1.spd.y);
            param1.dxy.x += this.sinMv.x;
            param1.dxy.y += this.sinMv.y;
         }
         this.drad = this._mathTool.formatRad(param1.radFace - _loc4_);
         this.drad = this.drad < 0 ? -this.drad : this.drad;
      }
   }
}

