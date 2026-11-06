package fullshit.tool
{
   import flash.display.BitmapData;
   import flash.geom.Rectangle;
   
   public final class BresenhamLine
   {
      
      private var _rect:Rectangle = new Rectangle();
      
      public function BresenhamLine()
      {
         super();
      }
      
      public function lineRect32(param1:Number, param2:Number, param3:Number, param4:Number, param5:int, param6:BitmapData, param7:uint) : void
      {
         var _loc9_:Number = NaN;
         var _loc14_:Number = NaN;
         if(param5 < 2)
         {
            param5 = 2;
         }
         this._rect.width = this._rect.height = param5;
         var _loc8_:Number = param5 / 2;
         var _loc10_:Boolean = (param4 - param3 < 0 ? param3 - param4 : param4 - param3) > (param2 - param1 < 0 ? param1 - param2 : param2 - param1);
         if(_loc10_)
         {
            _loc9_ = param1;
            param1 = param3;
            param3 = _loc9_;
            _loc9_ = param2;
            param2 = param4;
            param4 = _loc9_;
         }
         if(param1 > param2)
         {
            _loc9_ = param1;
            param1 = param2;
            param2 = _loc9_;
            _loc9_ = param3;
            param3 = param4;
            param4 = _loc9_;
         }
         var _loc11_:Number = param2 - param1;
         var _loc12_:Number = param4 - param3 < 0 ? param3 - param4 : param4 - param3;
         var _loc13_:Number = _loc11_ / 2;
         var _loc15_:Number = param3;
         if(param3 < param4)
         {
            _loc14_ = _loc8_;
         }
         else
         {
            _loc14_ = -_loc8_;
         }
         var _loc16_:Number = param1;
         while(_loc16_ <= param2)
         {
            if(_loc10_)
            {
               this._rect.x = _loc15_ - _loc8_;
               this._rect.y = _loc16_ - _loc8_;
               param6.fillRect(this._rect,255 << 24 | param7);
            }
            else
            {
               this._rect.x = _loc16_ - _loc8_;
               this._rect.y = _loc15_ - _loc8_;
               param6.fillRect(this._rect,255 << 24 | param7);
            }
            _loc13_ -= _loc12_;
            if(_loc13_ < 0)
            {
               _loc15_ += _loc14_;
               _loc13_ += _loc11_;
            }
            _loc16_ += _loc8_;
         }
      }
      
      public function lineRect(param1:Number, param2:Number, param3:Number, param4:Number, param5:int, param6:BitmapData, param7:uint) : void
      {
         var _loc9_:Number = NaN;
         var _loc14_:Number = NaN;
         if(param5 < 2)
         {
            param5 = 2;
         }
         this._rect.width = this._rect.height = param5;
         var _loc8_:Number = param5 / 2;
         var _loc10_:Boolean = (param4 - param3 < 0 ? param3 - param4 : param4 - param3) > (param2 - param1 < 0 ? param1 - param2 : param2 - param1);
         if(_loc10_)
         {
            _loc9_ = param1;
            param1 = param3;
            param3 = _loc9_;
            _loc9_ = param2;
            param2 = param4;
            param4 = _loc9_;
         }
         if(param1 > param2)
         {
            _loc9_ = param1;
            param1 = param2;
            param2 = _loc9_;
            _loc9_ = param3;
            param3 = param4;
            param4 = _loc9_;
         }
         var _loc11_:Number = param2 - param1;
         var _loc12_:Number = param4 - param3 < 0 ? param3 - param4 : param4 - param3;
         var _loc13_:Number = _loc11_ / 2;
         var _loc15_:Number = param3;
         if(param3 < param4)
         {
            _loc14_ = _loc8_;
         }
         else
         {
            _loc14_ = -_loc8_;
         }
         var _loc16_:Number = param1;
         while(_loc16_ <= param2)
         {
            if(_loc10_)
            {
               this._rect.x = _loc15_ - _loc8_;
               this._rect.y = _loc16_ - _loc8_;
               param6.fillRect(this._rect,param7);
            }
            else
            {
               this._rect.x = _loc16_ - _loc8_;
               this._rect.y = _loc15_ - _loc8_;
               param6.fillRect(this._rect,param7);
            }
            _loc13_ -= _loc12_;
            if(_loc13_ < 0)
            {
               _loc15_ += _loc14_;
               _loc13_ += _loc11_;
            }
            _loc16_ += _loc8_;
         }
      }
      
      public function line32(param1:int, param2:int, param3:int, param4:int, param5:BitmapData, param6:uint) : void
      {
         var _loc7_:int = 0;
         var _loc12_:int = 0;
         var _loc8_:Boolean = Math.abs(param4 - param3) > Math.abs(param2 - param1);
         if(_loc8_)
         {
            _loc7_ = param1;
            param1 = param3;
            param3 = _loc7_;
            _loc7_ = param2;
            param2 = param4;
            param4 = _loc7_;
         }
         if(param1 > param2)
         {
            _loc7_ = param1;
            param1 = param2;
            param2 = _loc7_;
            _loc7_ = param3;
            param3 = param4;
            param4 = _loc7_;
         }
         var _loc9_:int = param2 - param1;
         var _loc10_:int = Math.abs(param4 - param3);
         var _loc11_:int = _loc9_ >> 1;
         var _loc13_:int = param3;
         if(param3 < param4)
         {
            _loc12_ = 1;
         }
         else
         {
            _loc12_ = -1;
         }
         var _loc14_:int = param1;
         while(_loc14_ <= param2)
         {
            if(_loc8_)
            {
               param5.setPixel32(_loc13_,_loc14_,255 << 24 | param6);
            }
            else
            {
               param5.setPixel32(_loc14_,_loc13_,255 << 24 | param6);
            }
            _loc11_ -= _loc10_;
            if(_loc11_ < 0)
            {
               _loc13_ += _loc12_;
               _loc11_ += _loc9_;
            }
            _loc14_++;
         }
      }
      
      public function line32N24(param1:int, param2:int, param3:int, param4:int, param5:BitmapData, param6:uint) : void
      {
         var _loc7_:int = 0;
         var _loc12_:int = 0;
         var _loc8_:Boolean = Math.abs(param4 - param3) > Math.abs(param2 - param1);
         if(_loc8_)
         {
            _loc7_ = param1;
            param1 = param3;
            param3 = _loc7_;
            _loc7_ = param2;
            param2 = param4;
            param4 = _loc7_;
         }
         if(param1 > param2)
         {
            _loc7_ = param1;
            param1 = param2;
            param2 = _loc7_;
            _loc7_ = param3;
            param3 = param4;
            param4 = _loc7_;
         }
         var _loc9_:int = param2 - param1;
         var _loc10_:int = Math.abs(param4 - param3);
         var _loc11_:int = _loc9_ >> 1;
         var _loc13_:int = param3;
         if(param3 < param4)
         {
            _loc12_ = 1;
         }
         else
         {
            _loc12_ = -1;
         }
         var _loc14_:int = param1;
         while(_loc14_ <= param2)
         {
            if(_loc8_)
            {
               param5.setPixel32(_loc13_,_loc14_,param6);
            }
            else
            {
               param5.setPixel32(_loc14_,_loc13_,param6);
            }
            _loc11_ -= _loc10_;
            if(_loc11_ < 0)
            {
               _loc13_ += _loc12_;
               _loc11_ += _loc9_;
            }
            _loc14_++;
         }
      }
      
      public function line(param1:int, param2:int, param3:int, param4:int, param5:BitmapData, param6:uint) : void
      {
         var _loc7_:int = 0;
         var _loc12_:int = 0;
         var _loc8_:Boolean = Math.abs(param4 - param3) > Math.abs(param2 - param1);
         if(_loc8_)
         {
            _loc7_ = param1;
            param1 = param3;
            param3 = _loc7_;
            _loc7_ = param2;
            param2 = param4;
            param4 = _loc7_;
         }
         if(param1 > param2)
         {
            _loc7_ = param1;
            param1 = param2;
            param2 = _loc7_;
            _loc7_ = param3;
            param3 = param4;
            param4 = _loc7_;
         }
         var _loc9_:int = param2 - param1;
         var _loc10_:int = Math.abs(param4 - param3);
         var _loc11_:int = _loc9_ >> 1;
         var _loc13_:int = param3;
         if(param3 < param4)
         {
            _loc12_ = 1;
         }
         else
         {
            _loc12_ = -1;
         }
         var _loc14_:int = param1;
         while(_loc14_ <= param2)
         {
            if(_loc8_)
            {
               param5.setPixel(_loc13_,_loc14_,param6);
            }
            else
            {
               param5.setPixel(_loc14_,_loc13_,param6);
            }
            _loc11_ -= _loc10_;
            if(_loc11_ < 0)
            {
               _loc13_ += _loc12_;
               _loc11_ += _loc9_;
            }
            _loc14_++;
         }
      }
      
      public function drawCircle(param1:int, param2:int, param3:int, param4:uint, param5:BitmapData, param6:uint) : void
      {
         if(param4 < 1)
         {
            param4 = 1;
         }
         this._rect.width = this._rect.height = param4;
         if(param3 < 0)
         {
            param3 = -param3;
         }
         var _loc7_:int = -param3;
         var _loc8_:int = param3;
         var _loc9_:int = 0;
         while(_loc8_ >= _loc9_)
         {
            this._rect.x = param1 + _loc8_ - (param4 >> 1);
            this._rect.y = param2 + _loc9_ - (param4 >> 1);
            param5.fillRect(this._rect,param6);
            if(_loc8_ != 0)
            {
               this._rect.x = param1 - _loc8_ - (param4 >> 1);
               this._rect.y = param2 + _loc9_ - (param4 >> 1);
               param5.fillRect(this._rect,param6);
            }
            if(_loc9_ != 0)
            {
               this._rect.x = param1 + _loc8_ - (param4 >> 1);
               this._rect.y = param2 - _loc9_ - (param4 >> 1);
               param5.fillRect(this._rect,param6);
            }
            if(_loc8_ != 0 && _loc9_ != 0)
            {
               this._rect.x = param1 - _loc8_ - (param4 >> 1);
               this._rect.y = param2 - _loc9_ - (param4 >> 1);
               param5.fillRect(this._rect,param6);
            }
            if(_loc8_ != _loc9_)
            {
               this._rect.x = param1 + _loc9_ - (param4 >> 1);
               this._rect.y = param2 + _loc8_ - (param4 >> 1);
               param5.fillRect(this._rect,param6);
               if(_loc9_ != 0)
               {
                  this._rect.x = param1 - _loc9_ - (param4 >> 1);
                  this._rect.y = param2 + _loc8_ - (param4 >> 1);
                  param5.fillRect(this._rect,param6);
               }
               if(_loc8_ != 0)
               {
                  this._rect.x = param1 + _loc9_ - (param4 >> 1);
                  this._rect.y = param2 - _loc8_ - (param4 >> 1);
                  param5.fillRect(this._rect,param6);
               }
               if(_loc9_ != 0 && _loc8_ != 0)
               {
                  this._rect.x = param1 - _loc9_ - (param4 >> 1);
                  this._rect.y = param2 - _loc8_ - (param4 >> 1);
                  param5.fillRect(this._rect,param6);
               }
            }
            _loc7_ += _loc9_;
            _loc9_ += param4;
            _loc7_ += _loc9_;
            if(_loc7_ >= 0)
            {
               _loc8_ -= param4;
               _loc7_ -= _loc8_;
               _loc7_ = _loc7_ - _loc8_;
            }
         }
      }
      
      public function drawCircle32(param1:int, param2:int, param3:int, param4:uint, param5:BitmapData, param6:uint) : void
      {
         param6 = uint(255 << 24 | param6);
         if(param4 < 1)
         {
            param4 = 1;
         }
         this._rect.width = this._rect.height = param4;
         if(param3 < 0)
         {
            param3 = -param3;
         }
         var _loc7_:int = -param3;
         var _loc8_:int = param3;
         var _loc9_:int = 0;
         while(_loc8_ >= _loc9_)
         {
            this._rect.x = param1 + _loc8_ - (param4 >> 1);
            this._rect.y = param2 + _loc9_ - (param4 >> 1);
            param5.fillRect(this._rect,param6);
            if(_loc8_ != 0)
            {
               this._rect.x = param1 - _loc8_ - (param4 >> 1);
               this._rect.y = param2 + _loc9_ - (param4 >> 1);
               param5.fillRect(this._rect,param6);
            }
            if(_loc9_ != 0)
            {
               this._rect.x = param1 + _loc8_ - (param4 >> 1);
               this._rect.y = param2 - _loc9_ - (param4 >> 1);
               param5.fillRect(this._rect,param6);
            }
            if(_loc8_ != 0 && _loc9_ != 0)
            {
               this._rect.x = param1 - _loc8_ - (param4 >> 1);
               this._rect.y = param2 - _loc9_ - (param4 >> 1);
               param5.fillRect(this._rect,param6);
            }
            if(_loc8_ != _loc9_)
            {
               this._rect.x = param1 + _loc9_ - (param4 >> 1);
               this._rect.y = param2 + _loc8_ - (param4 >> 1);
               param5.fillRect(this._rect,param6);
               if(_loc9_ != 0)
               {
                  this._rect.x = param1 - _loc9_ - (param4 >> 1);
                  this._rect.y = param2 + _loc8_ - (param4 >> 1);
                  param5.fillRect(this._rect,param6);
               }
               if(_loc8_ != 0)
               {
                  this._rect.x = param1 + _loc9_ - (param4 >> 1);
                  this._rect.y = param2 - _loc8_ - (param4 >> 1);
                  param5.fillRect(this._rect,param6);
               }
               if(_loc9_ != 0 && _loc8_ != 0)
               {
                  this._rect.x = param1 - _loc9_ - (param4 >> 1);
                  this._rect.y = param2 - _loc8_ - (param4 >> 1);
                  param5.fillRect(this._rect,param6);
               }
            }
            _loc7_ += _loc9_;
            _loc9_ += param4;
            _loc7_ += _loc9_;
            if(_loc7_ >= 0)
            {
               _loc8_ -= param4;
               _loc7_ -= _loc8_;
               _loc7_ = _loc7_ - _loc8_;
            }
         }
      }
   }
}

