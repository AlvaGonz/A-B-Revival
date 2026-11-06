package fullshit.render
{
   import flash.filters.ColorMatrixFilter;
   
   public class ColorMatrixAdd
   {
      
      private var _matrix:Array;
      
      private var _tempArray:Array;
      
      public var color:ColorMatrixFilter;
      
      public var brightness:Number = 0;
      
      public var contrast:Number = 0;
      
      public var saturation:Number = 0;
      
      public var hue:Number = 0;
      
      public function ColorMatrixAdd(param1:ColorMatrixFilter)
      {
         super();
         this.color = param1;
         this._tempArray = new Array(20);
         this._matrix = [1,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,1,0];
      }
      
      public function resetMatrix() : void
      {
         this._matrix = [1,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,1,0];
      }
      
      public function setBrightness(param1:Number) : void
      {
         var _loc4_:Number = NaN;
         var _loc5_:int = 0;
         var _loc2_:Number = param1 * 255;
         this.brightness = param1;
         var _loc3_:Array = [1,0,0,0,_loc2_,0,1,0,0,_loc2_,0,0,1,0,_loc2_,0,0,0,1,0];
         var _loc6_:int = 0;
         while(_loc6_ < 19)
         {
            _loc5_ = 0;
            while(_loc5_ < 5)
            {
               _loc4_ = this._matrix[_loc6_] * _loc3_[_loc5_] + this._matrix[_loc6_ + 1] * _loc3_[_loc5_ + 5] + this._matrix[_loc6_ + 2] * _loc3_[_loc5_ + 10] + this._matrix[_loc6_ + 3] * _loc3_[_loc5_ + 15];
               if(_loc5_ == 4)
               {
                  _loc4_ += this._matrix[_loc6_ + 4];
               }
               this._tempArray[_loc6_ + _loc5_] = _loc4_;
               _loc5_++;
            }
            _loc6_ += 5;
         }
         _loc3_ = this._matrix;
         this._matrix = this._tempArray;
         this._tempArray = _loc3_;
      }
      
      public function setContrast(param1:Number) : void
      {
         var _loc5_:Number = NaN;
         var _loc6_:int = 0;
         this.contrast = param1;
         var _loc2_:Number = 1 + (this.contrast > 0 ? 4 * this.contrast : this.contrast);
         var _loc3_:Number = -128 * this.contrast * (this.contrast > 0 ? 5 : 1);
         var _loc4_:Array = [_loc2_,0,0,0,_loc3_,0,_loc2_,0,0,_loc3_,0,0,_loc2_,0,_loc3_,0,0,0,1,0];
         var _loc7_:int = 0;
         while(_loc7_ < 19)
         {
            _loc6_ = 0;
            while(_loc6_ < 5)
            {
               _loc5_ = this._matrix[_loc7_] * _loc4_[_loc6_] + this._matrix[_loc7_ + 1] * _loc4_[_loc6_ + 5] + this._matrix[_loc7_ + 2] * _loc4_[_loc6_ + 10] + this._matrix[_loc7_ + 3] * _loc4_[_loc6_ + 15];
               if(_loc6_ == 4)
               {
                  _loc5_ += this._matrix[_loc7_ + 4];
               }
               this._tempArray[_loc7_ + _loc6_] = _loc5_;
               _loc6_++;
            }
            _loc7_ += 5;
         }
         _loc4_ = this._matrix;
         this._matrix = this._tempArray;
         this._tempArray = _loc4_;
      }
      
      public function setSaturation(param1:Number) : void
      {
         var _loc14_:Number = NaN;
         var _loc15_:int = 0;
         var _loc2_:Number = 1 + (param1 > 0 ? 3 * param1 : param1);
         this.saturation = param1;
         var _loc3_:Number = 0.212671;
         var _loc4_:Number = 0.71516;
         var _loc5_:Number = 0.072169;
         var _loc6_:Number = 1 - _loc2_;
         var _loc7_:Number = _loc6_ * _loc3_ + _loc2_;
         var _loc8_:Number = _loc6_ * _loc3_;
         var _loc9_:Number = _loc6_ * _loc4_ + _loc2_;
         var _loc10_:Number = _loc6_ * _loc4_;
         var _loc11_:Number = _loc6_ * _loc5_ + _loc2_;
         var _loc12_:Number = _loc6_ * _loc5_;
         var _loc13_:Array = [_loc7_,_loc10_,_loc12_,0,0,_loc8_,_loc9_,_loc12_,0,0,_loc8_,_loc10_,_loc11_,0,0,0,0,0,1,0];
         var _loc16_:int = 0;
         while(_loc16_ < 19)
         {
            _loc15_ = 0;
            while(_loc15_ < 5)
            {
               _loc14_ = this._matrix[_loc16_] * _loc13_[_loc15_] + this._matrix[_loc16_ + 1] * _loc13_[_loc15_ + 5] + this._matrix[_loc16_ + 2] * _loc13_[_loc15_ + 10] + this._matrix[_loc16_ + 3] * _loc13_[_loc15_ + 15];
               if(_loc15_ == 4)
               {
                  _loc14_ += this._matrix[_loc16_ + 4];
               }
               this._tempArray[_loc16_ + _loc15_] = _loc14_;
               _loc15_++;
            }
            _loc16_ += 5;
         }
         _loc13_ = this._matrix;
         this._matrix = this._tempArray;
         this._tempArray = _loc13_;
      }
      
      public function setHue(param1:Number) : void
      {
         var _loc8_:Number = NaN;
         var _loc9_:int = 0;
         this.hue = param1;
         var _loc2_:Number = Math.cos(this.hue);
         var _loc3_:Number = Math.sin(this.hue);
         var _loc4_:Number = 0.213;
         var _loc5_:Number = 0.715;
         var _loc6_:Number = 0.072;
         var _loc7_:Array = [_loc4_ + _loc2_ * (1 - _loc4_) + _loc3_ * -_loc4_,_loc5_ + _loc2_ * -_loc5_ + _loc3_ * -_loc5_,_loc6_ + _loc2_ * -_loc6_ + _loc3_ * (1 - _loc6_),0,0,_loc4_ + _loc2_ * -_loc4_ + _loc3_ * 0.143,_loc5_ + _loc2_ * (1 - _loc5_) + _loc3_ * 0.14,_loc6_ + _loc2_ * -_loc6_ + _loc3_ * -0.283,0,0,_loc4_ + _loc2_ * -_loc4_ + _loc3_ * -(1 - _loc4_),_loc5_ + _loc2_ * -_loc5_ + _loc3_ * _loc5_,_loc6_ + _loc2_ * (1 - _loc6_) + _loc3_ * _loc6_,0,0,0,0,0,1,0];
         var _loc10_:int = 0;
         while(_loc10_ < 19)
         {
            _loc9_ = 0;
            while(_loc9_ < 5)
            {
               _loc8_ = this._matrix[_loc10_] * _loc7_[_loc9_] + this._matrix[_loc10_ + 1] * _loc7_[_loc9_ + 5] + this._matrix[_loc10_ + 2] * _loc7_[_loc9_ + 10] + this._matrix[_loc10_ + 3] * _loc7_[_loc9_ + 15];
               if(_loc9_ == 4)
               {
                  _loc8_ += this._matrix[_loc10_ + 4];
               }
               this._tempArray[_loc10_ + _loc9_] = _loc8_;
               _loc9_++;
            }
            _loc10_ += 5;
         }
         _loc7_ = this._matrix;
         this._matrix = this._tempArray;
         this._tempArray = _loc7_;
      }
      
      public function setColor() : void
      {
         this.color.matrix = this._matrix;
      }
   }
}

