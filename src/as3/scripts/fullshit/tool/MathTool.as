package fullshit.tool
{
   public final class MathTool
   {
      
      public function MathTool()
      {
         super();
      }
      
      public function formatAng(param1:Number) : Number
      {
         while(param1 > 180)
         {
            param1 -= 360;
         }
         while(param1 <= -180)
         {
            param1 += 360;
         }
         return param1;
      }
      
      public function formatAng90(param1:Number) : Number
      {
         if(param1 > 90)
         {
            param1 -= 180;
         }
         else if(param1 <= -90)
         {
            param1 += 180;
         }
         return param1;
      }
      
      public function formatRad(param1:Number) : Number
      {
         while(param1 > Math.PI)
         {
            param1 -= Math.PI * 2;
         }
         while(param1 <= -Math.PI)
         {
            param1 += Math.PI * 2;
         }
         return param1;
      }
      
      public function flag(param1:Number) : int
      {
         if(param1 > 0)
         {
            return 1;
         }
         if(param1 < 0)
         {
            return -1;
         }
         return 0;
      }
      
      public function a2toa1(param1:int, param2:int, param3:int) : int
      {
         return param2 * param3 + param1;
      }
      
      public function distance(param1:Number, param2:Number, param3:Number, param4:Number) : Number
      {
         return Math.sqrt((param1 - param3) * (param1 - param3) + (param2 - param4) * (param2 - param4));
      }
      
      public function numberIn(param1:Number, param2:Number) : Number
      {
         var _loc3_:Number = NaN;
         if(Math.abs(param1) <= Math.abs(param2))
         {
            return param1;
         }
         _loc3_ = Math.abs(param2);
         if(param1 > 0)
         {
            return _loc3_;
         }
         return -_loc3_;
      }
      
      public function h2d(param1:uint, param2:uint) : uint
      {
         switch(param2)
         {
            case 1:
               return param1 >> 16 & 0xFF;
            case 2:
               return param1 >> 8 & 0xFF;
            case 3:
               return param1 & 0xFF;
            case 4:
               return param1 >> 24 & 0xFF;
            default:
               return 0;
         }
      }
      
      public function d2h(param1:uint, param2:uint, param3:uint, param4:uint) : uint
      {
         return param4 << 24 | param1 << 16 | param2 << 8 | param3;
      }
      
      public function isInNum(param1:Number, param2:Number) : Boolean
      {
         var _loc3_:Number = Math.random() * param2;
         if(_loc3_ <= param1)
         {
            return true;
         }
         return false;
      }
      
      public function lowSin(param1:Number) : Number
      {
         var _loc2_:Number = NaN;
         if(param1 < -3.14159265)
         {
            param1 += 6.28318531;
         }
         else if(param1 > 3.14159265)
         {
            param1 -= 6.28318531;
         }
         if(param1 < 0)
         {
            _loc2_ = 1.27323954 * param1 + 0.405284735 * param1 * param1;
         }
         else
         {
            _loc2_ = 1.27323954 * param1 - 0.405284735 * param1 * param1;
         }
         return _loc2_;
      }
      
      public function lowCos(param1:Number) : Number
      {
         var _loc2_:Number = NaN;
         if(param1 < -3.14159265)
         {
            param1 += 6.28318531;
         }
         else if(param1 > 3.14159265)
         {
            param1 -= 6.28318531;
         }
         param1 += 1.57079632;
         if(param1 > 3.14159265)
         {
            param1 -= 6.28318531;
         }
         if(param1 < 0)
         {
            _loc2_ = 1.27323954 * param1 + 0.405284735 * param1 * param1;
         }
         else
         {
            _loc2_ = 1.27323954 * param1 - 0.405284735 * param1 * param1;
         }
         return _loc2_;
      }
      
      public function hiSin(param1:Number) : Number
      {
         var _loc2_:Number = NaN;
         if(param1 < -3.14159265)
         {
            param1 += 6.28318531;
         }
         else if(param1 > 3.14159265)
         {
            param1 -= 6.28318531;
         }
         if(param1 < 0)
         {
            _loc2_ = 1.27323954 * param1 + 0.405284735 * param1 * param1;
            if(_loc2_ < 0)
            {
               _loc2_ = 0.225 * (_loc2_ * -_loc2_ - _loc2_) + _loc2_;
            }
            else
            {
               _loc2_ = 0.225 * (_loc2_ * _loc2_ - _loc2_) + _loc2_;
            }
         }
         else
         {
            _loc2_ = 1.27323954 * param1 - 0.405284735 * param1 * param1;
            if(_loc2_ < 0)
            {
               _loc2_ = 0.225 * (_loc2_ * -_loc2_ - _loc2_) + _loc2_;
            }
            else
            {
               _loc2_ = 0.225 * (_loc2_ * _loc2_ - _loc2_) + _loc2_;
            }
         }
         return _loc2_;
      }
      
      public function hiCos(param1:Number) : Number
      {
         var _loc2_:Number = NaN;
         if(param1 < -3.14159265)
         {
            param1 += 6.28318531;
         }
         else if(param1 > 3.14159265)
         {
            param1 -= 6.28318531;
         }
         param1 += 1.57079632;
         if(param1 > 3.14159265)
         {
            param1 -= 6.28318531;
         }
         if(param1 < 0)
         {
            _loc2_ = 1.27323954 * param1 + 0.405284735 * param1 * param1;
            if(_loc2_ < 0)
            {
               _loc2_ = 0.225 * (_loc2_ * -_loc2_ - _loc2_) + _loc2_;
            }
            else
            {
               _loc2_ = 0.225 * (_loc2_ * _loc2_ - _loc2_) + _loc2_;
            }
         }
         else
         {
            _loc2_ = 1.27323954 * param1 - 0.405284735 * param1 * param1;
            if(_loc2_ < 0)
            {
               _loc2_ = 0.225 * (_loc2_ * -_loc2_ - _loc2_) + _loc2_;
            }
            else
            {
               _loc2_ = 0.225 * (_loc2_ * _loc2_ - _loc2_) + _loc2_;
            }
         }
         return _loc2_;
      }
   }
}

